using LYBT.Models.Sync;

namespace LYBT.Module.Sync.Interfaces {

    /// <summary>
    /// 数据同步仓储接口，定义同步任务与同步日志相关数据库操作
    /// </summary>
    public interface ISyncRepository {
        // ===================== 同步任务 =====================

        /// <summary>
        /// 根据同步任务ID获取同步任务
        /// </summary>
        Task<SyncTaskModel?> GetTaskByIdAsync(Guid id);

        /// <summary>
        /// 获取所有同步任务列表
        /// </summary>
        Task<List<SyncTaskModel>> GetTaskListAsync();

        /// <summary>
        /// 新增同步任务
        /// </summary>
        Task<bool> AddTaskAsync(SyncTaskModel syncTaskModel);

        /// <summary>
        /// 更新同步任务
        /// </summary>
        Task<bool> UpdateTaskAsync(SyncTaskModel syncTaskModel);

        /// <summary>
        /// 删除同步任务
        /// </summary>
        Task<bool> DeleteTaskAsync(Guid id);

        // ===================== 同步日志 =====================

        /// <summary>
        /// 获取所有同步日志列表
        /// </summary>
        Task<List<SyncLogModel>> GetLogListAsync();

        /// <summary>
        /// 新增同步日志
        /// </summary>
        Task<bool> AddLogAsync(SyncLogModel syncLogModel);

        /// <summary>
        /// 删除同步日志
        /// </summary>
        Task<bool> DeleteLogAsync(string id);

        /// <summary>
        /// 获取最新一条同步日志
        /// </summary>
        Task<SyncLogModel?> GetLastLogAsync();

        /// <summary>
        /// 分页获取同步日志
        /// </summary>
        Task<List<SyncLogModel>> GetLogPagedAsync(int page, int pageSize);

        /// <summary>
        /// 检测数据库连接是否可用
        /// </summary>
        Task<bool> CanConnectAsync();
    }
}