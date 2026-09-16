using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 诊断 API 客户端——包装 IDiagnosticsApi（Refit）以实现 IApiClientDiagnostics。
/// </summary>
internal sealed class DiagnosticsApiClient : IApiClientDiagnostics
{
    private readonly IDiagnosticsApi _api;

    public DiagnosticsApiClient(IDiagnosticsApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<ApiResponse<object>> GetLoggingStatusAsync(CancellationToken ct = default)
        => _api.GetLoggingStatusAsync(ct);

    public Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request, CancellationToken ct = default)
        => _api.EnableDebugModeAsync(request, ct);

    public Task<ApiResponse<object>> DisableDebugModeAsync(CancellationToken ct = default)
        => _api.DisableDebugModeAsync(ct);

    public Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request, CancellationToken ct = default)
        => _api.SetLoggingLevelAsync(request, ct);
}
