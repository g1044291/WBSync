using WBSync.Models;

namespace WBSync.Components.Pages;

public partial class ProjectList
{
    private List<Project> _projects = [];

    protected override async Task OnInitializedAsync()
    {
        _projects = await ProjectRepo.GetAllAsync();
    }

#if DEBUG
    private const bool _isDebug = true;
#else
    private const bool _isDebug = false;
#endif

    private bool _createModalOpen;
    private bool _holidayModalOpen;

    private void GoToGantt(int projectId) => Nav.NavigateTo($"/gantt/{projectId}");
    private void OpenCreateModal() => _createModalOpen = true;
    private void CloseCreateModal() => _createModalOpen = false;
    private void OpenHolidayModal() => _holidayModalOpen = true;
    private void CloseHolidayModal() => _holidayModalOpen = false;
    private async Task HandleProjectCreated(Project _)
    {
        _createModalOpen = false;
        _projects = await ProjectRepo.GetAllAsync();
    }
    private void GoToDev() => Nav.NavigateTo("/dev");
}
