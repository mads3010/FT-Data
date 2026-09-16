using FolketingetVotes.Core.Entities;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace FolketingetVotes.Data.Sync;

/// <summary>Ballots are by far the largest set (~1.8M rows); they go through COPY into a staging table and a single merge.</summary>
internal static class BallotBulkUpsert
{
    public static async Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<Ballot> rows, CancellationToken cancellationToken)
    {
        var distinct = rows.GroupBy(b => b.Id).Select(g => g.Last()).ToList();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var create = new NpgsqlCommand(
            "CREATE TEMP TABLE ballots_stage (id int PRIMARY KEY, vote_id int NOT NULL, actor_id int NOT NULL, type_id int NOT NULL, updated_at timestamp NOT NULL) ON COMMIT DROP",
            connection, transaction))
        {
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var writer = await connection.BeginBinaryImportAsync(
            "COPY ballots_stage (id, vote_id, actor_id, type_id, updated_at) FROM STDIN (FORMAT BINARY)", cancellationToken))
        {
            foreach (var ballot in distinct)
            {
                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(ballot.Id, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(ballot.VoteId, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(ballot.ActorId, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync((int)ballot.TypeId, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(ballot.UpdatedAt, NpgsqlDbType.Timestamp, cancellationToken);
            }

            await writer.CompleteAsync(cancellationToken);
        }

        await using (var merge = new NpgsqlCommand(
            """
            INSERT INTO ballots (id, vote_id, actor_id, type_id, updated_at)
            SELECT id, vote_id, actor_id, type_id, updated_at FROM ballots_stage
            ON CONFLICT (id) DO UPDATE SET
                vote_id = EXCLUDED.vote_id,
                actor_id = EXCLUDED.actor_id,
                type_id = EXCLUDED.type_id,
                updated_at = EXCLUDED.updated_at
            """,
            connection, transaction))
        {
            await merge.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
