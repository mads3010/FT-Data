using FolketingetVotes.Data.PartyAccounts;
using FolketingetVotes.Data.Persistence;
using FolketingetVotes.Data.Stats;
using FolketingetVotes.Data.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FolketingetVotes.Ingest;

/// <summary>Dispatches the command-line verbs. Kept dependency-free on purpose; the surface is small.</summary>
internal sealed class CommandRunner(
    IDbContextFactory<FolketingetDbContext> dbFactory,
    SyncOrchestrator sync,
    StatsRefresher stats,
    PartyAccountImporter partyAccounts,
    ILogger<CommandRunner> logger)
{
    private const string Usage = """
        Usage: dotnet run --project src/FolketingetVotes.Ingest -- <command> [options]

        Commands
          migrate                          Apply pending EF Core migrations and (re)build statistics views.
          sync [--full] [--only A,B]       Sync from oda.ft.dk. First run is always a full load; later runs are
                                           incremental unless --full. --only limits to named entity sets
                                           (Lookups, Periode, Aktør, AktørAktør, Møde, Sag, Sagstrin, SagAktør, Afstemning, Stemme).
               [--no-stats]                Skip the statistics refresh afterwards.
          refresh-stats                    Rebuild parties, party memberships and the mv_* materialized views.
          import-party-accounts <pdf...>   Parse party-account PDFs (partiregnskaber) into the database.
               [--year N]                  Accounting year when it cannot be read from the file name.
          inspect-party-accounts <pdf>     Print what the parser finds in a file (sections, donors, pages) without importing.
          status                           Print sync bookkeeping and row counts.
        """;

    public async Task<int> RunAsync(string[] args)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            return args.Length == 0 ? PrintUsage() : await DispatchAsync(args, cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancelled.");
            return 130;
        }
    }

    private async Task<int> DispatchAsync(string[] args, CancellationToken cancellationToken)
    {
        var options = args.Skip(1).ToList();
        switch (args[0])
        {
            case "migrate":
                await MigrateAsync(cancellationToken);
                return 0;
            case "sync":
                await SyncAsync(options, cancellationToken);
                return 0;
            case "refresh-stats":
                await stats.RefreshAsync(cancellationToken);
                return 0;
            case "import-party-accounts":
                return await ImportPartyAccountsAsync(options, cancellationToken);
            case "inspect-party-accounts":
                return InspectPartyAccounts(options);
            case "status":
                await StatusAsync(cancellationToken);
                return 0;
            default:
                return PrintUsage();
        }
    }

    private static int PrintUsage()
    {
        Console.WriteLine(Usage);
        return 1;
    }

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        logger.LogInformation("Applying migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        await stats.RefreshAsync(cancellationToken);
        logger.LogInformation("Database is up to date.");
    }

    private async Task SyncAsync(List<string> options, CancellationToken cancellationToken)
    {
        var mode = options.Remove("--full") ? SyncMode.Full : SyncMode.Incremental;
        var refresh = !options.Remove("--no-stats");
        HashSet<string>? only = null;
        var onlyIndex = options.IndexOf("--only");
        if (onlyIndex >= 0 && onlyIndex + 1 < options.Count)
        {
            only = options[onlyIndex + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unknown = only.Except(sync.EntityNames, StringComparer.OrdinalIgnoreCase).ToList();
            if (unknown.Count > 0)
            {
                throw new ArgumentException($"Unknown entity set(s): {string.Join(", ", unknown)}. Known: {string.Join(", ", sync.EntityNames)}");
            }
        }

        var results = await sync.RunAsync(mode, only, refresh, cancellationToken);
        foreach (var result in results)
        {
            Console.WriteLine($"{result.Name,-12} {result.Rows,10} rows  {result.Elapsed:hh\\:mm\\:ss}");
        }
    }

    private async Task<int> ImportPartyAccountsAsync(List<string> options, CancellationToken cancellationToken)
    {
        int? year = null;
        var yearIndex = options.IndexOf("--year");
        if (yearIndex >= 0 && yearIndex + 1 < options.Count)
        {
            year = int.Parse(options[yearIndex + 1], System.Globalization.CultureInfo.InvariantCulture);
            options.RemoveRange(yearIndex, 2);
        }

        if (options.Count == 0)
        {
            Console.WriteLine("Give at least one PDF path.");
            return 1;
        }

        foreach (var path in options)
        {
            var result = await partyAccounts.ImportAsync(path, year, cancellationToken);
            Console.WriteLine($"{result.File}: year {result.Year}, {result.Parties} parties, {result.Donations} disclosed contributions");
        }

        return 0;
    }

    private static int InspectPartyAccounts(List<string> options)
    {
        if (options.Count == 0)
        {
            Console.WriteLine("Give a PDF or .ocr.txt path.");
            return 1;
        }

        var pages = FolketingetVotes.Data.PartyAccounts.PartyAccountPdfReader.Read(options[0]);
        Console.WriteLine($"{pages.Count} pages");
        foreach (var account in FolketingetVotes.Data.PartyAccounts.PartyAccountParser.Parse(pages))
        {
            Console.WriteLine();
            Console.WriteLine($"## {account.PartyName} (short name {FolketingetVotes.Data.PartyAccounts.PartyAccountParser.ShortNameFor(account.PartyName) ?? "-"}, from page {account.FirstPage}): {account.Donations.Count} rows");
            foreach (var d in account.Donations)
            {
                var amount = d.Amount is null ? string.Empty : $"  [{d.Amount:N0}]";
                Console.WriteLine($"  p{d.PageNumber,-4} {d.DonorName} | {d.DonorAddress}{amount}");
            }
        }

        return 0;
    }

    private async Task StatusAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        Console.WriteLine($"{"Entity",-12} {"Full",-5} {"Last id",10} {"Checkpoint (opdateringsdato)",-30} {"Last run",-22} {"Rows"}");
        foreach (var state in await db.SyncStates.OrderBy(s => s.EntityName).ToListAsync(cancellationToken))
        {
            Console.WriteLine($"{state.EntityName,-12} {(state.FullLoadCompleted ? "yes" : "no"),-5} {state.LastId,10} {state.LastUpdatedAt,-30:yyyy-MM-dd HH:mm:ss} {state.LastRunCompletedAt,-22:yyyy-MM-dd HH:mm}Z {state.RowsUpserted}");
        }

        Console.WriteLine();
        Console.WriteLine($"actors {await db.Actors.CountAsync(cancellationToken)}, cases {await db.Cases.CountAsync(cancellationToken)}, votes {await db.Votes.CountAsync(cancellationToken)}, ballots {await db.Ballots.LongCountAsync(cancellationToken)}, party memberships {await db.PartyMemberships.CountAsync(cancellationToken)}");
    }
}
