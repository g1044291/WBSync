using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;

namespace WBSync.Repositories;

public class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public Task<List<Project>> GetAllAsync()
        => db.Projects.OrderBy(p => p.Id).ToListAsync();

    public async Task<Project> CreateAsync(Project project)
    {
        var now = Now();
        project.CreatedAt = now;
        project.UpdatedAt = now;
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
