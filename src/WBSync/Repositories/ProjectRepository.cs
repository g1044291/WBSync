using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;

namespace WBSync.Repositories;

/// <summary><see cref="IProjectRepository"/> の EF Core 実装。</summary>
public class ProjectRepository(AppDbContext db) : IProjectRepository
{
    /// <inheritdoc/>
    public Task<List<Project>> GetAllAsync()
        => db.Projects.OrderBy(p => p.Id).ToListAsync();

    /// <inheritdoc/>
    public async Task<Project> CreateAsync(Project project)
    {
        var now = Now();
        project.CreatedAt = now;
        project.UpdatedAt = now;
        db.Projects.Add(project);
        try
        {
            await db.SaveChangesAsync();
            return project;
        }
        catch
        {
            db.Entry(project).State = EntityState.Detached;
            throw;
        }
    }

    /// <summary>現在時刻を yyyy-MM-dd HH:mm:ss 形式の文字列で返す。</summary>
    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
