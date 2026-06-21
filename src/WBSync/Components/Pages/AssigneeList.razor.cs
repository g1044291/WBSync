using Microsoft.AspNetCore.Components;
using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>担当者一覧画面のコードビハインド。</summary>
public partial class AssigneeList
{
    /// <summary>プロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    private List<Assignee> _assignees = [];
    private List<GlobalAssignee> _globalAssignees = [];

    // 担当者追加モーダル
    private bool _addModalOpen;
    private bool _addFromGlobal = true;
    private int? _selectedGlobalId;
    private string _newName = string.Empty;
    private decimal _newCoefficient = 1.0m;
    private bool _saving;
    private string? _addError;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        _assignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
        _globalAssignees = await GlobalAssigneeRepo.GetAllAsync();
    }

    /// <summary>担当者追加モーダルを開く。</summary>
    private void OpenAddModal()
    {
        _addFromGlobal = true;
        _selectedGlobalId = null;
        _newName = string.Empty;
        _newCoefficient = 1.0m;
        _addError = null;
        _addModalOpen = true;
    }

    /// <summary>担当者追加モーダルを閉じる。</summary>
    private void CloseAddModal()
    {
        _addModalOpen = false;
        _addError = null;
    }

    /// <summary>追加モードを切り替える。</summary>
    private void SetAddMode(bool fromGlobal)
    {
        _addFromGlobal = fromGlobal;
        _selectedGlobalId = null;
        _newName = string.Empty;
        _newCoefficient = 1.0m;
        _addError = null;
    }

    /// <summary>新しい担当者を作成する（グローバル選択 or プロジェクト専用）。</summary>
    private async Task AddAssignee()
    {
        _addError = null;

        if (_addFromGlobal)
        {
            if (_selectedGlobalId is null)
            {
                _addError = "担当者を選択してください";
                return;
            }

            var alreadyAdded = _assignees.Any(a => a.GlobalAssigneeId == _selectedGlobalId);
            if (alreadyAdded)
            {
                _addError = "選択した担当者はすでにプロジェクトに追加されています";
                return;
            }

            var selected = _globalAssignees.First(g => g.Id == _selectedGlobalId);
            _saving = true;
            try
            {
                var nextSort = _assignees.Count > 0 ? _assignees.Max(a => a.SortOrder) + 1 : 0;
                await AssigneeRepo.CreateAsync(new Assignee
                {
                    ProjectId = ProjectId,
                    GlobalAssigneeId = _selectedGlobalId,
                    Name = selected.Name,
                    ProductivityCoefficient = selected.ProductivityCoefficient,
                    SortOrder = nextSort
                });
                _addModalOpen = false;
                _assignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
            }
            catch (Exception ex)
            {
                _addError = $"エラーが発生しました: {ex.InnerException?.Message ?? ex.Message}";
            }
            finally
            {
                _saving = false;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_newName))
            {
                _addError = "担当者名を入力してください";
                return;
            }
            if (_newCoefficient <= 0)
            {
                _addError = "生産性係数は 0 より大きい値を入力してください";
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
                    ProductivityCoefficient = _newCoefficient,
                    SortOrder = nextSort
                });
                _addModalOpen = false;
                _assignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
            }
            catch (Exception ex)
            {
                _addError = ex.InnerException?.Message.Contains("UNIQUE") == true
                    ? "同じ名前の担当者がすでに登録されています"
                    : $"エラーが発生しました: {ex.InnerException?.Message ?? ex.Message}";
            }
            finally
            {
                _saving = false;
            }
        }
    }

    /// <summary>担当者詳細画面に遷移する。</summary>
    private void GoToDetail(int assigneeId) => Nav.NavigateTo($"/assignees/{ProjectId}/{assigneeId}");

    /// <summary>ガントチャート画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");
}
