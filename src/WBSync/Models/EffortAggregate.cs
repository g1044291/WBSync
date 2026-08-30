namespace WBSync.Models;

/// <summary>
/// 工数管理画面での1タスク分の集計結果。リーフタスクは自身の値、親タスクは全子孫タスクからの動的集計値を持つ。
/// ただし親タスク自身に見積工数・予定工数が設定されている場合は、その値を集計値より優先する。
/// </summary>
/// <param name="EstimateWorkDays">見積工数（人日）。親タスクは自身に値があればその値、なければ子孫タスクの合計。</param>
/// <param name="PlannedWorkDays">予定工数（人日）。親タスクは自身に値があればその値、なければ子孫タスクの合計。</param>
/// <param name="ActualPersonDays">実績（人日）。親タスクは子孫タスクの合計。</param>
/// <param name="RemainingWorkDays">残工数（予定工数－実績。予定工数は表示値＝自身の値 or 集計値）。リーフタスクで予定工数が未設定の場合のみ <see langword="null"/>。</param>
/// <param name="DelayWorkDays">
/// 前倒し/遅れ工数（人日、実績－予定工数）。プラス＝遅れ（実績が予定工数を超過）、マイナス＝前倒し。
/// 前倒し（マイナス）はステータスが「完了」のリーフタスクでのみ算出し、それ以外は 0 とする。
/// リーフタスクで予定工数が未設定の場合は算出不可で <see langword="null"/>。親タスクは子孫タスクの値のうち最大値。
/// </param>
internal sealed record EffortAggregate(
    double EstimateWorkDays,
    double PlannedWorkDays,
    double ActualPersonDays,
    double? RemainingWorkDays,
    double? DelayWorkDays);
