using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;

namespace WBSync.Repositories;

/// <summary><see cref="ITaskRepository"/> の EF Core 実装。</summary>
public class TaskRepository(AppDbContext db) : ITaskRepository
{
    /// <inheritdoc/>
    public Task<List<WbsTask>> GetByProjectAsync(int projectId)
        => db.Tasks
             .AsNoTracking()
             .Where(t => t.ProjectId == projectId)
             .OrderBy(t => t.SortOrder)
             .ToListAsync();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
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
