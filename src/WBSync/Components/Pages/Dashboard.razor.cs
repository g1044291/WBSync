using Microsoft.AspNetCore.Components;
using WBSync.Helpers;
using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>ダッシュボード画面のコードビハインド。</summary>
public partial class Dashboard
{
    /// <summary>表示するプロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    private string _projectName = string.Empty;
    private List<WbsTask> _allTasks = [];
    private List<WorkLog> _allWorkLogs = [];
    private List<Assignee> _allAssignees = [];
    private List<WorkLog> _effectiveWorkLogs = [];
    private List<AssigneeSummary> _summaries = [];
    private AssigneeSummaryTotal _total = new(0, 0, 0);
    private readonly Dictionary<int, List<AssigneeTaskRow>> _taskRowsByAssigneeId = [];
    private readonly HashSet<int> _expandedAssigneeIds = [];

    /// <summary>集計期間の開始日。<see langword="null"/> の場合は下限なし。</summary>
    private DateOnly? _periodStart;

    /// <summary>集計期間の終了日。<see langword="null"/> の場合は上限なし。</summary>
    private DateOnly? _periodEnd;

    /// <summary>集計期間が指定されている（開始日・終了日のいずれかが設定されている）場合は <see langword="true"/>。</summary>
    /// <remarks>期間指定時は「期間内に記録された実績のみ」を集計し、予定工数・遅れは表示しない。</remarks>
    private bool PeriodSpecified => _periodStart is not null || _periodEnd is not null;

    /// <summary>集計期間の指定内容を説明する文言（期間指定時のヘッダー表示用）。</summary>
    private string PeriodRangeText => (_periodStart, _periodEnd) switch
    {
        ({ } s, { } e) => $"{s:yyyy/MM/dd} 〜 {e:yyyy/MM/dd}",
        ({ } s, null) => $"{s:yyyy/MM/dd} 以降",
        (null, { } e) => $"{e:yyyy/MM/dd} まで",
        _ => "全期間"
    };

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var projects = await ProjectRepo.GetAllAsync();
        var project = projects.FirstOrDefault(p => p.Id == ProjectId);
        if (project is null) { Nav.NavigateTo("/"); return; }
        _projectName = project.Name;

        _allTasks = await TaskRepo.GetByProjectAsync(ProjectId);
        _allWorkLogs = await WorkLogRepo.GetByProjectAsync(ProjectId);
        _allAssignees = await AssigneeRepo.GetByProjectAsync(ProjectId);

        Recalculate();
    }

    /// <summary>現在の集計期間設定で担当者別集計・合計・展開済みタスクツリーを再計算する。</summary>
    /// <remarks>期間指定時は期間内の実績のみを集計し、予定工数・遅れは算出しない。</remarks>
    private void Recalculate()
    {
        _effectiveWorkLogs = AssigneeSummaryHelper.FilterByPeriod(_allWorkLogs, _periodStart, _periodEnd);
        var includePlanned = !PeriodSpecified;

        _summaries = AssigneeSummaryHelper.BuildSummaries(_allTasks, _effectiveWorkLogs, _allAssignees, includePlanned);
        _total = AssigneeSummaryHelper.BuildTotal(_summaries, includePlanned);

        _taskRowsByAssigneeId.Clear();
        foreach (var assigneeId in _expandedAssigneeIds)
            _taskRowsByAssigneeId[assigneeId] = AssigneeSummaryHelper.BuildAssigneeTaskRows(_allTasks, _effectiveWorkLogs, assigneeId, includePlanned);
    }

    /// <summary>集計期間の開始日が変更されたときに呼び出す。</summary>
    /// <param name="value">新しい開始日。クリア時は <see langword="null"/>。</param>
    private void OnPeriodStartChanged(DateOnly? value)
    {
        _periodStart = value;
        Recalculate();
    }

    /// <summary>集計期間の終了日が変更されたときに呼び出す。</summary>
    /// <param name="value">新しい終了日。クリア時は <see langword="null"/>。</param>
    private void OnPeriodEndChanged(DateOnly? value)
    {
        _periodEnd = value;
        Recalculate();
    }

    /// <summary>集計期間の指定を解除し、全期間表示に戻す。</summary>
    private void ClearPeriod()
    {
        _periodStart = null;
        _periodEnd = null;
        Recalculate();
    }

    /// <summary>担当者行のタスクツリー展開状態を切り替える。初回展開時はタスクツリーを構築する。</summary>
    /// <param name="summary">対象担当者の集計結果。</param>
    private void ToggleExpand(AssigneeSummary summary)
    {
        if (!_expandedAssigneeIds.Add(summary.AssigneeId))
        {
            _expandedAssigneeIds.Remove(summary.AssigneeId);
            _taskRowsByAssigneeId.Remove(summary.AssigneeId);
            return;
        }

        _taskRowsByAssigneeId[summary.AssigneeId] = AssigneeSummaryHelper.BuildAssigneeTaskRows(
            _allTasks, _effectiveWorkLogs, summary.AssigneeId, !PeriodSpecified);
    }

    /// <summary>工数（人日）を表示用にフォーマットする。単位「人日」を付与する。</summary>
    /// <param name="value">工数（人日）。<see langword="null"/> の場合は "-"。</param>
    /// <returns>「n人日」形式の文字列。算出不可の場合は "-"。</returns>
    private static string FormatWorkDays(double? value)
        => value.HasValue ? $"{PersonDayHelper.FormatWorkDays(value)}人日" : "-";

    /// <summary>遅れ（人日）を表示用にフォーマットする。</summary>
    /// <param name="delayWorkDays">遅れ（予定工数合計 − 実績合計）。<see langword="null"/> の場合は "-"。</param>
    /// <returns>プラスは「+n人日（前倒し）」、マイナスは「n人日（遅れ）」、0は「0人日」、算出不可は「-」。</returns>
    private static string FormatDelay(double? delayWorkDays) => delayWorkDays switch
    {
        null => "-",
        > 0 => $"+{PersonDayHelper.FormatWorkDays(delayWorkDays)}人日（前倒し）",
        < 0 => $"{PersonDayHelper.FormatWorkDays(delayWorkDays)}人日（遅れ）",
        _ => "0人日"
    };

    /// <summary>ガントチャート画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");
}
