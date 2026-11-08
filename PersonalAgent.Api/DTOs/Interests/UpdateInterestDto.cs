using System.ComponentModel.DataAnnotations;

namespace PersonalAgent.Api.DTOs.Interests;

public class UpdateInterestDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;
}