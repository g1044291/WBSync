using Microsoft.AspNetCore.Components;
using WBSync.Models;

namespace WBSync.Components.Pages;

public partial class AssigneeList
{
    [Parameter] public int ProjectId { get; set; }

    private List<Assignee> _assignees = [];
    private bool _addModalOpen;
    private string _newName = string.Empty;
    private bool _saving;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        _assignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
    }

    private void OpenAddModal()
    {
        _newName = string.Empty;
        _error = null;
        _addModalOpen = true;
    }

    private void CloseAddModal()
    {
        _addModalOpen = false;
        _error = null;
    }

    private async Task AddAssignee()
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(_newName))
        {
            _error = "担当者名を入力してください";
            return;
        }

        _saving = true;
        try
        {
            var nextSort = _assignees.Count > 0 ? _assignees.Max(a => a.SortOrder) + 1 : 0;
            await AssigneeRepo.CreateAsync(new Assignee
            {
                ProjectId = ProjectId,
                Name = _newName.Trim(),
                SortOrder = nextSort
            });
            _addModalOpen = false;
            _assignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
        }
        finally
        {
            _saving = false;
        }
    }

    private void GoToDetail(int assigneeId) => Nav.NavigateTo($"/assignees/{ProjectId}/{assigneeId}");
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");
}
