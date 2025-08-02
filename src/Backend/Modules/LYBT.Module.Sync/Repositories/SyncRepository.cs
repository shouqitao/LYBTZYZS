using LYBT.Infrastructure.Data;
using LYBT.Models.Sync;
using LYBT.Module.Sync.Interfaces;

namespace LYBT.Module.Sync.Repositories {

    /// <summary>
    /// 数据同步仓储实现类，支持同步任务与同步日志数据库操作
    /// </summary>
    public class SyncRepository : ISyncRepository {
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public SyncRepository(AppDbContext context) {
            _context = context;
        }

        // ===================== 同步任务 =====================

        /// <summary>
        /// 根据ID获取同步任务
        /// </summary>
        public async Task<SyncTaskModel?> GetTaskByIdAsync(Guid id) {
            return await _context.SyncTasks.FindAsync(id);
        }

        /// <summary>
        /// 获取全部同步任务
        /// </summary>
        public async Task<List<SyncTaskModel>> GetTaskListAsync() {
            return await Task.FromResult(_context.SyncTasks.ToList());
        }

        /// <summary>
        /// 新增同步任务
        /// </summary>
        public async Task<bool> AddTaskAsync(SyncTaskModel syncTaskModel) {
            _context.SyncTasks.Add(syncTaskModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新同步任务
        /// </summary>
        public async Task<bool> UpdateTaskAsync(SyncTaskModel syncTaskModel) {
            _context.SyncTasks.Update(syncTaskModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除同步任务
        /// </summary>
        public async Task<bool> DeleteTaskAsync(Guid id) {
            var model = await _context.SyncTasks.FindAsync(id);
            if (model == null)
                return false;
            _context.SyncTasks.Remove(model);
            return await _context.SaveChangesAsync() > 0;
        }

        // ===================== 同步日志 =====================

        /// <summary>
        /// 获取所有同步日志
        /// </summary>
        public async Task<List<SyncLogModel>> GetLogListAsync() {
            return await Task.FromResult(_context.SyncLogs.ToList());
        }

        /// <summary>
        /// 新增同步日志
        /// </summary>
        public async Task<bool> AddLogAsync(SyncLogModel syncLogModel) {
            _context.SyncLogs.Add(syncLogModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除同步日志
        /// </summary>
        public async Task<bool> DeleteLogAsync(string id) {
            var model = await _context.SyncLogs.FindAsync(id);
            if (model == null)
                return false;
            _context.SyncLogs.Remove(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 获取最新一条同步日志
        /// </summary>
        public async Task<SyncLogModel?> GetLastLogAsync() {
            return await Task.FromResult(_context.SyncLogs
                .OrderByDescending(x => x.SyncTime).FirstOrDefault());
        }

        /// <summary>
        /// 分页获取同步日志
        /// </summary>
        public async Task<List<SyncLogModel>> GetLogPagedAsync(int page, int pageSize) {
            return await Task.FromResult(
                _context.SyncLogs
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
                return await _context.Database.CanConnectAsync();
            } catch {
                return false;
            }
        }
    }
}