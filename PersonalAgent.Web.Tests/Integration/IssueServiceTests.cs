
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Core.Entities;
using PersonalAgent.Core.Enums;
using PersonalAgent.Infrastructure.Services;

namespace PersonalAgent.Web.Tests.Integration;

public class IssueServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldSaveIssueInProject()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projectService = new ProjectService(scope.Db);
        var issueService = new IssueService(scope.Db);

        var project = await projectService.CreateAsync(
            "Testprosjekt", "Prosjektbeskrivelse");

        var issue = await issueService.CreateAsync(
            project.Id,
            "  Første sak  ",
            "  Beskrivelse av saken  ");

        Assert.True(issue.Id > 0);
        Assert.Equal(project.Id, issue.ProjectId);
        Assert.Equal("Første sak", issue.Title);
        Assert.Equal("Beskrivelse av saken", issue.Description);
        Assert.Equal(IssueStatus.Open, issue.Status);

        var saved = await scope.Db.Issues
            .SingleAsync(i => i.Id == issue.Id);

        Assert.Equal("Første sak", saved.Title);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNonExistingProject()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var service = new IssueService(scope.Db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                999999,
                "Sak uten prosjekt",
                null));

        Assert.Empty(await scope.Db.Issues.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ShouldRejectEmptyTitle(string title)
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projectService = new ProjectService(scope.Db);
        var issueService = new IssueService(scope.Db);

        var project = await projectService.CreateAsync(
            "Testprosjekt", null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => issueService.CreateAsync(
                project.Id,
                title,
                null));

        Assert.Empty(await scope.Db.Issues.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectTitleOver150Characters()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projectService = new ProjectService(scope.Db);
        var issueService = new IssueService(scope.Db);

        var project = await projectService.CreateAsync(
            "Testprosjekt", null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => issueService.CreateAsync(
                project.Id,
                new string('A', 151),
                null));
    }

    [Fact]
    public async Task GetByProjectIdAsync_ShouldOnlyReturnIssuesFromSelectedProject()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projectService = new ProjectService(scope.Db);
        var issueService = new IssueService(scope.Db);

        var firstProject = await projectService.CreateAsync(
            "Prosjekt A", null);

        var secondProject = await projectService.CreateAsync(
            "Prosjekt B", null);

        await issueService.CreateAsync(
            firstProject.Id, "Sak A1", null);

        await issueService.CreateAsync(
            firstProject.Id, "Sak A2", null);

        await issueService.CreateAsync(
            secondProject.Id, "Sak B1", null);

        var issues = await issueService.GetByProjectIdAsync(
            firstProject.Id);

        Assert.Equal(2, issues.Count);
        Assert.All(issues,
            issue => Assert.Equal(firstProject.Id, issue.ProjectId));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeProject()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projectService = new ProjectService(scope.Db);
        var issueService = new IssueService(scope.Db);

        var project = await projectService.CreateAsync(
            "Prosjekt med sak", null);

        var issue = await issueService.CreateAsync(
            project.Id, "Test sak", null);

        var saved = await issueService.GetByIdAsync(issue.Id);

        Assert.NotNull(saved);
        Assert.NotNull(saved.Project);
        Assert.Equal("Prosjekt med sak", saved.Project.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projectService = new ProjectService(scope.Db);
        var issueService = new IssueService(scope.Db);

        var project = await projectService.CreateAsync(
            "Testprosjekt", null);

        var issue = await issueService.CreateAsync(
            project.Id, "Gammel tittel", null);

        var updated = await issueService.UpdateAsync(
            issue.Id,
            "Ny tittel",
            "Ny beskrivelse");

        Assert.True(updated);

        var saved = await issueService.GetByIdAsync(issue.Id);

        Assert.NotNull(saved);
        Assert.Equal("Ny tittel", saved.Title);
        Assert.Equal("Ny beskrivelse", saved.Description);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalseForMissingIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var service = new IssueService(scope.Db);

        var updated = await service.UpdateAsync(
            999999,
            "Ny tittel",
            null);

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projectService = new ProjectService(scope.Db);
        var issueService = new IssueService(scope.Db);

        var project = await projectService.CreateAsync(
            "Testprosjekt", null);

        var issue = await issueService.CreateAsync(
            project.Id, "Sak som slettes", null);

        var deleted = await issueService.DeleteAsync(issue.Id);

        Assert.True(deleted);
        Assert.Null(await issueService.GetByIdAsync(issue.Id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalseForMissingIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var service = new IssueService(scope.Db);

        var deleted = await service.DeleteAsync(999999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeletingProject_ShouldAlsoDeleteItsIssues()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projectService = new ProjectService(scope.Db);
        var issueService = new IssueService(scope.Db);

        var project = await projectService.CreateAsync(
            "Prosjekt som slettes", null);

        var issue = await issueService.CreateAsync(
            project.Id, "Tilhørende sak", null);

        var deleted = await projectService.DeleteAsync(project.Id);

        Assert.True(deleted);
        Assert.Null(await issueService.GetByIdAsync(issue.Id));
    }
}
