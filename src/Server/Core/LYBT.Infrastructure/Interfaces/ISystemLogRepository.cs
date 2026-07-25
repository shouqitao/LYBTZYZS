using LYBT.Entities.Common;

namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// 系统日志仓储接口 — 仅支持查询，不支持写入（日志由 Serilog 写入）
/// </summary>
public interface ISystemLogRepository
{
    /// <summary>
    /// 获取最近的系统日志
    /// </summary>
    Task<List<SystemLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default);
}
