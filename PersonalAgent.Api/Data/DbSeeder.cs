using Microsoft.AspNetCore.Identity;
using PersonalAgent.Api.Models;

namespace PersonalAgent.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var users = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var db = scope.ServiceProvider
            .GetRequiredService<AgentDbContext>();

        await SeedUserAsync(
            users, db,
            "demo.a@example.com",
            "Magic the Gathering",
            "Commander");

        await SeedUserAsync(
            users, db,
            "demo.b@example.com",
            "Mini-PC",
            "SmallFormFactor");
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> users,
        AgentDbContext db,
        string email,
        string interestName,
        string preferenceValue)
    {
        if (await users.FindByEmailAsync(email) is not null)
        {
            return;
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await users.CreateAsync(user, "DemoAgent!2026");

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        var interest = new Interest
        {
            UserId = user.Id,
            Name = interestName,
            Category = "Hobby"
        };

        db.Interests.Add(interest);

        db.Preferences.Add(new Preference
        {
            UserId = user.Id,
            Interest = interest,
            Key = "PreferredStyle",
            Value = preferenceValue
        });

        db.WatchRules.Add(new WatchRule
        {
            UserId = user.Id,
            Interest = interest,
            Title = $"Overvåk {interestName}",
            SearchTerm = interestName,
            MaxPrice = 600,
            Currency = "NOK",
            Enabled = true
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}