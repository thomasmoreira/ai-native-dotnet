using Microsoft.Extensions.AI;

namespace AiService;

internal sealed record AskRequest(string Question);

internal sealed record Citation(int Index, string Source);

internal sealed record AskResult(string Answer, IReadOnlyList<Citation> Citations);

/// <summary>
/// The generation half of RAG: embed the question, retrieve top-k chunks from pgvector, build a
/// grounded prompt that forces the model to answer from the context and cite sources, then call
/// the chat model. Returns the answer plus the citations (the retrieved chunks).
/// </summary>
internal sealed class AskService(
    ChunkRepository repository,
    IEmbeddingGenerator<string, Embedding<float>> embedder,
    IChatClient chatClient,
    CorpusTools tools)
{
    private const int TopK = 3;

    // The model may call list_sources to discover what it can cite (tool-calling). The
    // function-invocation middleware runs it and loops the result back automatically.
    private readonly ChatOptions _chatOptions = new()
    {
        Tools = [AIFunctionFactory.Create(tools.ListSourcesAsync)],
    };

    public async Task<AskResult> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var queryVector = await embedder.GenerateVectorAsync(question, cancellationToken: cancellationToken);
        var hits = await repository.SearchAsync(queryVector, TopK, cancellationToken);

        var context = string.Join(
            "\n\n",
            hits.Select((hit, i) => $"[{i + 1}] (source: {hit.Source})\n{hit.Content}"));

        var prompt =
            $"""
            You are a precise assistant. Answer the question using ONLY the context below.
            Cite the sources you use by their [number]. If the context does not contain the
            answer, say you don't know — do not invent facts.

            Context:
            {context}

            Question: {question}
            """;

        var response = await chatClient.GetResponseAsync(prompt, _chatOptions, cancellationToken);

        var citations = hits.Select((hit, i) => new Citation(i + 1, hit.Source)).ToList();
        return new AskResult(response.Text, citations);
    }
}
