using System.Text.Json;

namespace AppHost.Tests;

/// <summary>
/// Verifies the retrieval plumbing against real pgvector with a deterministic fake embedder:
/// search returns top-k chunks from the ingested corpus. Semantic quality is the evals' job (ADR-004).
/// </summary>
[Collection("aspire-app")]
public class RetrievalTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Search_returns_top_k_chunks_from_the_corpus()
    {
        using var client = fixture.App.CreateHttpClient("aiservice");

        using var response = await client.GetAsync("/search?q=transactional%20outbox");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var hits = document.RootElement;
        Assert.InRange(hits.GetArrayLength(), 1, 3);
        foreach (var hit in hits.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(hit.GetProperty("source").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(hit.GetProperty("content").GetString()));
        }
    }
}
