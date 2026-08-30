using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>プロジェクト一覧画面のコードビハインド。</summary>
public partial class ProjectList
{
    private List<Project> _projects = [];

#if DEBUG
    private const bool _isDebug = true;
#else
    private const bool _isDebug = false;
#endif

    private bool _createModalOpen;
    private bool _holidayModalOpen;
    private bool _assigneeModalOpen;
    private bool _menuOpen;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        _projects = await ProjectRepo.GetAllAsync();
    }

    /// <summary>ガントチャート画面に遷移する。</summary>
    private void GoToGantt(int projectId) => Nav.NavigateTo($"/gantt/{projectId}");

    /// <summary>ハンバーガーメニューの開閉を切り替える。</summary>
    private void ToggleMenu() => _menuOpen = !_menuOpen;

    /// <summary>ハンバーガーメニューを閉じる。</summary>
    private void CloseMenu() => _menuOpen = false;

    /// <summary>メニューから横断ビュー画面へ遷移する。</summary>
    private void HandleMenuCrossProjectView()
    {
        _menuOpen = false;
        GoToCrossProjectView();
    }

    /// <summary>メニューから休日設定モーダルを開く。</summary>
    private void HandleMenuHoliday()
    {
        _menuOpen = false;
        OpenHolidayModal();
    }

    /// <summary>メニューから担当者設定モーダルを開く。</summary>
    private void HandleMenuAssignee()
    {
        _menuOpen = false;
        OpenAssigneeModal();
    }

    /// <summary>複数プロジェクトを横断して表示するWBS画面に遷移する。</summary>
    private void GoToCrossProjectView() => Nav.NavigateTo("/cross-project");

    /// <summary>プロジェクト作成モーダルを開く。</summary>
    private void OpenCreateModal() => _createModalOpen = true;

    /// <summary>プロジェクト作成モーダルを閉じる。</summary>
    private void CloseCreateModal() => _createModalOpen = false;

    /// <summary>プロジェクト作成完了時にリストを更新する。</summary>
    private async Task HandleProjectCreated(Project _)
    {
        _createModalOpen = false;
        _projects = await ProjectRepo.GetAllAsync();
    }

    /// <summary>休日設定モーダルを開く。</summary>
    private void OpenHolidayModal() => _holidayModalOpen = true;

    /// <summary>休日設定モーダルを閉じる。</summary>
    private void CloseHolidayModal() => _holidayModalOpen = false;

    /// <summary>担当者設定モーダルを開く。</summary>
    private void OpenAssigneeModal() => _assigneeModalOpen = true;

    /// <summary>担当者設定モーダルを閉じる。</summary>
    private void CloseAssigneeModal() => _assigneeModalOpen = false;

    /// <summary>開発メニュー画面に遷移する。</summary>
    private void GoToDev() => Nav.NavigateTo("/dev");
}
