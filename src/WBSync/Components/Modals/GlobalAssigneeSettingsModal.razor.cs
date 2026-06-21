using Microsoft.AspNetCore.Components;
using WBSync.Models;
using WBSync.Repositories;

namespace WBSync.Components.Modals;

/// <summary>グローバル担当者マスタ設定モーダルのコードビハインド。</summary>
public partial class GlobalAssigneeSettingsModal
{
    /// <summary>モーダルの開閉状態。</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>モーダルを閉じるときに呼び出されるコールバック。</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>グローバル担当者リポジトリ。</summary>
    [Parameter, EditorRequired] public IGlobalAssigneeRepository GlobalAssigneeRepo { get; set; } = null!;

    private List<GlobalAssignee> _assignees = [];

    private bool _isAdding;
    private string _newName = string.Empty;
    private bool _addSaving;
    private string? _addError;

    private int? _editingId;
    private string _editName = string.Empty;
    private bool _editSaving;
    private string? _editError;

    /// <summary>モーダルが開かれたときにマスタ一覧を読み込む。</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && !_assignees.Any())
            _assignees = await GlobalAssigneeRepo.GetAllAsync();
    }

    /// <summary>モーダルを閉じる。</summary>
    private async Task HandleClose()
    {
        _isAdding = false;
        _editingId = null;
        _addError = null;
        _editError = null;
        await OnClose.InvokeAsync();
    }

    #region 追加

    /// <summary>追加フォームを表示する。</summary>
    private void StartAdding()
    {
        _newName = string.Empty;
        _addError = null;
        _editingId = null;
        _isAdding = true;
    }

    /// <summary>追加フォームをキャンセルする。</summary>
    private void CancelAdding()
    {
        _isAdding = false;
        _addError = null;
    }

    /// <summary>グローバル担当者を追加する。</summary>
    private async Task AddAssignee()
    {
        _addError = null;
        if (string.IsNullOrWhiteSpace(_newName)) { _addError = "担当者名を入力してください"; return; }

        _addSaving = true;
        try
        {
            await GlobalAssigneeRepo.CreateAsync(new GlobalAssignee { Name = _newName.Trim() });
            _isAdding = false;
            _assignees = await GlobalAssigneeRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _addError = ex.InnerException?.Message.Contains("UNIQUE") == true
                ? "同じ名前の担当者がすでに登録されています"
                : $"エラー: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            _addSaving = false;
        }
    }

    #endregion

    #region 編集

    /// <summary>指定行をインライン編集モードにする。</summary>
    private void StartEditing(GlobalAssignee g)
    {
        _isAdding = false;
        _addError = null;
        _editingId = g.Id;
        _editName = g.Name;
        _editError = null;
    }

    /// <summary>インライン編集をキャンセルする。</summary>
    private void CancelEditing()
    {
        _editingId = null;
        _editError = null;
    }

    /// <summary>グローバル担当者名を保存する。</summary>
    private async Task SaveAssignee(GlobalAssignee g)
    {
        _editError = null;
        if (string.IsNullOrWhiteSpace(_editName)) { _editError = "担当者名を入力してください"; return; }

        _editSaving = true;
        try
        {
            g.Name = _editName.Trim();
            await GlobalAssigneeRepo.UpdateAsync(g);
            _editingId = null;
            _assignees = await GlobalAssigneeRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _editError = ex.InnerException?.Message.Contains("UNIQUE") == true
                ? "同じ名前の担当者がすでに登録されています"
                : $"エラー: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            _editSaving = false;
        }
    }

    #endregion

    #region 削除

    /// <summary>グローバル担当者を削除する。</summary>
    private async Task DeleteAssignee(int id)
    {
        try
        {
            await GlobalAssigneeRepo.DeleteAsync(id);
            _assignees = await GlobalAssigneeRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _editError = $"削除に失敗しました: {ex.InnerException?.Message ?? ex.Message}";
        }
    }

    #endregion
}
