using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IGlobalHolidayRepository"/> の EF Core 実装。</summary>
public class GlobalHolidayRepository(AppDbContext db) : IGlobalHolidayRepository
{
    /// <summary>全体休日を日付順で取得する。</summary>
    /// <returns>全体休日のリスト。</returns>
    public Task<List<GlobalHoliday>> GetAllAsync()
        => db.GlobalHolidays.OrderBy(h => h.Date).ToListAsync();

    /// <summary>全体休日を新規作成する。</summary>
    /// <param name="holiday">作成する休日。</param>
    /// <returns>DB に保存された休日。</returns>
    public async Task<GlobalHoliday> CreateAsync(GlobalHoliday holiday)
    {
        db.GlobalHolidays.Add(holiday);
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

    /// <summary>全体休日を削除する。</summary>
    /// <param name="id">休日ID。</param>
    public async Task DeleteAsync(int id)
    {
        await db.GlobalHolidays.Where(h => h.Id == id).ExecuteDeleteAsync();
    }

    /// <summary>複数の全体休日をまとめて作成する。既存データおよびリスト内の重複日付はスキップする。</summary>
    /// <param name="holidays">作成する休日のリスト。</param>
    /// <returns>実際に作成された件数。</returns>
    public async Task<int> CreateManyAsync(IEnumerable<GlobalHoliday> holidays)
    {
        var existingDates = (await db.GlobalHolidays.Select(h => h.Date).ToListAsync()).ToHashSet();
        var newHolidays = holidays.Where(h => existingDates.Add(h.Date)).ToList();
        if (newHolidays.Count == 0)
            return 0;

        db.GlobalHolidays.AddRange(newHolidays);
        await db.SaveChangesAsync();
        return newHolidays.Count;
    }
}
