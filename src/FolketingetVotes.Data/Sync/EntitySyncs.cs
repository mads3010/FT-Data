using System.Globalization;
using System.Text.RegularExpressions;
using FolketingetVotes.Core.Entities;
using FolketingetVotes.Core.Enums;
using FolketingetVotes.Data.Oda;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolketingetVotes.Data.Sync;

public sealed class PeriodSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<PeriodSync> logger)
    : EntitySync<OdaPeriode, Period>(oda, db, options, logger)
{
    public override string Name => "Periode";

    public override int Order => 10;

    protected override Period Map(OdaPeriode dto) => new()
    {
        Id = dto.Id,
        Code = dto.Code,
        Title = dto.Title,
        Type = dto.Type,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<Period> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
}

public sealed partial class ActorSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<ActorSync> logger)
    : EntitySync<OdaAktør, Actor>(oda, db, options, logger)
{
    public override string Name => "Aktør";

    public override int Order => 20;

    protected override int BatchSize => 200; // biographies are large

    protected override Actor Map(OdaAktør dto) => new()
    {
        Id = dto.Id,
        TypeId = (ActorType)dto.TypeId,
        GroupShortName = Clean(dto.GroupShortName),
        Name = Clean(dto.Name) ?? string.Join(' ', new[] { dto.FirstName, dto.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
        FirstName = Clean(dto.FirstName),
        LastName = Clean(dto.LastName),
        BiographyXml = string.IsNullOrWhiteSpace(dto.Biography) ? null : dto.Biography,
        PeriodId = dto.PeriodId,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        UpdatedAt = dto.UpdatedAt,
        PictureUrl = Extract(PictureRegex(), dto.Biography),
        BiographyPartyShortName = Extract(PartyShortRegex(), dto.Biography),
        Born = ParseBorn(Extract(BornRegex(), dto.Biography)),
        Sex = Extract(SexRegex(), dto.Biography),
    };

    private static DateOnly? ParseBorn(string? value)
        => DateOnly.TryParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    /// <summary>Upserts the actors and replaces each person's biography-derived party terms.</summary>
    protected override async Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<Actor> batch, CancellationToken cancellationToken)
    {
        var personIds = batch.Where(a => a.TypeId == ActorType.Person).Select(a => a.Id).Distinct().ToArray();
        if (personIds.Length > 0)
        {
            await db.BiographyMemberships.Where(m => personIds.Contains(m.PersonId)).ExecuteDeleteAsync(cancellationToken);
            foreach (var person in batch.Where(a => a.TypeId == ActorType.Person))
            {
                db.BiographyMemberships.AddRange(BiographyParser.ParseMemberships(person.Id, person.BiographyXml));
            }
        }

        await EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Extract(Regex regex, string? biography)
    {
        if (string.IsNullOrEmpty(biography))
        {
            return null;
        }

        var match = regex.Match(biography);
        return match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value) ? match.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex("<pictureMiRes>(.*?)</pictureMiRes>", RegexOptions.Singleline)]
    private static partial Regex PictureRegex();

    [GeneratedRegex("<partyShortname>(.*?)</partyShortname>", RegexOptions.Singleline)]
    private static partial Regex PartyShortRegex();

    [GeneratedRegex("<born>(.*?)</born>", RegexOptions.Singleline)]
    private static partial Regex BornRegex();

    [GeneratedRegex("<sex>(.*?)</sex>", RegexOptions.Singleline)]
    private static partial Regex SexRegex();
}

public sealed class ActorRelationSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<ActorRelationSync> logger)
    : EntitySync<OdaAktørAktør, ActorRelation>(oda, db, options, logger)
{
    public override string Name => "AktørAktør";

    public override int Order => 30;

    protected override bool LoadInParallelRanges => true;

    protected override ActorRelation Map(OdaAktørAktør dto) => new()
    {
        Id = dto.Id,
        FromActorId = dto.FromActorId,
        ToActorId = dto.ToActorId,
        RoleId = dto.RoleId,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<ActorRelation> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
}

public sealed class MeetingSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<MeetingSync> logger)
    : EntitySync<OdaMøde, Meeting>(oda, db, options, logger)
{
    public override string Name => "Møde";

    public override int Order => 40;

    protected override Meeting Map(OdaMøde dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title ?? string.Empty,
        Room = dto.Room,
        Number = dto.Number,
        Date = dto.Date,
        StatusId = dto.StatusId,
        TypeId = dto.TypeId,
        PeriodId = dto.PeriodId,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<Meeting> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
}

public sealed class CaseSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<CaseSync> logger)
    : EntitySync<OdaSag, ParliamentaryCase>(oda, db, options, logger)
{
    public override string Name => "Sag";

    public override int Order => 50;

    protected override bool LoadInParallelRanges => true;

    protected override int BatchSize => 500;

    protected override ParliamentaryCase Map(OdaSag dto) => new()
    {
        Id = dto.Id,
        TypeId = (CaseType)dto.TypeId,
        CategoryId = dto.CategoryId,
        StatusId = dto.StatusId,
        Title = dto.Title ?? string.Empty,
        ShortTitle = string.IsNullOrWhiteSpace(dto.ShortTitle) ? null : dto.ShortTitle.Trim(),
        Number = string.IsNullOrWhiteSpace(dto.Number) ? null : dto.Number.Trim(),
        NumberPrefix = string.IsNullOrWhiteSpace(dto.NumberPrefix) ? null : dto.NumberPrefix,
        NumberNumeric = ParseInt(dto.NumberNumeric),
        NumberPostfix = string.IsNullOrWhiteSpace(dto.NumberPostfix) ? null : dto.NumberPostfix,
        Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary,
        VotingConclusion = string.IsNullOrWhiteSpace(dto.VotingConclusion) ? null : dto.VotingConclusion,
        PeriodId = dto.PeriodId,
        LawNumber = ParseInt(dto.LawNumber),
        LawDate = dto.LawDate,
        RetsinformationUrl = string.IsNullOrWhiteSpace(dto.RetsinformationUrl) ? null : dto.RetsinformationUrl,
        IsBudgetCase = dto.IsBudgetCase ?? false,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<ParliamentaryCase> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);

    private static int? ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
}

