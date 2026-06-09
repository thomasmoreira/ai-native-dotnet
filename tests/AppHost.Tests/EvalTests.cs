using System.Text.Json;
using Xunit.Abstractions;

namespace AppHost.Tests;

/// <summary>
/// Retrieval eval: over a golden-set (question → the document that should answer it), measure
/// recall@3 with the real embedding model and gate on a threshold (ADR-004). Quality is measured,
/// not guessed — the same idea as the availability SLO gating the observability lab. Runs locally
/// (needs Ollama); CI is build-only.
/// </summary>
public class EvalTests(EvalFixture fixture, ITestOutputHelper output) : IClassFixture<EvalFixture>
{
    private const double RecallThreshold = 0.75;

    private static readonly (string Question, string ExpectedSource)[] GoldenSet =
    [
        ("How does the transactional outbox guarantee at-least-once delivery?", "01-distributed-consistency-lab"),
        ("What is an idempotent consumer inbox and how does it avoid duplicates?", "01-distributed-consistency-lab"),
        ("What are RED metrics and error-budget SLOs?", "02-observability-from-scratch"),
        ("How does trace to log correlation work in Grafana?", "02-observability-from-scratch"),
        ("How does service discovery work with Aspire ServiceDefaults?", "03-dotnet-aspire-reference"),
        ("What is the distributed trace crossing services in the dashboard?", "03-dotnet-aspire-reference"),
        ("What is retrieval augmented generation over pgvector?", "04-ai-native-dotnet"),
        ("How are evals used as a quality gate?", "04-ai-native-dotnet"),
    ];

    [Fact]
    public async Task Retrieval_recall_at_3_meets_threshold()
    {
        using var client = fixture.App.CreateHttpClient("aiservice");

        var hits = 0;
        foreach (var (question, expected) in GoldenSet)
        {
            using var response = await client.GetAsync($"/search?q={Uri.EscapeDataString(question)}");
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var sources = document.RootElement.EnumerateArray()
                .Select(hit => hit.GetProperty("source").GetString())
                .ToList();

            var recalled = sources.Contains(expected);
            if (recalled)
            {
                hits++;
            }

            output.WriteLine($"{(recalled ? "PASS" : "MISS")}  expected={expected,-32}  q=\"{question}\"");
        }

        var recall = (double)hits / GoldenSet.Length;
        output.WriteLine($"Retrieval recall@3: {hits}/{GoldenSet.Length} ({recall:P0}) — threshold {RecallThreshold:P0}");

        Assert.True(recall >= RecallThreshold, $"retrieval recall@3 {recall:P0} is below the {RecallThreshold:P0} gate");
    }
}
