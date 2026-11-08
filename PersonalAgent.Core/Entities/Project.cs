using System.ComponentModel.DataAnnotations;

namespace PersonalAgent.Core.Entities;

public class Project
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Prosjektnavn er påkrevd.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Issue> Issues { get; set; }
        = new List<Issue>();
}