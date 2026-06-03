using WBSync.Models;

namespace WBSync.Repositories;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync();
    Task<Project> CreateAsync(Project project);
}
