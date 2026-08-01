using Microsoft.AspNetCore.Components;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

namespace WBSync.Components.Modals;

/// <summary>プロジェクト作成モーダルのコードビハインド。</summary>
public partial class ProjectCreateModal
{
    /// <summary>モーダルの開閉状態。</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>モーダルを閉じるときに呼び出されるコールバック。</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>プロジェクト作成完了時に呼び出されるコールバック。作成されたプロジェクトを渡す。</summary>
    [Parameter] public EventCallback<Project> OnCreated { get; set; }

    /// <summary>プロジェクトリポジトリ。</summary>
    [Parameter, EditorRequired] public IProjectRepository ProjectRepo { get; set; } = null!;

    private string _name = string.Empty;
    private DateOnly? _startDate = DateOnly.FromDateTime(DateTime.Today);
    private StatusMessage? _formStatus;
    private bool _disableCreate;

    /// <summary>モーダルを閉じてフォームをリセットする。</summary>
    private async Task HandleClose()
    {
        Reset();
        await OnClose.InvokeAsync();
    }

    /// <summary>フォームを検証してプロジェクトを作成する。</summary>
    private async Task HandleSubmit()
    {
        _formStatus = null;

        if (string.IsNullOrWhiteSpace(_name))
        {
            _formStatus = StatusMessage.Error("プロジェクト名を入力してください");
            return;
        }
        if (_startDate is null)
        {
            _formStatus = StatusMessage.Error("開始日を入力してください");
            return;
        }

        _disableCreate = true;
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
        catch (Exception ex)
        {
            _formStatus = StatusMessage.Error(
                ex.InnerException?.Message.Contains("UNIQUE") == true
                    ? "同じ名前のプロジェクトがすでに登録されています"
                    : $"エラーが発生しました: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            _disableCreate = false;
        }
    }

    /// <summary>フォームの入力値を初期状態にリセットする。</summary>
    private void Reset()
    {
        _name = string.Empty;
        _startDate = DateOnly.FromDateTime(DateTime.Today);
        _formStatus = null;
    }
}
