using Microsoft.EntityFrameworkCore;
using WBSync.Data;
using WBSync.Models;

namespace WBSync.Repositories;

public class AssigneeRepository(AppDbContext db) : IAssigneeRepository
{
    public Task<List<Assignee>> GetByProjectAsync(int projectId)
        => db.Assignees.AsNoTracking().Where(a => a.ProjectId == projectId).OrderBy(a => a.SortOrder).ToListAsync();

    public async Task<Assignee> CreateAsync(Assignee assignee)
    {
        var now = Now();
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

    public async Task<Assignee> UpdateAsync(Assignee assignee)
    {
        assignee.UpdatedAt = Now();
        var tracked = db.ChangeTracker.Entries<Assignee>()
            .FirstOrDefault(e => e.Entity.Id == assignee.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.Assignees.Update(assignee);
        await db.SaveChangesAsync();
        return assignee;
    }

    public async Task DeleteAsync(int id)
    {
        await db.Assignees.Where(a => a.Id == id).ExecuteDeleteAsync();
    }

    public async Task UpdateSortOrderAsync(int id, int sortOrder)
    {
        await db.Assignees
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.SortOrder, sortOrder)
                .SetProperty(a => a.UpdatedAt, Now()));
    }

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
