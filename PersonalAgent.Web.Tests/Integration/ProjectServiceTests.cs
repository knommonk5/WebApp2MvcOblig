
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Infrastructure.Services;

namespace PersonalAgent.Web.Tests.Integration;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldSaveProject()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var service = new ProjectService(scope.Db);

        var project = await service.CreateAsync(
            "  Webapp 2  ",
            "  MVC-oppgave  ");

        Assert.True(project.Id > 0);
        Assert.Equal("Webapp 2", project.Name);
        Assert.Equal("MVC-oppgave", project.Description);

        var saved = await scope.Db.Projects
            .SingleAsync(p => p.Id == project.Id);

        Assert.Equal("Webapp 2", saved.Name);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectEmptyName()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var service = new ProjectService(scope.Db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync("   ", null));

        Assert.Empty(await scope.Db.Projects.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingProject()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var service = new ProjectService(scope.Db);

        var project = await service.CreateAsync(
            "Gammelt navn",
            null);

        var updated = await service.UpdateAsync(
            project.Id,
            "Nytt navn",
            "Oppdatert beskrivelse");

        Assert.True(updated);

        var saved = await service.GetByIdAsync(project.Id);

        Assert.NotNull(saved);
        Assert.Equal("Nytt navn", saved.Name);
        Assert.Equal("Oppdatert beskrivelse", saved.Description);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProject()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var service = new ProjectService(scope.Db);

        var project = await service.CreateAsync(
            "Skal slettes",
            null);

        var deleted = await service.DeleteAsync(project.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(project.Id));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnProjectsSortedByName()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var service = new ProjectService(scope.Db);

        await service.CreateAsync("Zulu", null);
        await service.CreateAsync("Alpha", null);

        var projects = await service.GetAllAsync();

        Assert.Equal(
            new[] { "Alpha", "Zulu" },
            projects.Select(p => p.Name));
    }
}
