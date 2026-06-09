using Microsoft.Extensions.AI;

namespace AiService;

/// <summary>
/// Deterministic chat client for tests/offline (ADR-006). It does not call a model; it returns a
/// fixed grounded-looking answer so the RAG pipeline (retrieve → prompt → answer + citations) can
/// be tested without an LLM. Real answer quality is the job of the evals (ADR-004).
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var answer = "Based on the retrieved context, here is a grounded answer [1].";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Streaming is not used by the RAG endpoint.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
        // Nothing to dispose.
    }
}
