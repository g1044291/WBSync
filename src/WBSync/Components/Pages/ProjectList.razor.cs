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

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        _projects = await ProjectRepo.GetAllAsync();
    }

    /// <summary>ガントチャート画面に遷移する。</summary>
    /// <param name="projectId">対象プロジェクトID。</param>
    private void GoToGantt(int projectId) => Nav.NavigateTo($"/gantt/{projectId}");

    /// <summary>プロジェクト作成モーダルを開く。</summary>
    private void OpenCreateModal() => _createModalOpen = true;

    /// <summary>プロジェクト作成モーダルを閉じる。</summary>
    private void CloseCreateModal() => _createModalOpen = false;

    /// <summary>休日設定モーダルを開く。</summary>
    private void OpenHolidayModal() => _holidayModalOpen = true;

    /// <summary>休日設定モーダルを閉じる。</summary>
    private void CloseHolidayModal() => _holidayModalOpen = false;

    /// <summary>プロジェクト作成完了時にリストを更新する。</summary>
    /// <param name="_">作成されたプロジェクト（未使用）。</param>
    private async Task HandleProjectCreated(Project _)
    {
        _createModalOpen = false;
        _projects = await ProjectRepo.GetAllAsync();
    }

    /// <summary>開発メニュー画面に遷移する。</summary>
    private void GoToDev() => Nav.NavigateTo("/dev");
}
