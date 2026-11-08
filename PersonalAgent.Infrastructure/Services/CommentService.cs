
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Core.Entities;
using PersonalAgent.Infrastructure.Data;

namespace PersonalAgent.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly MvcDbContext _db;

    public CommentService(MvcDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<IssueComment>>
        GetByIssueIdAsync(int issueId)
    {
        return await _db.IssueComments
            .AsNoTracking()
            .Where(c => c.IssueId == issueId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<IssueComment> CreateAsync(
        int issueId,
        string authorId,
        string content)
    {
        if (string.IsNullOrWhiteSpace(authorId))
            throw new ArgumentException(
                "Forfatter må være angitt.",
                nameof(authorId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException(
                "Kommentaren kan ikke være tom.",
                nameof(content));

        content = content.Trim();

        if (content.Length > 2000)
            throw new ArgumentException(
                "Kommentaren kan ikke overstige 2000 tegn.",
                nameof(content));

        var issueExists = await _db.Issues
            .AnyAsync(i => i.Id == issueId);

        if (!issueExists)
            throw new ArgumentException(
                "Saken finnes ikke.",
                nameof(issueId));

        var authorExists = await _db.Users
            .AnyAsync(u => u.Id == authorId);

        if (!authorExists)
            throw new ArgumentException(
                "Brukeren finnes ikke.",
                nameof(authorId));

        var comment = new IssueComment
        {
            IssueId = issueId,
            AuthorId = authorId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.IssueComments.Add(comment);
        await _db.SaveChangesAsync();

        return comment;
    }
}
