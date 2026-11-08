using PersonalAgent.Core.Entities;

namespace PersonalAgent.Infrastructure.Services;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetAllAsync();

    Task<Project?> GetByIdAsync(int id);

    Task<Project> CreateAsync(
        string name,
        string? description);

    Task<bool> UpdateAsync(
        int id,
        string name,
        string? description);

    Task<bool> DeleteAsync(int id);
}