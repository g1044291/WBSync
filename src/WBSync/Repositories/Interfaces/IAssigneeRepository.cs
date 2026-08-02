using WBSync.Models;

namespace WBSync.Repositories.Interfaces;

/// <summary>担当者リポジトリのインターフェース。</summary>
public interface IAssigneeRepository
{
    /// <summary>指定プロジェクトの担当者を表示順で取得する。</summary>
    /// <param name="projectId">プロジェクトID。</param>
    /// <returns>担当者のリスト。</returns>
    Task<List<Assignee>> GetByProjectAsync(int projectId);

    /// <summary>指定した複数プロジェクトの担当者をプロジェクトID・表示順で取得する。</summary>
    /// <param name="projectIds">対象プロジェクトIDのコレクション。</param>
    /// <returns>担当者のリスト。</returns>
    Task<List<Assignee>> GetByProjectsAsync(IEnumerable<int> projectIds);

    /// <summary>担当者を新規作成する。</summary>
    /// <param name="assignee">作成する担当者。</param>
    /// <returns>DB に保存された担当者。</returns>
    Task<Assignee> CreateAsync(Assignee assignee);

    /// <summary>担当者情報を更新する。</summary>
    /// <param name="assignee">更新する担当者。</param>
    /// <returns>更新後の担当者。</returns>
    Task<Assignee> UpdateAsync(Assignee assignee);

    /// <summary>担当者を削除する。</summary>
    /// <param name="id">担当者ID。</param>
    Task DeleteAsync(int id);

    /// <summary>担当者の表示順を更新する。</summary>
    /// <param name="id">担当者ID。</param>
    /// <param name="sortOrder">新しい表示順。</param>
    Task UpdateSortOrderAsync(int id, int sortOrder);
}
