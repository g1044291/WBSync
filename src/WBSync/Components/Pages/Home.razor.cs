namespace WBSync.Components.Pages;

public partial class Home
{
    private void GoToTop() => Nav.NavigateTo("/");
    private void GoToUiDemo() => Nav.NavigateTo("/ui-demo");
    private void GoToDbDemo() => Nav.NavigateTo("/db-demo");
}
