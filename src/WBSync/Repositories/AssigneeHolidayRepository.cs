using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IAssigneeHolidayRepository"/> の EF Core 実装。</summary>
public class AssigneeHolidayRepository(AppDbContext db) : IAssigneeHolidayRepository
{
    /// <summary>指定担当者の個人休日を日付順で取得する。</summary>
    /// <param name="assigneeId">担当者ID。</param>
    /// <returns>個人休日のリスト。</returns>
    public Task<List<AssigneeHoliday>> GetByAssigneeAsync(int assigneeId)
        => db.AssigneeHolidays.Where(h => h.AssigneeId == assigneeId).OrderBy(h => h.Date).ToListAsync();

    /// <summary>個人休日を新規作成する。</summary>
    /// <param name="holiday">作成する休日。</param>
    /// <returns>DB に保存された休日。</returns>
    public async Task<AssigneeHoliday> CreateAsync(AssigneeHoliday holiday)
    {
        db.AssigneeHolidays.Add(holiday);
        try
        {
            await db.SaveChangesAsync();
            return holiday;
        }
        catch
        {
            db.Entry(holiday).State = EntityState.Detached;
            throw;
        }
    }

    /// <summary>個人休日を削除する。</summary>
    /// <param name="id">休日ID。</param>
    public async Task DeleteAsync(int id)
    {
        await db.AssigneeHolidays.Where(h => h.Id == id).ExecuteDeleteAsync();
    }
}
