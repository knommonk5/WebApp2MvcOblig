
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using PersonalAgent.Infrastructure.Data;
using PersonalAgent.Infrastructure.Identity;

namespace PersonalAgent.Web.Tests.Integration;

public sealed class RegistrationWebFactory : WebApplicationFactory<Program>
{
    private string? _databaseName;
    private string? _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:MvcDatabase",
            "Host=localhost;Database=unused_test_placeholder;Username=unused");

        builder.ConfigureTestServices(services =>
        {
            var connectionString = Environment.GetEnvironmentVariable(
                "PERSONALAGENT_TEST_CONNECTION");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "PERSONALAGENT_TEST_CONNECTION mangler.");
            }

            var connectionBuilder =
                new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Database = $"personalagent_mvc_test_{Guid.NewGuid():N}",
                    Pooling = false
                };

            _databaseName = connectionBuilder.Database;
            _connectionString = connectionBuilder.ConnectionString;

            services.RemoveAll<DbContextOptions<MvcDbContext>>();

            services.AddDbContext<MvcDbContext>(options =>
                options.UseNpgsql(_connectionString));

            // Behold ekte Identity-autentisering.
            // Ikke registrer TestAuthHandler her.
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<MvcDbContext>();

        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("User"))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole("User"));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        result.Errors.Select(e => e.Description)));
            }
        }
    }

    public async Task DeleteDatabaseAsync()
    {
        if (_databaseName is null || _connectionString is null)
            return;

        if (!_databaseName.StartsWith(
                "personalagent_mvc_test_",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Ugyldig testdatabasenavn. Sletting avbrutt.");
        }

        var options = new DbContextOptionsBuilder<MvcDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        await using var db = new MvcDbContext(options);

        if (await db.Database.CanConnectAsync())
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
