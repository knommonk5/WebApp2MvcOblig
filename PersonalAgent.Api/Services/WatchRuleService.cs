using Microsoft.EntityFrameworkCore;
using PersonalAgent.Api.Data;
using PersonalAgent.Api.DTOs.WatchRules;
using PersonalAgent.Api.Models;

namespace PersonalAgent.Api.Services;

public class WatchRuleService
{
    private readonly AgentDbContext _db;

    public WatchRuleService(AgentDbContext db)
    {
        _db = db;
    }

    public async Task<List<WatchRuleDto>> GetAllAsync(
        string userId, int? interestId, bool? enabled)
    {
        var query = _db.WatchRules
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (interestId.HasValue)
        {
            query = query.Where(x => x.InterestId == interestId.Value);
        }

        if (enabled.HasValue)
        {
            query = query.Where(x => x.Enabled == enabled.Value);
        }

        return await query
            .OrderBy(x => x.Title)
            .Select(x => new WatchRuleDto(
                x.Id,
                x.InterestId,
                x.Title,
                x.SearchTerm,
                x.MaxPrice,
                x.Currency,
                x.Enabled,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync();
    }

    public async Task<WatchRuleDto?> GetByIdAsync(int id, string userId)
    {
        var rule = await _db.WatchRules
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        return rule is null ? null : ToDto(rule);
    }

    public async Task<WatchRuleDto> CreateAsync(
        SaveWatchRuleDto dto, string userId)
    {
        await ValidateAsync(dto, userId);

        var rule = new WatchRule
        {
            UserId = userId,
            InterestId = dto.InterestId,
            Title = dto.Title.Trim(),
            SearchTerm = dto.SearchTerm.Trim(),
            MaxPrice = dto.MaxPrice,
            Currency = dto.Currency,
            Enabled = dto.Enabled
        };

        _db.WatchRules.Add(rule);
        await _db.SaveChangesAsync();

        return ToDto(rule);
    }

    public async Task<WatchRuleDto?> UpdateAsync(
        int id, SaveWatchRuleDto dto, string userId)
    {
        var rule = await _db.WatchRules
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (rule is null)
        {
            return null;
        }

        await ValidateAsync(dto, userId);

        rule.InterestId = dto.InterestId;
        rule.Title = dto.Title.Trim();
        rule.SearchTerm = dto.SearchTerm.Trim();
        rule.MaxPrice = dto.MaxPrice;
        rule.Currency = dto.Currency;
        rule.Enabled = dto.Enabled;

        await _db.SaveChangesAsync();

        return ToDto(rule);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var rule = await _db.WatchRules
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (rule is null)
        {
            return false;
        }

        _db.WatchRules.Remove(rule);
        await _db.SaveChangesAsync();

        return true;
    }

    private async Task ValidateAsync(SaveWatchRuleDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)
            || dto.Title.Length > 100
            || string.IsNullOrWhiteSpace(dto.SearchTerm)
            || dto.SearchTerm.Length > 200)
        {
            throw new ArgumentException(
                "Tittel og søkeord er påkrevd. Maks lengde er 100 og 200 tegn.");
        }

        if (dto.MaxPrice.HasValue)
        {
            var price = dto.MaxPrice.Value;

            if (price <= 0
                || price > 9999999999999999.99m
                || decimal.Round(price, 2) != price)
            {
                throw new ArgumentException(
                    "Makspris må være positiv, passe i prisfeltet og ha maks to desimaler.");
            }
        }

        if (dto.Currency is not ("NOK" or "SEK" or "EUR" or "USD"))
        {
            throw new ArgumentException(
                "Valuta må være NOK, SEK, EUR eller USD.");
        }

        var ownsInterest = await _db.Interests.AnyAsync(
            x => x.Id == dto.InterestId && x.UserId == userId);

        if (!ownsInterest)
        {
            throw new ArgumentException(
                "Interessen finnes ikke eller tilhører ikke deg.");
        }
    }

    private static WatchRuleDto ToDto(WatchRule rule)
    {
        return new WatchRuleDto(
            rule.Id,
            rule.InterestId,
            rule.Title,
            rule.SearchTerm,
            rule.MaxPrice,
            rule.Currency,
            rule.Enabled,
            rule.CreatedAt,
            rule.UpdatedAt);
    }
}