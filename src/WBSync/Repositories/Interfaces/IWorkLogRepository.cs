using WBSync.Models;

namespace WBSync.Repositories.Interfaces;

/// <summary>作業実績リポジトリのインターフェース。</summary>
public interface IWorkLogRepository
{
    /// <summary>指定タスクの作業実績を日付順で取得する。</summary>
    /// <param name="taskId">タスクID。</param>
    /// <returns>作業実績のリスト。</returns>
    Task<List<WorkLog>> GetByTaskAsync(int taskId);

    /// <summary>作業実績を新規作成する。</summary>
    /// <param name="workLog">作成する作業実績。</param>
    /// <returns>DB に保存された作業実績。</returns>
    Task<WorkLog> CreateAsync(WorkLog workLog);

    /// <summary>作業実績を更新する。</summary>
    /// <param name="workLog">更新する作業実績。</param>
    /// <returns>更新後の作業実績。</returns>
    Task<WorkLog> UpdateAsync(WorkLog workLog);

    /// <summary>作業実績を削除する。</summary>
    /// <param name="id">作業実績ID。</param>
    Task DeleteAsync(int id);
}
