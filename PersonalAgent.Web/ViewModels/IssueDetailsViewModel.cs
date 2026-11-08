using PersonalAgent.Core.Enums;

namespace PersonalAgent.Web.ViewModels;

public class IssueDetailsViewModel
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IssueStatus Status { get; set; }

    public string? AssignedUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public List<IssueCommentViewModel> Comments { get; set; } = new();

    public string? AssignedUserName { get; set; }
}