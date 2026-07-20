using WBSync.Models;

namespace WBSync.Helpers;

/// <summary>ガントチャートの列ビルドとピクセルオフセット計算ユーティリティ。</summary>
internal static class GanttChartLayoutHelper
{
    /// <summary>指定スケールに応じてチャート列一覧を構築する。</summary>
    /// <param name="scale">チャートのスケール。</param>
    /// <param name="chartStart">チャート表示開始日。</param>
    /// <param name="chartEnd">チャート表示終了日。</param>
    /// <param name="globalHolidays">祝日セット。</param>
    internal static List<ChartColumn> BuildColumns(
        ChartScale scale, DateOnly chartStart, DateOnly chartEnd,
        HashSet<DateOnly> globalHolidays) => scale switch
    {
        ChartScale.Day => BuildDayColumns(chartStart, chartEnd, globalHolidays),
        ChartScale.Week => BuildWeekColumns(chartStart, chartEnd),
        ChartScale.Month => BuildMonthColumns(chartStart, chartEnd),
        _ => []
    };

    /// <summary>日単位のチャート列一覧を構築する。</summary>
    /// <param name="chartStart">チャート表示開始日。</param>
    /// <param name="chartEnd">チャート表示終了日。</param>
    /// <param name="globalHolidays">祝日セット。</param>
    private static List<ChartColumn> BuildDayColumns(
        DateOnly chartStart, DateOnly chartEnd, HashSet<DateOnly> globalHolidays)
    {
        var cols = new List<ChartColumn>();
        for (var d = chartStart; d <= chartEnd; d = d.AddDays(1))
        {
            var isWeekend = d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
            var isHoliday = globalHolidays.Contains(d);
            cols.Add(new ChartColumn(d.ToString("M/d"), d, isWeekend, isHoliday));
        }
        return cols;
    }

    /// <summary>週単位のチャート列一覧を構築する。</summary>
    /// <param name="chartStart">チャート表示開始日。</param>
    /// <param name="chartEnd">チャート表示終了日。</param>
    private static List<ChartColumn> BuildWeekColumns(DateOnly chartStart, DateOnly chartEnd)
    {
        var cols = new List<ChartColumn>();
        var weekStart = chartStart;
        while (weekStart.DayOfWeek != DayOfWeek.Monday)
            weekStart = weekStart.AddDays(-1);

        for (var d = weekStart; d <= chartEnd; d = d.AddDays(7))
        {
            var weekEnd = d.AddDays(6);
            cols.Add(new ChartColumn($"{d:M/d}〜{weekEnd:M/d}", d, false));
        }
        return cols;
    }

    /// <summary>月単位のチャート列一覧を構築する。</summary>
    /// <param name="chartStart">チャート表示開始日。</param>
    /// <param name="chartEnd">チャート表示終了日。</param>
    private static List<ChartColumn> BuildMonthColumns(DateOnly chartStart, DateOnly chartEnd)
    {
        var cols = new List<ChartColumn>();
        var monthStart = new DateOnly(chartStart.Year, chartStart.Month, 1);
        var chartEndMonth = new DateOnly(chartEnd.Year, chartEnd.Month, 1);

        for (var d = monthStart; d <= chartEndMonth; d = d.AddMonths(1))
            cols.Add(new ChartColumn($"{d:M月}", d, false));

        return cols;
    }

    /// <summary>指定日のチャート左端からのピクセルオフセットを返す。</summary>
    /// <param name="scale">チャートのスケール。</param>
    /// <param name="chartStart">チャート表示開始日。</param>
    /// <param name="date">対象日付。</param>
    internal static double GetPixelOffset(ChartScale scale, DateOnly chartStart, DateOnly date) => scale switch
    {
        ChartScale.Day => (date.DayNumber - chartStart.DayNumber) * 32.0,
        ChartScale.Week => (date.DayNumber - GetWeekStart(chartStart).DayNumber) * (80.0 / 7.0),
        ChartScale.Month => GetMonthPixelOffset(chartStart, date),
        _ => 0
    };

    /// <summary>月表示スケールにおける、指定日のチャート左端からのピクセルオフセットを返す。</summary>
    /// <param name="chartStart">チャート表示開始日。</param>
    /// <param name="date">対象日付。</param>
    private static double GetMonthPixelOffset(DateOnly chartStart, DateOnly date)
    {
        var origin = new DateOnly(chartStart.Year, chartStart.Month, 1);
        var cur = origin;
        var px = 0.0;
        while (new DateOnly(date.Year, date.Month, 1) > cur)
        {
            px += 90.0;
            cur = cur.AddMonths(1);
        }
        px += (date.Day - 1) * (90.0 / DateTime.DaysInMonth(date.Year, date.Month));
        return px;
    }

    /// <summary>指定日を含む週の月曜日を返す。</summary>
    /// <param name="date">起点の日付。</param>
    internal static DateOnly GetWeekStart(DateOnly date)
    {
        var d = date;
        while (d.DayOfWeek != DayOfWeek.Monday)
            d = d.AddDays(-1);
        return d;
    }
}
