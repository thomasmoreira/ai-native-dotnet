# ai-native-dotnet

An AI-native .NET service built like an architect: RAG over pgvector, tool-calling, automated
evals, LLM observability (OTel GenAI), provider-pluggable (Ollama by default), via .NET Aspire.

## RAG pipeline
A question is embedded, the top-k most similar chunks are retrieved from pgvector, a grounded
prompt is built, and the LLM answers with citations. Microsoft.Extensions.AI abstracts the
provider, so local Ollama and cloud models are interchangeable.

## Evals as a gate
A golden-set measures groundedness, relevance and retrieval precision as a test — quality is
measured, not guessed, analogous to how an SLO gates availability.
