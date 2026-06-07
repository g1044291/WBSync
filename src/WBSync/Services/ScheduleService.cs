using WBSync.Repositories;

namespace WBSync.Services;

/// <summary><see cref="IScheduleService"/> の実装。休日データを DB から読み込み <see cref="WorkdayCalculator"/> で計算する。</summary>
public class ScheduleService(
    IGlobalHolidayRepository globalHolidayRepo,
    IAssigneeHolidayRepository assigneeHolidayRepo) : IScheduleService
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

    /// <summary>休日データを DB から読み込んで <see cref="WorkdayCalculator"/> を生成する。</summary>
    /// <param name="assigneeId">個人休日を読み込む担当者ID。<see langword="null"/> の場合は個人休日を読み込まない。</param>
    private async Task<WorkdayCalculator> BuildCalculatorAsync(int? assigneeId)
    {
        var globalHolidays = await globalHolidayRepo.GetAllAsync();
        var globalSet = globalHolidays
            .Select(h => DateOnly.TryParse(h.Date, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToHashSet();

        var assigneeDict = new Dictionary<int, IReadOnlySet<DateOnly>>();
        if (assigneeId.HasValue)
        {
            var ah = await assigneeHolidayRepo.GetByAssigneeAsync(assigneeId.Value);
            assigneeDict[assigneeId.Value] = ah
                .Select(h => DateOnly.TryParse(h.Date, out var d) ? d : (DateOnly?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToHashSet();
        }

        return new WorkdayCalculator(globalSet, assigneeDict);
    }
}
