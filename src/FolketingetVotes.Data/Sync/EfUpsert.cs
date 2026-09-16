using FolketingetVotes.Core.Entities;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Sync;

internal static class EfUpsert
{
    /// <summary>Insert-or-update a batch keyed by <see cref="IHasId.Id"/>; the change tracker is cleared afterwards.</summary>
    public static async Task UpsertByIdAsync<TEntity>(FolketingetDbContext db, IReadOnlyList<TEntity> batch, CancellationToken cancellationToken)
        where TEntity : class, IHasId
    {
        var distinct = batch.GroupBy(e => e.Id).Select(g => g.Last()).ToList();
        var ids = distinct.Select(e => e.Id).ToArray();
        var existing = await db.Set<TEntity>().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, cancellationToken);

        foreach (var incoming in distinct)
        {
            if (existing.TryGetValue(incoming.Id, out var current))
            {
                db.Entry(current).CurrentValues.SetValues(incoming);
            }
            else
            {
                db.Add(incoming);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }
}
