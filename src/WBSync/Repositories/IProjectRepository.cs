using WBSync.Models;

namespace WBSync.Repositories;

/// <summary>プロジェクトリポジトリのインターフェース。</summary>
public interface IProjectRepository
{
    /// <summary>全プロジェクトを ID 順で取得する。</summary>
    /// <returns>プロジェクトのリスト。</returns>
    Task<List<Project>> GetAllAsync();

    /// <summary>プロジェクトを新規作成する。</summary>
    /// <param name="project">作成するプロジェクト。</param>
    /// <returns>DB に保存されたプロジェクト。</returns>
    Task<Project> CreateAsync(Project project);
}
