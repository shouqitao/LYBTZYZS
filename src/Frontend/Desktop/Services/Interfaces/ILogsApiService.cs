using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Shared.Models.Common;
using Refit;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 日志API服务接口
    /// </summary>
    public interface ILogsApiService
    {
        /// <summary>
        /// 分页查询日志
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>分页日志结果</returns>
        [Post("/api/UnifiedLogs/query")]
        Task<ApiResponse<PaginatedResult<LogDto>>> GetLogsAsync([Body] LogQueryDto queryDto);

        /// <summary>
        /// 根据ID获取日志详情
        /// </summary>
        /// <param name="id">日志ID</param>
        /// <returns>日志详情</returns>
        [Get("/api/UnifiedLogs/{id}")]
        Task<ApiResponse<LogDto>> GetLogByIdAsync(Guid id);

        /// <summary>
        /// 分页查询系统日志
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>分页系统日志结果</returns>
        [Post("/api/UnifiedLogs/system/query")]
        Task<ApiResponse<PaginatedResult<SystemLogDto>>> GetSystemLogsAsync([Body] LogQueryDto queryDto);

        /// <summary>
        /// 分页查询用户操作日志
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>分页用户操作日志结果</returns>
        [Post("/api/UnifiedLogs/user-actions/query")]
        Task<ApiResponse<PaginatedResult<UserActionLogDto>>> GetUserActionLogsAsync([Body] LogQueryDto queryDto);

        /// <summary>
        /// 根据用户ID查询操作日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="queryDto">查询条件</param>
        /// <returns>分页用户操作日志结果</returns>
        [Post("/api/UnifiedLogs/user-actions/user/{userId}")]
        Task<ApiResponse<PaginatedResult<UserActionLogDto>>> GetUserActionLogsByUserIdAsync(Guid userId, [Body] LogQueryDto queryDto);

        /// <summary>
        /// 清理过期日志
        /// </summary>
        /// <param name="days">保留天数</param>
        /// <returns>清理结果</returns>
        [Delete("/api/UnifiedLogs/cleanup/{days}")]
        Task<ApiResponse<bool>> CleanupLogsAsync(int days);

        /// <summary>
        /// 导出日志
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>导出文件流</returns>
        [Post("/api/UnifiedLogs/export")]
        Task<ApiResponse<Stream>> ExportLogsAsync([Body] LogQueryDto queryDto);

        /// <summary>
        /// 获取日志统计信息
        /// </summary>
        /// <param name="days">统计天数</param>
        /// <returns>统计信息</returns>
        [Get("/api/UnifiedLogs/statistics/{days}")]
        Task<ApiResponse<Dictionary<string, object>>> GetLogStatisticsAsync(int days = 7);
    }
}