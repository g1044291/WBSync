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
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<DateOnly> CalcEndDateAsync(DateOnly startDate, double workDays, int? assigneeId)
    {
        var (globalHolidays, assigneeHolidays) = await LoadHolidaysAsync(assigneeId);
        return WorkdayHelper.CalcEndDate(startDate, workDays, globalHolidays, assigneeHolidays, assigneeId);
    }

    /// <inheritdoc/>
    public async Task PropagateSuccessorsAsync(int projectId, WbsTask savedTask)
    {
        if (string.IsNullOrEmpty(savedTask.EndDate)) return;

        var allTasks = await taskRepo.GetByProjectAsync(projectId);
        var (globalHolidays, assigneeHolidays) = await LoadHolidaysForProjectAsync(allTasks);

        await PropagateRecursiveAsync(allTasks, savedTask, globalHolidays, assigneeHolidays);
    }

    /// <summary>後続タスクへの伝播を再帰的に実行する。</summary>
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
            var newEnd = successor.WorkDays.HasValue
                ? WorkdayHelper.CalcEndDate(newStart, successor.WorkDays.Value, globalHolidays, assigneeHolidays, successor.AssigneeId)
                : (DateOnly?)null;

            var newStartStr = newStart.ToString("yyyy-MM-dd");
            var newEndStr = newEnd?.ToString("yyyy-MM-dd");

            if (successor.StartDate == newStartStr && successor.EndDate == newEndStr) continue;

            successor.StartDate = newStartStr;
            successor.EndDate = newEndStr;
            await taskRepo.UpdateAsync(successor);

            await PropagateRecursiveAsync(allTasks, successor, globalHolidays, assigneeHolidays);
        }
    }

    /// <inheritdoc/>
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
    private async Task<HashSet<DateOnly>> LoadGlobalHolidaysAsync()
    {
        var holidays = await globalHolidayRepo.GetAllAsync();
        return ParseDates(holidays.Select(h => h.Date));
    }

    /// <summary>yyyy-MM-dd 形式の日付文字列列挙を <see cref="DateOnly"/> の <see cref="HashSet{T}"/> に変換する。</summary>
    private static HashSet<DateOnly> ParseDates(IEnumerable<string> dates)
        => dates
            .Select(d => DateOnly.TryParse(d, out var date) ? date : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToHashSet();
}
