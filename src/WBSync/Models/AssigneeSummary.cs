namespace WBSync.Models;

/// <summary>ダッシュボード画面での担当者ごとの工数集計結果。</summary>
/// <param name="AssigneeId">担当者ID。</param>
/// <param name="AssigneeName">担当者名。</param>
/// <param name="PlannedWorkDays">対象タスクの予定工数合計（人日）。集計期間指定時（期間内実績のみを集計する表示）は <see langword="null"/>。</param>
/// <param name="ActualPersonDays">対象タスクの実績合計（人日）。集計期間指定時は期間内に記録された実績のみの合計。</param>
/// <param name="DelayWorkDays">遅れ（予定工数合計 − 実績合計）。プラスは前倒し、マイナスは遅れ。集計期間指定時は <see langword="null"/>。</param>
internal sealed record AssigneeSummary(
    int AssigneeId,
    string AssigneeName,
    double? PlannedWorkDays,
    double ActualPersonDays,
    double? DelayWorkDays);
