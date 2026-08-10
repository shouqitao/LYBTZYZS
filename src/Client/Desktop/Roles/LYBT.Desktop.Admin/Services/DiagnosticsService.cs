using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;

namespace LYBT.Desktop.Admin.Services
{
    /// <summary>
    /// 诊断/日志服务实现 — 封装 IApiClient.Diagnostics 供 VM 消费（D6: DP10 收口）。
    /// </summary>
    internal class DiagnosticsService : IDiagnosticsService
    {
        private readonly IApiClientDiagnostics _diagnosticsApi;

        public DiagnosticsService(IApiClientDiagnostics diagnosticsApi)
        {
            _diagnosticsApi = diagnosticsApi ?? throw new ArgumentNullException(nameof(diagnosticsApi));
        }

        public Task<ApiResponse<object>> GetLoggingStatusAsync()
            => _diagnosticsApi.GetLoggingStatusAsync();

        public Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request)
            => _diagnosticsApi.SetLoggingLevelAsync(request);

        public Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request)
            => _diagnosticsApi.EnableDebugModeAsync(request);

        public Task<ApiResponse<object>> DisableDebugModeAsync()
            => _diagnosticsApi.DisableDebugModeAsync();
    }
}
