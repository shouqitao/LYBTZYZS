using System;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Services;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// API健康监控服务接口
    /// </summary>
    public interface IApiHealthMonitor
    {
        /// <summary>
        /// API是否在线
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// 当前状态消息
        /// </summary>
        string StatusMessage { get; }

        /// <summary>
        /// 最后检查时间
        /// </summary>
        DateTime LastCheckTime { get; }

        /// <summary>
        /// 连续失败次数
        /// </summary>
        int ConsecutiveFailures { get; }

        /// <summary>
        /// 状态变更事件
        /// </summary>
        event EventHandler<ApiHealthStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// 启动监控
        /// </summary>
        Task StartMonitoringAsync();

        /// <summary>
        /// 停止监控
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// 手动触发健康检查
        /// </summary>
        Task<bool> CheckHealthAsync();
    }
}
