
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Core.Entities;
using PersonalAgent.Infrastructure.Data;
using PersonalAgent.Core.Enums;

namespace PersonalAgent.Infrastructure.Services;

public class IssueService : IIssueService
{
    private readonly MvcDbContext _db;

    public IssueService(MvcDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Issue>> GetAllAsync()
    {
        return await _db.Issues
            .AsNoTracking()
            .Include(i => i.Project)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Issue>> GetByProjectIdAsync(int projectId)
    {
        return await _db.Issues
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Issue?> GetByIdAsync(int id)
    {
        return await _db.Issues
            .AsNoTracking()
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Issue> CreateAsync(
        int projectId,
        string title,
        string? description)
    {
        Validate(title, description);

        var projectExists = await _db.Projects
            .AnyAsync(p => p.Id == projectId);

        if (!projectExists)
            throw new ArgumentException(
                "Prosjektet finnes ikke.",
                nameof(projectId));

        var issue = new Issue
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description?.Trim()
        };

        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        return issue;
    }

    public async Task<bool> UpdateAsync(
        int id,
        string title,
        string? description)
    {
        Validate(title, description);

        var issue = await _db.Issues.FindAsync(id);

        if (issue is null)
            return false;

        issue.Title = title.Trim();
        issue.Description = description?.Trim();

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var issue = await _db.Issues.FindAsync(id);

        if (issue is null)
            return false;

        _db.Issues.Remove(issue);
        await _db.SaveChangesAsync();

        return true;
    }
    
    
    
    public async Task<bool> AssignAsync(
        int issueId,
        string? assignedUserId)
    {
        var issue = await _db.Issues.FindAsync(issueId);

        if (issue is null)
            return false;

        var normalizedUserId =
            string.IsNullOrWhiteSpace(assignedUserId)
                ? null
                : assignedUserId.Trim();

        if (normalizedUserId is not null)
        {
            var userExists = await _db.Users
                .AnyAsync(u => u.Id == normalizedUserId);

            if (!userExists)
                throw new ArgumentException(
                    "Brukeren finnes ikke.",
                    nameof(assignedUserId));
        }

        // Ingen endring dersom samme bruker allerede er tildelt.
        if (issue.AssignedUserId == normalizedUserId)
            return true;

        issue.AssignedUserId = normalizedUserId;

        // Ny tildeling eller bytte av ansvarlig nullstiller fremdriften.
        issue.Status = normalizedUserId is null
            ? IssueStatus.Open
            : IssueStatus.Assigned;

        await _db.SaveChangesAsync();

        return true;
    }


   
    public async Task<bool> ChangeStatusAsync(
        int issueId,
        IssueStatus newStatus,
        string actorUserId,
        bool isAdmin)
    {
        if (!Enum.IsDefined(newStatus))
            throw new ArgumentOutOfRangeException(nameof(newStatus));

        if (string.IsNullOrWhiteSpace(actorUserId))
            throw new UnauthorizedAccessException(
                "Brukeren er ikke identifisert.");

        var issue = await _db.Issues.FindAsync(issueId);

        if (issue is null)
            return false;

        if (!isAdmin)
        {
            if (issue.AssignedUserId != actorUserId)
                throw new UnauthorizedAccessException(
                    "Du er ikke tildelt denne saken.");

            if (issue.Status == IssueStatus.Closed)
                throw new InvalidOperationException(
                    "Kun administrator kan gjenåpne en lukket sak.");

            var allowed =
                (issue.Status == IssueStatus.Assigned &&
                 newStatus == IssueStatus.InProgress)
                ||
                (issue.Status == IssueStatus.InProgress &&
                 newStatus == IssueStatus.Closed);

            if (!allowed)
                throw new InvalidOperationException(
                    "Denne statusovergangen er ikke tillatt.");
        }

        // Admin kan overstyre status.
        issue.Status = newStatus;

        await _db.SaveChangesAsync();

        return true;
    }

    private static void Validate(
        string title,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "Sakstittel er påkrevd.",
                nameof(title));

        if (title.Trim().Length > 150)
            throw new ArgumentException(
                "Sakstittel kan ikke overstige 150 tegn.",
                nameof(title));

        if (description?.Trim().Length > 3000)
            throw new ArgumentException(
                "Beskrivelse kan ikke overstige 3000 tegn.",
                nameof(description));
    }
}
