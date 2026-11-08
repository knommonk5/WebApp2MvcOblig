using System.ComponentModel.DataAnnotations;

namespace PersonalAgent.Web.ViewModels;

public class ProjectFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Prosjektnavn er påkrevd.")]
    [StringLength(100, ErrorMessage = "Maksimalt 100 tegn.")]
    [Display(Name = "Prosjektnavn")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Maksimalt 1000 tegn.")]
    [Display(Name = "Beskrivelse")]
    public string? Description { get; set; }
}