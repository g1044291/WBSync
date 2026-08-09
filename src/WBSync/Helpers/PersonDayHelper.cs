namespace WBSync.Helpers;

/// <summary>人日（工数）の換算・表示フォーマットに関するユーティリティ。</summary>
internal static class PersonDayHelper
{
    /// <summary>1人日あたりの分数（8時間固定）。担当者ごとの稼働時間設定は行わない。</summary>
    internal const int MinutesPerPersonDay = 480;

    /// <summary>分を人日に換算する。</summary>
    /// <param name="minutes">実績の分数。</param>
    /// <returns>人日換算値（分 ÷ <see cref="MinutesPerPersonDay"/>）。</returns>
    internal static double ToPersonDays(int minutes) => minutes / (double)MinutesPerPersonDay;

    /// <summary>工数（人日）を表示用にフォーマットする。</summary>
    /// <param name="value">工数（人日）。<see langword="null"/> の場合は "-"。</param>
    /// <returns>小数第4位までの文字列。</returns>
    internal static string FormatWorkDays(double? value) => value.HasValue ? value.Value.ToString("0.####") : "-";
}
