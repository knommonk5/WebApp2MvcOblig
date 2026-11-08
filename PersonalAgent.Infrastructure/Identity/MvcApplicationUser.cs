using Microsoft.AspNetCore.Identity;

namespace PersonalAgent.Infrastructure.Identity;

public class MvcApplicationUser : IdentityUser
{
    public ICollection<PersonalAgent.Core.Entities.Issue>
        AssignedIssues { get; set; } = new List<PersonalAgent.Core.Entities.Issue>();
}