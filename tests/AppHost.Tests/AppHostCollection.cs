namespace AppHost.Tests;

/// <summary>Shares one running app (corpus already ingested) across every test class.</summary>
[CollectionDefinition("aspire-app")]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>;
