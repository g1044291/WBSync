using WBSync.Models;

namespace WBSync.Repositories;

public interface IAssigneeHolidayRepository
{
    Task<List<AssigneeHoliday>> GetByAssigneeAsync(int assigneeId);
    Task<AssigneeHoliday> CreateAsync(AssigneeHoliday holiday);
    Task DeleteAsync(int id);
}
