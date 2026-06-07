using WBSync.Models;
using WBSync.Repositories;

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

        var calculator = await BuildCalculatorAsync(assigneeId);
        var startDate = calculator.GetNextWorkday(predEnd.AddDays(1), assigneeId);
        DateOnly? endDate = workDays.HasValue
            ? calculator.CalcEndDate(startDate, workDays.Value, assigneeId)
            : null;

        return (startDate, endDate);
    }

    /// <inheritdoc/>
    public async Task<DateOnly> CalcEndDateAsync(DateOnly startDate, double workDays, int? assigneeId)
    {
        var calculator = await BuildCalculatorAsync(assigneeId);
        return calculator.CalcEndDate(startDate, workDays, assigneeId);
    }

    /// <inheritdoc/>
    public async Task PropagateSuccessorsAsync(int projectId, WbsTask savedTask)
    {
        if (string.IsNullOrEmpty(savedTask.EndDate)) return;

        var allTasks = await taskRepo.GetByProjectAsync(projectId);
        var calculator = await BuildCalculatorForProjectAsync(allTasks);

        await PropagateRecursiveAsync(allTasks, savedTask, calculator);
    }

    /// <summary>後続タスクへの伝播を再帰的に実行する。</summary>
    private async Task PropagateRecursiveAsync(
        List<WbsTask> allTasks,
        WbsTask predecessor,
        WorkdayCalculator calculator)
    {
        if (!DateOnly.TryParse(predecessor.EndDate, out var predEnd)) return;

        var successors = allTasks.Where(t => t.PredecessorId == predecessor.Id).ToList();

        foreach (var successor in successors)
        {
            var newStart = calculator.GetNextWorkday(predEnd.AddDays(1), successor.AssigneeId);
            var newEnd = successor.WorkDays.HasValue
                ? calculator.CalcEndDate(newStart, successor.WorkDays.Value, successor.AssigneeId)
                : (DateOnly?)null;

            var newStartStr = newStart.ToString("yyyy-MM-dd");
            var newEndStr = newEnd?.ToString("yyyy-MM-dd");

            if (successor.StartDate == newStartStr && successor.EndDate == newEndStr) continue;

            successor.StartDate = newStartStr;
            successor.EndDate = newEndStr;
            await taskRepo.UpdateAsync(successor);

            await PropagateRecursiveAsync(allTasks, successor, calculator);
        }
    }

    /// <summary>単一担当者の休日のみを含む <see cref="WorkdayCalculator"/> を生成する。</summary>
    private async Task<WorkdayCalculator> BuildCalculatorAsync(int? assigneeId)
    {
        var globalSet = await LoadGlobalHolidaysAsync();

        var assigneeDict = new Dictionary<int, IReadOnlySet<DateOnly>>();
        if (assigneeId.HasValue)
        {
            var ah = await assigneeHolidayRepo.GetByAssigneeAsync(assigneeId.Value);
            assigneeDict[assigneeId.Value] = ParseDates(ah.Select(h => h.Date));
        }

        return new WorkdayCalculator(globalSet, assigneeDict);
    }

    /// <summary>プロジェクト全担当者の休日を含む <see cref="WorkdayCalculator"/> を生成する。</summary>
    private async Task<WorkdayCalculator> BuildCalculatorForProjectAsync(List<WbsTask> allTasks)
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

        return new WorkdayCalculator(globalSet, assigneeDict);
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
