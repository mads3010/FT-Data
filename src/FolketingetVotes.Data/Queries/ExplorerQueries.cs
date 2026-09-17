using System.Globalization;
using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

/// <summary>
/// Runs user-designed queries over the ballot view: one grouped SQL query per request (entity × month), then
/// buckets and metrics are computed in memory so any grouping and metric share the same code path.
/// </summary>
internal sealed class ExplorerQueries(FolketingetDbContext db) : IExplorerQueries
{
    public async Task<ExplorerResult> RunAsync(ExplorerQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var notes = new List<string>();
        var from = query.From ?? DateOnly.FromDateTime(DateTime.Today).AddMonths(-6);
        var to = query.To ?? DateOnly.FromDateTime(DateTime.Today);
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var periods = await db.Periods.ToDictionaryAsync(p => p.Id, p => (p.Title, Start: DateOnly.FromDateTime(p.StartDate)), cancellationToken);
        var partyNames = await db.Parties.ToDictionaryAsync(p => p.ShortName, p => (p.Name, p.IsIndependentGroup), cancellationToken);
        string? keywordName = null;
        if (query.KeywordId is { } kid)
        {
            keywordName = await db.Keywords.Where(k => k.Id == kid).Select(k => k.Name).FirstOrDefaultAsync(cancellationToken);
        }

        var series = query.Metric == ExplorerMetric.Questions
            ? await QuestionSeriesAsync(query, fromDt, toDt, periods, partyNames, cancellationToken)
            : await BallotSeriesAsync(query, fromDt, toDt, periods, partyNames, notes, cancellationToken);

        if (query.Metric == ExplorerMetric.Cohesion && series.Any(s => partyNames.TryGetValue(s.Key, out var p) && p.IsIndependentGroup))
        {
            notes.Add("Løsgængere har ingen fælles gruppelinje; sammenhold beregnes ikke for dem.");
        }

        if (query.Metric == ExplorerMetric.Questions && (query.VoteType is not null || query.KeywordId is not null))
        {
            notes.Add("Afstemningstype og emne gælder ikke for spørgsmål og er ignoreret.");
        }

        return new ExplorerResult(query with { From = from, To = to }, ExplorerMath.Label(query.Metric), series, notes, keywordName);
    }

