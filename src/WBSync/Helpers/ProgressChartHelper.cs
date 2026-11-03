using WBSync.Models;

namespace WBSync.Helpers;

/// <summary>ダッシュボードの予実推移グラフ（日別実績・累積予定・累積実績・完了予測）のデータを算出するユーティリティ。</summary>
internal static class ProgressChartHelper
{
    /// <summary>
    /// プロジェクトの予実推移グラフの日別データ点を算出する。
    /// 累積予定は、予定工数と開始日・終了日が設定された各リーフタスクの予定工数を、期間内の稼働日（担当者の個人休日を考慮）に均等配分して日ごとに累積した値。
    /// 累積実績は日別実績の累積で、今日（今日より後の実績がある場合はその最終日）まで描画する。
    /// 完了予測は、累積実績線の終端を起点に、予測総工数（各リーフタスクの実績予測の合計）へ向けて、
    /// 起点の翌日〜予定の最終終了日の稼働日（全体休日のみ考慮）に残りを均等に積み上げた値。
    /// 予定の最終終了日を過ぎている場合は、起点の翌稼働日に予測総工数へ到達させる。
    /// </summary>
    /// <param name="allTasks">プロジェクト内の全タスク。</param>
    /// <param name="allWorkLogs">プロジェクト内の全実績ログ。</param>
    /// <param name="globalHolidays">全体休日の日付セット。</param>
    /// <param name="assigneeHolidays">担当者IDをキーとした個人休日の日付セット。</param>
    /// <param name="today">今日の日付。</param>
    /// <returns>
    /// 横軸の開始日〜終了日の1日ごとのデータ点。開始日はリーフタスクの最小開始日と最初の実績日の早い方、
    /// 終了日はリーフタスクの最大終了日・最後の実績日・今日・完了予測の到達日のうち最も遅い日。
    /// 期間が設定されたリーフタスクも実績も1件もない場合は空リスト。
    /// </returns>
    internal static List<ProgressChartPoint> BuildPoints(
        List<WbsTask> allTasks,
        List<WorkLog> allWorkLogs,
        IReadOnlySet<DateOnly> globalHolidays,
        IReadOnlyDictionary<int, IReadOnlySet<DateOnly>> assigneeHolidays,
        DateOnly today)
    {
        var leafTasks = TaskTreeHelper.GetAllLeafNodes(TaskTreeHelper.BuildTree(allTasks)).Select(n => n.Task).ToList();
        var noPersonalHolidays = new Dictionary<int, IReadOnlySet<DateOnly>>();

        var plannedByDate = BuildPlannedByDate(leafTasks, globalHolidays, assigneeHolidays);

        var datedLogs = allWorkLogs
            .Select(w => (Log: w, Date: DateOnly.TryParse(w.Date, out var d) ? d : (DateOnly?)null))
            .Where(x => x.Date.HasValue)
            .Select(x => (x.Log, Date: x.Date!.Value))
            .ToList();
        var dailyActualByDate = datedLogs
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => PersonDayHelper.ToPersonDays(g.Sum(x => x.Log.Minutes)));
        var actualByTaskId = datedLogs
            .GroupBy(x => x.Log.TaskId)
            .ToDictionary(g => g.Key, g => PersonDayHelper.ToPersonDays(g.Sum(x => x.Log.Minutes)));

        var taskDates = leafTasks
            .SelectMany(t => new[] { ParseDate(t.StartDate), ParseDate(t.EndDate) })
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
        if (taskDates.Count == 0 && dailyActualByDate.Count == 0) return [];

        var planEnd = taskDates.Count > 0 ? taskDates.Max() : (DateOnly?)null;
        var lastLogDate = dailyActualByDate.Count > 0 ? dailyActualByDate.Keys.Max() : (DateOnly?)null;
        var axisStart = taskDates.Concat(dailyActualByDate.Keys).Min();

        var hasActual = dailyActualByDate.Count > 0;
        var actualEnd = lastLogDate is { } last && last > today ? last : today;
        var totalActual = dailyActualByDate.Values.Sum();

