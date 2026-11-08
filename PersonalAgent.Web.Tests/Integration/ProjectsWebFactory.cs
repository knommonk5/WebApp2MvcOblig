using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using PersonalAgent.Infrastructure.Data;

namespace PersonalAgent.Web.Tests.Integration;

public class ProjectsWebFactory : WebApplicationFactory<Program>
{
    private string? _databaseName;
    private string? _testConnectionString;

    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:MvcDatabase",
            "Host=localhost;Database=unused_test_placeholder;Username=unused");

        builder.ConfigureTestServices(services =>
        {
            var connectionString =
                Environment.GetEnvironmentVariable(
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
            _testConnectionString = connectionBuilder.ConnectionString;

            services.RemoveAll<DbContextOptions<MvcDbContext>>();

            services.AddDbContext<MvcDbContext>(options =>
                options.UseNpgsql(_testConnectionString));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                    options.DefaultForbidScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.Scheme,
                    _ => { });
        });
    }


    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<MvcDbContext>();

        await db.Database.MigrateAsync();
    }

    
    public async Task DeleteDatabaseAsync()
    {
        // Hvis oppstarten feilet før testdatabasen ble
        // konfigurert, finnes det ingenting å slette.
        if (_databaseName is null)
            return;

        if (!_databaseName.StartsWith(
                "personalagent_mvc_test_",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Ugyldig testdatabasenavn. Sletting avbrutt.");
        }

        // Ikke opprett testserveren på nytt under opprydding.
        // Bruk den samme tilkoblingen som testen fikk.
        if (_testConnectionString is null)
            return;

        var options = new DbContextOptionsBuilder<MvcDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;

        await using var db = new MvcDbContext(options);

        if (await db.Database.CanConnectAsync())
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

}