public sealed class CaseStepSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<CaseStepSync> logger)
    : EntitySync<OdaSagstrin, CaseStep>(oda, db, options, logger)
{
    public override string Name => "Sagstrin";

    public override int Order => 60;

    protected override bool LoadInParallelRanges => true;

    protected override CaseStep Map(OdaSagstrin dto) => new()
    {
        Id = dto.Id,
        CaseId = dto.CaseId,
        Title = dto.Title ?? string.Empty,
        Date = dto.Date,
        TypeId = dto.TypeId,
        StatusId = dto.StatusId,
        FolketingstidendeUrl = string.IsNullOrWhiteSpace(dto.FolketingstidendeUrl) ? null : dto.FolketingstidendeUrl,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<CaseStep> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
}

public sealed class CaseActorSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<CaseActorSync> logger)
    : EntitySync<OdaSagAktør, CaseActor>(oda, db, options, logger)
{
    public override string Name => "SagAktør";

    public override int Order => 70;

    protected override bool LoadInParallelRanges => true;

    protected override CaseActor Map(OdaSagAktør dto) => new()
    {
        Id = dto.Id,
        CaseId = dto.CaseId,
        ActorId = dto.ActorId,
        RoleId = dto.RoleId,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<CaseActor> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
}

public sealed class KeywordSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<KeywordSync> logger)
    : EntitySync<OdaEmneord, Keyword>(oda, db, options, logger)
{
    public override string Name => "Emneord";

    public override int Order => 55;

    protected override bool LoadInParallelRanges => true;

    protected override Keyword Map(OdaEmneord dto) => new()
    {
        Id = dto.Id,
        TypeId = dto.TypeId,
        Name = (dto.Name ?? string.Empty).Trim(),
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<Keyword> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
}

public sealed class CaseKeywordSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<CaseKeywordSync> logger)
    : EntitySync<OdaEmneordSag, CaseKeyword>(oda, db, options, logger)
{
    public override string Name => "EmneordSag";

    public override int Order => 56;

    protected override bool LoadInParallelRanges => true;

    protected override CaseKeyword Map(OdaEmneordSag dto) => new()
    {
        Id = dto.Id,
        CaseId = dto.CaseId,
        KeywordId = dto.KeywordId,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<CaseKeyword> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
}

public sealed class VoteSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<VoteSync> logger)
    : EntitySync<OdaAfstemning, Vote>(oda, db, options, logger)
{
    public override string Name => "Afstemning";

    public override int Order => 80;

    protected override Vote Map(OdaAfstemning dto) => new()
    {
        Id = dto.Id,
        Number = dto.Number,
        Conclusion = string.IsNullOrWhiteSpace(dto.Conclusion) ? null : dto.Conclusion.Trim(),
        Passed = dto.Passed,
        Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
        TypeId = (VoteType)dto.TypeId,
        MeetingId = dto.MeetingId,
        CaseStepId = dto.CaseStepId,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<Vote> batch, CancellationToken cancellationToken)
        => EfUpsert.UpsertByIdAsync(db, batch, cancellationToken);
}

public sealed class BallotSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> db, IOptions<OdaOptions> options, ILogger<BallotSync> logger)
    : EntitySync<OdaStemme, Ballot>(oda, db, options, logger)
{
    public override string Name => "Stemme";

    public override int Order => 90;

    protected override bool LoadInParallelRanges => true;

    protected override int BatchSize => 5000;

    protected override Ballot Map(OdaStemme dto) => new()
    {
        Id = dto.Id,
        VoteId = dto.VoteId,
        ActorId = dto.ActorId,
        TypeId = (BallotType)dto.TypeId,
        UpdatedAt = dto.UpdatedAt,
    };

    protected override Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<Ballot> batch, CancellationToken cancellationToken)
        => BallotBulkUpsert.UpsertAsync(db, batch, cancellationToken);
}
