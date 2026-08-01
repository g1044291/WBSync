using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IGlobalAssigneeRepository"/> の EF Core 実装。</summary>
public class GlobalAssigneeRepository(AppDbContext db) : IGlobalAssigneeRepository
{
    /// <summary>全グローバル担当者を名前順で取得する。</summary>
    /// <returns>グローバル担当者のリスト。</returns>
    public Task<List<GlobalAssignee>> GetAllAsync()
        => db.GlobalAssignees.AsNoTracking().OrderBy(g => g.Name).ToListAsync();

    /// <summary>グローバル担当者を新規作成する。</summary>
    /// <param name="globalAssignee">作成するグローバル担当者。</param>
    /// <returns>DB に保存されたグローバル担当者。</returns>
    public async Task<GlobalAssignee> CreateAsync(GlobalAssignee globalAssignee)
    {
        db.GlobalAssignees.Add(globalAssignee);
        try
        {
            await db.SaveChangesAsync();
            return globalAssignee;
        }
        catch
        {
            db.Entry(globalAssignee).State = EntityState.Detached;
            throw;
        }
    }

    /// <summary>グローバル担当者情報を更新する。</summary>
    /// <param name="globalAssignee">更新するグローバル担当者。</param>
    /// <returns>更新後のグローバル担当者。</returns>
    public async Task<GlobalAssignee> UpdateAsync(GlobalAssignee globalAssignee)
    {
        var tracked = db.ChangeTracker.Entries<GlobalAssignee>()
            .FirstOrDefault(e => e.Entity.Id == globalAssignee.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.GlobalAssignees.Update(globalAssignee);
        await db.SaveChangesAsync();
        return globalAssignee;
    }

    /// <summary>グローバル担当者を削除する。</summary>
    /// <param name="id">グローバル担当者ID。</param>
    public async Task DeleteAsync(int id)
    {
        await db.GlobalAssignees.Where(g => g.Id == id).ExecuteDeleteAsync();
    }
}
