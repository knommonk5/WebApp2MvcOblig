using PersonalAgent.Core.Enums;

namespace PersonalAgent.Web.Tests.Unit;

public class IssueStatusTests
{
    [Fact]
    public void IssueStatus_ShouldContainFourExpectedValues()
    {
        var values = Enum.GetValues<IssueStatus>();

        Assert.Equal(4, values.Length);

        Assert.Contains(IssueStatus.Open, values);
        Assert.Contains(IssueStatus.Assigned, values);
        Assert.Contains(IssueStatus.InProgress, values);
        Assert.Contains(IssueStatus.Closed, values);
    }

    [Fact]
    public void IssueStatus_ShouldPreserveDatabaseValues()
    {
        Assert.Equal(0, (int)IssueStatus.Open);
        Assert.Equal(1, (int)IssueStatus.InProgress);
        Assert.Equal(2, (int)IssueStatus.Closed);
        Assert.Equal(3, (int)IssueStatus.Assigned);
    }
}