using Microsoft.AspNetCore.Components;
using WBSync.Models;
using WBSync.Repositories;

namespace WBSync.Components.Modals;

public partial class ProjectCreateModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<Project> OnCreated { get; set; }
    [Parameter, EditorRequired] public IProjectRepository ProjectRepo { get; set; } = null!;

    private string _name = string.Empty;
    private DateOnly? _startDate = DateOnly.FromDateTime(DateTime.Today);
    private string? _error;
    private bool _saving;

    private async Task HandleClose()
    {
        Reset();
        await OnClose.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_name))
        {
            _error = "プロジェクト名を入力してください";
            return;
        }
        if (_startDate is null)
        {
            _error = "開始日を入力してください";
            return;
        }

        _saving = true;
        try
        {
            var project = await ProjectRepo.CreateAsync(new Project
            {
                Name = _name.Trim(),
                StartDate = _startDate.Value.ToString("yyyy-MM-dd")
            });
            Reset();
            await OnCreated.InvokeAsync(project);
        }
        finally
        {
            _saving = false;
        }
    }

    private void Reset()
    {
        _name = string.Empty;
        _startDate = DateOnly.FromDateTime(DateTime.Today);
        _error = null;
    }
}
