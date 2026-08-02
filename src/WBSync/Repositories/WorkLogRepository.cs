using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IWorkLogRepository"/> の EF Core 実装。</summary>
public class WorkLogRepository(AppDbContext db) : IWorkLogRepository
{
    /// <summary>指定タスクの作業実績を日付順で取得する。</summary>
    /// <param name="taskId">タスクID。</param>
    /// <returns>作業実績のリスト。</returns>
    public Task<List<WorkLog>> GetByTaskAsync(int taskId)
        => db.WorkLogs.Where(w => w.TaskId == taskId).OrderBy(w => w.Date).ToListAsync();

    /// <summary>指定プロジェクトの全作業実績を取得する（タスク経由で結合）。</summary>
    /// <param name="projectId">プロジェクトID。</param>
    /// <returns>作業実績のリスト。</returns>
    public Task<List<WorkLog>> GetByProjectAsync(int projectId)
        => db.WorkLogs.AsNoTracking().Where(w => w.Task.ProjectId == projectId).ToListAsync();

    /// <summary>作業実績を新規作成する。</summary>
    /// <param name="workLog">作成する作業実績。</param>
    /// <returns>DB に保存された作業実績。</returns>
    public async Task<WorkLog> CreateAsync(WorkLog workLog)
    {
        db.WorkLogs.Add(workLog);
        try
        {
            await db.SaveChangesAsync();
            return workLog;
        }
        catch
        {
            db.Entry(workLog).State = EntityState.Detached;
            throw;
        }
    }

    /// <summary>作業実績を更新する。</summary>
    /// <param name="workLog">更新する作業実績。</param>
    /// <returns>更新後の作業実績。</returns>
    public async Task<WorkLog> UpdateAsync(WorkLog workLog)
    {
        var tracked = db.ChangeTracker.Entries<WorkLog>()
            .FirstOrDefault(e => e.Entity.Id == workLog.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.WorkLogs.Update(workLog);
        await db.SaveChangesAsync();
        return workLog;
    }

    /// <summary>作業実績を削除する。</summary>
    /// <param name="id">作業実績ID。</param>
    public async Task DeleteAsync(int id)
    {
        await db.WorkLogs.Where(w => w.Id == id).ExecuteDeleteAsync();
    }
}
