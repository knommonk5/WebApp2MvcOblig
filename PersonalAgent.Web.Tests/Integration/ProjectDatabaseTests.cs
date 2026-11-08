using Microsoft.EntityFrameworkCore;
using PersonalAgent.Core.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PersonalAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace PersonalAgent.Web.Tests.Integration;

public class ProjectDatabaseTests
{
    [Fact]
    public async Task CanCreateAndReadProject()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        scope.Db.Projects.Add(new Project
        {
            Name = "Testprosjekt",
            Description = "Opprettet av integrasjonstest"
        });

        await scope.Db.SaveChangesAsync();

        var project = await scope.Db.Projects
            .SingleAsync(p => p.Name == "Testprosjekt");

        Assert.Equal("Testprosjekt", project.Name);
        Assert.Equal(
            "Opprettet av integrasjonstest",
            project.Description);
    }

    [Fact]
    public async Task ProjectCanHaveMultipleIssues()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var project = new Project
        {
            Name = "Prosjekt med saker",
            Issues =
            {
                new Issue { Title = "Første sak" },
                new Issue { Title = "Andre sak" }
            }
        };

        scope.Db.Projects.Add(project);
        await scope.Db.SaveChangesAsync();

        var savedProject = await scope.Db.Projects
            .Include(p => p.Issues)
            .SingleAsync(p => p.Id == project.Id);

        Assert.Equal(2, savedProject.Issues.Count);
    }
    
    
    [Fact]
    public void MvcModel_ShouldMatchMigrationSnapshot()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "PERSONALAGENT_TEST_CONNECTION");

        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var options = new DbContextOptionsBuilder<MvcDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var db = new MvcDbContext(options);

        var migrationsAssembly =
            db.GetService<IMigrationsAssembly>();

        var snapshot = migrationsAssembly.ModelSnapshot;

        Assert.NotNull(snapshot);

        var modelInitializer =
            db.GetService<IModelRuntimeInitializer>();

        // Initialiser modellen fra migrasjonens snapshot.
        var snapshotModel = modelInitializer.Initialize(
            snapshot!.Model,
            designTime: true);

        // Hent den ferdige modellen fra DbContext.
        var currentModel =
            db.GetService<IDesignTimeModel>().Model;

        var modelDiffer =
            db.GetService<IMigrationsModelDiffer>();

        var differences = modelDiffer.GetDifferences(
            snapshotModel.GetRelationalModel(),
            currentModel.GetRelationalModel());

        
        var details = differences.Select(operation =>
        {
            if (operation is AlterColumnOperation column)
            {
                return
                    $"Tabell: {column.Table}, " +
                    $"Kolonne: {column.Name}, " +
                    $"Gammel type: {column.OldColumn.ColumnType}, " +
                    $"Ny type: {column.ColumnType}, " +
                    $"Gammel nullable: {column.OldColumn.IsNullable}, " +
                    $"Ny nullable: {column.IsNullable}";
            }

            return operation.GetType().Name;
        });

        Assert.True(
            differences.Count == 0,
            "Modellforskjeller:\n" +
            string.Join("\n", details));

    }
}