using System.Globalization;
using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Web.Services;

/// <summary>Maps the explorer's URL parameters to and from <see cref="ExplorerQuery"/> so every design is a shareable link.</summary>
public static class ExplorerQueryString
{
    public static ExplorerQuery Parse(string? metric, string? parties, string? politicians, string? from, string? to, string? range, string? grouping, string? voteType, int? keywordId, string? chart)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly? fromDate = ParseDate(from);
        DateOnly? toDate = ParseDate(to);
        if (fromDate is null && toDate is null)
        {
            fromDate = range switch
            {
                "3m" => today.AddMonths(-3),
                "12m" => today.AddMonths(-12),
                "24m" => today.AddMonths(-24),
                "5y" => today.AddYears(-5),
                "all" => new DateOnly(2004, 10, 1),
                _ => today.AddMonths(-6),
            };
        }

        return new ExplorerQuery(
            Enum.TryParse<ExplorerMetric>(metric, true, out var m) ? m : ExplorerMetric.Attendance,
            Split(parties),
            Split(politicians).Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0).Where(id => id > 0).Take(8).ToList(),
            fromDate,
            toDate,
            Enum.TryParse<ExplorerGrouping>(grouping, true, out var g) ? g : ExplorerGrouping.None,
            Enum.TryParse<VoteType>(voteType, true, out var vt) ? vt : null,
            keywordId,
            Enum.TryParse<ExplorerChart>(chart, true, out var c) ? c : ExplorerChart.Bar);
    }

    public static string Build(ExplorerQuery q, string? keywordText = null, ExplorerChart? chart = null) => QueryString.Build("/udforsk",
        ("metric", q.Metric.ToString()),
        ("parties", q.Parties.Count > 0 ? string.Join(',', q.Parties) : null),
        ("politicians", q.Politicians.Count > 0 ? string.Join(',', q.Politicians.Select(p => p.ToString(CultureInfo.InvariantCulture))) : null),
        ("from", q.From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ("to", q.To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ("grouping", q.Grouping == ExplorerGrouping.None ? null : q.Grouping.ToString()),
        ("voteType", q.VoteType?.ToString()),
        ("keyword", q.KeywordId?.ToString(CultureInfo.InvariantCulture)),
        ("topic", keywordText),
        ("chart", (chart ?? q.Chart).ToString()));

    private static List<string> Split(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(8).ToList();

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
