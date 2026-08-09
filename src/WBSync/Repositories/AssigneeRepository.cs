using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Helpers;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IAssigneeRepository"/> の EF Core 実装。</summary>
public class AssigneeRepository(AppDbContext db) : IAssigneeRepository
{
    /// <summary>指定プロジェクトの担当者を表示順で取得する。</summary>
    /// <param name="projectId">プロジェクトID。</param>
    /// <returns>担当者のリスト。</returns>
    public Task<List<Assignee>> GetByProjectAsync(int projectId)
        => db.Assignees.AsNoTracking().Where(a => a.ProjectId == projectId).OrderBy(a => a.SortOrder).ToListAsync();

    /// <summary>指定した複数プロジェクトの担当者をプロジェクトID・表示順で取得する。</summary>
    /// <param name="projectIds">対象プロジェクトIDのコレクション。</param>
    /// <returns>担当者のリスト。</returns>
    public Task<List<Assignee>> GetByProjectsAsync(IEnumerable<int> projectIds)
        => db.Assignees
             .AsNoTracking()
             .Where(a => projectIds.Contains(a.ProjectId))
             .OrderBy(a => a.ProjectId)
             .ThenBy(a => a.SortOrder)
             .ToListAsync();

    /// <summary>担当者を新規作成する。</summary>
    /// <param name="assignee">作成する担当者。</param>
    /// <returns>DB に保存された担当者。</returns>
    public async Task<Assignee> CreateAsync(Assignee assignee)
    {
        var now = DateTimeHelper.Now();
        assignee.CreatedAt = now;
        assignee.UpdatedAt = now;
        db.Assignees.Add(assignee);
        try
        {
            await db.SaveChangesAsync();
            return assignee;
        }
        catch
        {
            db.Entry(assignee).State = EntityState.Detached;
            throw;
        }
    }

    /// <summary>担当者情報を更新する。</summary>
    /// <param name="assignee">更新する担当者。</param>
    /// <returns>更新後の担当者。</returns>
    public async Task<Assignee> UpdateAsync(Assignee assignee)
    {
        assignee.UpdatedAt = DateTimeHelper.Now();
        var tracked = db.ChangeTracker.Entries<Assignee>()
            .FirstOrDefault(e => e.Entity.Id == assignee.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.Assignees.Update(assignee);
        await db.SaveChangesAsync();
        return assignee;
    }

    /// <summary>担当者を削除する。</summary>
    /// <param name="id">担当者ID。</param>
    public async Task DeleteAsync(int id)
    {
        await db.Assignees.Where(a => a.Id == id).ExecuteDeleteAsync();
    }

    /// <summary>担当者の表示順を更新する。</summary>
    /// <param name="id">担当者ID。</param>
    /// <param name="sortOrder">新しい表示順。</param>
    public async Task UpdateSortOrderAsync(int id, int sortOrder)
    {
        await db.Assignees
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.SortOrder, sortOrder)
                .SetProperty(a => a.UpdatedAt, DateTimeHelper.Now()));
    }
}
