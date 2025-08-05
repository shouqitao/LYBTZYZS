using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Infrastructure.Logging.Enums;
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
        /// 分页查询系统日志
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="keyword">关键词</param>
        /// <param name="level">日志级别</param>
        /// <param name="source">日志来源</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="onlyWithExceptions">仅显示异常日志</param>
        /// <returns>分页系统日志结果</returns>
        [Get("/api/SystemLogs")]
        Task<ApiResponse<PaginatedResult<SystemLogDto>>> GetSystemLogsAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] LogLevel? level = null,
            [Query] string? source = null,
            [Query] DateTime? startTime = null,
            [Query] DateTime? endTime = null,
            [Query] bool onlyWithExceptions = false);

        /// <summary>
        /// 分页查询用户操作日志
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="keyword">关键词</param>
        /// <param name="actionType">操作类型</param>
        /// <param name="module">模块</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>分页用户操作日志结果</returns>
        [Get("/api/UserActionLogs")]
        Task<ApiResponse<PaginatedResult<UserActionLogDto>>> GetUserActionLogsAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] LogActionType? actionType = null,
            [Query] string? module = null,
            [Query] DateTime? startTime = null,
            [Query] DateTime? endTime = null);

        /// <summary>
        /// 清理过期日志
        /// </summary>
        /// <param name="days">保留天数</param>
        /// <returns>清理结果</returns>
        [Delete("/api/Logs/cleanup/{days}")]
        Task<ApiResponse<bool>> CleanupLogsAsync(int days);

        /// <summary>
        /// 获取日志统计信息
        /// </summary>
        /// <param name="days">统计天数</param>
        /// <returns>统计信息</returns>
        [Get("/api/Logs/statistics/{days}")]
        Task<ApiResponse<Dictionary<string, object>>> GetLogStatisticsAsync(int days = 7);
    }
}