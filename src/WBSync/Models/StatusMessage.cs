namespace WBSync.Models;

/// <summary>画面に表示するステータスメッセージ。</summary>
/// <param name="Message">表示するメッセージ。</param>
/// <param name="IsError">エラーメッセージの場合は <see langword="true"/>。</param>
internal readonly record struct StatusMessage(string Message, bool IsError)
{
    /// <summary>エラーメッセージを生成する。</summary>
    /// <param name="message">表示するメッセージ。</param>
    internal static StatusMessage Error(string message) => new(message, true);
}
