using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// Diagnostics API client — wraps IDiagnosticsApi (Refit) to implement IApiClientDiagnostics.
/// </summary>
internal sealed class DiagnosticsApiClient : IApiClientDiagnostics
{
    private readonly IDiagnosticsApi _api;

    public DiagnosticsApiClient(IDiagnosticsApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<ApiResponse<object>> GetLoggingStatusAsync()
        => _api.GetLoggingStatusAsync();

    public Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request)
        => _api.EnableDebugModeAsync(request);

    public Task<ApiResponse<object>> DisableDebugModeAsync()
        => _api.DisableDebugModeAsync();

    public Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request)
        => _api.SetLoggingLevelAsync(request);
}