        var forecastTotal = Math.Max(leafTasks.Sum(t => CalcForecastWorkDays(t, actualByTaskId.GetValueOrDefault(t.Id))), totalActual);
        var hasForecast = forecastTotal > 0;
        var forecastIncByDate = new Dictionary<DateOnly, double>();
        var forecastEnd = actualEnd;
        if (hasForecast && forecastTotal > totalActual)
        {
            var remainingWorkdays = new List<DateOnly>();
            if (planEnd is { } pe)
                for (var d = actualEnd.AddDays(1); d <= pe; d = d.AddDays(1))
                    if (!WorkdayHelper.IsHoliday(d, globalHolidays, noPersonalHolidays))
                        remainingWorkdays.Add(d);
            if (remainingWorkdays.Count == 0)
                remainingWorkdays.Add(WorkdayHelper.GetNextWorkday(actualEnd.AddDays(1), globalHolidays, noPersonalHolidays));

            var inc = (forecastTotal - totalActual) / remainingWorkdays.Count;
            foreach (var d in remainingWorkdays) forecastIncByDate[d] = inc;
            forecastEnd = remainingWorkdays[^1];
        }

        var axisEnd = new[] { planEnd, lastLogDate, hasActual || hasForecast ? actualEnd : null, hasForecast ? forecastEnd : null }
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .Max();
        if (axisEnd < axisStart) axisEnd = axisStart;

        var hasPlanned = plannedByDate.Count > 0;
        var points = new List<ProgressChartPoint>();
        double cumPlanned = 0, cumActual = 0, forecast = totalActual;
        for (var d = axisStart; d <= axisEnd; d = d.AddDays(1))
        {
            var daily = dailyActualByDate.GetValueOrDefault(d);
            cumPlanned += plannedByDate.GetValueOrDefault(d);
            cumActual += daily;
            forecast += forecastIncByDate.GetValueOrDefault(d);
            points.Add(new ProgressChartPoint(
                d,
                daily,
                hasPlanned ? cumPlanned : null,
                hasActual && d <= actualEnd ? cumActual : null,
                hasForecast && d >= actualEnd ? forecast : null));
        }

        return points;
    }

    /// <summary>
    /// 各リーフタスクの予定工数を、開始日〜終了日の稼働日（担当者の個人休日を考慮）に均等配分した日別予定工数を算出する。
    /// 予定工数が未設定または0以下、開始日・終了日のいずれかが未設定、または終了日が開始日より前のタスクは配分しない。
    /// 期間内に稼働日が1日もない場合は開始日に全量を配分する。
    /// </summary>
    /// <param name="leafTasks">リーフタスクの一覧。</param>
    /// <param name="globalHolidays">全体休日の日付セット。</param>
    /// <param name="assigneeHolidays">担当者IDをキーとした個人休日の日付セット。</param>
    /// <returns>日付をキーとした予定工数（人日）の配分値。</returns>
    private static Dictionary<DateOnly, double> BuildPlannedByDate(
        List<WbsTask> leafTasks,
        IReadOnlySet<DateOnly> globalHolidays,
        IReadOnlyDictionary<int, IReadOnlySet<DateOnly>> assigneeHolidays)
    {
        var plannedByDate = new Dictionary<DateOnly, double>();
        foreach (var task in leafTasks)
        {
            if (task.PlannedWorkDays is not > 0) continue;
            if (ParseDate(task.StartDate) is not { } start || ParseDate(task.EndDate) is not { } end || end < start) continue;

            var workdays = new List<DateOnly>();
            for (var d = start; d <= end; d = d.AddDays(1))
                if (!WorkdayHelper.IsHoliday(d, globalHolidays, assigneeHolidays, task.AssigneeId))
                    workdays.Add(d);
            if (workdays.Count == 0) workdays.Add(start);

            var perDay = task.PlannedWorkDays.Value / workdays.Count;
            foreach (var d in workdays)
                plannedByDate[d] = plannedByDate.GetValueOrDefault(d) + perDay;
        }
        return plannedByDate;
    }

    /// <summary>
    /// リーフタスク1件の実績予測（完了までに要する総工数の見込み）を算出する。
    /// 進捗率が0より大きい場合は 実績 ÷ 進捗率、0%の場合は予定工数（未設定は0扱い）と実績の大きい方とする。
    /// </summary>
    /// <param name="task">対象のリーフタスク。</param>
    /// <param name="actualPersonDays">タスクの実績合計（人日）。</param>
    /// <returns>実績予測（人日）。</returns>
    private static double CalcForecastWorkDays(WbsTask task, double actualPersonDays)
    {
        var progress = Math.Clamp(task.Progress, 0, 100);
        return progress > 0
            ? actualPersonDays * 100 / progress
            : Math.Max(task.PlannedWorkDays ?? 0, actualPersonDays);
    }

    /// <summary>yyyy-MM-dd 形式の日付文字列を解析する。</summary>
    /// <param name="date">日付文字列。</param>
    /// <returns>解析結果。未設定・解析不可の場合は <see langword="null"/>。</returns>
    private static DateOnly? ParseDate(string? date)
        => DateOnly.TryParse(date, out var d) ? d : null;
}
