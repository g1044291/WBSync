namespace WBSync.Models;

/// <summary>ダッシュボード画面の担当者別集計一覧の合計行。</summary>
/// <param name="PlannedWorkDays">全担当者の予定工数合計（人日）。</param>
/// <param name="ActualPersonDays">全担当者の実績合計（人日）。</param>
/// <param name="DelayWorkDays">遅れ（予定工数合計 − 実績合計）。プラスは前倒し、マイナスは遅れ。</param>
internal sealed record AssigneeSummaryTotal(
    double PlannedWorkDays,
    double ActualPersonDays,
    double DelayWorkDays);
