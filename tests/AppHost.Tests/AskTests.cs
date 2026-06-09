using System.Net.Http.Json;
using System.Text.Json;

namespace AppHost.Tests;

/// <summary>
/// Verifies the full RAG pipeline plumbing with deterministic fakes: POST /ask retrieves grounded
/// context and returns an answer plus citations. The fake chat client makes the answer fixed; what
/// is asserted here is the shape (answer + citations wired to the retrieved chunks), not quality.
/// </summary>
[Collection("aspire-app")]
public class AskTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Ask_returns_an_answer_with_citations()
    {
        using var client = fixture.App.CreateHttpClient("aiservice");

        using var response = await client.PostAsJsonAsync("/ask", new { question = "What is the transactional outbox?" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        // The fake chat client only returns this answer after the function-invocation pipeline
        // ran the offered tool and looped the result back — so this asserts tool-calling worked.
        Assert.Equal("Answer grounded with tool assistance [1].", root.GetProperty("answer").GetString());

        var citations = root.GetProperty("citations");
        Assert.InRange(citations.GetArrayLength(), 1, 3);
        foreach (var citation in citations.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(citation.GetProperty("source").GetString()));
            Assert.True(citation.GetProperty("index").GetInt32() >= 1);
        }
    }
}
