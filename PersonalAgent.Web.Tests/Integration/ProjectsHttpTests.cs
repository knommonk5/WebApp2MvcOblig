
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace PersonalAgent.Web.Tests.Integration;

public class ProjectsHttpTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("User", HttpStatusCode.OK)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task ProjectIndex_ShouldRespectAuthentication(
        string? role,
        HttpStatusCode expectedStatus)
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

            if (role is not null)
                client.DefaultRequestHeaders.Add("X-Test-Role", role);

            var response = await client.GetAsync("/Projects");

            Assert.Equal(expectedStatus, response.StatusCode);
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
    public async Task ProjectCreate_ShouldRequireAdmin(
        string? role,
        HttpStatusCode expectedStatus)
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

            if (role is not null)
                client.DefaultRequestHeaders.Add("X-Test-Role", role);

            var response = await client.GetAsync("/Projects/Create");

            Assert.Equal(expectedStatus, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Theory]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task ProjectEdit_ShouldRequireAdmin(
        string role,
        HttpStatusCode expectedStatus)
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<
                        PersonalAgent.Infrastructure.Data.MvcDbContext>();

                db.Projects.Add(new PersonalAgent.Core.Entities.Project
                {
                    Name = "Testprosjekt"
                });

                await db.SaveChangesAsync();
            }

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

            client.DefaultRequestHeaders.Add("X-Test-Role", role);

            var response = await client.GetAsync("/Projects/Edit/1");

            Assert.Equal(expectedStatus, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }
    
    [Theory]
    [InlineData("/Projects/Create")]
    [InlineData("/Projects/Edit/1")]
    [InlineData("/Projects/Delete/1")]
    public async Task ProjectWritePost_ShouldRejectRegularUser(
        string url)
    {
        using var factory = new ProjectsWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

            client.DefaultRequestHeaders.Add(
                "X-Test-Role", "User");

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Id"] = "1",
                    ["Name"] = "Uautorisert endring",
                    ["Description"] = "Skal ikke lagres"
                });

            var response = await client.PostAsync(url, content);

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
public async Task Admin_ShouldCreateProject_WithValidAntiForgeryToken()
{
    using var factory = new ProjectsWebFactory();

    try
    {
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        // Hent skjemaet og anti-forgery-tokenet.
        var getResponse = await client.GetAsync("/Projects/Create");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var html = await getResponse.Content.ReadAsStringAsync();

        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

        Assert.True(match.Success, "Fant ikke anti-forgery-token.");

        var token = System.Net.WebUtility.HtmlDecode(
            match.Groups[1].Value);

        // Send skjemaet som administrator.
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Name"] = "HTTP-testprosjekt",
                ["Description"] = "Opprettet gjennom MVC-integrasjonstest",
                ["__RequestVerificationToken"] = token
            });

        var postResponse = await client.PostAsync(
            "/Projects/Create",
            content);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        // Kontroller at prosjektet faktisk er lagret.
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<MvcDbContext>();

        var saved = await db.Projects.SingleOrDefaultAsync(
            p => p.Name == "HTTP-testprosjekt");

        Assert.NotNull(saved);
        Assert.Equal(
            "Opprettet gjennom MVC-integrasjonstest",
            saved.Description);
    }
    finally
    {
        await factory.DeleteDatabaseAsync();
    }
}

}
