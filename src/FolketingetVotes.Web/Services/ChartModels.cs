namespace FolketingetVotes.Web.Services;

/// <summary>One data point in a chart: a category label and a value (null = no data).</summary>
public sealed record ChartPoint(string Label, double? Value, string? Detail = null);

/// <summary>A named series of points; <see cref="Slot"/> picks the categorical colour (1–8, assigned in order, never cycled).</summary>
public sealed record ChartSeries(string Name, IReadOnlyList<ChartPoint> Points, int Slot);

/// <summary>Number formatting shared by the SVG charts.</summary>
public static class ChartFormat
{
    public static string Value(double value, bool percent) =>
        percent ? Labels.Percent(value) : Labels.Number((long)Math.Round(value));

    /// <summary>A "nice" axis maximum: 0–1 for shares, otherwise the value rounded up to 1/2/5 × 10^n.</summary>
    public static double NiceMax(double max, bool percent)
    {
        if (percent)
        {
            return 1;
        }

        if (max <= 0)
        {
            return 1;
        }

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(max)));
        var normalized = max / magnitude;
        var step = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return step * magnitude;
    }

    /// <summary>CSS variable for a categorical slot (validated palette in app.css).</summary>
    public static string SeriesColor(int slot) => $"var(--series-{Math.Clamp(slot, 1, 8)})";
}
