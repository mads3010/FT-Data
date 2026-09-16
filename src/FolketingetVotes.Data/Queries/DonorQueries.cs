using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.PartyAccounts;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

/// <summary>Donor-centric view over the imported party accounts (a few hundred rows per year; grouped in memory).</summary>
internal sealed class DonorQueries(FolketingetDbContext db) : IDonorQueries
{
    public async Task<PagedResult<DonorListItem>> SearchAsync(DonorFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var rows = await LoadRowsAsync(cancellationToken);
        var members = await MemberNamesAsync(cancellationToken);

        var groups = rows
            .Where(r => r.DonorName != PartyAccountParser.UnreadableName)
            .GroupBy(r => DonorNames.Normalize(r.DonorName))
            .Where(g => g.Key.Length > 0)
            .Select(g => new DonorListItem(
                g.Key,
                g.GroupBy(r => r.DonorName).OrderByDescending(x => x.Count()).First().Key,
                g.Where(r => r.PartyShortName is not null).Select(r => r.PartyShortName!).Distinct().OrderBy(p => p).ToList(),
                g.Min(r => r.Year),
                g.Max(r => r.Year),
                g.Count(),
                g.Any(r => r.Amount is not null) ? g.Sum(r => r.Amount ?? 0) : null,
                members.GetValueOrDefault(g.Key)));

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var needle = DonorNames.Normalize(filter.Query);
            groups = groups.Where(d => d.Key.Contains(needle, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(filter.PartyShortName))
        {
            groups = groups.Where(d => d.Parties.Contains(filter.PartyShortName, StringComparer.OrdinalIgnoreCase));
        }

        if (filter.Year is { } year)
        {
            groups = groups.Where(d => rows.Any(r => r.Year == year && DonorNames.Normalize(r.DonorName) == d.Key));
        }

        var list = groups.OrderByDescending(d => d.Parties.Count).ThenByDescending(d => d.RowCount).ThenBy(d => d.DisplayName).ToList();
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<DonorListItem>(items, page, pageSize, list.Count);
    }

    public async Task<DonorDetail?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var rows = (await LoadRowsAsync(cancellationToken)).Where(r => DonorNames.Normalize(r.DonorName) == key).ToList();
        if (rows.Count == 0)
        {
            return null;
        }

        var members = await MemberNamesAsync(cancellationToken);
        var display = rows.GroupBy(r => r.DonorName).OrderByDescending(g => g.Count()).First().Key;
        return new DonorDetail(key, display, members.GetValueOrDefault(key),
            rows.OrderByDescending(r => r.Year).ThenBy(r => r.PartyName)
                .Select(r => new DonorContributionRow(r.Year, r.PartyName, r.PartyShortName, r.DonorAddress, r.Amount, r.Note, r.SourcePage, r.SourceFile, r.RawText)).ToList());
    }

    public async Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken cancellationToken = default)
        => await db.PartyAccounts.Select(a => a.Year).Distinct().OrderByDescending(y => y).ToListAsync(cancellationToken);

    private Task<List<Row>> LoadRowsAsync(CancellationToken cancellationToken) =>
        (from d in db.PartyDonations
         join a in db.PartyAccounts on d.PartyAccountId equals a.Id
         select new Row(a.Year, a.PartyName, a.PartyShortName, a.SourceFile, d.DonorName, d.DonorAddress, d.Amount, d.Note, d.SourcePage, d.RawText))
        .AsNoTracking().ToListAsync(cancellationToken);

    /// <summary>Normalised names of everyone who has cast a ballot, to flag donors who are (or were) members themselves.</summary>
    private async Task<Dictionary<string, int>> MemberNamesAsync(CancellationToken cancellationToken)
    {
        var people = await (from a in db.Actors where a.TypeId == ActorType.Person && db.PoliticianStats.Any(s => s.ActorId == a.Id) select new { a.Id, a.Name }).ToListAsync(cancellationToken);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var p in people)
        {
            result.TryAdd(DonorNames.Normalize(p.Name), p.Id);
        }

        return result;
    }

    private sealed record Row(int Year, string PartyName, string? PartyShortName, string SourceFile, string DonorName, string? DonorAddress, decimal? Amount, string? Note, int? SourcePage, string RawText);
}
