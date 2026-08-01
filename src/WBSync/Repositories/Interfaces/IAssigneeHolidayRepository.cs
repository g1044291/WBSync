using WBSync.Models;

namespace WBSync.Repositories.Interfaces;

/// <summary>担当者個人休日リポジトリのインターフェース。</summary>
public interface IAssigneeHolidayRepository
{
    /// <summary>指定担当者の個人休日を日付順で取得する。</summary>
    /// <param name="assigneeId">担当者ID。</param>
    /// <returns>個人休日のリスト。</returns>
    Task<List<AssigneeHoliday>> GetByAssigneeAsync(int assigneeId);

    /// <summary>個人休日を新規作成する。</summary>
    /// <param name="holiday">作成する休日。</param>
    /// <returns>DB に保存された休日。</returns>
    Task<AssigneeHoliday> CreateAsync(AssigneeHoliday holiday);

    /// <summary>個人休日を削除する。</summary>
    /// <param name="id">休日ID。</param>
    Task DeleteAsync(int id);
}
