using PersonalAgent.Core.Entities;
using PersonalAgent.Core.Enums;

namespace PersonalAgent.Infrastructure.Services;

public interface IIssueService
{
    Task<IReadOnlyList<Issue>> GetAllAsync();

    Task<IReadOnlyList<Issue>> GetByProjectIdAsync(int projectId);

    Task<Issue?> GetByIdAsync(int id);

    Task<Issue> CreateAsync(
        int projectId,
        string title,
        string? description);

    Task<bool> UpdateAsync(
        int id,
        string title,
        string? description);

    Task<bool> DeleteAsync(int id);
    
    
    Task<bool> AssignAsync(
        int issueId,
        string? assignedUserId);

    Task<bool> ChangeStatusAsync(
        int issueId,
        IssueStatus newStatus,
        string actorUserId,
        bool isAdmin);

}