
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PersonalAgent.Infrastructure.Identity;
using Xunit;

namespace PersonalAgent.Web.Tests.Integration;

public sealed class RegistrationTests
{
    [Fact]
    public async Task Register_ValidUser_AssignsUserRole_NotAdmin()
    {
        using var factory = new RegistrationWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = true
                });

            var email = $"registration_{Guid.NewGuid():N}@example.com";
            const string password = "TestPassword123!";

            var response = await RegisterAsync(
                client, email, password);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            using var scope = factory.Services.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<MvcApplicationUser>>();

            var user = await userManager.FindByEmailAsync(email);

            Assert.NotNull(user);
            Assert.True(await userManager.IsInRoleAsync(user, "User"));
            Assert.False(await userManager.IsInRoleAsync(user, "Admin"));
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    [Fact]
    public async Task Register_DuplicateEmail_DoesNotCreateSecondAccount()
    {
        using var factory = new RegistrationWebFactory();

        try
        {
            await factory.InitializeDatabaseAsync();

            using var client = factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = true
                });

            var email = $"duplicate_{Guid.NewGuid():N}@example.com";
            const string password = "TestPassword123!";

            var firstResponse = await RegisterAsync(
                client, email, password);

            Assert.Equal(
                HttpStatusCode.Redirect,
                firstResponse.StatusCode);

            var secondResponse = await RegisterAsync(
                client, email, password);

            Assert.Equal(
                HttpStatusCode.OK,
                secondResponse.StatusCode);

            using var scope = factory.Services.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<MvcApplicationUser>>();

            var users = userManager.Users
                .Where(u => u.NormalizedEmail == email.ToUpperInvariant())
                .ToList();

            Assert.Single(users);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email,
        string password)
    {
        var page = await client.GetAsync("/Identity/Account/Register");
        page.EnsureSuccessStatusCode();

        var html = await page.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

        Assert.True(
            tokenMatch.Success,
            "Fant ikke antiforgery-token på registreringssiden.");

        var form = new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password,
            ["__RequestVerificationToken"] =
                WebUtility.HtmlDecode(tokenMatch.Groups[1].Value)
        };

        return await client.PostAsync(
            "/Identity/Account/Register",
            new FormUrlEncodedContent(form));
    }
}
