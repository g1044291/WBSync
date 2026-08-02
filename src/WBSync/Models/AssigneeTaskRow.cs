namespace WBSync.Models;

/// <summary>ダッシュボード画面の担当者行を展開した際のタスクツリーの1行。</summary>
/// <param name="TaskId">タスクID。</param>
/// <param name="Name">タスク名。</param>
/// <param name="Level">ツリーの階層レベル（0がルート直下）。</param>
/// <param name="HasChildren">子タスクを持つ場合は <see langword="true"/>。この場合、工数系の値はすべて算出せず表示しない。</param>
/// <param name="IsOwned">現在このタスクの担当者である場合は <see langword="true"/>。<see langword="false"/> の場合、実績のみ（この担当者が記録した分のみ）を表示する。</param>
/// <param name="PlannedWorkDays">予定工数（人日）。<see cref="IsOwned"/> が <see langword="false"/>、または <see cref="HasChildren"/> が <see langword="true"/> の場合は <see langword="null"/>。</param>
/// <param name="ActualPersonDays">実績（人日）。<see cref="IsOwned"/> が <see langword="true"/> の場合はタスクの実績合計、<see langword="false"/> の場合はこの担当者が記録した分のみ。<see cref="HasChildren"/> が <see langword="true"/> の場合は <see langword="null"/>。</param>
/// <param name="DelayWorkDays">遅れ（予定工数 − 実績）。<see cref="IsOwned"/> が <see langword="false"/>、または <see cref="HasChildren"/> が <see langword="true"/> の場合は <see langword="null"/>。</param>
internal sealed record AssigneeTaskRow(
    int TaskId,
    string Name,
    int Level,
    bool HasChildren,
    bool IsOwned,
    double? PlannedWorkDays,
    double? ActualPersonDays,
    double? DelayWorkDays);
