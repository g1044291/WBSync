namespace WBSync.Helpers;

/// <summary>日時に関するユーティリティ。</summary>
internal static class DateTimeHelper
{
    /// <summary>現在時刻を yyyy-MM-dd HH:mm:ss 形式の文字列で返す。</summary>
    /// <returns>フォーマット済みの現在時刻文字列。</returns>
    internal static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
