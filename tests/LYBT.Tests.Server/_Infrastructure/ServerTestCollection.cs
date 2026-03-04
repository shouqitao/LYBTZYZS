using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

[CollectionDefinition("Server")]
public sealed class ServerTestCollection : ICollectionFixture<ServerFixture>;
