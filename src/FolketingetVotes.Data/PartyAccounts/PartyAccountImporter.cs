using System.Globalization;
using System.Text.RegularExpressions;
using FolketingetVotes.Core.Entities;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FolketingetVotes.Data.PartyAccounts;

public sealed record PartyAccountImportResult(string File, int Year, int Parties, int Donations);

/// <summary>
/// Imports one or more party-account PDFs (downloaded manually from ft.dk, which blocks scripted access)
/// into <c>party_accounts</c> / <c>party_donations</c>. Re-importing a file replaces its rows.
/// </summary>
public sealed partial class PartyAccountImporter(IDbContextFactory<FolketingetDbContext> dbFactory, ILogger<PartyAccountImporter> logger)
{
    public async Task<PartyAccountImportResult> ImportAsync(string path, int? year = null, CancellationToken cancellationToken = default)
    {
        var resolvedYear = year ?? YearFromFileName(path)
            ?? throw new ArgumentException($"Could not determine the accounting year from '{path}'. Pass --year.", nameof(path));

        var pages = PartyAccountPdfReader.Read(path);
        var parsed = PartyAccountParser.Parse(pages);
        var fileName = Path.GetFileName(path);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var donationCount = 0;
        foreach (var account in parsed)
        {
            var existing = await db.PartyAccounts.Include(a => a.Donations)
                .FirstOrDefaultAsync(a => a.Year == resolvedYear && a.PartyName == account.PartyName, cancellationToken);
            if (existing is not null)
            {
                db.PartyDonations.RemoveRange(existing.Donations);
            }
            else
            {
                existing = new PartyAccount { Year = resolvedYear, PartyName = account.PartyName, SourceFile = fileName };
                db.PartyAccounts.Add(existing);
            }

            existing.PartyShortName = PartyAccountParser.ShortNameFor(account.PartyName);
            existing.SourceFile = fileName;
            existing.SourcePage = account.FirstPage;
            existing.ImportedAt = DateTime.UtcNow;
            existing.Donations = account.Donations.Select(d => new PartyDonation
            {
                DonorName = d.DonorName,
                DonorAddress = d.DonorAddress,
                Amount = d.Amount,
                Note = d.Note,
                SourcePage = d.PageNumber,
                RawText = d.RawText,
            }).ToList();
            donationCount += existing.Donations.Count;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Imported {File}: {Parties} parties, {Donations} disclosed contributions", fileName, parsed.Count, donationCount);
        return new PartyAccountImportResult(fileName, resolvedYear, parsed.Count, donationCount);
    }

    internal static int? YearFromFileName(string path)
    {
        var match = YearRegex().Match(Path.GetFileNameWithoutExtension(path));
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : null;
    }

    [GeneratedRegex(@"(19|20)\d{2}")]
    private static partial Regex YearRegex();
}
