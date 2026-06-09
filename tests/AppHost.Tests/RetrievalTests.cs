using System.Text.Json;

namespace AppHost.Tests;

/// <summary>
/// Verifies the retrieval plumbing end-to-end against real pgvector with a deterministic fake
/// embedder: ingest the bundled corpus, then search returns top-k chunks. Semantic quality is
/// the job of the evals (ADR-004), not of this deterministic test.
/// </summary>
public class RetrievalTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    [Fact]
    public async Task Ingest_then_search_returns_top_k_chunks()
    {
        using var client = fixture.App.CreateHttpClient("aiservice");

        using var ingestResponse = await client.PostAsync("/ingest", content: null);
        Assert.Equal(HttpStatusCode.OK, ingestResponse.StatusCode);

        using var ingestDoc = JsonDocument.Parse(await ingestResponse.Content.ReadAsStringAsync());
        var ingested = ingestDoc.RootElement.GetProperty("ingested").GetInt32();
        Assert.True(ingested >= 8, $"expected several chunks ingested, got {ingested}");

        using var searchResponse = await client.GetAsync("/search?q=transactional%20outbox");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        using var searchDoc = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
        var hits = searchDoc.RootElement;
        Assert.InRange(hits.GetArrayLength(), 1, 3);
        foreach (var hit in hits.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(hit.GetProperty("source").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(hit.GetProperty("content").GetString()));
        }
    }
}
