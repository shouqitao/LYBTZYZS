using LYBT.Infrastructure;
using LYBT.Models.Queueing;
using LYBT.Module.Queueing.Interfaces;

namespace LYBT.Module.Queueing.Repositories {

    /// <summary>
    /// 排队仓储实现类，实现数据库操作
    /// </summary>
    public class QueueingRepository : IQueueingRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public QueueingRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取排队详情
        /// </summary>
        public async Task<QueueingModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.Queueings.FindAsync(id);
        }

        /// <summary>
        /// 获取所有排队信息
        /// </summary>
        public async Task<List<QueueingModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.Queueings.ToList());
        }

        /// <summary>
        /// 新增排队信息
        /// </summary>
        public async Task<bool> AddAsync(QueueingModel model) {
            _appDbContext.Queueings.Add(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新排队信息
        /// </summary>
        public async Task<bool> UpdateAsync(QueueingModel model) {
            _appDbContext.Queueings.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除排队信息
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _appDbContext.Queueings.FindAsync(id);
            if (model == null)
                return false;
            _appDbContext.Queueings.Remove(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 取消排队信息，标记状态为已取消
        /// </summary>
        public async Task<bool> CancelAsync(Guid id) {
            var model = await _appDbContext.Queueings.FindAsync(id);
            if (model == null)
                return false;
            model.Status = LYBT.Common.Enums.QueueStatus.Cancelled;
            _appDbContext.Queueings.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行CompleteAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<bool> CompleteAsync(Guid id) {
            var model = await _appDbContext.Queueings.FindAsync(id);
            if (model == null)
                return false;
            model.Status = LYBT.Common.Enums.QueueStatus.Finished;
            _appDbContext.Queueings.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行HoldAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<bool> HoldAsync(Guid id) {
            var model = await _appDbContext.Queueings.FindAsync(id);
            if (model == null)
                return false;
            model.Status = LYBT.Common.Enums.QueueStatus.OnHold;
            _appDbContext.Queueings.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
