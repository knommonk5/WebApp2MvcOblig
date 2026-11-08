
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalAgent.Core.Entities;
using PersonalAgent.Core.Enums;
using PersonalAgent.Infrastructure.Data;
using PersonalAgent.Infrastructure.Identity;

namespace PersonalAgent.Web.Tests.Integration;

public class IssuesHttpTests
{
    private static HttpClient CreateClient(
        ProjectsWebFactory factory,
        string? role)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        if (role is not null)
            client.DefaultRequestHeaders.Add(
                "X-Test-Role", role);

        return client;
    }

    private static async Task<int> SeedIssueAsync(
        ProjectsWebFactory factory,
        bool assignToTestUser = false,
        IssueStatus status = IssueStatus.Open)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<MvcDbContext>();

        if (!await db.Users.AnyAsync(
                u => u.Id == "test-user"))
        {
            db.Users.Add(new MvcApplicationUser
            {
                Id = "test-user",
                UserName = "test-user@test.local",
                Email = "test-user@test.local"
            });
        }

        var project = new Project
        {
            Name = "HTTP-testprosjekt"
        };

        db.Projects.Add(project);

        var issue = new Issue
        {
            Title = "HTTP-testsak",
            Project = project,
            Status = status,
            AssignedUserId = assignToTestUser
                ? "test-user"
                : null
        };

        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        return issue.Id;
    }

    private static async Task<string> GetTokenAsync(
        HttpClient client,
        string url)
    {
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

        Assert.True(
            match.Success,
            $"Fant ikke antiforgery-token på {url}.");

        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("User", HttpStatusCode.OK)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task Index_ShouldRespectAuthentication(
        string? role,
        HttpStatusCode expected)
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = CreateClient(factory, role);

            var response = await client.GetAsync("/Issues");

            Assert.Equal(expected, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task Create_ShouldRequireAdmin(
        string? role,
        HttpStatusCode expected)
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = CreateClient(factory, role);

            var response = await client.GetAsync(
                "/Issues/Create");

            Assert.Equal(expected, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Theory]
    [InlineData("/Issues/Create")]
    [InlineData("/Issues/Edit/1")]
    [InlineData("/Issues/Delete/1")]
    [InlineData("/Issues/Assign/1")]
    public async Task User_ShouldNotAccessAdminPostActions(
        string url)
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = CreateClient(factory, "User");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Id"] = "1",
                    ["ProjectId"] = "1",
                    ["Title"] = "Uautorisert endring",
                    ["assignedUserId"] = "test-user"
                });

            var response = await client.PostAsync(
                url, content);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task User_ShouldNotChangeUnassignedIssue()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            var issueId = await SeedIssueAsync(factory);

            using var client = CreateClient(factory, "User");

            var token = await GetTokenAsync(
                client, $"/Issues/Details/{issueId}");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["newStatus"] =
                        ((int)IssueStatus.Closed).ToString(),
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                $"/Issues/ChangeStatus/{issueId}",
                content);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            var issue = await db.Issues.FindAsync(issueId);

            Assert.NotNull(issue);
            Assert.Equal(IssueStatus.Open, issue.Status);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task AssignedUser_ShouldStartIssue()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            var issueId = await SeedIssueAsync(
                factory,
                assignToTestUser: true,
                status: IssueStatus.Assigned);

            using var client = CreateClient(factory, "User");

            var token = await GetTokenAsync(
                client, $"/Issues/Details/{issueId}");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["newStatus"] =
                        ((int)IssueStatus.InProgress).ToString(),
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                $"/Issues/ChangeStatus/{issueId}",
                content);

            Assert.Equal(
                HttpStatusCode.Redirect,
                response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            var issue = await db.Issues.FindAsync(issueId);

            Assert.NotNull(issue);
            Assert.Equal(
                IssueStatus.InProgress,
                issue.Status);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task Admin_ShouldReopenClosedIssue()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            var issueId = await SeedIssueAsync(
                factory,
                status: IssueStatus.Closed);

            using var client = CreateClient(factory, "Admin");

            var token = await GetTokenAsync(
                client, $"/Issues/Details/{issueId}");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["newStatus"] =
                        ((int)IssueStatus.Open).ToString(),
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                $"/Issues/ChangeStatus/{issueId}",
                content);

            Assert.Equal(
                HttpStatusCode.Redirect,
                response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            var issue = await db.Issues.FindAsync(issueId);

            Assert.NotNull(issue);
            Assert.Equal(IssueStatus.Open, issue.Status);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task User_ShouldAddComment()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            var issueId = await SeedIssueAsync(factory);

            using var client = CreateClient(factory, "User");

            var token = await GetTokenAsync(
                client, $"/Issues/Details/{issueId}");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["IssueId"] = issueId.ToString(),
                    ["Content"] = "Kommentar fra HTTP-test",
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                "/Issues/AddComment",
                content);

            Assert.Equal(
                HttpStatusCode.Redirect,
                response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            var comment = await db.IssueComments
                .SingleOrDefaultAsync(
                    c => c.IssueId == issueId);

            Assert.NotNull(comment);
            Assert.Equal(
                "Kommentar fra HTTP-test",
                comment.Content);
            Assert.Equal("test-user", comment.AuthorId);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task PostWithoutAntiforgeryToken_ShouldFail()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            var issueId = await SeedIssueAsync(factory);

            using var client = CreateClient(factory, "Admin");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["newStatus"] =
                        ((int)IssueStatus.Closed).ToString()
                });

            var response = await client.PostAsync(
                $"/Issues/ChangeStatus/{issueId}",
                content);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            var issue = await db.Issues.FindAsync(issueId);

            Assert.NotNull(issue);
            Assert.Equal(IssueStatus.Open, issue.Status);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }
}