    public async Task<(int Id, string Name)?> ResolveKeywordAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var pattern = VoteProjections.Pattern(name);
        var exact = name.Trim();
        var hit = await (
            from k in db.Keywords
            join t in db.TopicStats on k.Id equals t.KeywordId
            where EF.Functions.ILike(k.Name, pattern)
            orderby EF.Functions.ILike(k.Name, exact) descending, t.VoteCount descending
            select new { k.Id, k.Name }).FirstOrDefaultAsync(cancellationToken);
        return hit is null ? null : (hit.Id, hit.Name);
    }

    private async Task<List<ExplorerSeries>> BallotSeriesAsync(
        ExplorerQuery query, DateTime fromDt, DateTime toDt,
        Dictionary<int, (string Title, DateOnly Start)> periods, Dictionary<string, (string Name, bool IsIndependentGroup)> partyNames,
        List<string> notes, CancellationToken cancellationToken)
    {
        var ballots =
            from b in db.BallotParties
            join v in db.Votes on b.VoteId equals v.Id
            join p0 in db.VotePartyBreakdowns on new { b.VoteId, Party = b.PartyShortName } equals new { p0.VoteId, Party = p0.PartyShortName } into pp
            from p in pp.DefaultIfEmpty()
            where b.VoteDate >= fromDt && b.VoteDate <= toDt
            select new { b, v, Majority = p!.MajorityBallotType };

        if (query.VoteType is { } voteType)
        {
            ballots = ballots.Where(x => x.v.TypeId == voteType);
        }

        if (query.KeywordId is { } keywordId)
        {
            ballots = ballots.Where(x => db.CaseSteps.Any(s => s.Id == x.v.CaseStepId && db.CaseKeywords.Any(ck => ck.CaseId == s.CaseId && ck.KeywordId == keywordId)));
        }

        if (query.ByPoliticians)
        {
            var ids = query.Politicians.ToArray();
            ballots = ballots.Where(x => ids.Contains(x.b.ActorId));
        }
        else if (query.Parties.Count > 0)
        {
            var parties = query.Parties.ToArray();
            ballots = ballots.Where(x => x.b.PartyShortName != null && parties.Contains(x.b.PartyShortName));
        }
        else
        {
            ballots = ballots.Where(x => x.b.PartyShortName != null);
        }

        // Group in SQL by entity and month (fine enough for every grouping); bucket further in memory.
        var byPoliticians = query.ByPoliticians;
        var grouped = (await ballots
            .GroupBy(x => new { ActorId = byPoliticians ? x.b.ActorId : 0, Party = byPoliticians ? null : x.b.PartyShortName, x.b.VoteDate.Year, x.b.VoteDate.Month, x.b.PeriodId })
            .Select(g => new
            {
                g.Key.ActorId,
                g.Key.Party,
                g.Key.Year,
                g.Key.Month,
                g.Key.PeriodId,
                Total = g.Count(),
                Absent = g.Count(x => x.b.BallotType == BallotType.Absent),
                For = g.Count(x => x.b.BallotType == BallotType.For),
                Against = g.Count(x => x.b.BallotType == BallotType.Against),
                Abstain = g.Count(x => x.b.BallotType == BallotType.Abstain),
                WithMajority = g.Count(x => x.b.BallotType != BallotType.Absent && x.Majority != null && x.b.BallotType == x.Majority),
                AgainstMajority = g.Count(x => x.b.BallotType != BallotType.Absent && x.Majority != null && x.b.BallotType != x.Majority),
                Votes = g.Select(x => x.b.VoteId).Distinct().Count(),
            })
            .ToListAsync(cancellationToken))
            .Select(r => new { Entity = byPoliticians ? r.ActorId.ToString(CultureInfo.InvariantCulture) : r.Party!, r.Year, r.Month, r.PeriodId, r.Total, r.Absent, r.For, r.Against, r.Abstain, r.WithMajority, r.AgainstMajority, r.Votes })
            .ToList();

        var names = await EntityNamesAsync(query, partyNames, cancellationToken);
        var entityOrder = query.ByPoliticians
            ? query.Politicians.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList()
            : query.Parties.Count > 0 ? query.Parties.ToList() : grouped.GroupBy(g => g.Entity).OrderByDescending(g => g.Sum(x => x.Total)).Select(g => g.Key).ToList();

        var series = new List<ExplorerSeries>();
        foreach (var entity in entityOrder)
        {
            var rows = grouped.Where(g => g.Entity == entity).ToList();
            var buckets = rows
                .GroupBy(r =>
                {
                    var date = new DateOnly(r.Year, r.Month, 1);
                    (string Title, DateOnly Start) period = periods.TryGetValue(r.PeriodId, out var found) ? found : ("?", date);
                    return ExplorerMath.Bucket(query.Grouping, date, period.Title, period.Start);
                })
                .Select(g =>
                {
                    var c = new ExplorerMath.Counts(g.Sum(r => r.Total), g.Sum(r => r.Absent), g.Sum(r => r.For), g.Sum(r => r.Against), g.Sum(r => r.Abstain),
                        g.Sum(r => r.WithMajority), g.Sum(r => r.AgainstMajority), g.Sum(r => r.Votes), g.Sum(r => r.AgainstMajority), 0);
                    var (value, basis) = ExplorerMath.Evaluate(query.Metric, c);
                    return new ExplorerPoint(g.Key.Label, g.Key.SortKey, value, basis);
                })
                .OrderBy(p => p.SortKey)
                .ToList();
            series.Add(new ExplorerSeries(entity, names.GetValueOrDefault(entity, entity), buckets));
        }

        if (series.Count == 0)
        {
            notes.Add("Ingen stemmer matcher valget.");
        }

        return series;
    }

    private async Task<Dictionary<string, string>> EntityNamesAsync(ExplorerQuery query, Dictionary<string, (string Name, bool IsIndependentGroup)> partyNames, CancellationToken cancellationToken)
    {
        if (!query.ByPoliticians)
        {
            return partyNames.ToDictionary(kv => kv.Key, kv => kv.Value.Name);
        }

        var ids = query.Politicians.ToArray();
        var people = await db.Actors.Where(a => ids.Contains(a.Id)).Select(a => new { a.Id, a.Name }).ToListAsync(cancellationToken);
        return people.ToDictionary(a => a.Id.ToString(CultureInfo.InvariantCulture), a => a.Name);
    }

    private async Task<List<ExplorerSeries>> QuestionSeriesAsync(
        ExplorerQuery query, DateTime fromDt, DateTime toDt,
        Dictionary<int, (string Title, DateOnly Start)> periods, Dictionary<string, (string Name, bool IsIndependentGroup)> partyNames,
        CancellationToken cancellationToken)
    {
        var questions = db.Questions.Where(q => q.AskedDate >= fromDt && q.AskedDate <= toDt);
        if (query.ByPoliticians)
        {
            var ids = query.Politicians.ToArray();
            questions = questions.Where(q => q.AskerId != null && ids.Contains(q.AskerId.Value));
        }
        else if (query.Parties.Count > 0)
        {
            var parties = query.Parties.ToArray();
            questions = questions.Where(q => q.AskerParty != null && parties.Contains(q.AskerParty));
        }
        else
        {
            questions = questions.Where(q => q.AskerParty != null);
        }

        var byPoliticians = query.ByPoliticians;
        var rows = (await questions.Select(q => new { q.AskerId, q.AskerParty, q.AskedDate, q.PeriodId }).ToListAsync(cancellationToken))
            .Select(q => new { Entity = byPoliticians ? q.AskerId!.Value.ToString(CultureInfo.InvariantCulture) : q.AskerParty!, q.AskedDate, q.PeriodId })
            .ToList();
        var names = await EntityNamesAsync(query, partyNames, cancellationToken);
        var entityOrder = query.ByPoliticians
            ? query.Politicians.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList()
            : query.Parties.Count > 0 ? query.Parties.ToList() : rows.GroupBy(r => r.Entity).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();

        return entityOrder.Select(entity =>
        {
            var points = rows.Where(r => r.Entity == entity)
                .GroupBy(r =>
                {
                    var date = DateOnly.FromDateTime(r.AskedDate!.Value);
                    (string Title, DateOnly Start) period = periods.TryGetValue(r.PeriodId, out var found) ? found : ("?", date);
                    return ExplorerMath.Bucket(query.Grouping, date, period.Title, period.Start);
                })
                .Select(g => new ExplorerPoint(g.Key.Label, g.Key.SortKey, g.Count(), g.Count()))
                .OrderBy(p => p.SortKey)
                .ToList();
            return new ExplorerSeries(entity, names.GetValueOrDefault(entity, entity), points);
        }).ToList();
    }
}
