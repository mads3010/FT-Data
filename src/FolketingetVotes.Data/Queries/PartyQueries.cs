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
                p.LastSeen)).ToListAsync(cancellationToken);
        return items.OrderByDescending(i => i.CurrentMembers).ThenByDescending(i => i.LastSeen).ThenBy(i => i.Name).ToList();
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

        return new PartyDetail(party.ShortName, party.Name, members, perPeriod, accountRows);
    }
}
