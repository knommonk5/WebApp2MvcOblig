using System.ComponentModel.DataAnnotations;

namespace PersonalAgent.Core.Entities;

public class IssueComment
{
    public int Id { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public int IssueId { get; set; }

    public Issue Issue { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}