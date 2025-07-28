using LYBT.Models.Sync;
using LYBT.Module.Sync.Data;
using LYBT.Module.Sync.Interfaces;

namespace LYBT.Module.Sync.Repositories {

    /// <summary>
    /// 数据同步仓储实现类，支持同步任务与同步日志数据库操作
    /// </summary>
    public class SyncRepository : ISyncRepository {
        private readonly SyncDbContext _syncDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public SyncRepository(SyncDbContext syncDbContext) {
            _syncDbContext = syncDbContext;
        }

        // ===================== 同步任务 =====================

        /// <summary>
        /// 根据ID获取同步任务
        /// </summary>
        public async Task<SyncTaskModel?> GetTaskByIdAsync(Guid id) {
            return await _syncDbContext.SyncTasks.FindAsync(id);
        }

        /// <summary>
        /// 获取全部同步任务
        /// </summary>
        public async Task<List<SyncTaskModel>> GetTaskListAsync() {
            return await Task.FromResult(_syncDbContext.SyncTasks.ToList());
        }

        /// <summary>
        /// 新增同步任务
        /// </summary>
        public async Task<bool> AddTaskAsync(SyncTaskModel syncTaskModel) {
            _syncDbContext.SyncTasks.Add(syncTaskModel);
            return await _syncDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新同步任务
        /// </summary>
        public async Task<bool> UpdateTaskAsync(SyncTaskModel syncTaskModel) {
            _syncDbContext.SyncTasks.Update(syncTaskModel);
            return await _syncDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除同步任务
        /// </summary>
        public async Task<bool> DeleteTaskAsync(Guid id) {
            var model = await _syncDbContext.SyncTasks.FindAsync(id);
            if (model == null)
                return false;
            _syncDbContext.SyncTasks.Remove(model);
            return await _syncDbContext.SaveChangesAsync() > 0;
        }

        // ===================== 同步日志 =====================

        /// <summary>
        /// 获取所有同步日志
        /// </summary>
        public async Task<List<SyncLogModel>> GetLogListAsync() {
            return await Task.FromResult(_syncDbContext.SyncLogs.ToList());
        }

        /// <summary>
        /// 新增同步日志
        /// </summary>
        public async Task<bool> AddLogAsync(SyncLogModel syncLogModel) {
            _syncDbContext.SyncLogs.Add(syncLogModel);
            return await _syncDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除同步日志
        /// </summary>
        public async Task<bool> DeleteLogAsync(string id) {
            var model = await _syncDbContext.SyncLogs.FindAsync(id);
            if (model == null)
                return false;
            _syncDbContext.SyncLogs.Remove(model);
            return await _syncDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 获取最新一条同步日志
        /// </summary>
        public async Task<SyncLogModel?> GetLastLogAsync() {
            return await Task.FromResult(_syncDbContext.SyncLogs
                .OrderByDescending(x => x.SyncTime).FirstOrDefault());
        }

        /// <summary>
        /// 分页获取同步日志
        /// </summary>
        public async Task<List<SyncLogModel>> GetLogPagedAsync(int page, int pageSize) {
            return await Task.FromResult(
                _syncDbContext.SyncLogs
                    .OrderByDescending(x => x.SyncTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList());
        }

        /// <summary>
        /// 检测数据库连接是否可用
        /// </summary>
        public async Task<bool> CanConnectAsync() {
            try {
                return await _syncDbContext.Database.CanConnectAsync();
            } catch {
                return false;
            }
        }
    }
}