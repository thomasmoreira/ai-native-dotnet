using Aspire.Hosting;

namespace AppHost.Tests;

/// <summary>
/// Starts the distributed app once for the whole test collection — with <c>UseOllama=false</c>, so
/// the AppHost skips the Ollama container and the AiService falls back to deterministic fakes
/// (ADR-006). Only pgvector (Postgres) is a real container. The bundled corpus is ingested once.
/// </summary>
public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
            ["UseOllama=false"], CancellationToken.None);
        App = await builder.BuildAsync();
        await App.StartAsync();
        await App.ResourceNotifications.WaitForResourceHealthyAsync("aiservice").WaitAsync(StartupTimeout);

        using var client = App.CreateHttpClient("aiservice");
        using var ingest = await client.PostAsync("/ingest", content: null);
        ingest.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync() => await App.DisposeAsync();
}
