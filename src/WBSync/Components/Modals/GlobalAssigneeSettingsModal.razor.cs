using Microsoft.AspNetCore.Components;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

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
    private bool _disableAdd;
    private StatusMessage? _addStatus;

    private int? _editingId;
    private string _editName = string.Empty;
    private bool _disableEdit;
    private StatusMessage? _editStatus;

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
        _addStatus = null;
        _editStatus = null;
        await OnClose.InvokeAsync();
    }

    #region 追加

    /// <summary>追加フォームを表示する。</summary>
    private void StartAdding()
    {
        _newName = string.Empty;
        _addStatus = null;
        _editingId = null;
        _isAdding = true;
    }

    /// <summary>追加フォームをキャンセルする。</summary>
    private void CancelAdding()
    {
        _isAdding = false;
        _addStatus = null;
    }

    /// <summary>グローバル担当者を追加する。</summary>
    private async Task AddAssignee()
    {
        _addStatus = null;
        if (string.IsNullOrWhiteSpace(_newName)) { _addStatus = StatusMessage.Error("担当者名を入力してください"); return; }

        _disableAdd = true;
        try
        {
            await GlobalAssigneeRepo.CreateAsync(new GlobalAssignee { Name = _newName.Trim() });
            _isAdding = false;
            _assignees = await GlobalAssigneeRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _addStatus = StatusMessage.Error(
                ex.InnerException?.Message.Contains("UNIQUE") == true
                    ? "同じ名前の担当者がすでに登録されています"
                    : $"エラー: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            _disableAdd = false;
        }
    }

    #endregion

    #region 編集

    /// <summary>指定行をインライン編集モードにする。</summary>
    private void StartEditing(GlobalAssignee g)
    {
        _isAdding = false;
        _addStatus = null;
        _editingId = g.Id;
        _editName = g.Name;
        _editStatus = null;
    }

    /// <summary>インライン編集をキャンセルする。</summary>
    private void CancelEditing()
    {
        _editingId = null;
        _editStatus = null;
    }

    /// <summary>グローバル担当者名を保存する。</summary>
    private async Task SaveAssignee(GlobalAssignee g)
    {
        _editStatus = null;
        if (string.IsNullOrWhiteSpace(_editName)) { _editStatus = StatusMessage.Error("担当者名を入力してください"); return; }

        _disableEdit = true;
        try
        {
            g.Name = _editName.Trim();
            await GlobalAssigneeRepo.UpdateAsync(g);
            _editingId = null;
            _assignees = await GlobalAssigneeRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _editStatus = StatusMessage.Error(
                ex.InnerException?.Message.Contains("UNIQUE") == true
                    ? "同じ名前の担当者がすでに登録されています"
                    : $"エラー: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            _disableEdit = false;
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
            _editStatus = StatusMessage.Error($"削除に失敗しました: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    #endregion
}
