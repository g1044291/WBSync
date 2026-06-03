using WBSync.Models;

namespace WBSync.Repositories;

public interface IGlobalHolidayRepository
{
    Task<List<GlobalHoliday>> GetAllAsync();
    Task<GlobalHoliday> CreateAsync(GlobalHoliday holiday);
    Task DeleteAsync(int id);
}
