namespace PersonalAgent.Api.Models;

public class Interest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Preference> Preferences { get; set; } = new List<Preference>();
    public ICollection<WatchRule> WatchRules { get; set; } = new List<WatchRule>();
}