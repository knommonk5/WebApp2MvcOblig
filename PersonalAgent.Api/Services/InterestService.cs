using Microsoft.EntityFrameworkCore;
using PersonalAgent.Api.Data;
using PersonalAgent.Api.DTOs.Interests;
using PersonalAgent.Api.Models;

namespace PersonalAgent.Api.Services;

public enum DeleteInterestResult
{
    Deleted,
    NotFound,
    HasDependencies
}

public class InterestService
{
    private readonly AgentDbContext _db;

    public InterestService(AgentDbContext db)
    {
        _db = db;
    }

    public async Task<List<InterestDto>> GetAllAsync(string userId)
    {
        return await _db.Interests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => new InterestDto
            {
                Id = x.Id,
                Name = x.Name,
                Category = x.Category,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<InterestDto?> GetByIdAsync(int id, string userId)
    {
        var interest = await _db.Interests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        return interest is null ? null : ToDto(interest);
    }

    public async Task<InterestDto> CreateAsync(
        CreateInterestDto dto,
        string userId)
    {
        var interest = new Interest
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim()
        };

        _db.Interests.Add(interest);
        await _db.SaveChangesAsync();

        return ToDto(interest);
    }

    public async Task<InterestDto?> UpdateAsync(
        int id,
        UpdateInterestDto dto,
        string userId)
    {
        var interest = await _db.Interests
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (interest is null)
        {
            return null;
        }

        interest.Name = dto.Name.Trim();
        interest.Category = dto.Category.Trim();

        await _db.SaveChangesAsync();

        return ToDto(interest);
    }

    public async Task<DeleteInterestResult> DeleteAsync(int id, string userId)
    {
        var interest = await _db.Interests
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (interest is null)
        {
            return DeleteInterestResult.NotFound;
        }

        var hasPreferences = await _db.Preferences
            .AnyAsync(x => x.InterestId == id);

        var hasWatchRules = await _db.WatchRules
            .AnyAsync(x => x.InterestId == id);

        if (hasPreferences || hasWatchRules)
        {
            return DeleteInterestResult.HasDependencies;
        }

        _db.Interests.Remove(interest);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is Npgsql.PostgresException
                  { SqlState: "23503" })
        {
            // En tilknyttet registrering kan ha blitt opprettet
            // mellom kontrollen ovenfor og slettingen.
            _db.Entry(interest).State = EntityState.Unchanged;
            return DeleteInterestResult.HasDependencies;
        }

        return DeleteInterestResult.Deleted;
    }

    private static InterestDto ToDto(Interest interest)
    {
        return new InterestDto
        {
            Id = interest.Id,
            Name = interest.Name,
            Category = interest.Category,
            CreatedAt = interest.CreatedAt,
            UpdatedAt = interest.UpdatedAt
        };
    }
}