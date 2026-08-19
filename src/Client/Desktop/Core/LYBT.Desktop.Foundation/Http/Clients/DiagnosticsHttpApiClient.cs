// ---------------------------------------------------------------------------
// DiagnosticsHttpApiClient — HttpClient adapter for IApiClientDiagnostics
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientDiagnostics (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式诊断 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class DiagnosticsHttpApiClient : HttpApiClientBase, IApiClientDiagnostics
{
    public DiagnosticsHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    public Task<ApiResponse<object>> GetLoggingStatusAsync()
        => GetAndWrapAsync<object>("/api/v1/diagnostics/logging/status");

    public Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request)
        => PostAndWrapAsync<object>("/api/v1/diagnostics/logging/debug/enable", request);

    public Task<ApiResponse<object>> DisableDebugModeAsync()
        => PostAndWrapAsync<object>("/api/v1/diagnostics/logging/debug/disable", new object());

    public Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request)
        => PostAndWrapAsync<object>("/api/v1/diagnostics/logging/level", request);
}
