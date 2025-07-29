using LYBT.Common.Enums.Queueing;
using LYBT.Models.Queueing;
using LYBT.Infrastructure.Data;
using LYBT.Module.Queueing.Interfaces;

namespace LYBT.Module.Queueing.Repositories {

    /// <summary>
    /// 排队仓储实现类，实现数据库操作
    /// </summary>
    public class QueueingRepository : IQueueingRepository {
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public QueueingRepository(AppDbContext context) {
            _context = context;
        }

        /// <summary>
        /// 根据ID获取排队详情
        /// </summary>
        public async Task<QueueingModel?> GetByIdAsync(Guid id) {
            return await _context.Queueings.FindAsync(id);
        }

        /// <summary>
        /// 获取所有排队信息
        /// </summary>
        public async Task<List<QueueingModel>> GetListAsync() {
            return await Task.FromResult(_context.Queueings.ToList());
        }

        /// <summary>
        /// 新增排队信息
        /// </summary>
        public async Task<bool> AddAsync(QueueingModel model) {
            _context.Queueings.Add(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新排队信息
        /// </summary>
        public async Task<bool> UpdateAsync(QueueingModel model) {
            _context.Queueings.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除排队信息
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _context.Queueings.FindAsync(id);
            if (model == null)
                return false;
            _context.Queueings.Remove(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 取消排队信息，标记状态为已取消
        /// </summary>
        public async Task<bool> CancelAsync(Guid id) {
            var model = await _context.Queueings.FindAsync(id);
            if (model == null)
                return false;
            model.Status = QueueStatus.Cancelled;
            _context.Queueings.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行CompleteAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<bool> CompleteAsync(Guid id) {
            var model = await _context.Queueings.FindAsync(id);
            if (model == null)
                return false;
            model.Status = QueueStatus.Completed;
            _context.Queueings.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行HoldAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<bool> HoldAsync(Guid id) {
            var model = await _context.Queueings.FindAsync(id);
            if (model == null)
                return false;
            model.Status = QueueStatus.Skipped;
            _context.Queueings.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}