using System.ComponentModel.DataAnnotations;

namespace PersonalAgent.Api.DTOs.Preferences;

public class SavePreferenceDto
{
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Value { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? InterestId { get; set; }
}

public record PreferenceDto(
    int Id,
    string Key,
    string Value,
    int? InterestId,
    DateTime CreatedAt,
    DateTime UpdatedAt);