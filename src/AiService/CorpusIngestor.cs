using Microsoft.Extensions.AI;

namespace AiService;

/// <summary>Reads the bundled markdown corpus, chunks it, embeds each chunk and stores it in pgvector.</summary>
internal sealed class CorpusIngestor(
    ChunkRepository repository,
    IEmbeddingGenerator<string, Embedding<float>> embedder)
{
    public async Task<int> IngestAsync(CancellationToken cancellationToken = default)
    {
        await repository.EnsureSchemaAsync(cancellationToken);
        await repository.ClearAsync(cancellationToken);

        var directory = Path.Combine(AppContext.BaseDirectory, "data");
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(directory, "*.md"))
        {
            var source = Path.GetFileNameWithoutExtension(file);
            var text = await File.ReadAllTextAsync(file, cancellationToken);

            foreach (var chunk in Chunk(text))
            {
                var vector = await embedder.GenerateVectorAsync(chunk, cancellationToken: cancellationToken);
                await repository.AddAsync(source, chunk, vector, cancellationToken);
                count++;
            }
        }

        return count;
    }

    // Naive chunking: split on blank lines (paragraphs) and keep the non-trivial ones. Good enough
    // for the lab; a production splitter would respect headings, token budgets and overlap.
    private static IEnumerable<string> Chunk(string text) =>
        text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(paragraph => paragraph.Length > 30);
}
