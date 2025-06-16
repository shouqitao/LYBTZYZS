using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Infrastructure;
using LYBT.Models;
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
    }
}
