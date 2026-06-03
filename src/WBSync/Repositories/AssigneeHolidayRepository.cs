using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;

namespace WBSync.Repositories;

public class AssigneeHolidayRepository(AppDbContext db) : IAssigneeHolidayRepository
{
    public Task<List<AssigneeHoliday>> GetByAssigneeAsync(int assigneeId)
        => db.AssigneeHolidays.Where(h => h.AssigneeId == assigneeId).OrderBy(h => h.Date).ToListAsync();

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

    public async Task DeleteAsync(int id)
    {
        await db.AssigneeHolidays.Where(h => h.Id == id).ExecuteDeleteAsync();
    }
}
