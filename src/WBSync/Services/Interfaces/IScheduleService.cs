using WBSync.Models;

namespace WBSync.Services.Interfaces;

/// <summary>スケジュール計算サービスのインターフェース。</summary>
public interface IScheduleService
{
    /// <summary>
    /// 先行タスクの終了日から開始日・終了日を計算する。
    /// 開始日 = 先行タスク終了日の翌稼働日（当該タスクの担当者の休日で判定）。
    /// 終了日 = 工数が設定されている場合のみ算出。
    /// </summary>
    /// <param name="predecessorEndDate">先行タスクの終了日（yyyy-MM-dd 形式）。</param>
    /// <param name="workDays">当該タスクの工数（人日）。<see langword="null"/> の場合は終了日を計算しない。</param>
    /// <param name="assigneeId">当該タスクの担当者ID。<see langword="null"/> の場合は個人休日を考慮しない。</param>
    /// <returns>計算された開始日と終了日（工数なしの場合は終了日が <see langword="null"/>）。</returns>
    Task<(DateOnly StartDate, DateOnly? EndDate)> CalcDatesFromPredecessorAsync(
        string predecessorEndDate,
        double? workDays,
        int? assigneeId);

    /// <summary>
    /// 開始日と工数から終了日を計算する。
    /// </summary>
    /// <param name="startDate">開始日。</param>
    /// <param name="workDays">工数（人日）。</param>
    /// <param name="assigneeId">担当者ID。<see langword="null"/> の場合は個人休日を考慮しない。</param>
    /// <returns>計算された終了日。</returns>
    Task<DateOnly> CalcEndDateAsync(DateOnly startDate, double workDays, int? assigneeId);

    /// <summary>
    /// 保存されたタスクの後続タスクに開始日・終了日の変更を連鎖伝播させ DB を更新する。
    /// 後続タスクの開始日 = 前タスク終了日の翌稼働日。終了日は後続タスクの既存の期間（開始日〜終了日の日数）を維持したままずらす
    /// （工数からの再計算は行わない）。既存の開始日・終了日が未設定の場合のみ、工数から終了日を算出する。
    /// 保存前後で開始日・終了日が変わっていない場合は、伝播処理自体を行わない。
    /// </summary>
    /// <param name="projectId">対象プロジェクトID。</param>
    /// <param name="savedTask">保存されたタスク（最新の EndDate を持つこと）。</param>
    /// <param name="oldStartDate">保存前の開始日（yyyy-MM-dd 形式）。新規作成の場合は <see langword="null"/>。</param>
    /// <param name="oldEndDate">保存前の終了日（yyyy-MM-dd 形式）。新規作成の場合は <see langword="null"/>。</param>
    Task PropagateSuccessorsAsync(int projectId, WbsTask savedTask, string? oldStartDate, string? oldEndDate);

    /// <summary>
    /// 同一担当者で稼働日が1日以上重複するタスクの ID セットを返す。
    /// 担当者未設定・開始日未設定・工数未設定のタスクは対象外。
    /// </summary>
    /// <param name="projectId">対象プロジェクトID。</param>
    /// <returns>重複警告対象タスクIDのセット。</returns>
    Task<HashSet<int>> GetOverlappingTaskIdsAsync(int projectId);
}
