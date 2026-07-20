namespace WBSync.Helpers;

/// <summary>稼働日判定と稼働日計算を行うユーティリティ。</summary>
internal static class WorkdayHelper
{
    /// <summary>
    /// 指定日が休日かどうかを判定する。
    /// 判定順序：土日 → 全体休日 → 担当者個人休日。
    /// </summary>
    /// <param name="date">判定する日付。</param>
    /// <param name="globalHolidays">全体休日の日付セット。</param>
    /// <param name="assigneeHolidays">担当者IDをキーとした個人休日の日付セット。</param>
    /// <param name="assigneeId">個人休日を考慮する担当者ID。<see langword="null"/> の場合は個人休日を無視する。</param>
    /// <returns>休日の場合は <see langword="true"/>。</returns>
    internal static bool IsHoliday(
        DateOnly date,
        IReadOnlySet<DateOnly> globalHolidays,
        IReadOnlyDictionary<int, IReadOnlySet<DateOnly>> assigneeHolidays,
        int? assigneeId = null)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return true;

        if (globalHolidays.Contains(date))
            return true;

        if (assigneeId.HasValue
            && assigneeHolidays.TryGetValue(assigneeId.Value, out var personal)
            && personal.Contains(date))
            return true;

        return false;
    }

    /// <summary>
    /// 指定日以降で最初の稼働日を返す。
    /// 指定日が稼働日であればその日をそのまま返す。
    /// </summary>
    /// <param name="date">起点となる日付。</param>
    /// <param name="globalHolidays">全体休日の日付セット。</param>
    /// <param name="assigneeHolidays">担当者IDをキーとした個人休日の日付セット。</param>
    /// <param name="assigneeId">個人休日を考慮する担当者ID。<see langword="null"/> の場合は個人休日を無視する。</param>
    /// <returns>最初の稼働日。</returns>
    internal static DateOnly GetNextWorkday(
        DateOnly date,
        IReadOnlySet<DateOnly> globalHolidays,
        IReadOnlyDictionary<int, IReadOnlySet<DateOnly>> assigneeHolidays,
        int? assigneeId = null)
    {
        while (IsHoliday(date, globalHolidays, assigneeHolidays, assigneeId))
            date = date.AddDays(1);
        return date;
    }

    /// <summary>
    /// 開始日から工数分の稼働日を列挙する。
    /// 開始日自体を1日目としてカウントする。
    /// 小数の工数は切り上げて日数に変換する（例：1.5人日 → 2日）。
    /// </summary>
    /// <param name="startDate">開始日。稼働日であることを前提とする。</param>
    /// <param name="workDays">工数（人日）。0 以下の場合は空列挙を返す。</param>
    /// <param name="globalHolidays">全体休日の日付セット。</param>
    /// <param name="assigneeHolidays">担当者IDをキーとした個人休日の日付セット。</param>
    /// <param name="assigneeId">個人休日を考慮する担当者ID。<see langword="null"/> の場合は個人休日を無視する。</param>
    /// <returns>稼働日の列挙。</returns>
    internal static IEnumerable<DateOnly> GetWorkdays(
        DateOnly startDate,
        double workDays,
        IReadOnlySet<DateOnly> globalHolidays,
        IReadOnlyDictionary<int, IReadOnlySet<DateOnly>> assigneeHolidays,
        int? assigneeId = null)
    {
        if (workDays <= 0) yield break;

        var days = (int)Math.Ceiling(workDays);
        var current = startDate;
        var counted = 0;

        while (counted < days)
        {
            if (!IsHoliday(current, globalHolidays, assigneeHolidays, assigneeId))
            {
                yield return current;
                counted++;
            }
            current = current.AddDays(1);
        }
    }

    /// <summary>
    /// 開始日から工数分の稼働日をカウントして終了日を算出する。
    /// 開始日自体を1日目としてカウントする。
    /// 小数の工数は切り上げて日数に変換する（例：1.5人日 → 2日）。
    /// </summary>
    /// <param name="startDate">開始日。稼働日であることを前提とする。</param>
    /// <param name="workDays">工数（人日）。0 以下の場合は開始日をそのまま返す。</param>
    /// <param name="globalHolidays">全体休日の日付セット。</param>
    /// <param name="assigneeHolidays">担当者IDをキーとした個人休日の日付セット。</param>
    /// <param name="assigneeId">個人休日を考慮する担当者ID。<see langword="null"/> の場合は個人休日を無視する。</param>
    /// <returns>終了日。</returns>
    internal static DateOnly CalcEndDate(
        DateOnly startDate,
        double workDays,
        IReadOnlySet<DateOnly> globalHolidays,
        IReadOnlyDictionary<int, IReadOnlySet<DateOnly>> assigneeHolidays,
        int? assigneeId = null)
    {
        if (workDays <= 0) return startDate;

        var days = (int)Math.Ceiling(workDays);
        var current = startDate;
        var counted = 0;

        while (true)
        {
            if (!IsHoliday(current, globalHolidays, assigneeHolidays, assigneeId))
            {
                counted++;
                if (counted == days) return current;
            }
            current = current.AddDays(1);
        }
    }
}
