// The AppHost IS the distributed application. Aspire provisions pgvector (Postgres) and Ollama
// as containers, wires connection strings, and feeds telemetry into the dashboard.

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with the pgvector extension image — the vector store for embeddings.
var postgres = builder.AddPostgres("postgres").WithImage("pgvector/pgvector", "pg17");
var vectordb = postgres.AddDatabase("vectordb");

var aiservice = builder.AddProject<Projects.AiService>("aiservice")
    .WithReference(vectordb)
    .WaitFor(vectordb);

// Ollama is optional. A real run adds it (local embeddings/chat); tests set UseOllama=false and
// the service falls back to a deterministic fake — fast, no model, no external calls (ADR-006).
if (builder.Configuration["UseOllama"] is not "false")
{
    var ollama = builder.AddOllama("ollama").WithDataVolume();
    var embeddings = ollama.AddModel("embeddings", "all-minilm");
    aiservice.WithReference(embeddings).WaitFor(embeddings);
}

builder.Build().Run();
