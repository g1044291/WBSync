using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IGlobalHolidayRepository"/> の EF Core 実装。</summary>
public class GlobalHolidayRepository(AppDbContext db) : IGlobalHolidayRepository
{
    /// <inheritdoc/>
    public Task<List<GlobalHoliday>> GetAllAsync()
        => db.GlobalHolidays.OrderBy(h => h.Date).ToListAsync();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        await db.GlobalHolidays.Where(h => h.Id == id).ExecuteDeleteAsync();
    }

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
