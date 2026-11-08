using System.ComponentModel.DataAnnotations;

namespace PersonalAgent.Api.DTOs.WatchRules;

public class SaveWatchRuleDto
{
    [Range(1, int.MaxValue)]
    public int InterestId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string SearchTerm { get; set; } = string.Empty;

    public decimal? MaxPrice { get; set; }

    [Required]
    [RegularExpression("^(NOK|SEK|EUR|USD)$",
        ErrorMessage = "Valuta må være NOK, SEK, EUR eller USD.")]
    public string Currency { get; set; } = "NOK";

    public bool Enabled { get; set; } = true;
}

public record WatchRuleDto(
    int Id,
    int InterestId,
    string Title,
    string SearchTerm,
    decimal? MaxPrice,
    string Currency,
    bool Enabled,
    DateTime CreatedAt,
    DateTime UpdatedAt);