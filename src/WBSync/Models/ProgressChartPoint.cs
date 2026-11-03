namespace WBSync.Models;

/// <summary>ダッシュボードの予実推移グラフにおける1日分のデータ点。</summary>
/// <param name="Date">対象日。</param>
/// <param name="DailyActual">その日に記録された実績の合計（人日）。</param>
/// <param name="CumulativePlanned">累積予定（人日）。予定工数・期間が設定されたリーフタスクが1件もない場合は <see langword="null"/>。</param>
/// <param name="CumulativeActual">累積実績（人日）。実績線の描画範囲外（実績線の終端日より後）は <see langword="null"/>。</param>
/// <param name="Forecast">完了予測（人日）。予測線の描画範囲外（予測線の起点日より前）は <see langword="null"/>。</param>
/// <remarks>DB非対応の画面専用モデルだが、Blazor コンポーネントのパラメーター（public 必須）として渡すため public とする。</remarks>
public sealed record ProgressChartPoint(
    DateOnly Date,
    double DailyActual,
    double? CumulativePlanned,
    double? CumulativeActual,
    double? Forecast);
