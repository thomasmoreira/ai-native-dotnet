using Aspire.Hosting;

namespace AppHost.Tests;

/// <summary>
/// Starts the distributed app once for the test class — but with <c>UseOllama=false</c>, so the
/// AppHost skips the Ollama container and the AiService falls back to a deterministic fake
/// embedder (ADR-006). Only pgvector (Postgres) is a real container: fast and reproducible.
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
    }

    public async Task DisposeAsync() => await App.DisposeAsync();
}
