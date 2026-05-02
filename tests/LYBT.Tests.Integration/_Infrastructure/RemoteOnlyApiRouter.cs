using LYBT.Desktop.Contracts.Services;

namespace LYBT.Tests.Integration._Infrastructure;

/// <summary>
/// Test-only IApiRouter that always routes to Remote mode.
/// Used in integration tests that test the Desktop Repository -> Server API chain.
/// </summary>
public sealed class RemoteOnlyApiRouter : IApiRouter
{
    public ApiMode CurrentMode => ApiMode.Remote;
    public bool IsOffline => false;
    public ApiMode? ManualOverride { get; set; }
    public event EventHandler<ApiModeChangedEventArgs>? ModeChanged;
    public void SwitchTo(ApiMode mode) { }
    public void ClearManualOverride() { }
}
