using WBSync.Models;

namespace WBSync.Repositories;

public interface ITaskRepository
{
    Task<List<WbsTask>> GetByProjectAsync(int projectId);
    Task<WbsTask> CreateAsync(WbsTask task);
    Task<WbsTask> UpdateAsync(WbsTask task);
    Task DeleteAsync(int id);
    Task UpdateSortOrderAsync(int id, int sortOrder);
}
