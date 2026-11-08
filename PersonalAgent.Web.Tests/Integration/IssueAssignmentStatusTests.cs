
using Microsoft.AspNetCore.Identity;
using PersonalAgent.Core.Enums;
using PersonalAgent.Infrastructure.Identity;
using PersonalAgent.Infrastructure.Services;

namespace PersonalAgent.Web.Tests.Integration;

public class IssueAssignmentStatusTests
{
    [Fact]
    public async Task AssignAsync_ShouldAssignExistingUser()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var user = new MvcApplicationUser
        {
            UserName = "assigned@test.local",
            Email = "assigned@test.local"
        };

        scope.Db.Users.Add(user);
        await scope.Db.SaveChangesAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        Assert.True(await issues.AssignAsync(issue.Id, user.Id));

        var saved = await issues.GetByIdAsync(issue.Id);
        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved.AssignedUserId);
    }

    [Fact]
    public async Task AssignAsync_ShouldRejectUnknownUser()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => issues.AssignAsync(issue.Id, "unknown-user"));

        var saved = await issues.GetByIdAsync(issue.Id);
        Assert.NotNull(saved);
        Assert.Null(saved.AssignedUserId);
    }

    [Fact]
    public async Task AssignedUser_ShouldBeAbleToStartAndCloseIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var user = new MvcApplicationUser
        {
            UserName = "worker@test.local",
            Email = "worker@test.local"
        };

        scope.Db.Users.Add(user);
        await scope.Db.SaveChangesAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        await issues.AssignAsync(issue.Id, user.Id);
        
        var assigned = await issues.GetByIdAsync(issue.Id);

        Assert.NotNull(assigned);
        Assert.Equal(IssueStatus.Assigned, assigned.Status);

        Assert.True(await issues.ChangeStatusAsync(
            issue.Id, IssueStatus.InProgress, user.Id, false));

        Assert.True(await issues.ChangeStatusAsync(
            issue.Id, IssueStatus.Closed, user.Id, false));

        var saved = await issues.GetByIdAsync(issue.Id);
        Assert.NotNull(saved);
        Assert.Equal(IssueStatus.Closed, saved.Status);
    }

    [Fact]
    public async Task OtherUser_ShouldNotBeAbleToChangeStatus()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => issues.ChangeStatusAsync(
                issue.Id,
                IssueStatus.Closed,
                "unassigned-user",
                false));

        var saved = await issues.GetByIdAsync(issue.Id);
        Assert.NotNull(saved);
        Assert.Equal(IssueStatus.Open, saved.Status);
    }

    
    [Fact]
    public async Task AssignedUser_ShouldNotBeAbleToReopenClosedIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var user = new MvcApplicationUser
        {
            UserName = "worker@test.local",
            Email = "worker@test.local"
        };

        scope.Db.Users.Add(user);
        await scope.Db.SaveChangesAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        // 1. Administrator tildeler saken
        await issues.AssignAsync(issue.Id, user.Id);

        // 2. Brukeren starter arbeidet
        await issues.ChangeStatusAsync(
            issue.Id,
            IssueStatus.InProgress,
            user.Id,
            false);

        // 3. Brukeren lukker saken
        await issues.ChangeStatusAsync(
            issue.Id,
            IssueStatus.Closed,
            user.Id,
            false);

        // 4. Brukeren forsøker å gjenåpne saken
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => issues.ChangeStatusAsync(
                issue.Id,
                IssueStatus.InProgress,
                user.Id,
                false));

        // 5. Kontroller at status fortsatt er Lukket
        var saved = await issues.GetByIdAsync(issue.Id);

        Assert.NotNull(saved);
        Assert.Equal(IssueStatus.Closed, saved.Status);
    }


    [Fact]
    public async Task Admin_ShouldBeAbleToReopenClosedIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        await issues.ChangeStatusAsync(
            issue.Id, IssueStatus.Closed, "admin-id", true);

        Assert.True(await issues.ChangeStatusAsync(
            issue.Id, IssueStatus.Open, "admin-id", true));

        var saved = await issues.GetByIdAsync(issue.Id);
        Assert.NotNull(saved);
        Assert.Equal(IssueStatus.Open, saved.Status);
    }

    [Fact]
    public async Task AssignAsync_ShouldAllowUnassigningIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var user = new MvcApplicationUser
        {
            UserName = "worker@test.local",
            Email = "worker@test.local"
        };

        scope.Db.Users.Add(user);
        await scope.Db.SaveChangesAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        await issues.AssignAsync(issue.Id, user.Id);
        Assert.True(await issues.AssignAsync(issue.Id, null));

        var saved = await issues.GetByIdAsync(issue.Id);
        Assert.NotNull(saved);
        Assert.Null(saved.AssignedUserId);
    }
    
    
