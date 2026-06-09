# ADR-002 — pgvector como vector store

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
RAG precisa de busca por similaridade sobre embeddings. Opções: um vector DB dedicado
(Qdrant, Milvus, Weaviate) ou estender o Postgres que o portfólio já usa.

## Decisão
**Postgres + pgvector** (`pgvector/pgvector`): coluna `vector`, índice e operadores de
distância (`<->`, `<=>`), via Aspire + Npgsql + a lib `Pgvector`.

## Consequências
- ✅ Zero serviço novo — reusa o domínio de Postgres dos outros labs; produção-realista.
- ✅ Transações: dados e embeddings no mesmo banco.
- ✅ Índice HNSW/IVFFlat quando o corpus crescer.
- ⚠️ Para bilhões de vetores um store dedicado escala melhor; fora do escopo do lab.
