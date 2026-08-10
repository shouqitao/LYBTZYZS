using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;

namespace LYBT.Desktop.Admin.Services
{
    /// <summary>
    /// 诊断/日志服务接口（D6: DP10 收口——封装 IApiClient.Diagnostics，VM 禁止直注 IApiClient）。
    /// 对齐 SysadminHomeViewModel 经 IAuthHealthService 服务门面封装先例。
    /// </summary>
    public interface IDiagnosticsService
    {
        /// <summary>获取当前日志级别状态。</summary>
        Task<ApiResponse<object>> GetLoggingStatusAsync();

        /// <summary>设置日志级别。</summary>
        Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request);

        /// <summary>启用调试模式。</summary>
        Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request);

        /// <summary>禁用调试模式。</summary>
        Task<ApiResponse<object>> DisableDebugModeAsync();
    }
}
