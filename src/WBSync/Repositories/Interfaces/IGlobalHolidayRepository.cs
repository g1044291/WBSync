using WBSync.Models;

namespace WBSync.Repositories.Interfaces;

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

    /// <summary>複数の全体休日をまとめて作成する。既存データおよびリスト内の重複日付はスキップする。</summary>
    /// <param name="holidays">作成する休日のリスト。</param>
    /// <returns>実際に作成された件数。</returns>
    Task<int> CreateManyAsync(IEnumerable<GlobalHoliday> holidays);

    /// <summary>全体休日を削除する。</summary>
    /// <param name="id">休日ID。</param>
    Task DeleteAsync(int id);
}