[Fact]
public async Task ReassigningIssue_ShouldResetStatusToAssigned()
{
    await using var scope = await DatabaseScope.CreateAsync();

    var firstUser = new MvcApplicationUser
    {
        UserName = "first@test.local",
        Email = "first@test.local"
    };

    var secondUser = new MvcApplicationUser
    {
        UserName = "second@test.local",
        Email = "second@test.local"
    };

    scope.Db.Users.AddRange(firstUser, secondUser);
    await scope.Db.SaveChangesAsync();

    var projects = new ProjectService(scope.Db);
    var issues = new IssueService(scope.Db);

    var project = await projects.CreateAsync("Prosjekt", null);
    var issue = await issues.CreateAsync(project.Id, "Sak", null);

    await issues.AssignAsync(issue.Id, firstUser.Id);

    await issues.ChangeStatusAsync(
        issue.Id, IssueStatus.Closed, "admin-id", true);

    await issues.AssignAsync(issue.Id, secondUser.Id);

    var saved = await issues.GetByIdAsync(issue.Id);

    Assert.NotNull(saved);
    Assert.Equal(secondUser.Id, saved.AssignedUserId);
    Assert.Equal(IssueStatus.Assigned, saved.Status);
}

[Fact]
public async Task AssigningSameUser_ShouldPreserveStatus()
{
    await using var scope = await DatabaseScope.CreateAsync();

    var user = new MvcApplicationUser
    {
        UserName = "worker@test.local",
        Email = "worker@test.local"
    };

    scope.Db.Users.Add(user);
    await scope.Db.SaveChangesAsync();

    var projects = new ProjectService(scope.Db);
    var issues = new IssueService(scope.Db);

    var project = await projects.CreateAsync("Prosjekt", null);
    var issue = await issues.CreateAsync(project.Id, "Sak", null);

    await issues.AssignAsync(issue.Id, user.Id);

    await issues.ChangeStatusAsync(
        issue.Id, IssueStatus.InProgress, user.Id, false);

    await issues.AssignAsync(issue.Id, user.Id);

    var saved = await issues.GetByIdAsync(issue.Id);

    Assert.NotNull(saved);
    Assert.Equal(IssueStatus.InProgress, saved.Status);
}


[Fact]
public async Task AssignedUser_ShouldNotBeAbleToSkipInProgress()
{
    await using var scope = await DatabaseScope.CreateAsync();

    var user = new MvcApplicationUser
    {
        UserName = "worker@test.local",
        Email = "worker@test.local"
    };

    scope.Db.Users.Add(user);
    await scope.Db.SaveChangesAsync();

    var projects = new ProjectService(scope.Db);
    var issues = new IssueService(scope.Db);

    var project = await projects.CreateAsync("Prosjekt", null);
    var issue = await issues.CreateAsync(project.Id, "Sak", null);

    await issues.AssignAsync(issue.Id, user.Id);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => issues.ChangeStatusAsync(
            issue.Id,
            IssueStatus.Closed,
            user.Id,
            false));

    var saved = await issues.GetByIdAsync(issue.Id);

    Assert.NotNull(saved);
    Assert.Equal(IssueStatus.Assigned, saved.Status);
}

[Fact]
public async Task ChangeStatusAsync_ShouldRejectInvalidEnumValue()
{
    await using var scope = await DatabaseScope.CreateAsync();

    var projects = new ProjectService(scope.Db);
    var issues = new IssueService(scope.Db);

    var project = await projects.CreateAsync("Prosjekt", null);
    var issue = await issues.CreateAsync(project.Id, "Sak", null);

    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
        () => issues.ChangeStatusAsync(
            issue.Id,
            (IssueStatus)999,
            "admin-id",
            true));

    var saved = await issues.GetByIdAsync(issue.Id);

    Assert.NotNull(saved);
    Assert.Equal(IssueStatus.Open, saved.Status);
}

[Fact]
public async Task ChangeStatusAsync_ShouldRejectMissingActor()
{
    await using var scope = await DatabaseScope.CreateAsync();

    var projects = new ProjectService(scope.Db);
    var issues = new IssueService(scope.Db);

    var project = await projects.CreateAsync("Prosjekt", null);
    var issue = await issues.CreateAsync(project.Id, "Sak", null);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => issues.ChangeStatusAsync(
            issue.Id,
            IssueStatus.Closed,
            "",
            true));

    var saved = await issues.GetByIdAsync(issue.Id);

    Assert.NotNull(saved);
    Assert.Equal(IssueStatus.Open, saved.Status);
}

[Theory]
[InlineData(IssueStatus.Open)]
[InlineData(IssueStatus.Assigned)]
[InlineData(IssueStatus.InProgress)]
[InlineData(IssueStatus.Closed)]
public async Task Admin_ShouldBeAbleToOverrideAnyStatus(
    IssueStatus targetStatus)
{
    await using var scope = await DatabaseScope.CreateAsync();

    var projects = new ProjectService(scope.Db);
    var issues = new IssueService(scope.Db);

    var project = await projects.CreateAsync("Prosjekt", null);
    var issue = await issues.CreateAsync(project.Id, "Sak", null);

    await issues.ChangeStatusAsync(
        issue.Id, IssueStatus.Closed, "admin-id", true);

    var changed = await issues.ChangeStatusAsync(
        issue.Id, targetStatus, "admin-id", true);

    Assert.True(changed);

    var saved = await issues.GetByIdAsync(issue.Id);

    Assert.NotNull(saved);
    Assert.Equal(targetStatus, saved.Status);
}

}
