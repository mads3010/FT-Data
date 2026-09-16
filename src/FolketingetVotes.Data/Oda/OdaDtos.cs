using System.Text.Json.Serialization;

namespace FolketingetVotes.Data.Oda;

/// <summary>A record from oda.ft.dk; every entity set has an integer id and an update timestamp.</summary>
public interface IOdaRecord
{
    int Id { get; }

    DateTime UpdatedAt { get; }
}

/// <summary>One page of an OData v3 JSON response.</summary>
public sealed class OdaPage<T>
{
    [JsonPropertyName("odata.count")]
    public string? Count { get; init; }

    [JsonPropertyName("odata.nextLink")]
    public string? NextLink { get; init; }

    [JsonPropertyName("value")]
    public IReadOnlyList<T> Value { get; init; } = [];

    public int? TotalCount => int.TryParse(Count, out var n) ? n : null;
}

public sealed record OdaPeriode(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("startdato")] DateTime StartDate,
    [property: JsonPropertyName("slutdato")] DateTime? EndDate,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("kode")] string Code,
    [property: JsonPropertyName("titel")] string Title,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

public sealed record OdaAktør(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("typeid")] int TypeId,
    [property: JsonPropertyName("gruppenavnkort")] string? GroupShortName,
    [property: JsonPropertyName("navn")] string? Name,
    [property: JsonPropertyName("fornavn")] string? FirstName,
    [property: JsonPropertyName("efternavn")] string? LastName,
    [property: JsonPropertyName("biografi")] string? Biography,
    [property: JsonPropertyName("periodeid")] int? PeriodId,
    [property: JsonPropertyName("startdato")] DateTime? StartDate,
    [property: JsonPropertyName("slutdato")] DateTime? EndDate,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

public sealed record OdaAktørAktør(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("fraaktørid")] int FromActorId,
    [property: JsonPropertyName("tilaktørid")] int ToActorId,
    [property: JsonPropertyName("rolleid")] int RoleId,
    [property: JsonPropertyName("startdato")] DateTime? StartDate,
    [property: JsonPropertyName("slutdato")] DateTime? EndDate,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

public sealed record OdaMøde(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("titel")] string? Title,
    [property: JsonPropertyName("lokale")] string? Room,
    [property: JsonPropertyName("nummer")] string? Number,
    [property: JsonPropertyName("dato")] DateTime Date,
    [property: JsonPropertyName("statusid")] int StatusId,
    [property: JsonPropertyName("typeid")] int TypeId,
    [property: JsonPropertyName("periodeid")] int PeriodId,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

public sealed record OdaSag(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("typeid")] int TypeId,
    [property: JsonPropertyName("kategoriid")] int? CategoryId,
    [property: JsonPropertyName("statusid")] int StatusId,
    [property: JsonPropertyName("titel")] string? Title,
    [property: JsonPropertyName("titelkort")] string? ShortTitle,
    [property: JsonPropertyName("nummer")] string? Number,
    [property: JsonPropertyName("nummerprefix")] string? NumberPrefix,
    [property: JsonPropertyName("nummernumerisk")] string? NumberNumeric,
    [property: JsonPropertyName("nummerpostfix")] string? NumberPostfix,
    [property: JsonPropertyName("resume")] string? Summary,
    [property: JsonPropertyName("afstemningskonklusion")] string? VotingConclusion,
    [property: JsonPropertyName("periodeid")] int PeriodId,
    [property: JsonPropertyName("lovnummer")] string? LawNumber,
    [property: JsonPropertyName("lovnummerdato")] DateTime? LawDate,
    [property: JsonPropertyName("retsinformationsurl")] string? RetsinformationUrl,
    [property: JsonPropertyName("statsbudgetsag")] bool? IsBudgetCase,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

public sealed record OdaSagstrin(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("titel")] string? Title,
    [property: JsonPropertyName("dato")] DateTime? Date,
    [property: JsonPropertyName("sagid")] int CaseId,
    [property: JsonPropertyName("typeid")] int TypeId,
    [property: JsonPropertyName("statusid")] int StatusId,
    [property: JsonPropertyName("folketingstidendeurl")] string? FolketingstidendeUrl,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

public sealed record OdaSagAktør(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("aktørid")] int ActorId,
    [property: JsonPropertyName("sagid")] int CaseId,
    [property: JsonPropertyName("rolleid")] int RoleId,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

public sealed record OdaAfstemning(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("nummer")] int Number,
    [property: JsonPropertyName("konklusion")] string? Conclusion,
    [property: JsonPropertyName("vedtaget")] bool Passed,
    [property: JsonPropertyName("kommentar")] string? Comment,
    [property: JsonPropertyName("mødeid")] int MeetingId,
    [property: JsonPropertyName("typeid")] int TypeId,
    [property: JsonPropertyName("sagstrinid")] int? CaseStepId,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

public sealed record OdaStemme(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("typeid")] int TypeId,
    [property: JsonPropertyName("afstemningid")] int VoteId,
    [property: JsonPropertyName("aktørid")] int ActorId,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord;

/// <summary>Shape shared by the code tables; the display column is named type/rolle/status/kategori depending on the set.</summary>
public sealed record OdaLookup(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("rolle")] string? Role,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("kategori")] string? Category,
    [property: JsonPropertyName("opdateringsdato")] DateTime UpdatedAt) : IOdaRecord
{
    public string Name => Type ?? Role ?? Status ?? Category ?? string.Empty;
}
