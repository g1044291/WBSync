namespace WBSync.Services;

/// <summary>スケジュール計算サービスのインターフェース。</summary>
public interface IScheduleService
{
    /// <summary>
    /// 先行タスクの終了日から開始日・終了日を計算する。
    /// 開始日 = 先行タスク終了日の翌稼働日（当該タスクの担当者の休日で判定）。
    /// 終了日 = 工数が設定されている場合のみ <see cref="WorkdayCalculator.CalcEndDate"/> で算出。
    /// </summary>
    /// <param name="predecessorEndDate">先行タスクの終了日（yyyy-MM-dd 形式）。</param>
    /// <param name="workDays">当該タスクの工数（人日）。<see langword="null"/> の場合は終了日を計算しない。</param>
    /// <param name="assigneeId">当該タスクの担当者ID。<see langword="null"/> の場合は個人休日を考慮しない。</param>
    /// <returns>計算された開始日と終了日（工数なしの場合は終了日が <see langword="null"/>）。</returns>
    Task<(DateOnly StartDate, DateOnly? EndDate)> CalcDatesFromPredecessorAsync(
        string predecessorEndDate,
        double? workDays,
        int? assigneeId);
}
