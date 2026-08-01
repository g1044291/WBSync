using WBSync.Helpers;
using WBSync.Models;
using WBSync.Repositories.Interfaces;
using WBSync.Services.Interfaces;

namespace WBSync.Services;

/// <summary><see cref="IScheduleService"/> の実装。</summary>
public class ScheduleService(
    IGlobalHolidayRepository globalHolidayRepo,
    IAssigneeHolidayRepository assigneeHolidayRepo,
    ITaskRepository taskRepo) : IScheduleService
{
    /// <summary>
    /// 先行タスクの終了日から開始日・終了日を計算する。
    /// 開始日 = 先行タスク終了日の翌稼働日（当該タスクの担当者の休日で判定）。
    /// 終了日 = 工数が設定されている場合のみ算出。
    /// </summary>
    /// <param name="predecessorEndDate">先行タスクの終了日（yyyy-MM-dd 形式）。</param>
    /// <param name="workDays">当該タスクの工数（人日）。<see langword="null"/> の場合は終了日を計算しない。</param>
    /// <param name="assigneeId">当該タスクの担当者ID。<see langword="null"/> の場合は個人休日を考慮しない。</param>
    /// <returns>計算された開始日と終了日（工数なしの場合は終了日が <see langword="null"/>）。</returns>
    public async Task<(DateOnly StartDate, DateOnly? EndDate)> CalcDatesFromPredecessorAsync(
        string predecessorEndDate,
        double? workDays,
        int? assigneeId)
    {
        if (!DateOnly.TryParse(predecessorEndDate, out var predEnd))
            throw new ArgumentException($"無効な終了日: {predecessorEndDate}", nameof(predecessorEndDate));

        var (globalHolidays, assigneeHolidays) = await LoadHolidaysAsync(assigneeId);
        var startDate = WorkdayHelper.GetNextWorkday(predEnd.AddDays(1), globalHolidays, assigneeHolidays, assigneeId);
        DateOnly? endDate = workDays.HasValue
            ? WorkdayHelper.CalcEndDate(startDate, workDays.Value, globalHolidays, assigneeHolidays, assigneeId)
            : null;

        return (startDate, endDate);
    }

    /// <summary>
    /// 開始日と工数から終了日を計算する。
    /// </summary>
    /// <param name="startDate">開始日。</param>
    /// <param name="workDays">工数（人日）。</param>
    /// <param name="assigneeId">担当者ID。<see langword="null"/> の場合は個人休日を考慮しない。</param>
    /// <returns>計算された終了日。</returns>
    public async Task<DateOnly> CalcEndDateAsync(DateOnly startDate, double workDays, int? assigneeId)
    {
        var (globalHolidays, assigneeHolidays) = await LoadHolidaysAsync(assigneeId);
        return WorkdayHelper.CalcEndDate(startDate, workDays, globalHolidays, assigneeHolidays, assigneeId);
    }

    /// <summary>
    /// 保存されたタスクの後続タスクに開始日・終了日の変更を連鎖伝播させ DB を更新する。
    /// 後続タスクの開始日 = 前タスク終了日の翌稼働日。終了日は後続タスクの既存の期間（開始日〜終了日の日数）を維持したままずらす
    /// （工数からの再計算は行わない）。既存の開始日・終了日が未設定の場合のみ、工数から終了日を算出する。
    /// 保存前後で開始日・終了日が変わっていない場合は、伝播処理自体を行わない。
    /// </summary>
    /// <param name="projectId">対象プロジェクトID。</param>
    /// <param name="savedTask">保存されたタスク（最新の EndDate を持つこと）。</param>
    /// <param name="oldStartDate">保存前の開始日（yyyy-MM-dd 形式）。新規作成の場合は <see langword="null"/>。</param>
    /// <param name="oldEndDate">保存前の終了日（yyyy-MM-dd 形式）。新規作成の場合は <see langword="null"/>。</param>
    public async Task PropagateSuccessorsAsync(int projectId, WbsTask savedTask, string? oldStartDate, string? oldEndDate)
    {
        if (oldStartDate == savedTask.StartDate && oldEndDate == savedTask.EndDate) return;
        if (string.IsNullOrEmpty(savedTask.EndDate)) return;

        var allTasks = await taskRepo.GetByProjectAsync(projectId);
        var (globalHolidays, assigneeHolidays) = await LoadHolidaysForProjectAsync(allTasks);

        await PropagateRecursiveAsync(allTasks, savedTask, globalHolidays, assigneeHolidays);
    }

    /// <summary>後続タスクへの伝播を再帰的に実行する。</summary>
    /// <param name="allTasks">プロジェクト内の全タスク。</param>
    /// <param name="predecessor">起点となる前タスク（最新の EndDate を持つこと）。</param>
    /// <param name="globalHolidays">全体休日の日付セット。</param>
    /// <param name="assigneeHolidays">担当者IDをキーとした個人休日の日付セット。</param>
    private async Task PropagateRecursiveAsync(
        List<WbsTask> allTasks,
        WbsTask predecessor,
        HashSet<DateOnly> globalHolidays,
        IReadOnlyDictionary<int, IReadOnlySet<DateOnly>> assigneeHolidays)
    {
        if (!DateOnly.TryParse(predecessor.EndDate, out var predEnd)) return;

        var successors = allTasks.Where(t => t.PredecessorId == predecessor.Id).ToList();

        foreach (var successor in successors)
        {
            var newStart = WorkdayHelper.GetNextWorkday(predEnd.AddDays(1), globalHolidays, assigneeHolidays, successor.AssigneeId);

            DateOnly? newEnd;
            if (DateOnly.TryParse(successor.StartDate, out var oldStart) && DateOnly.TryParse(successor.EndDate, out var oldEnd))
            {
                // 期間（開始日〜終了日の日数）を維持したままずらす（工数からの再計算は行わない）
                var duration = oldEnd.DayNumber - oldStart.DayNumber;
                newEnd = newStart.AddDays(duration);
            }
            else
            {
                newEnd = successor.WorkDays.HasValue
                    ? WorkdayHelper.CalcEndDate(newStart, successor.WorkDays.Value, globalHolidays, assigneeHolidays, successor.AssigneeId)
                    : (DateOnly?)null;
            }

            var newStartStr = newStart.ToString("yyyy-MM-dd");
            var newEndStr = newEnd?.ToString("yyyy-MM-dd");

            if (successor.StartDate == newStartStr && successor.EndDate == newEndStr) continue;

            successor.StartDate = newStartStr;
            successor.EndDate = newEndStr;
            await taskRepo.UpdateAsync(successor);

            await PropagateRecursiveAsync(allTasks, successor, globalHolidays, assigneeHolidays);
        }
    }

    /// <summary>
    /// 同一担当者で稼働日が1日以上重複するタスクの ID セットを返す。
    /// 担当者未設定・開始日未設定・工数未設定のタスクは対象外。
    /// </summary>
    /// <param name="projectId">対象プロジェクトID。</param>
    /// <returns>重複警告対象タスクIDのセット。</returns>
    public async Task<HashSet<int>> GetOverlappingTaskIdsAsync(int projectId)
    {
        var allTasks = await taskRepo.GetByProjectAsync(projectId);
        var (globalHolidays, assigneeHolidays) = await LoadHolidaysForProjectAsync(allTasks);

        var assigneeWorkdays = new Dictionary<int, List<(int TaskId, HashSet<DateOnly> Workdays)>>();

        foreach (var task in allTasks)
        {
            if (!task.AssigneeId.HasValue) continue;
            if (!DateOnly.TryParse(task.StartDate, out var startDate)) continue;
            if (!task.WorkDays.HasValue) continue;

            var workdays = WorkdayHelper.GetWorkdays(startDate, task.WorkDays.Value, globalHolidays, assigneeHolidays, task.AssigneeId).ToHashSet();
            if (workdays.Count == 0) continue;

            if (!assigneeWorkdays.TryGetValue(task.AssigneeId.Value, out var list))
            {
                list = [];
                assigneeWorkdays[task.AssigneeId.Value] = list;
            }
            list.Add((task.Id, workdays));
        }

        var warningIds = new HashSet<int>();

        foreach (var (_, taskList) in assigneeWorkdays)
        {
            for (var i = 0; i < taskList.Count; i++)
            {
                for (var j = i + 1; j < taskList.Count; j++)
                {
                    if (taskList[i].Workdays.Overlaps(taskList[j].Workdays))
                    {
                        warningIds.Add(taskList[i].TaskId);
                        warningIds.Add(taskList[j].TaskId);
                    }
                }
            }
        }

        return warningIds;
    }

    /// <summary>単一担当者の休日のみを含む休日データを DB から読み込む。</summary>
    /// <param name="assigneeId">個人休日を読み込む担当者ID。<see langword="null"/> の場合は個人休日を読み込まない。</param>
    /// <returns>全体休日のセットと、担当者IDをキーとした個人休日のセット。</returns>
    private async Task<(HashSet<DateOnly> GlobalHolidays, Dictionary<int, IReadOnlySet<DateOnly>> AssigneeHolidays)> LoadHolidaysAsync(int? assigneeId)
    {
        var globalSet = await LoadGlobalHolidaysAsync();

        var assigneeDict = new Dictionary<int, IReadOnlySet<DateOnly>>();
        if (assigneeId.HasValue)
        {
            var ah = await assigneeHolidayRepo.GetByAssigneeAsync(assigneeId.Value);
            assigneeDict[assigneeId.Value] = ParseDates(ah.Select(h => h.Date));
        }

        return (globalSet, assigneeDict);
    }

    /// <summary>プロジェクト全担当者の休日を含む休日データを DB から読み込む。</summary>
    /// <param name="allTasks">担当者IDを収集する対象のタスク一覧。</param>
    /// <returns>全体休日のセットと、担当者IDをキーとした個人休日のセット。</returns>
    private async Task<(HashSet<DateOnly> GlobalHolidays, Dictionary<int, IReadOnlySet<DateOnly>> AssigneeHolidays)> LoadHolidaysForProjectAsync(List<WbsTask> allTasks)
    {
        var globalSet = await LoadGlobalHolidaysAsync();

        var assigneeIds = allTasks
            .Where(t => t.AssigneeId.HasValue)
            .Select(t => t.AssigneeId!.Value)
            .Distinct();

        var assigneeDict = new Dictionary<int, IReadOnlySet<DateOnly>>();
        foreach (var assigneeId in assigneeIds)
        {
            var ah = await assigneeHolidayRepo.GetByAssigneeAsync(assigneeId);
            assigneeDict[assigneeId] = ParseDates(ah.Select(h => h.Date));
        }

        return (globalSet, assigneeDict);
    }

    /// <summary>全体休日を DB から読み込んで <see cref="DateOnly"/> のセットに変換する。</summary>
    /// <returns>全体休日の日付セット。</returns>
    private async Task<HashSet<DateOnly>> LoadGlobalHolidaysAsync()
    {
        var holidays = await globalHolidayRepo.GetAllAsync();
        return ParseDates(holidays.Select(h => h.Date));
    }

    /// <summary>yyyy-MM-dd 形式の日付文字列列挙を <see cref="DateOnly"/> の <see cref="HashSet{T}"/> に変換する。</summary>
    /// <param name="dates">yyyy-MM-dd 形式の日付文字列の列挙。パース不能な値は無視する。</param>
    /// <returns>変換後の日付セット。</returns>
    private static HashSet<DateOnly> ParseDates(IEnumerable<string> dates)
        => dates
            .Select(d => DateOnly.TryParse(d, out var date) ? date : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToHashSet();
}
