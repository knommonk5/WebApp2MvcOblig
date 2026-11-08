namespace PersonalAgent.Api.Models;

public class Preference
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int? InterestId { get; set; }
    public Interest? Interest { get; set; }

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}