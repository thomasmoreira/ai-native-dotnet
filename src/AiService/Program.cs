using AiService;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// pgvector data source. Vectors are bound as pgvector text literals cast with ::vector, so no
// Npgsql type-mapping package is needed (and we stay decoupled from its versioning).
builder.AddNpgsqlDataSource("vectordb");

// AI providers: real Ollama when the AppHost wired it, else deterministic fakes (tests/offline).
if (builder.Configuration.GetConnectionString("embeddings") is not null)
{
    builder.AddOllamaApiClient("embeddings").AddEmbeddingGenerator();
    builder.AddOllamaApiClient("chat").AddChatClient().UseFunctionInvocation();
}
else
{
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new FakeEmbeddingGenerator());
    builder.Services.AddChatClient(new FakeChatClient()).UseFunctionInvocation();
}

builder.Services.AddSingleton<ChunkRepository>();
builder.Services.AddSingleton<CorpusTools>();
builder.Services.AddSingleton<CorpusIngestor>();
builder.Services.AddSingleton<AskService>();

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

// Full RAG: retrieve grounded context and answer the question with citations.
app.MapPost("/ask", async (AskRequest request, AskService ask, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest("Field 'question' is required.");
    }

    return Results.Ok(await ask.AskAsync(request.Question, ct));
});

await app.Services.GetRequiredService<ChunkRepository>().EnsureSchemaAsync();

app.Run();
