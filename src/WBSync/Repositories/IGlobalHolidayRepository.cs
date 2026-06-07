using WBSync.Models;

namespace WBSync.Repositories;

/// <summary>全体休日リポジトリのインターフェース。</summary>
public interface IGlobalHolidayRepository
{
    /// <summary>全体休日を日付順で取得する。</summary>
    /// <returns>全体休日のリスト。</returns>
    Task<List<GlobalHoliday>> GetAllAsync();

    /// <summary>全体休日を新規作成する。</summary>
    /// <param name="holiday">作成する休日。</param>
    /// <returns>DB に保存された休日。</returns>
    Task<GlobalHoliday> CreateAsync(GlobalHoliday holiday);

    /// <summary>全体休日を削除する。</summary>
    /// <param name="id">休日ID。</param>
    Task DeleteAsync(int id);
}
