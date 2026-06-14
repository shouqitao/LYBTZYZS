using LYBT.Desktop.Contracts.Services;

namespace LYBT.Tests.Integration._Infrastructure;

/// <summary>
/// Test-only IApiRouter that always reports a remote URL.
/// Used in integration tests that test the Desktop Repository -> Server API chain.
/// </summary>
public sealed class RemoteOnlyApiRouter : IApiRouter
{
    public string CurrentUrl => "http://remote-test:5000";
    public bool IsLocal => false;
}
