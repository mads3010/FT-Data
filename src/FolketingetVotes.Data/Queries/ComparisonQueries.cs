using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class ComparisonQueries(FolketingetDbContext db) : IComparisonQueries
{
    public async Task<ComparisonResult?> CompareAsync(int actorA, int actorB, int? periodId, CancellationToken cancellationToken = default)
    {
        var politicians = new PoliticianQueries(db);
        var a = await politicians.GetListItemAsync(actorA, cancellationToken);
        var b = await politicians.GetListItemAsync(actorB, cancellationToken);
        if (a is null || b is null || actorA == actorB)
        {
            return null;
        }

        var ballots = db.BallotParties.Where(x => x.ActorId == actorA || x.ActorId == actorB);
        if (periodId is { } pid)
        {
            ballots = ballots.Where(x => x.PeriodId == pid);
        }

        var rows = await ballots.Select(x => new { x.VoteId, x.ActorId, x.BallotType }).ToListAsync(cancellationToken);
        var byVote = rows.GroupBy(r => r.VoteId)
            .Select(g => new { VoteId = g.Key, A = g.FirstOrDefault(r => r.ActorId == actorA)?.BallotType, B = g.FirstOrDefault(r => r.ActorId == actorB)?.BallotType })
            .Where(x => x.A is not null && x.B is not null)
            .ToList();

        var bothPresent = byVote.Where(x => x.A != BallotType.Absent && x.B != BallotType.Absent).ToList();
        var agreed = bothPresent.Count(x => x.A == x.B);
        var differing = bothPresent.Where(x => x.A != x.B).Select(x => x.VoteId).ToHashSet();

        var differences = (await VoteProjections.Rows(db)
            .Where(v => differing.Contains(v.VoteId))
            .OrderByDescending(v => v.Date).ThenByDescending(v => v.VoteId)
            .Take(200).ToListAsync(cancellationToken))
            .Select(v => new ComparisonDifference(VoteProjections.ToItem(v), byVote.First(x => x.VoteId == v.VoteId).A!.Value, byVote.First(x => x.VoteId == v.VoteId).B!.Value))
            .ToList();

        return new ComparisonResult(a, b, byVote.Count, bothPresent.Count, agreed, differences);
    }
}
