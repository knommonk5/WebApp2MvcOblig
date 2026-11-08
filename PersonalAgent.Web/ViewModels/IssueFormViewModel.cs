using System.ComponentModel.DataAnnotations;

namespace PersonalAgent.Web.ViewModels;

public class IssueFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Velg et prosjekt.")]
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Sakstittel er påkrevd.")]
    [StringLength(150, ErrorMessage = "Maksimalt 150 tegn.")]
    [Display(Name = "Tittel")]
    public string Title { get; set; } = string.Empty;

    [StringLength(3000, ErrorMessage = "Maksimalt 3000 tegn.")]
    [Display(Name = "Beskrivelse")]
    public string? Description { get; set; }
}