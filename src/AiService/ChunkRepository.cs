using System.Globalization;
using System.Text;
using Npgsql;

namespace AiService;

/// <summary>A retrieved chunk with its L2 distance to the query (smaller = more similar).</summary>
internal sealed record SearchHit(string Source, string Content, double Distance);

/// <summary>
/// Stores document chunks + their embeddings in pgvector and does similarity search. Vectors are
/// passed as the pgvector text literal (<c>[0.1,0.2,...]</c>) cast with <c>::vector</c> — this is
/// version-agnostic and avoids coupling to a specific Npgsql type-mapping library.
/// </summary>
internal sealed class ChunkRepository(NpgsqlDataSource dataSource)
{
    private const int Dimensions = 384; // all-minilm (and the deterministic fake) both emit 384-d.

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        // WaitFor in the AppHost holds startup until Postgres is healthy, but the first connection
        // can still race the container — retry until it accepts the DDL.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
                await using var command = new NpgsqlCommand(
                    "CREATE EXTENSION IF NOT EXISTS vector;" +
                    $"CREATE TABLE IF NOT EXISTS chunks (id bigserial PRIMARY KEY, source text NOT NULL, content text NOT NULL, embedding vector({Dimensions}));",
                    connection);
                await command.ExecuteNonQueryAsync(cancellationToken);
                return;
            }
            catch (NpgsqlException) when (attempt < 15)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("TRUNCATE chunks RESTART IDENTITY;", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddAsync(string source, string content, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO chunks (source, content, embedding) VALUES ($1, $2, $3::vector)", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = source });
        command.Parameters.Add(new NpgsqlParameter { Value = content });
        command.Parameters.Add(new NpgsqlParameter { Value = ToLiteral(embedding.Span) });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(ReadOnlyMemory<float> query, int k, CancellationToken cancellationToken = default)
    {
        var hits = new List<SearchHit>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT source, content, embedding <-> $1::vector AS distance FROM chunks ORDER BY embedding <-> $1::vector LIMIT $2",
            connection);
        command.Parameters.Add(new NpgsqlParameter { Value = ToLiteral(query.Span) });
        command.Parameters.Add(new NpgsqlParameter { Value = k });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hits.Add(new SearchHit(reader.GetString(0), reader.GetString(1), reader.GetDouble(2)));
        }

        return hits;
    }

    // pgvector text literal, e.g. [0.1,0.2,0.3] — invariant culture so the decimal separator is '.'.
    private static string ToLiteral(ReadOnlySpan<float> vector)
    {
        var builder = new StringBuilder(vector.Length * 8);
        builder.Append('[');
        for (var i = 0; i < vector.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(vector[i].ToString(CultureInfo.InvariantCulture));
        }

        builder.Append(']');
        return builder.ToString();
    }
}
