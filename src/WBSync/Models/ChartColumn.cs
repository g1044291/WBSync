namespace WBSync.Models;

/// <summary>チャートの列定義。</summary>
/// <param name="Label">表示ラベル。</param>
/// <param name="Date">対象日付。</param>
/// <param name="IsWeekend">土曜・日曜かどうか。</param>
/// <param name="IsHoliday">祝日かどうか。</param>
internal record ChartColumn(string Label, DateOnly Date, bool IsWeekend, bool IsHoliday = false);
