
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalAgent.Core.Entities;
using PersonalAgent.Infrastructure.Data;

namespace PersonalAgent.Web.Tests.Integration;

public class MvcValidationTests
{
    private static HttpClient CreateAdminClient(
        ProjectsWebFactory factory)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "Admin");

        return client;
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

    private static async Task<int> SeedProjectAsync(
        ProjectsWebFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<MvcDbContext>();

        var project = new Project
        {
            Name = "Valideringstestprosjekt"
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return project.Id;
    }

    [Fact]
    public async Task ProjectCreate_WithoutName_ShouldReturnForm()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = CreateAdminClient(factory);

            var token = await GetTokenAsync(
                client, "/Projects/Create");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Name"] = "",
                    ["Description"] = "Skal ikke lagres",
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                "/Projects/Create", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            Assert.Equal(0, await db.Projects.CountAsync());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task ProjectCreate_TooLongName_ShouldNotSave()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = CreateAdminClient(factory);

            var token = await GetTokenAsync(
                client, "/Projects/Create");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Name"] = new string('A', 101),
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                "/Projects/Create", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            Assert.Equal(0, await db.Projects.CountAsync());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task IssueCreate_WithoutTitle_ShouldNotSave()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            var projectId = await SeedProjectAsync(factory);

            using var client = CreateAdminClient(factory);

            var token = await GetTokenAsync(
                client,
                $"/Issues/Create?projectId={projectId}");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["ProjectId"] = projectId.ToString(),
                    ["Title"] = "",
                    ["Description"] = "Ugyldig sak",
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                "/Issues/Create", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            Assert.Equal(0, await db.Issues.CountAsync());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task IssueCreate_InvalidProjectId_ShouldNotSave()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = CreateAdminClient(factory);

            var token = await GetTokenAsync(
                client, "/Issues/Create");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["ProjectId"] = "0",
                    ["Title"] = "Sak uten prosjekt",
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                "/Issues/Create", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            Assert.Equal(0, await db.Issues.CountAsync());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task IssueCreate_ValidInput_ShouldRedirectAndSave()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            var projectId = await SeedProjectAsync(factory);

            using var client = CreateAdminClient(factory);

            var token = await GetTokenAsync(
                client,
                $"/Issues/Create?projectId={projectId}");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["ProjectId"] = projectId.ToString(),
                    ["Title"] = "Gyldig HTTP-testsak",
                    ["Description"] = "Opprettet gjennom MVC",
                    ["__RequestVerificationToken"] = token
                });

            var response = await client.PostAsync(
                "/Issues/Create", content);

            Assert.Equal(
                HttpStatusCode.Redirect,
                response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MvcDbContext>();

            var issue = await db.Issues.SingleOrDefaultAsync(
                i => i.Title == "Gyldig HTTP-testsak");

            Assert.NotNull(issue);
            Assert.Equal(projectId, issue.ProjectId);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task IssueDetails_UnknownId_ShouldReturnNotFound()
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = CreateAdminClient(factory);

            var response = await client.GetAsync(
                "/Issues/Details/999999");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }
}
