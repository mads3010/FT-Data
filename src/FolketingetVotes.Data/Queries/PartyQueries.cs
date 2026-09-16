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
        var today = DateOnly.FromDateTime(DateTime.Today);
        var items = await (
            from p in db.Parties
            select new PartyListItem(
                p.ShortName,
                p.Name,
                db.PartyMemberships.Where(pm => pm.PartyShortName == p.ShortName && pm.Source != PartyMembershipSource.BiographyParty && (pm.EndDate == null || pm.EndDate >= today)).Select(pm => pm.PersonId).Distinct().Count(),
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

        var today = DateOnly.FromDateTime(DateTime.Today);
        var members = await (
            from pm in db.PartyMemberships
            join a in db.Actors on pm.PersonId equals a.Id
            where pm.PartyShortName == shortName && pm.Source != PartyMembershipSource.BiographyParty && (pm.EndDate == null || pm.EndDate >= today)
            group new { a, pm } by new { a.Id, a.Name, a.PictureUrl } into g
            orderby g.Key.Name
            select new PartyMemberRow(g.Key.Id, g.Key.Name, g.Key.PictureUrl, g.Min(x => x.pm.StartDate))).ToListAsync(cancellationToken);

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
