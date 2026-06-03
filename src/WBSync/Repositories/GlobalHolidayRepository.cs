using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;

namespace WBSync.Repositories;

public class GlobalHolidayRepository(AppDbContext db) : IGlobalHolidayRepository
{
    public Task<List<GlobalHoliday>> GetAllAsync()
        => db.GlobalHolidays.OrderBy(h => h.Date).ToListAsync();

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

    public async Task DeleteAsync(int id)
    {
        await db.GlobalHolidays.Where(h => h.Id == id).ExecuteDeleteAsync();
    }
}
