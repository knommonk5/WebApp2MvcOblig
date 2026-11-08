
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Core.Entities;
using PersonalAgent.Infrastructure.Data;

namespace PersonalAgent.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly MvcDbContext _db;

    public ProjectService(MvcDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync()
    {
        return await _db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        return await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> CreateAsync(
        string name,
        string? description)
    {
        Validate(name, description);

        var project = new Project
        {
            Name = name.Trim(),
            Description = description?.Trim()
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return project;
    }

    public async Task<bool> UpdateAsync(
        int id,
        string name,
        string? description)
    {
        Validate(name, description);

        var project = await _db.Projects.FindAsync(id);

        if (project is null)
            return false;

        project.Name = name.Trim();
        project.Description = description?.Trim();

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);

        if (project is null)
            return false;

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();

        return true;
    }

    private static void Validate(
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Prosjektnavn er påkrevd.",
                nameof(name));

        if (name.Trim().Length > 100)
            throw new ArgumentException(
                "Prosjektnavn kan ikke overstige 100 tegn.",
                nameof(name));

        if (description?.Trim().Length > 1000)
            throw new ArgumentException(
                "Beskrivelse kan ikke overstige 1000 tegn.",
                nameof(description));
    }
}
