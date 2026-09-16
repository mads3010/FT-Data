using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.PartyAccounts;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class DataQualityQueries(FolketingetDbContext db) : IDataQualityQueries
{
    public async Task<DataQualityReport> GetAsync(CancellationToken cancellationToken = default)
    {
        var sync = await db.SyncStates.OrderBy(s => s.EntityName)
            .Select(s => new SyncStateRow(s.EntityName, s.FullLoadCompleted, s.LastRunCompletedAt, s.LastUpdatedAt, s.RowsUpserted)).ToListAsync(cancellationToken);

        var conclusion = await db.Database.SqlQuery<ConclusionCheck>($"""
            WITH parsed AS (
              SELECT v.id,
                (regexp_match(v.conclusion, 'For stemte (\d+)'))[1]::int AS f,
                (regexp_match(v.conclusion, 'imod stemte (\d+)'))[1]::int AS a,
                (regexp_match(v.conclusion, 'hverken for eller imod stemte (\d+)'))[1]::int AS h
              FROM votes v WHERE v.conclusion ~ 'For stemte')
            SELECT COUNT(*)::int AS checked,
                   COUNT(*) FILTER (WHERE t.for_count = p.f AND t.against_count = p.a AND COALESCE(t.abstain_count, 0) = COALESCE(p.h, 0))::int AS matching
            FROM parsed p JOIN mv_vote_totals t ON t.vote_id = p.id
            """).FirstAsync(cancellationToken);

        var totals = await db.VoteTotals.Select(t => t.ForCount + t.AgainstCount + t.AbstainCount + t.AbsentCount).ToListAsync(cancellationToken);
        var perVote = totals.GroupBy(c => c).Select(g => new BallotsPerVoteRow(g.Key, g.Count())).OrderByDescending(r => r.Votes).ToList();

        return new DataQualityReport(
            sync,
            await db.Votes.CountAsync(cancellationToken),
            await db.Ballots.LongCountAsync(cancellationToken),
            conclusion.Checked,
            conclusion.Matching,
            await db.BallotParties.LongCountAsync(b => b.PartyShortName == null, cancellationToken),
            perVote,
            await db.PartyDonations.CountAsync(cancellationToken),
            await db.PartyDonations.CountAsync(d => d.DonorName == PartyAccountParser.UnreadableName, cancellationToken),
            await db.CurrentMembers.CountAsync(cancellationToken),
            await db.BallotParties.MaxAsync(b => (DateTime?)b.VoteDate, cancellationToken));
    }

    private sealed class ConclusionCheck
    {
        public int Checked { get; init; }
        public int Matching { get; init; }
    }
}
