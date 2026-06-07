using Microsoft.AspNetCore.Components;
using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>担当者一覧画面のコードビハインド。</summary>
public partial class AssigneeList
{
    /// <summary>プロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    private List<Assignee> _assignees = [];
    private bool _addModalOpen;
    private string _newName = string.Empty;
    private bool _saving;
    private string? _error;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        _assignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
    }

    /// <summary>担当者追加モーダルを開く。</summary>
    private void OpenAddModal()
    {
        _newName = string.Empty;
        _error = null;
        _addModalOpen = true;
    }

    /// <summary>担当者追加モーダルを閉じる。</summary>
    private void CloseAddModal()
    {
        _addModalOpen = false;
        _error = null;
    }

    /// <summary>新しい担当者を作成する。</summary>
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

    /// <summary>担当者詳細画面に遷移する。</summary>
    /// <param name="assigneeId">対象担当者ID。</param>
    private void GoToDetail(int assigneeId) => Nav.NavigateTo($"/assignees/{ProjectId}/{assigneeId}");

    /// <summary>ガントチャート画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");
}
