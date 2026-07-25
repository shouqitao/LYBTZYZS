using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>Diagnostics/logging API endpoints.</summary>
public interface IApiClientDiagnostics
{
    Task<ApiResponse<object>> GetLoggingStatusAsync();
    Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request);
    Task<ApiResponse<object>> DisableDebugModeAsync();
    Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request);
}
