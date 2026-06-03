using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;

namespace WBSync.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<List<WbsTask>> GetByProjectAsync(int projectId)
        => db.Tasks
             .Where(t => t.ProjectId == projectId)
             .OrderBy(t => t.SortOrder)
             .ToListAsync();

    public async Task<WbsTask> CreateAsync(WbsTask task)
    {
        var now = Now();
        task.CreatedAt = now;
        task.UpdatedAt = now;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<WbsTask> UpdateAsync(WbsTask task)
    {
        task.UpdatedAt = Now();
        db.Tasks.Update(task);
        await db.SaveChangesAsync();
        return task;
    }

    public async Task DeleteAsync(int id)
    {
        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync();
    }

    public async Task UpdateSortOrderAsync(int id, int sortOrder)
    {
        await db.Tasks
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.SortOrder, sortOrder)
                .SetProperty(t => t.UpdatedAt, Now()));
    }

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
