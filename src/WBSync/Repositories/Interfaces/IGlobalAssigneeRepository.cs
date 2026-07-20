using WBSync.Models;

namespace WBSync.Repositories.Interfaces;

/// <summary>グローバル担当者マスタリポジトリのインターフェース。</summary>
public interface IGlobalAssigneeRepository
{
    /// <summary>全グローバル担当者を名前順で取得する。</summary>
    /// <returns>グローバル担当者のリスト。</returns>
    Task<List<GlobalAssignee>> GetAllAsync();

    /// <summary>グローバル担当者を新規作成する。</summary>
    /// <param name="globalAssignee">作成するグローバル担当者。</param>
    /// <returns>DB に保存されたグローバル担当者。</returns>
    Task<GlobalAssignee> CreateAsync(GlobalAssignee globalAssignee);

    /// <summary>グローバル担当者情報を更新する。</summary>
    /// <param name="globalAssignee">更新するグローバル担当者。</param>
    /// <returns>更新後のグローバル担当者。</returns>
    Task<GlobalAssignee> UpdateAsync(GlobalAssignee globalAssignee);

    /// <summary>グローバル担当者を削除する。</summary>
    /// <param name="id">グローバル担当者ID。</param>
    Task DeleteAsync(int id);
}
