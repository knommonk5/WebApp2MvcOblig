using Microsoft.EntityFrameworkCore;
using PersonalAgent.Api.Data;
using PersonalAgent.Api.DTOs.Preferences;
using PersonalAgent.Api.Models;

namespace PersonalAgent.Api.Services;

public class PreferenceService
{
    private readonly AgentDbContext _db;

    public PreferenceService(AgentDbContext db)
    {
        _db = db;
    }

    public async Task<List<PreferenceDto>> GetAllAsync(
        string userId, int? interestId)
    {
        var query = _db.Preferences
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (interestId.HasValue)
        {
            query = query.Where(x => x.InterestId == interestId.Value);
        }

        return await query
            .OrderBy(x => x.Key)
            .Select(x => new PreferenceDto(
                x.Id,
                x.Key,
                x.Value,
                x.InterestId,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync();
    }

    public async Task<PreferenceDto?> GetByIdAsync(int id, string userId)
    {
        var preference = await _db.Preferences
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        return preference is null ? null : ToDto(preference);
    }

    public async Task<PreferenceDto> CreateAsync(
        SavePreferenceDto dto, string userId)
    {
        await ValidateAsync(dto, userId);

        var preference = new Preference
        {
            UserId = userId,
            Key = dto.Key.Trim(),
            Value = dto.Value.Trim(),
            InterestId = dto.InterestId
        };

        _db.Preferences.Add(preference);
        await _db.SaveChangesAsync();

        return ToDto(preference);
    }

    public async Task<PreferenceDto?> UpdateAsync(
        int id, SavePreferenceDto dto, string userId)
    {
        var preference = await _db.Preferences
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (preference is null)
        {
            return null;
        }

        await ValidateAsync(dto, userId);

        preference.Key = dto.Key.Trim();
        preference.Value = dto.Value.Trim();
        preference.InterestId = dto.InterestId;

        await _db.SaveChangesAsync();

        return ToDto(preference);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var preference = await _db.Preferences
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (preference is null)
        {
            return false;
        }

        _db.Preferences.Remove(preference);
        await _db.SaveChangesAsync();

        return true;
    }

    private async Task ValidateAsync(SavePreferenceDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Key)
            || dto.Key.Length > 100
            || string.IsNullOrWhiteSpace(dto.Value)
            || dto.Value.Length > 500)
        {
            throw new ArgumentException(
                "Nøkkel og verdi er påkrevd. Maks lengde er 100 og 500 tegn.");
        }

        if (dto.InterestId.HasValue)
        {
            var ownsInterest = await _db.Interests.AnyAsync(
                x => x.Id == dto.InterestId.Value && x.UserId == userId);

            if (!ownsInterest)
            {
                throw new ArgumentException(
                    "Interessen finnes ikke eller tilhører ikke deg.");
            }
        }
    }

    private static PreferenceDto ToDto(Preference preference)
    {
        return new PreferenceDto(
            preference.Id,
            preference.Key,
            preference.Value,
            preference.InterestId,
            preference.CreatedAt,
            preference.UpdatedAt);
    }
}