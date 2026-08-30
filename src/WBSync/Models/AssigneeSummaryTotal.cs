namespace WBSync.Models;

/// <summary>ダッシュボード画面の担当者別集計一覧の合計行。</summary>
/// <param name="StartDate">全担当者の担当リーフタスクの開始日の最小値（<c>yyyy-MM-dd</c> 形式）。該当なしの場合は <see langword="null"/>。</param>
/// <param name="EndDate">全担当者の担当リーフタスクの終了日の最大値（<c>yyyy-MM-dd</c> 形式）。該当なしの場合は <see langword="null"/>。</param>
/// <param name="EstimateWorkDays">全担当者の見積工数合計（人日）。集計期間指定時（期間内実績のみを集計する表示）は <see langword="null"/>。</param>
/// <param name="PlannedWorkDays">全担当者の予定工数合計（人日）。集計期間指定時は <see langword="null"/>。</param>
/// <param name="ActualPersonDays">全担当者の実績合計（人日）。集計期間指定時は期間内に記録された実績のみの合計。</param>
/// <param name="RemainingWorkDays">全担当者の残工数合計（各担当者の残工数の単純合計）。集計期間指定時は <see langword="null"/>。</param>
/// <param name="DelayWorkDays">前倒し/遅れの合計（各担当者の判定条件適用後の値の単純合計）。予定工数合計 − 実績合計とは一致しない場合がある。集計期間指定時は <see langword="null"/>。</param>
internal sealed record AssigneeSummaryTotal(
    string? StartDate,
    string? EndDate,
    double? EstimateWorkDays,
    double? PlannedWorkDays,
    double ActualPersonDays,
    double? RemainingWorkDays,
    double? DelayWorkDays);
