using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IProjectRepository"/> の EF Core 実装。</summary>
public class ProjectRepository(AppDbContext db) : IProjectRepository
{
    /// <summary>全プロジェクトを ID 順で取得する。</summary>
    /// <returns>プロジェクトのリスト。</returns>
    public Task<List<Project>> GetAllAsync()
        => db.Projects.OrderBy(p => p.Id).ToListAsync();

    /// <summary>プロジェクトを新規作成する。</summary>
    /// <param name="project">作成するプロジェクト。</param>
    /// <returns>DB に保存されたプロジェクト。</returns>
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
