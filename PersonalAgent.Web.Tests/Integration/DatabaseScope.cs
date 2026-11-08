
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalAgent.Infrastructure.Data;

namespace PersonalAgent.Web.Tests.Integration;

public sealed class DatabaseScope : IAsyncDisposable
{
    public MvcDbContext Db { get; }

    private readonly string _databaseName;

    private DatabaseScope(
        MvcDbContext db,
        string databaseName)
    {
        Db = db;
        _databaseName = databaseName;
    }

    public static async Task<DatabaseScope> CreateAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "PERSONALAGENT_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Sett PERSONALAGENT_TEST_CONNECTION før testene kjøres.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = $"personalagent_mvc_test_{Guid.NewGuid():N}",
            Pooling = false
        };

        var options = new DbContextOptionsBuilder<MvcDbContext>()
            .UseNpgsql(
                builder.ConnectionString,
                npgsql => npgsql.MigrationsAssembly(
                    typeof(MvcDbContext).Assembly.GetName().Name))
            .Options;

        var db = new MvcDbContext(options);
        var scope = new DatabaseScope(db, builder.Database!);

        try
        {
            await db.Database.MigrateAsync();
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
            // Beskytt eksisterende databaser mot sletting.
            if (!_databaseName.StartsWith(
                    "personalagent_mvc_test_",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Ugyldig testdatabasenavn. Sletting avbrutt.");
            }

            await Db.Database.EnsureDeletedAsync();
        }
        finally
        {
            await Db.DisposeAsync();
        }
    }
}
