using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>诊断/日志 API 端点。</summary>
public interface IApiClientDiagnostics
{
    Task<ApiResponse<object>> GetLoggingStatusAsync(CancellationToken ct = default);
    Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request, CancellationToken ct = default);
    Task<ApiResponse<object>> DisableDebugModeAsync(CancellationToken ct = default);
    Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request, CancellationToken ct = default);
}
