using AiService;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// pgvector data source. Vectors are bound as pgvector text literals cast with ::vector, so no
// Npgsql type-mapping package is needed (and we stay decoupled from its versioning).
builder.AddNpgsqlDataSource("vectordb");

// Embeddings: real Ollama when the AppHost wired it, else a deterministic fake (tests/offline).
if (builder.Configuration.GetConnectionString("embeddings") is not null)
{
    builder.AddOllamaApiClient("embeddings").AddEmbeddingGenerator();
}
else
{
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new FakeEmbeddingGenerator());
}

builder.Services.AddSingleton<ChunkRepository>();
builder.Services.AddSingleton<CorpusIngestor>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Index the bundled corpus (chunk → embed → store).
app.MapPost("/ingest", async (CorpusIngestor ingestor, CancellationToken ct) =>
    Results.Ok(new { ingested = await ingestor.IngestAsync(ct) }));

// Embed the query and return the top-k most similar chunks (the retrieval half of RAG).
app.MapGet("/search", async (string q, ChunkRepository repository, IEmbeddingGenerator<string, Embedding<float>> embedder, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest("Query parameter 'q' is required.");
    }

    var vector = await embedder.GenerateVectorAsync(q, cancellationToken: ct);
    var hits = await repository.SearchAsync(vector, k: 3, ct);
    return Results.Ok(hits);
});

await app.Services.GetRequiredService<ChunkRepository>().EnsureSchemaAsync();

app.Run();
