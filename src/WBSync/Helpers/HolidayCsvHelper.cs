using System.Globalization;

namespace WBSync.Helpers;

/// <summary>休日CSVのパース結果。</summary>
/// <param name="Dates">パースに成功した日付（yyyy-MM-dd 形式）のリスト。</param>
/// <param name="InvalidLineCount">日付として解釈できなかった行数。</param>
public record HolidayCsvParseResult(List<string> Dates, int InvalidLineCount);

/// <summary>全体休日のCSVインポート用パーサー。1列目の日付のみを読み取る。</summary>
public static class HolidayCsvHelper
{
    private static readonly string[] DateFormats = ["yyyy/MM/dd", "yyyy-MM-dd", "yyyy/M/d", "yyyy-M-d"];

    /// <summary>CSVテキストをパースし、日付のリストを返す。</summary>
    /// <param name="csvText">CSVファイルの内容。</param>
    /// <returns>パース結果。</returns>
    public static HolidayCsvParseResult Parse(string csvText)
    {
        var dates = new List<string>();
        var invalidLineCount = 0;

        var lines = csvText.Replace("\r\n", "\n").Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var dateText = line.Split(',')[0].Trim();
            if (DateOnly.TryParseExact(dateText, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                dates.Add(date.ToString("yyyy-MM-dd"));
            else
                invalidLineCount++;
        }

        return new HolidayCsvParseResult(dates, invalidLineCount);
    }
}
