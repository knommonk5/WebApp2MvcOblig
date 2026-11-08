using PersonalAgent.Core.Entities;

namespace PersonalAgent.Infrastructure.Services;

public interface ICommentService
{
    Task<IReadOnlyList<IssueComment>> GetByIssueIdAsync(
        int issueId);

    Task<IssueComment> CreateAsync(
        int issueId,
        string authorId,
        string content);
}