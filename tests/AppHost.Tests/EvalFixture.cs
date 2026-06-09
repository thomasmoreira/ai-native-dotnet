using Aspire.Hosting;

namespace AppHost.Tests;

/// <summary>
/// Like <see cref="AppHostFixture"/> but with REAL Ollama embeddings (no UseOllama override), so
/// the retrieval eval measures actual semantic quality. Models are cached in the Ollama data
/// volume, so startup reuses them. The chat model is available but never invoked by the eval.
/// </summary>
public sealed class EvalFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(8);

    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(CancellationToken.None);
        App = await builder.BuildAsync();
        await App.StartAsync();
        await App.ResourceNotifications.WaitForResourceHealthyAsync("aiservice").WaitAsync(StartupTimeout);

        using var client = App.CreateHttpClient("aiservice");
        using var ingest = await client.PostAsync("/ingest", content: null);
        ingest.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync() => await App.DisposeAsync();
}
