
using PersonalAgent.Infrastructure.Identity;
using PersonalAgent.Infrastructure.Services;

namespace PersonalAgent.Web.Tests.Integration;

public class CommentServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldSaveComment()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var user = new MvcApplicationUser
        {
            UserName = "commenter@test.local",
            Email = "commenter@test.local"
        };

        scope.Db.Users.Add(user);
        await scope.Db.SaveChangesAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);
        var comments = new CommentService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        var comment = await comments.CreateAsync(
            issue.Id,
            user.Id,
            "Dette er en testkommentar.");

        Assert.True(comment.Id > 0);
        Assert.Equal(issue.Id, comment.IssueId);
        Assert.Equal(user.Id, comment.AuthorId);
        Assert.Equal("Dette er en testkommentar.", comment.Content);
    }

    [Fact]
    public async Task GetByIssueIdAsync_ShouldReturnCommentsInOrder()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var user = new MvcApplicationUser
        {
            UserName = "commenter@test.local",
            Email = "commenter@test.local"
        };

        scope.Db.Users.Add(user);
        await scope.Db.SaveChangesAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);
        var comments = new CommentService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        await comments.CreateAsync(issue.Id, user.Id, "Første");
        await comments.CreateAsync(issue.Id, user.Id, "Andre");

        var result = await comments.GetByIssueIdAsync(issue.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal("Første", result[0].Content);
        Assert.Equal("Andre", result[1].Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ShouldRejectEmptyContent(
        string content)
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var comments = new CommentService(scope.Db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => comments.CreateAsync(1, "some-user", content));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectTooLongContent()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var comments = new CommentService(scope.Db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => comments.CreateAsync(
                1,
                "some-user",
                new string('A', 2001)));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUnknownIssue()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var comments = new CommentService(scope.Db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => comments.CreateAsync(
                999999,
                "some-user",
                "Kommentar"));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUnknownAuthor()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);
        var comments = new CommentService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => comments.CreateAsync(
                issue.Id,
                "unknown-user",
                "Kommentar"));
    }

    [Fact]
    public async Task DeletingIssue_ShouldDeleteItsComments()
    {
        await using var scope = await DatabaseScope.CreateAsync();

        var user = new MvcApplicationUser
        {
            UserName = "commenter@test.local",
            Email = "commenter@test.local"
        };

        scope.Db.Users.Add(user);
        await scope.Db.SaveChangesAsync();

        var projects = new ProjectService(scope.Db);
        var issues = new IssueService(scope.Db);
        var comments = new CommentService(scope.Db);

        var project = await projects.CreateAsync("Prosjekt", null);
        var issue = await issues.CreateAsync(project.Id, "Sak", null);

        await comments.CreateAsync(
            issue.Id,
            user.Id,
            "Kommentar som skal slettes.");

        await issues.DeleteAsync(issue.Id);

        var result = await comments.GetByIssueIdAsync(issue.Id);

        Assert.Empty(result);
    }
}
