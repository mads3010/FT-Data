using FolketingetVotes.Core.Entities;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class PartyQueries(FolketingetDbContext db) : IPartyQueries
{
    public async Task<IReadOnlyList<PartyListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await (
            from p in db.Parties
            select new PartyListItem(
                p.ShortName,
                p.Name,
                db.CurrentMembers.Count(cm => cm.PartyShortName == p.ShortName),
                p.FirstSeen,
                p.LastSeen,
                p.IsIndependentGroup)).ToListAsync(cancellationToken);
        return items.OrderByDescending(i => i.CurrentMembers).ThenByDescending(i => i.LastSeen).ThenBy(i => i.Name).ToList();
    }

    public async Task<IReadOnlyList<PartySwitchRow>> GetSwitchesAsync(CancellationToken cancellationToken = default)
    {
        var spans = await (
            from pm in db.PartyMemberships
            join a in db.Actors on pm.PersonId equals a.Id
            where pm.Source != PartyMembershipSource.BiographyParty
            orderby pm.PersonId, pm.StartDate
            select new { pm.PersonId, a.Name, pm.PartyShortName, pm.StartDate, pm.EndDate }).ToListAsync(cancellationToken);

        return spans.GroupBy(s => s.PersonId)
            .SelectMany(g => PartySwitches.From(g.Key, g.First().Name,
                PoliticianQueries.MergeConsecutive(g.Select(s => new PartyMembershipRow(s.PartyShortName, s.PartyShortName, s.StartDate, s.EndDate)).ToList())))
            .OrderByDescending(s => s.Date).ThenBy(s => s.Name)
            .ToList();
    }

    public async Task<PartyComparison?> CompareAsync(string partyA, string partyB, int? periodId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var names = await db.Parties.Where(p => p.ShortName == partyA || p.ShortName == partyB).ToDictionaryAsync(p => p.ShortName, p => p.Name, cancellationToken);
        if (names.Count != 2)
        {
            return null;
        }

        var rows = db.VotePartyBreakdowns.Where(b => (b.PartyShortName == partyA || b.PartyShortName == partyB) && b.MajorityBallotType != null);
        if (periodId is { } pid)
        {
            rows = rows.Where(b => db.Votes.Any(v => v.Id == b.VoteId && db.Meetings.Any(m => m.Id == v.MeetingId && m.PeriodId == pid)));
        }

        var majorities = await rows.Select(b => new { b.VoteId, b.PartyShortName, b.MajorityBallotType }).ToListAsync(cancellationToken);
        var byVote = majorities.GroupBy(m => m.VoteId)
            .Select(g => new { VoteId = g.Key, A = g.FirstOrDefault(x => x.PartyShortName == partyA)?.MajorityBallotType, B = g.FirstOrDefault(x => x.PartyShortName == partyB)?.MajorityBallotType })
            .Where(x => x.A is not null && x.B is not null)
            .ToList();
        var differing = byVote.Where(x => x.A != x.B).ToDictionary(x => x.VoteId, x => (x.A!.Value, x.B!.Value));

        var ids = differing.Keys.ToArray();
        var query = VoteProjections.Rows(db).Where(v => ids.Contains(v.VoteId));
        var total = ids.Length;
        var pageRows = await query.OrderByDescending(v => v.Date).ThenByDescending(v => v.VoteId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var differences = pageRows.Select(r => new PartyDifferenceRow(VoteProjections.ToItem(r), differing[r.VoteId].Item1, differing[r.VoteId].Item2)).ToList();

        return new PartyComparison(partyA, partyB, names[partyA], names[partyB], byVote.Count, byVote.Count - differing.Count,
            new PagedResult<PartyDifferenceRow>(differences, page, pageSize, total));
    }

    public async Task<PartyDetail?> GetAsync(string shortName, CancellationToken cancellationToken = default)
    {
        var party = await db.Parties.AsNoTracking().FirstOrDefaultAsync(p => p.ShortName == shortName, cancellationToken);
        if (party is null)
        {
            return null;
        }

        var current = await (
            from cm in db.CurrentMembers
            join a in db.Actors on cm.PersonId equals a.Id
            where cm.PartyShortName == shortName
            orderby a.Name
            select new { a.Id, a.Name, a.PictureUrl, cm.StartDate }).ToListAsync(cancellationToken);

        // "Since" is the start of the member's unbroken run in this group, not the current session's start.
        var ids = current.Select(c => c.Id).ToArray();
        var spans = await db.PartyMemberships.AsNoTracking()
            .Where(pm => ids.Contains(pm.PersonId) && pm.Source != PartyMembershipSource.BiographyParty)
            .OrderBy(pm => pm.PersonId).ThenBy(pm => pm.StartDate)
            .Select(pm => new { pm.PersonId, pm.PartyShortName, pm.StartDate, pm.EndDate })
            .ToListAsync(cancellationToken);
        var since = spans.GroupBy(s => s.PersonId).ToDictionary(
            g => g.Key,
            g => PoliticianQueries.MergeConsecutive(g.Select(s => new PartyMembershipRow(s.PartyShortName, s.PartyShortName, s.StartDate, s.EndDate)).ToList())
                .LastOrDefault(m => m.PartyShortName == shortName)?.StartDate);
        var members = current.Select(c => new PartyMemberRow(c.Id, c.Name, c.PictureUrl, since.GetValueOrDefault(c.Id) ?? c.StartDate)).ToList();

        var perPeriod = await (
            from s in db.PartyStats
            join p in db.Periods on s.PeriodId equals p.Id
            where s.PartyShortName == shortName
            orderby p.StartDate descending
            select new PartyPeriodStatsRow(p.Id, p.Title, p.StartDate, s.Members, s.Ballots, s.PresentBallots, s.WithMajority, s.AgainstMajority)).ToListAsync(cancellationToken);

        var accounts = await db.PartyAccounts.AsNoTracking()
            .Where(a => a.PartyShortName == shortName)
            .Include(a => a.Donations)
            .OrderByDescending(a => a.Year)
            .ToListAsync(cancellationToken);
        var accountRows = accounts.Select(a => new PartyAccountRow(
            a.Year,
            a.SourceFile,
            a.Donations.OrderByDescending(d => d.Amount ?? 0).ThenBy(d => d.DonorName)
                .Select(d => new DonationRow(d.DonorName, d.DonorAddress, d.Amount, d.Note, d.SourcePage, d.RawText)).ToList())).ToList();

        return new PartyDetail(party.ShortName, party.Name, members, perPeriod, accountRows, party.IsIndependentGroup);
    }
}
