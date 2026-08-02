using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="ITaskRepository"/> の EF Core 実装。</summary>
public class TaskRepository(AppDbContext db) : ITaskRepository
{
    /// <summary>指定プロジェクトのタスクを表示順で取得する。</summary>
    /// <param name="projectId">プロジェクトID。</param>
    /// <returns>タスクのリスト。</returns>
    public Task<List<WbsTask>> GetByProjectAsync(int projectId)
        => db.Tasks
             .AsNoTracking()
             .Where(t => t.ProjectId == projectId)
             .OrderBy(t => t.SortOrder)
             .ToListAsync();

    /// <summary>指定した複数プロジェクトのタスクをプロジェクトID・表示順で取得する。</summary>
    /// <param name="projectIds">対象プロジェクトIDのコレクション。</param>
    /// <returns>タスクのリスト。</returns>
    public Task<List<WbsTask>> GetByProjectsAsync(IEnumerable<int> projectIds)
        => db.Tasks
             .AsNoTracking()
             .Where(t => projectIds.Contains(t.ProjectId))
             .OrderBy(t => t.ProjectId)
             .ThenBy(t => t.SortOrder)
             .ToListAsync();

    /// <summary>タスクを新規作成する。</summary>
    /// <param name="task">作成するタスク。</param>
    /// <returns>DB に保存されたタスク。</returns>
    public async Task<WbsTask> CreateAsync(WbsTask task)
    {
        var now = Now();
        task.CreatedAt = now;
        task.UpdatedAt = now;
        db.Tasks.Add(task);
        try
        {
            await db.SaveChangesAsync();
            return task;
        }
        catch
        {
            db.Entry(task).State = EntityState.Detached;
            throw;
        }
    }

    /// <summary>タスク情報を更新する。</summary>
    /// <param name="task">更新するタスク。</param>
    /// <returns>更新後のタスク。</returns>
    public async Task<WbsTask> UpdateAsync(WbsTask task)
    {
        task.UpdatedAt = Now();
        var tracked = db.ChangeTracker.Entries<WbsTask>()
            .FirstOrDefault(e => e.Entity.Id == task.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.Tasks.Update(task);
        await db.SaveChangesAsync();
        return task;
    }

    /// <summary>タスクを削除する。子タスクも ON DELETE CASCADE で連鎖削除される。</summary>
    /// <param name="id">タスクID。</param>
    public async Task DeleteAsync(int id)
    {
        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync();
    }

    /// <summary>タスクの表示順を更新する。</summary>
    /// <param name="id">タスクID。</param>
    /// <param name="sortOrder">新しい表示順。</param>
    public async Task UpdateSortOrderAsync(int id, int sortOrder)
    {
        await db.Tasks
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.SortOrder, sortOrder)
                .SetProperty(t => t.UpdatedAt, Now()));
    }

    /// <summary>現在時刻を yyyy-MM-dd HH:mm:ss 形式の文字列で返す。</summary>
    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
