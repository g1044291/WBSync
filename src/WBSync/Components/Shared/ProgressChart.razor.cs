using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using WBSync.Helpers;
using WBSync.Models;

namespace WBSync.Components.Shared;

/// <summary>日別実績（棒）と累積予定・累積実績・完了予測（線）を重ねて描画する予実推移グラフ。</summary>
public partial class ProgressChart
{
    /// <summary>描画する日別データ点（日付昇順・1日ごと）。</summary>
    [Parameter] public List<ProgressChartPoint> Points { get; set; } = [];

    /// <summary>今日の日付。横軸の範囲内であれば今日の位置に縦線を描画する。</summary>
    [Parameter] public DateOnly Today { get; set; }

    #region 描画領域（SVG viewBox 座標）

    private const double ViewWidth = 1000;
    private const double ViewHeight = 300;
    private const double PlotLeft = 56;
    private const double PlotRight = ViewWidth - 16;
    private const double PlotTop = 24;
    private const double PlotBottom = ViewHeight - 32;

    /// <summary>横軸ラベルの最大表示数（日数が多い場合は間引く）。</summary>
    private const int MaxXLabels = 12;

    #endregion

    private List<double> _yTicks = [];
    private double _yMax = 1;
    private int _xLabelStep = 1;
    private int? _todayIndex;
    private int? _hoverIndex;

    /// <summary>累積予定線を描画する場合は <see langword="true"/>。</summary>
    private bool HasPlanned => Points.Any(p => p.CumulativePlanned.HasValue);

    /// <summary>累積実績線を描画する場合は <see langword="true"/>。</summary>
    private bool HasActual => Points.Any(p => p.CumulativeActual.HasValue);

    /// <summary>完了予測線を描画する場合は <see langword="true"/>。</summary>
    private bool HasForecast => Points.Any(p => p.Forecast.HasValue);

    /// <summary>1日あたりの横幅（viewBox 座標）。</summary>
    private double SlotWidth => (PlotRight - PlotLeft) / Math.Max(Points.Count, 1);

    /// <summary>日別実績の棒の幅（viewBox 座標）。</summary>
    private double BarWidth => Math.Max(SlotWidth * 0.7, 1);

    /// <summary>
    /// パラメーター変更時に縦軸の目盛り・横軸ラベルの間引き間隔・今日の位置を再計算する。
    /// </summary>
    protected override void OnParametersSet()
    {
        var maxValue = Points
            .SelectMany(p => new[] { p.DailyActual, p.CumulativePlanned ?? 0, p.CumulativeActual ?? 0, p.Forecast ?? 0 })
            .DefaultIfEmpty(0)
            .Max();
        var step = CalcNiceStep(maxValue);
        _yMax = step * Math.Max(Math.Ceiling(maxValue / step), 1);
        _yTicks = [];
        for (var tick = 0d; tick <= _yMax + step / 2; tick += step)
            _yTicks.Add(Math.Round(tick, 6));

        _xLabelStep = Math.Max((int)Math.Ceiling(Points.Count / (double)MaxXLabels), 1);
        var todayIndex = Points.FindIndex(p => p.Date == Today);
        _todayIndex = todayIndex >= 0 ? todayIndex : null;
        if (_hoverIndex >= Points.Count) _hoverIndex = null;
    }

    /// <summary>最大値から縦軸目盛りの間隔（1・2・5 × 10^n）を算出する。目盛りは5本前後になる。</summary>
    /// <param name="maxValue">描画する値の最大値。</param>
    /// <returns>目盛り間隔（人日）。最大値が0以下の場合は 0.2。</returns>
    private static double CalcNiceStep(double maxValue)
    {
        if (maxValue <= 0) return 0.2;
        var raw = maxValue / 5;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var normalized = raw / magnitude;
        var nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return nice * magnitude;
    }

    /// <summary>データ点のインデックスから横軸の中心座標を算出する。</summary>
    /// <param name="index">データ点のインデックス。</param>
    /// <returns>viewBox 座標の x。</returns>
    private double X(int index) => PlotLeft + (index + 0.5) * SlotWidth;

    /// <summary>値（人日）から縦軸座標を算出する。</summary>
    /// <param name="value">値（人日）。</param>
    /// <returns>viewBox 座標の y。</returns>
    private double Y(double value) => PlotBottom - value / _yMax * (PlotBottom - PlotTop);

    /// <summary>指定した系列の値を持つデータ点を polyline の points 属性値に変換する。</summary>
    /// <param name="selector">データ点から系列の値を取り出す関数。値がない点は描画しない。</param>
    /// <returns>「x,y x,y …」形式の文字列。</returns>
    private string BuildPolyline(Func<ProgressChartPoint, double?> selector)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Points.Count; i++)
        {
            if (selector(Points[i]) is not { } value) continue;
            sb.Append(F(X(i))).Append(',').Append(F(Y(value))).Append(' ');
        }
        return sb.ToString();
    }

    /// <summary>ホバー中のデータ点をクリアする。</summary>
    private void ClearHover() => _hoverIndex = null;

    /// <summary>SVG 属性値用に数値をカルチャ非依存でフォーマットする。</summary>
    /// <param name="value">数値。</param>
    /// <returns>小数第2位までの文字列。</returns>
    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>ツールチップ用に工数（人日）をフォーマットする。</summary>
    /// <param name="value">工数（人日）。</param>
    /// <returns>「n人日」形式の文字列。</returns>
    private static string FormatValue(double value) => $"{PersonDayHelper.FormatWorkDays(value)}人日";
}
