namespace WBSync.Models;

/// <summary>ダッシュボード画面の担当者行を展開した際のタスクツリーの1行。</summary>
/// <param name="TaskId">タスクID。</param>
/// <param name="Name">タスク名。</param>
/// <param name="Level">ツリーの階層レベル（0がルート直下）。</param>
/// <param name="HasChildren">子タスクを持つ場合は <see langword="true"/>。この場合、見積工数・予定工数・実績・残工数・前倒し/遅れの値はすべて算出せず表示しない。</param>
/// <param name="IsOwned">現在このタスクの担当者である場合は <see langword="true"/>。<see langword="false"/> の場合、実績のみ（この担当者が記録した分のみ）を表示する。</param>
/// <param name="StartDate">開始日（<c>yyyy-MM-dd</c> 形式）。子タスクを持つ行は子孫からの動的計算値。未設定の場合は <see langword="null"/>。</param>
/// <param name="EndDate">終了日（<c>yyyy-MM-dd</c> 形式）。子タスクを持つ行は子孫からの動的計算値。未設定の場合は <see langword="null"/>。</param>
/// <param name="Status">タスクのステータス（未着手 / 進行中 / 完了 / 保留）。子タスクを持つ行は <see langword="null"/>。</param>
/// <param name="EstimateWorkDays">見積工数（人日）。<see cref="IsOwned"/> が <see langword="false"/>、<see cref="HasChildren"/> が <see langword="true"/>、または集計期間指定時は <see langword="null"/>。</param>
/// <param name="PlannedWorkDays">予定工数（人日）。<see cref="IsOwned"/> が <see langword="false"/>、<see cref="HasChildren"/> が <see langword="true"/>、または集計期間指定時は <see langword="null"/>。</param>
/// <param name="ActualPersonDays">実績（人日）。<see cref="IsOwned"/> が <see langword="true"/> の場合はタスクの実績合計、<see langword="false"/> の場合はこの担当者が記録した分のみ。<see cref="HasChildren"/> が <see langword="true"/> の場合は <see langword="null"/>。</param>
/// <param name="RemainingWorkDays">残工数（予定工数 − 実績）。予定工数が未設定、<see cref="IsOwned"/> が <see langword="false"/>、<see cref="HasChildren"/> が <see langword="true"/>、または集計期間指定時は <see langword="null"/>。</param>
/// <param name="DelayWorkDays">
/// 前倒し/遅れ（予定工数 − 実績）。マイナス（実績が予定を超過）は遅れとして常に算出する。
/// プラス（前倒し）はタスクのステータスが「完了」の場合のみ算出し、それ以外は 0。
/// <see cref="IsOwned"/> が <see langword="false"/>、<see cref="HasChildren"/> が <see langword="true"/>、または集計期間指定時は <see langword="null"/>。
/// </param>
internal sealed record AssigneeTaskRow(
    int TaskId,
    string Name,
    int Level,
    bool HasChildren,
    bool IsOwned,
    string? StartDate,
    string? EndDate,
    string? Status,
    double? EstimateWorkDays,
    double? PlannedWorkDays,
    double? ActualPersonDays,
    double? RemainingWorkDays,
    double? DelayWorkDays);
