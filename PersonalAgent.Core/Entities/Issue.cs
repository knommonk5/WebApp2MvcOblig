using System.ComponentModel.DataAnnotations;
using PersonalAgent.Core.Enums;

namespace PersonalAgent.Core.Entities;

public class Issue
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(3000)]
    public string? Description { get; set; }

    public IssueStatus Status { get; set; } = IssueStatus.Open;

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public string? AssignedUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<IssueComment> Comments { get; set; }
        = new List<IssueComment>();
}