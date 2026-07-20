using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Repositories;

/// <summary><see cref="IGlobalAssigneeRepository"/> の EF Core 実装。</summary>
public class GlobalAssigneeRepository(AppDbContext db) : IGlobalAssigneeRepository
{
    /// <inheritdoc/>
    public Task<List<GlobalAssignee>> GetAllAsync()
        => db.GlobalAssignees.AsNoTracking().OrderBy(g => g.Name).ToListAsync();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        await db.GlobalAssignees.Where(g => g.Id == id).ExecuteDeleteAsync();
    }
}
