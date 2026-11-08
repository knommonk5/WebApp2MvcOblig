using Microsoft.EntityFrameworkCore;
using PersonalAgent.Api.DTOs.Interests;
using PersonalAgent.Api.DTOs.Preferences;
using PersonalAgent.Api.DTOs.WatchRules;
using PersonalAgent.Api.Services;
using Xunit;

namespace PersonalAgent.Tests.Integration;

public class AgentCoreTests
{
    [Fact]
    public async Task InterestCrud_PersistsChangesAndDeletes()
    {
        await using var scope = await DatabaseScope.CreateAsync();
        var service = new InterestService(scope.Db);

        var created = await service.CreateAsync(new CreateInterestDto
        {
            Name = "  Magic the Gathering  ",
            Category = "Hobby"
        }, "user-a");

        Assert.Equal("Magic the Gathering", created.Name);
        Assert.True(created.Id > 0);
        Assert.NotEqual(default(DateTime), created.CreatedAt);

        scope.Db.ChangeTracker.Clear();

        var loaded = await service.GetByIdAsync(created.Id, "user-a");
        Assert.NotNull(loaded);
        Assert.Equal(created.Name, loaded.Name);

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateInterestDto
            {
                Name = "Commander",
                Category = "Kortspill"
            },
            "user-a");

        Assert.NotNull(updated);
        Assert.Equal(loaded.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= loaded.UpdatedAt);

        scope.Db.ChangeTracker.Clear();

        var reloaded = await service.GetByIdAsync(created.Id, "user-a");
        Assert.Equal("Commander", reloaded!.Name);

        Assert.Equal(
            DeleteInterestResult.Deleted,
            await service.DeleteAsync(created.Id, "user-a"));

