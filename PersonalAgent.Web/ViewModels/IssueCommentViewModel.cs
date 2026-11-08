using System.ComponentModel.DataAnnotations;

namespace PersonalAgent.Web.ViewModels;

public class IssueCommentViewModel
{
    public int Id { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CreateIssueCommentViewModel
{
    [Range(1, int.MaxValue)]
    public int IssueId { get; set; }

    [Required(ErrorMessage = "Du må skrive en kommentar.")]
    [StringLength(
        2000,
        ErrorMessage = "Kommentaren kan ikke overstige 2000 tegn.")]
    public string Content { get; set; } = string.Empty;
}