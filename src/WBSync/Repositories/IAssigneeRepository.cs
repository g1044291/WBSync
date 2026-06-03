using WBSync.Models;

namespace WBSync.Repositories;

public interface IAssigneeRepository
{
    Task<List<Assignee>> GetByProjectAsync(int projectId);
    Task<Assignee> CreateAsync(Assignee assignee);
    Task<Assignee> UpdateAsync(Assignee assignee);
    Task DeleteAsync(int id);
    Task UpdateSortOrderAsync(int id, int sortOrder);
}