        Assert.Null(await service.GetByIdAsync(created.Id, "user-a"));
    }

    [Fact]
    public async Task OtherUser_CannotReadUpdateOrDeleteInterest()
    {
        await using var scope = await DatabaseScope.CreateAsync();
        var service = new InterestService(scope.Db);

        var interest = await service.CreateAsync(new CreateInterestDto
        {
            Name = "Commander",
            Category = "Hobby"
        }, "user-a");

        Assert.Empty(await service.GetAllAsync("user-b"));
        Assert.Null(await service.GetByIdAsync(interest.Id, "user-b"));

        Assert.Null(await service.UpdateAsync(
            interest.Id,
            new UpdateInterestDto
            {
                Name = "Endret av B",
                Category = "Hobby"
            },
            "user-b"));

        Assert.Equal(
            DeleteInterestResult.NotFound,
            await service.DeleteAsync(interest.Id, "user-b"));

        var unchanged = await service.GetByIdAsync(interest.Id, "user-a");
        Assert.Equal("Commander", unchanged!.Name);
    }

    [Fact]
    public async Task PreferenceCrud_RespectsOwnershipAndDeletionRestriction()
    {
        await using var scope = await DatabaseScope.CreateAsync();
        var interests = new InterestService(scope.Db);
        var preferences = new PreferenceService(scope.Db);

        var interest = await interests.CreateAsync(new CreateInterestDto
        {
            Name = "Magic",
            Category = "Hobby"
        }, "user-a");

        var preference = await preferences.CreateAsync(
            new SavePreferenceDto
            {
                Key = "PreferredFormat",
                Value = "Commander",
                InterestId = interest.Id
            },
            "user-a");

        Assert.Equal(
            DeleteInterestResult.HasDependencies,
            await interests.DeleteAsync(interest.Id, "user-a"));

        Assert.Empty(await preferences.GetAllAsync("user-b", null));
        Assert.Null(await preferences.GetByIdAsync(preference.Id, "user-b"));

        Assert.Null(await preferences.UpdateAsync(
            preference.Id,
            new SavePreferenceDto { Key = "Format", Value = "Other" },
            "user-b"));

        Assert.False(await preferences.DeleteAsync(preference.Id, "user-b"));

        var updated = await preferences.UpdateAsync(
            preference.Id,
            new SavePreferenceDto
            {
                Key = "PreferredFormat",
                Value = "Standard",
                InterestId = interest.Id
            },
            "user-a");

        Assert.Equal("Standard", updated!.Value);

        scope.Db.ChangeTracker.Clear();

        Assert.Equal(
            "Standard",
            (await preferences.GetByIdAsync(preference.Id, "user-a"))!.Value);

        Assert.True(await preferences.DeleteAsync(preference.Id, "user-a"));

        Assert.Equal(
            DeleteInterestResult.Deleted,
            await interests.DeleteAsync(interest.Id, "user-a"));
    }

    [Fact]
    public async Task Preference_CannotLinkToAnotherUsersInterest()
    {
        await using var scope = await DatabaseScope.CreateAsync();
        var interests = new InterestService(scope.Db);
        var preferences = new PreferenceService(scope.Db);

        var interest = await interests.CreateAsync(new CreateInterestDto
        {
            Name = "Magic",
            Category = "Hobby"
        }, "user-a");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            preferences.CreateAsync(new SavePreferenceDto
            {
                Key = "Format",
                Value = "Commander",
                InterestId = interest.Id
            }, "user-b"));

        Assert.Empty(await scope.Db.Preferences.ToListAsync());
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(12.345)]
    public async Task WatchRule_RejectsInvalidPrice(double price)
    {
        await using var scope = await DatabaseScope.CreateAsync();
        var interests = new InterestService(scope.Db);
        var rules = new WatchRuleService(scope.Db);

        var interest = await interests.CreateAsync(new CreateInterestDto
        {
            Name = "Magic",
            Category = "Hobby"
        }, "user-a");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            rules.CreateAsync(new SaveWatchRuleDto
            {
                InterestId = interest.Id,
                Title = "Commander",
                SearchTerm = "Commander deck",
                MaxPrice = (decimal)price,
                Currency = "NOK"
            }, "user-a"));

        Assert.Empty(await scope.Db.WatchRules.ToListAsync());
    }

    [Fact]
    public async Task WatchRuleCrud_FiltersAndProtectsOwnership()
    {
        await using var scope = await DatabaseScope.CreateAsync();
        var interests = new InterestService(scope.Db);
        var rules = new WatchRuleService(scope.Db);

        var interest = await interests.CreateAsync(new CreateInterestDto
        {
            Name = "Magic",
            Category = "Hobby"
        }, "user-a");

        var dto = new SaveWatchRuleDto
        {
            InterestId = interest.Id,
            Title = "Commander",
            SearchTerm = "Commander deck",
            MaxPrice = 600,
            Currency = "NOK",
            Enabled = true
        };

        var rule = await rules.CreateAsync(dto, "user-a");

        Assert.Single(await rules.GetAllAsync("user-a", interest.Id, true));
        Assert.Empty(await rules.GetAllAsync("user-b", null, null));
        Assert.Null(await rules.GetByIdAsync(rule.Id, "user-b"));
        Assert.Null(await rules.UpdateAsync(rule.Id, dto, "user-b"));
        Assert.False(await rules.DeleteAsync(rule.Id, "user-b"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            rules.CreateAsync(dto, "user-b"));

        Assert.Equal(
            DeleteInterestResult.HasDependencies,
            await interests.DeleteAsync(interest.Id, "user-a"));

        dto.Enabled = false;
        await rules.UpdateAsync(rule.Id, dto, "user-a");

        scope.Db.ChangeTracker.Clear();

        Assert.False((await rules.GetByIdAsync(rule.Id, "user-a"))!.Enabled);
        Assert.Empty(await rules.GetAllAsync("user-a", null, true));
        Assert.Single(await rules.GetAllAsync("user-a", interest.Id, false));

        Assert.True(await rules.DeleteAsync(rule.Id, "user-a"));
        Assert.Null(await rules.GetByIdAsync(rule.Id, "user-a"));
    }
}