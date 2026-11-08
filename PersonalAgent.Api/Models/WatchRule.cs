namespace PersonalAgent.Api.Models;

public class WatchRule
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int InterestId { get; set; }
    public Interest Interest { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string SearchTerm { get; set; } = string.Empty;

    public decimal? MaxPrice { get; set; }
    public string Currency { get; set; } = "NOK";
    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}