using Microsoft.Extensions.AI;

namespace AiService;

/// <summary>
/// Deterministic chat client for tests/offline (ADR-006). It simulates a two-turn tool-calling
/// exchange so the function-invocation pipeline is exercised without a real model: when a tool is
/// offered it requests the call; once the tool result is present it returns a fixed final answer.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Second turn: a tool has already run — produce the final grounded answer.
        var hasToolResult = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any();
        if (hasToolResult)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Answer grounded with tool assistance [1].")));
        }

        // First turn: if a tool is offered, call it once (drives the function-invocation loop).
        if (options?.Tools is { Count: > 0 } tools && tools[0] is AIFunction function)
        {
            var call = new FunctionCallContent("call-1", function.Name);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
        }

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Based on the retrieved context, here is a grounded answer [1].")));
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
