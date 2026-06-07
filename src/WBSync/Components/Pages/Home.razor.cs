namespace WBSync.Components.Pages;

/// <summary>開発メニュー画面のコードビハインド。</summary>
public partial class Home
{
    /// <summary>プロジェクト一覧に遷移する。</summary>
    private void GoToTop() => Nav.NavigateTo("/");

    /// <summary>UI デモ画面に遷移する。</summary>
    private void GoToUiDemo() => Nav.NavigateTo("/ui-demo");

    /// <summary>DB 動作確認画面に遷移する。</summary>
    private void GoToDbDemo() => Nav.NavigateTo("/db-demo");
}
