using Xunit;

namespace LYBT.Tests.Server.RateLimiting;

/// <summary>
/// xUnit collection for rate limiting tests.
/// Isolated from ServerTestCollection because this collection enables rate limiting
/// (Security:RateLimiting:Enabled=true), while the main collection disables it via appsettings.Test.json.
/// </summary>
[CollectionDefinition("RateLimiting")]
public sealed class RateLimitingCollection : ICollectionFixture<RateLimitingFixture>;
