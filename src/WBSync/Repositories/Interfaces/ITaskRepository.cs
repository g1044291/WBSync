using WBSync.Models;

namespace WBSync.Repositories.Interfaces;

/// <summary>タスクリポジトリのインターフェース。</summary>
public interface ITaskRepository
{
    /// <summary>指定プロジェクトのタスクを表示順で取得する。</summary>
    /// <param name="projectId">プロジェクトID。</param>
    /// <returns>タスクのリスト。</returns>
    Task<List<WbsTask>> GetByProjectAsync(int projectId);

    /// <summary>タスクを新規作成する。</summary>
    /// <param name="task">作成するタスク。</param>
    /// <returns>DB に保存されたタスク。</returns>
    Task<WbsTask> CreateAsync(WbsTask task);

    /// <summary>タスク情報を更新する。</summary>
    /// <param name="task">更新するタスク。</param>
    /// <returns>更新後のタスク。</returns>
    Task<WbsTask> UpdateAsync(WbsTask task);

    /// <summary>タスクを削除する。子タスクも ON DELETE CASCADE で連鎖削除される。</summary>
    /// <param name="id">タスクID。</param>
    Task DeleteAsync(int id);

    /// <summary>タスクの表示順を更新する。</summary>
    /// <param name="id">タスクID。</param>
    /// <param name="sortOrder">新しい表示順。</param>
    Task UpdateSortOrderAsync(int id, int sortOrder);
}
