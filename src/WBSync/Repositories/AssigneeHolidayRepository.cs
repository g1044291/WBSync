using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IAssigneeHolidayRepository"/> の EF Core 実装。</summary>
public class AssigneeHolidayRepository(AppDbContext db) : IAssigneeHolidayRepository
{
    /// <inheritdoc/>
    public Task<List<AssigneeHoliday>> GetByAssigneeAsync(int assigneeId)
        => db.AssigneeHolidays.Where(h => h.AssigneeId == assigneeId).OrderBy(h => h.Date).ToListAsync();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        await db.AssigneeHolidays.Where(h => h.Id == id).ExecuteDeleteAsync();
    }
}
