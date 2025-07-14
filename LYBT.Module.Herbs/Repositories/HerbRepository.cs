using LYBT.Infrastructure;
using LYBT.Models;
using LYBT.Module.Herbs.Interfaces;

namespace LYBT.Module.Herbs.Repositories {

    /// <summary>
    /// 药材仓储实现类，实现数据库操作
    /// </summary>
    public class HerbRepository : IHerbRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public HerbRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        public async Task<HerbModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.Herbs.FindAsync(id);
        }

        /// <summary>
        /// 获取所有药材列表
        /// </summary>
        public async Task<List<HerbModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.Herbs.ToList());
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<bool> AddAsync(HerbModel herb) {
            _appDbContext.Herbs.Add(herb);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新药材
        /// </summary>
        public async Task<bool> UpdateAsync(HerbModel herb) {
            _appDbContext.Herbs.Update(herb);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _appDbContext.Herbs.FindAsync(id);
            if (model == null)
                return false;
            _appDbContext.Herbs.Remove(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddRangeAsync(List<HerbModel> herbs) {
            await _appDbContext.Herbs.AddRangeAsync(herbs);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}