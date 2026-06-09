using System.ComponentModel;

namespace AiService;

/// <summary>
/// Functions the chat model can call (tool-calling). Exposed to the LLM via AIFunction; the
/// Microsoft.Extensions.AI function-invocation middleware runs them and feeds the result back.
/// </summary>
internal sealed class CorpusTools(ChunkRepository repository)
{
    [Description("Lists the source documents currently indexed and available to cite.")]
    public Task<string[]> ListSourcesAsync() => repository.ListSourcesAsync();
}
