using FolketingetVotes.Core.Entities;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class CompositionQueries(FolketingetDbContext db) : ICompositionQueries
{
    public async Task<CompositionReport> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var members = await (
            from cm in db.CurrentMembers
            join a in db.Actors on cm.PersonId equals a.Id
            join p0 in db.Parties on cm.PartyShortName equals p0.ShortName into pp
            from p in pp.DefaultIfEmpty()
            select new
            {
                cm.PersonId,
                cm.PartyShortName,
                PartyName = p != null ? p.Name : cm.PartyShortName,
                a.Born,
                a.Sex,
                FirstMembership = db.PartyMemberships.Where(pm => pm.PersonId == cm.PersonId && pm.Source != PartyMembershipSource.BiographyParty).Min(pm => (DateOnly?)pm.StartDate),
            }).ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        CompositionRow Row(string shortName, string name, IEnumerable<dynamic> group)
        {
            var list = group.ToList();
            return new CompositionRow(
                shortName, name, list.Count,
                list.Count(m => (string?)m.Sex == "Kvinde"),
                list.Count(m => (string?)m.Sex == "Mand"),
                list.Count(m => (string?)m.Sex != "Kvinde" && (string?)m.Sex != "Mand"),
                Median.Of(list.Where(m => (DateOnly?)m.Born != null).Select(m => Years((DateOnly)m.Born!, today))),
                Median.Of(list.Where(m => (DateOnly?)m.FirstMembership != null).Select(m => Years((DateOnly)m.FirstMembership!, today))));
        }

        var rows = members.GroupBy(m => new { m.PartyShortName, m.PartyName })
            .Select(g => Row(g.Key.PartyShortName, g.Key.PartyName, g))
            .OrderByDescending(r => r.Members).ThenBy(r => r.PartyShortName)
            .ToList();
        return new CompositionReport(today, rows, Row("Alle", "Folketinget", members));
    }

    private static double Years(DateOnly from, DateOnly to) => (to.DayNumber - from.DayNumber) / 365.25;
}
