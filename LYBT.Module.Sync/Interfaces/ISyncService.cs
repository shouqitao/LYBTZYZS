using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Sync.Dtos;

namespace LYBT.Module.Sync.Interfaces {
    /// <summary>
    /// 数据同步业务服务接口，定义同步任务和日志的业务操作
    /// </summary>
    public interface ISyncService {
        // ========== 日志相关 ==========

        /// <summary>
        /// 获取所有同步日志
        /// </summary>
        Task<List<SyncLogDto>> GetLogListAsync();

        /// <summary>
        /// 新增同步日志
        /// </summary>
        Task<bool> AddLogAsync(SyncLogCreateDto syncLogCreateDto);

        /// <summary>
        /// 删除同步日志
        /// </summary>
        Task<bool> DeleteLogAsync(string id);

        /// <summary>
        /// 获取最近一次同步信息
        /// </summary>
        Task<SyncLogDto?> GetLastSyncInfoAsync();

        /// <summary>
        /// 分页查询同步历史日志
        /// </summary>
        Task<List<SyncLogDto>> GetSyncLogPagedAsync(int page, int pageSize);

        /// <summary>
        /// 检测中心数据库是否可连接
        /// </summary>
        Task<bool> CheckConnectionStatusAsync();

        /// <summary>
        /// 手动触发同步
        /// </summary>
        Task<bool> TriggerManualSyncAsync();

        // ========== 任务相关 ==========

        /// <summary>
        /// 获取同步任务列表
        /// </summary>
        Task<List<SyncTaskDto>> GetTaskListAsync();

        /// <summary>
        /// 获取同步任务详情
        /// </summary>
        Task<SyncTaskDetailDto?> GetTaskDetailAsync(Guid id);

        /// <summary>
        /// 新增同步任务
        /// </summary>
        Task<bool> AddTaskAsync(SyncTaskCreateDto syncTaskCreateDto);

        /// <summary>
        /// 更新同步任务
        /// </summary>
        Task<bool> UpdateTaskAsync(SyncTaskEditDto syncTaskEditDto);

        /// <summary>
        /// 删除同步任务
        /// </summary>
        Task<bool> DeleteTaskAsync(Guid id);
    }
}
