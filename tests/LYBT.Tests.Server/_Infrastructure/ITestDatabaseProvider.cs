using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Test database lifecycle abstraction.
/// Implementations create/destroy a unique database for each test run.
/// </summary>
public interface ITestDatabaseProvider : IAsyncLifetime
{
    /// <summary>
    /// Connection string to the test database (available after InitializeAsync).
    /// </summary>
    string ConnectionString { get; }
}
