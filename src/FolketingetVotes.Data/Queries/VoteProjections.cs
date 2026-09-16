using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;

namespace FolketingetVotes.Data.Queries;

/// <summary>
/// Intermediate projection of a vote in lists (vote + meeting date + case + totals). It uses an object
/// initialiser rather than the read-model constructor so EF Core can still translate filters and ordering
/// applied after the projection; <see cref="VoteProjections.ToItem"/> turns it into the read model.
/// </summary>
internal sealed class VoteListRow
{
    public int VoteId { get; init; }
    public int PeriodId { get; init; }
    public DateTime Date { get; init; }
    public VoteType Type { get; init; }
    public bool Passed { get; init; }
    public int? CaseId { get; init; }
    public string? CaseNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? StepTitle { get; init; }
    public int ForCount { get; init; }
    public int AgainstCount { get; init; }
    public int AbstainCount { get; init; }
    public int AbsentCount { get; init; }
}

internal static class VoteProjections
{
    public static IQueryable<VoteListRow> Rows(FolketingetDbContext db) =>
        from v in db.Votes
        join m in db.Meetings on v.MeetingId equals m.Id
        join t0 in db.VoteTotals on v.Id equals t0.VoteId into tt
        from t in tt.DefaultIfEmpty()
        join s0 in db.CaseSteps on v.CaseStepId equals s0.Id into ss
        from s in ss.DefaultIfEmpty()
        join c0 in db.Cases on s.CaseId equals c0.Id into cc
        from c in cc.DefaultIfEmpty()
        select new VoteListRow
        {
            VoteId = v.Id,
            PeriodId = m.PeriodId,
            Date = m.Date,
            Type = v.TypeId,
            Passed = v.Passed,
            CaseId = c != null ? c.Id : (int?)null,
            CaseNumber = c != null ? c.Number : null,
            Title = c != null ? (c.ShortTitle ?? c.Title) : (s != null ? s.Title : "Afstemning nr. " + v.Number),
            StepTitle = s != null ? s.Title : null,
            ForCount = t != null ? t.ForCount : 0,
            AgainstCount = t != null ? t.AgainstCount : 0,
            AbstainCount = t != null ? t.AbstainCount : 0,
            AbsentCount = t != null ? t.AbsentCount : 0,
        };

    public static VoteListItem ToItem(VoteListRow r) => new(
        r.VoteId, r.Date, r.Type, r.Passed, r.CaseId, r.CaseNumber, r.Title, r.StepTitle,
        r.ForCount, r.AgainstCount, r.AbstainCount, r.AbsentCount);

    public static string Pattern(string query) => "%" + query.Trim() + "%";
}
