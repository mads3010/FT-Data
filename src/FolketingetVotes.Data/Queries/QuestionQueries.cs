using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using FolketingetVotes.Data.Persistence.Views;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class QuestionQueries(FolketingetDbContext db) : IQuestionQueries
{
    public async Task<PagedResult<QuestionListItem>> SearchAsync(QuestionFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Filtered(filter);
        var total = await query.CountAsync(cancellationToken);
        var items = await (
            from q in query.OrderByDescending(q => q.AskedDate).ThenByDescending(q => q.CaseId).Skip((page - 1) * pageSize).Take(pageSize)
            join p in db.Periods on q.PeriodId equals p.Id
            join a0 in db.Actors on q.AskerId equals a0.Id into aa
            from a in aa.DefaultIfEmpty()
            join m0 in db.Actors on q.MinisterPersonId equals m0.Id into mm
            from m in mm.DefaultIfEmpty()
            select new QuestionListItem(q.CaseId, q.Number, q.Title, q.AskerId, a != null ? a.Name : null, q.AskerParty, q.MinisterTitle,
                q.MinisterPersonId, m != null ? m.Name : null, q.AskedDate, q.AnsweredDate, q.Oral, q.Withdrawn, p.Title, p.Code)).ToListAsync(cancellationToken);
        return new PagedResult<QuestionListItem>(items.OrderByDescending(i => i.AskedDate).ThenByDescending(i => i.CaseId).ToList(), page, pageSize, total);
    }

    public async Task<QuestionStats> GetStatsAsync(QuestionFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await Filtered(filter).Select(q => new { q.AskedDate, q.AnsweredDate, q.Oral, q.Withdrawn }).ToListAsync(cancellationToken);
        var days = rows.Where(r => r.AskedDate is not null && r.AnsweredDate is not null).Select(r => (r.AnsweredDate!.Value.Date - r.AskedDate!.Value.Date).TotalDays);
        return new QuestionStats(rows.Count, rows.Count(r => r.AnsweredDate is not null), rows.Count(r => r.Oral), rows.Count(r => r.Withdrawn), Median.Of(days));
    }

    public async Task<IReadOnlyList<MinisterQuestionRow>> GetByMinisterAsync(int? periodId, CancellationToken cancellationToken = default)
    {
        var query = db.Questions.AsQueryable();
        if (periodId is { } pid)
        {
            query = query.Where(q => q.PeriodId == pid);
        }

        var rows = await (
            from q in query
            join m0 in db.Actors on q.MinisterPersonId equals m0.Id into mm
            from m in mm.DefaultIfEmpty()
            where q.MinisterTitle != null
            select new { q.MinisterTitle, q.MinisterPersonId, MinisterName = m != null ? m.Name : null, q.AskedDate, q.AnsweredDate }).ToListAsync(cancellationToken);

        return rows.GroupBy(r => r.MinisterTitle!)
            .Select(g =>
            {
                var top = g.Where(r => r.MinisterPersonId != null).GroupBy(r => r.MinisterPersonId).OrderByDescending(x => x.Count()).FirstOrDefault();
                var days = g.Where(r => r.AskedDate is not null && r.AnsweredDate is not null).Select(r => (r.AnsweredDate!.Value.Date - r.AskedDate!.Value.Date).TotalDays);
                return new MinisterQuestionRow(g.Key, top?.Key, top?.First().MinisterName, g.Count(), Median.Of(days));
            })
            .OrderByDescending(r => r.Questions)
            .ToList();
    }

    private IQueryable<QuestionView> Filtered(QuestionFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var query = db.Questions.AsQueryable();
        if (filter.PeriodId is { } periodId)
        {
            query = query.Where(q => q.PeriodId == periodId);
        }

        if (filter.AskerId is { } asker)
        {
            query = query.Where(q => q.AskerId == asker);
        }

        if (filter.MinisterId is { } minister)
        {
            query = query.Where(q => q.MinisterPersonId == minister);
        }

        if (!string.IsNullOrWhiteSpace(filter.MinisterTitle))
        {
            query = query.Where(q => q.MinisterTitle == filter.MinisterTitle);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = VoteProjections.Pattern(filter.Query);
            query = query.Where(q => EF.Functions.ILike(q.Title, pattern) || EF.Functions.ILike(q.Number!, pattern));
        }

        return query;
    }
}
