using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.ReadModels;

/// <summary>What the explorer measures. Shares are of registered ballots (attendance/absence) or of present ballots (for/against/abstain).</summary>
public enum ExplorerMetric
{
    Attendance,
    Absence,
    ForShare,
    AgainstShare,
    AbstainShare,
    Cohesion,
    Ballots,
    Votes,
    Dissents,
    Questions,
}

public enum ExplorerGrouping
{
    None,
    Month,
    Quarter,
    Year,
    Session,
}

public enum ExplorerChart
{
    Bar,
    Line,
    Pie,
    Table,
}

/// <summary>A user-designed query: metric × entities (parties or members) × time, with optional vote-type and topic filters.</summary>
public sealed record ExplorerQuery(
    ExplorerMetric Metric,
    IReadOnlyList<string> Parties,
    IReadOnlyList<int> Politicians,
    DateOnly? From,
    DateOnly? To,
    ExplorerGrouping Grouping,
    VoteType? VoteType,
    int? KeywordId,
    ExplorerChart Chart)
{
    public bool IsPercent => Metric is ExplorerMetric.Attendance or ExplorerMetric.Absence or ExplorerMetric.ForShare or ExplorerMetric.AgainstShare or ExplorerMetric.AbstainShare or ExplorerMetric.Cohesion;

    public bool ByPoliticians => Politicians.Count > 0;
}

/// <summary>One value for one entity in one time bucket; <see cref="Basis"/> is the denominator (or count) behind it.</summary>
public sealed record ExplorerPoint(string Label, string SortKey, double? Value, int Basis);

public sealed record ExplorerSeries(string Key, string Name, IReadOnlyList<ExplorerPoint> Points);

public sealed record ExplorerResult(ExplorerQuery Query, string MetricLabel, IReadOnlyList<ExplorerSeries> Series, IReadOnlyList<string> Notes, string? KeywordName)
{
    public IReadOnlyList<string> Categories => Series.SelectMany(s => s.Points).OrderBy(p => p.SortKey).Select(p => p.Label).Distinct().ToList();
}

/// <summary>Pure aggregation of raw ballot counts into a metric value.</summary>
public static class ExplorerMath
{
    public sealed record Counts(int Total, int Absent, int For, int Against, int Abstain, int WithMajority, int AgainstMajority, int Votes, int Dissents, int Questions);

    public static (double? Value, int Basis) Evaluate(ExplorerMetric metric, Counts c)
    {
        ArgumentNullException.ThrowIfNull(c);
        var present = c.Total - c.Absent;
        var decided = c.WithMajority + c.AgainstMajority;
        return metric switch
        {
            ExplorerMetric.Attendance => (c.Total == 0 ? null : present / (double)c.Total, c.Total),
            ExplorerMetric.Absence => (c.Total == 0 ? null : c.Absent / (double)c.Total, c.Total),
            ExplorerMetric.ForShare => (present == 0 ? null : c.For / (double)present, present),
            ExplorerMetric.AgainstShare => (present == 0 ? null : c.Against / (double)present, present),
            ExplorerMetric.AbstainShare => (present == 0 ? null : c.Abstain / (double)present, present),
            ExplorerMetric.Cohesion => (decided == 0 ? null : c.WithMajority / (double)decided, decided),
            ExplorerMetric.Ballots => (c.Total, c.Total),
            ExplorerMetric.Votes => (c.Votes, c.Votes),
            ExplorerMetric.Dissents => (c.Dissents, present),
            ExplorerMetric.Questions => (c.Questions, c.Questions),
            _ => (null, 0),
        };
    }

    public static string Label(ExplorerMetric metric) => metric switch
    {
        ExplorerMetric.Attendance => "Fremmøde (andel af registrerede stemmer)",
        ExplorerMetric.Absence => "Fravær (andel af registrerede stemmer)",
        ExplorerMetric.ForShare => "Andel for (af afgivne stemmer)",
        ExplorerMetric.AgainstShare => "Andel imod (af afgivne stemmer)",
        ExplorerMetric.AbstainShare => "Andel hverken for eller imod (af afgivne stemmer)",
        ExplorerMetric.Cohesion => "Sammenhold (andel af afgivne stemmer på gruppens flertal)",
        ExplorerMetric.Ballots => "Registrerede stemmer",
        ExplorerMetric.Votes => "Afstemninger",
        ExplorerMetric.Dissents => "Afvigelser fra gruppens flertal",
        ExplorerMetric.Questions => "§ 20-spørgsmål stillet",
        _ => metric.ToString(),
    };

    /// <summary>Bucket label and sort key for a date under a grouping.</summary>
    public static (string Label, string SortKey) Bucket(ExplorerGrouping grouping, DateOnly date, string sessionTitle, DateOnly sessionStart) => grouping switch
    {
        ExplorerGrouping.Month => (date.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), date.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)),
        ExplorerGrouping.Quarter => ($"{date.Year} K{(date.Month - 1) / 3 + 1}", $"{date.Year}-{(date.Month - 1) / 3 + 1}"),
        ExplorerGrouping.Year => (date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        ExplorerGrouping.Session => (sessionTitle, sessionStart.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
        _ => ("I alt", "0"),
    };
}
