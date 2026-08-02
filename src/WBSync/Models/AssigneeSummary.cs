namespace WBSync.Models;

/// <summary>ダッシュボード画面での担当者ごとの工数集計結果。</summary>
/// <param name="AssigneeName">担当者名。</param>
/// <param name="PlannedWorkDays">対象タスクの予定工数合計（人日）。</param>
/// <param name="ActualPersonDays">対象タスクの実績合計（人日）。</param>
/// <param name="DelayWorkDays">遅れ（予定工数合計 − 実績合計）。プラスは前倒し、マイナスは遅れ。</param>
internal sealed record AssigneeSummary(
    string AssigneeName,
    double PlannedWorkDays,
    double ActualPersonDays,
    double DelayWorkDays);
