using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalAgent.Api.Data;
using PersonalAgent.Api.Models;

namespace PersonalAgent.Tests.Integration;

public sealed class DatabaseScope : IAsyncDisposable
{
    public AgentDbContext Db { get; }

    private DatabaseScope(AgentDbContext db)
    {
        Db = db;
    }

    public static async Task<DatabaseScope> CreateAsync()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PERSONALAGENT_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Sett PERSONALAGENT_TEST_CONNECTION før testene kjøres.");
        }

        // Hver test får en egen database.
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = $"personalagent_test_{Guid.NewGuid():N}",
            Pooling = false
        };

        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;

        var db = new AgentDbContext(options);
        var scope = new DatabaseScope(db);

        try
        {
            await db.Database.MigrateAsync();

            db.Users.AddRange(
                new ApplicationUser
                {
                    Id = "user-a",
                    UserName = "a@example.com",
                    NormalizedUserName = "A@EXAMPLE.COM",
                    Email = "a@example.com",
                    NormalizedEmail = "A@EXAMPLE.COM"
                },
                new ApplicationUser
                {
                    Id = "user-b",
                    UserName = "b@example.com",
                    NormalizedUserName = "B@EXAMPLE.COM",
                    Email = "b@example.com",
                    NormalizedEmail = "B@EXAMPLE.COM"
                });

            await db.SaveChangesAsync();

            return scope;
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Db.Database.EnsureDeletedAsync();
        }
        finally
        {
            await Db.DisposeAsync();
        }
    }
}