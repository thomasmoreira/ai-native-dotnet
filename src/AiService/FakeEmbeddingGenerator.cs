using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;

namespace AiService;

/// <summary>
/// Deterministic 384-d unit-vector embeddings derived from a hash of the text. The same string
/// always yields the same vector, so retrieval is reproducible in tests without a real model or
/// network call (ADR-006). It is NOT semantic — only for deterministic plumbing tests.
/// </summary>
internal sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private const int Dimensions = 384;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var value in values)
        {
            embeddings.Add(new Embedding<float>(Embed(value)));
        }

        return Task.FromResult(embeddings);
    }

    private static float[] Embed(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var random = new Random(BitConverter.ToInt32(hash, 0));

        var vector = new float[Dimensions];
        double norm = 0;
        for (var i = 0; i < Dimensions; i++)
        {
            var value = (float)((random.NextDouble() * 2) - 1);
            vector[i] = value;
            norm += value * value;
        }

        norm = Math.Sqrt(norm);
        for (var i = 0; i < Dimensions; i++)
        {
            vector[i] = (float)(vector[i] / norm);
        }

        return vector;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
        // Nothing to dispose.
    }
}
