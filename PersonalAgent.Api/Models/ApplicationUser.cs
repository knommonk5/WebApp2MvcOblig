using Microsoft.AspNetCore.Identity;

namespace PersonalAgent.Api.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Interest> Interests { get; set; } = new List<Interest>();
    public ICollection<Preference> Preferences { get; set; } = new List<Preference>();
    public ICollection<WatchRule> WatchRules { get; set; } = new List<WatchRule>();
}