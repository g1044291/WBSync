using Microsoft.AspNetCore.Components;
using WBSync.Models;
using WBSync.Repositories;

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
    private string? _error;
    private bool _saving;

    /// <summary>モーダルを閉じてフォームをリセットする。</summary>
    private async Task HandleClose()
    {
        Reset();
        await OnClose.InvokeAsync();
    }

    /// <summary>フォームを検証してプロジェクトを作成する。</summary>
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
        catch (Exception ex)
        {
            _error = ex.InnerException?.Message.Contains("UNIQUE") == true
                ? "同じ名前のプロジェクトがすでに登録されています"
                : $"エラーが発生しました: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>フォームの入力値を初期状態にリセットする。</summary>
    private void Reset()
    {
        _name = string.Empty;
        _startDate = DateOnly.FromDateTime(DateTime.Today);
        _error = null;
    }
}
