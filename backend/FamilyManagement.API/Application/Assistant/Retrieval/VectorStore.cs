using System.Data;
using Npgsql;

namespace FamilyManagement.API.Application.Assistant.Retrieval;

public sealed class VectorStore
{
    private readonly string _connectionString;

    public VectorStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task UpsertEmbeddingAsync(Guid familyId, string src, Guid? srcId, string chunk, float[] embedding, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
insert into ai_embeddings (family_id, src, src_id, chunk, embedding)
values (@family_id, @src, @src_id, @chunk, @embedding::vector)
";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("family_id", familyId);
        cmd.Parameters.AddWithValue("src", src);
        cmd.Parameters.AddWithValue("src_id", (object?)srcId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("chunk", chunk);
        // pgvector literal formatted as '[v1, v2, ...]'
        var literal = "[" + string.Join(",", embedding.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
        cmd.Parameters.AddWithValue("embedding", literal);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<(Guid id, string src, Guid? srcId, string chunk)>> SimilarAsync(Guid familyId, float[] query, int topK, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
select id, src, src_id, chunk
from ai_embeddings
where family_id = @family_id
order by embedding <#> @query
limit @k
";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("family_id", familyId);
        var qliteral = "[" + string.Join(",", query.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
        cmd.Parameters.AddWithValue("query", qliteral);
        cmd.Parameters.AddWithValue("k", topK);

        var results = new List<(Guid, string, Guid?, string)>();
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);
        while (await reader.ReadAsync(ct))
        {
            var id = reader.GetGuid(0);
            var src = reader.GetString(1);
            Guid? srcId = await reader.IsDBNullAsync(2, ct) ? (Guid?)null : reader.GetGuid(2);
            var chunk = reader.GetString(3);
            results.Add((id, src, srcId, chunk));
        }
        return results;
    }
}
