using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class TopicQueries(FolketingetDbContext db) : ITopicQueries
{
    public async Task<PagedResult<TopicListItem>> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q =
            from t in db.TopicStats
            join k in db.Keywords on t.KeywordId equals k.Id
            where t.VoteCount > 0
            select new { k, t };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = VoteProjections.Pattern(query);
            q = q.Where(x => EF.Functions.ILike(x.k.Name, pattern));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.t.VoteCount).ThenBy(x => x.k.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TopicListItem(x.k.Id, x.k.Name, x.k.TypeId, x.t.CaseCount, x.t.VoteCount))
            .ToListAsync(cancellationToken);
        return new PagedResult<TopicListItem>(items, page, pageSize, total);
    }

    public async Task<TopicDetail?> GetAsync(int keywordId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var keyword = await db.Keywords.AsNoTracking().FirstOrDefaultAsync(k => k.Id == keywordId, cancellationToken);
        if (keyword is null)
        {
            return null;
        }

        var caseIds = db.CaseKeywords.Where(ck => ck.KeywordId == keywordId).Select(ck => ck.CaseId);
        var caseCount = await caseIds.CountAsync(cancellationToken);

        var votesQuery = VoteProjections.Rows(db).Where(v => v.CaseId != null && caseIds.Contains(v.CaseId.Value));
        var voteTotal = await votesQuery.CountAsync(cancellationToken);
        var votes = (await votesQuery.OrderByDescending(v => v.Date).ThenByDescending(v => v.VoteId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken)).Select(VoteProjections.ToItem).ToList();

        // Party majorities in final-passage votes on the topic's cases.
        var finalVoteIds =
            from v in db.Votes
            join s in db.CaseSteps on v.CaseStepId equals s.Id
            where v.TypeId == VoteType.FinalPassage && caseIds.Contains(s.CaseId)
            select v.Id;
        var positions = await (
            from b in db.VotePartyBreakdowns
            join p in db.Parties on b.PartyShortName equals p.ShortName
            where finalVoteIds.Contains(b.VoteId) && b.MajorityBallotType != null
            group b by new { p.ShortName, p.Name } into g
            select new PartyTopicPosition(
                g.Key.ShortName,
                g.Key.Name,
                g.Count(x => x.MajorityBallotType == BallotType.For),
                g.Count(x => x.MajorityBallotType == BallotType.Against),
                g.Count(x => x.MajorityBallotType == BallotType.Abstain))).ToListAsync(cancellationToken);

        return new TopicDetail(keyword.Id, keyword.Name, keyword.TypeId, caseCount,
            positions.OrderByDescending(p => p.Total).ThenBy(p => p.PartyShortName).ToList(),
            new PagedResult<VoteListItem>(votes, page, pageSize, voteTotal));
    }
}
