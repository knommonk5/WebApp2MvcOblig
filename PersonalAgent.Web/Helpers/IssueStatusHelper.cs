using PersonalAgent.Core.Enums;

namespace PersonalAgent.Web.Helpers;

public static class IssueStatusHelper
{
    public static string GetDisplayName(IssueStatus status)
    {
        return status switch
        {
            IssueStatus.Open => "Åpen",
            IssueStatus.Assigned => "Tildelt",
            IssueStatus.InProgress => "Pågår",
            IssueStatus.Closed => "Lukket",
            _ => "Ukjent"
        };
    }

    public static string GetBadgeClass(IssueStatus status)
    {
        return status switch
        {
            IssueStatus.Open => "bg-secondary",
            IssueStatus.Assigned => "bg-info text-dark",
            IssueStatus.InProgress => "bg-primary",
            IssueStatus.Closed => "bg-success",
            _ => "bg-dark"
        };
    }
}