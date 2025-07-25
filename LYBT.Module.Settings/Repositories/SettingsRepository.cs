using LYBT.Infrastructure;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Models;

namespace LYBT.Module.Settings.Repositories {

    /// <summary>
    /// 系统设置仓储实现类
    /// </summary>
    public class SettingsRepository : ISettingsRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造函数，注入数据库上下文
        /// </summary>
        public SettingsRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取设置项
        /// </summary>
        public async Task<SettingsModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.Settings.FindAsync(id);
        }

        /// <summary>
        /// 获取所有设置项
        /// </summary>
        public async Task<List<SettingsModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.Settings.ToList());
        }

        /// <summary>
        /// 新增设置项
        /// </summary>
        public async Task<bool> AddAsync(SettingsModel settingsModel) {
            _appDbContext.Settings.Add(settingsModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新设置项
        /// </summary>
        public async Task<bool> UpdateAsync(SettingsModel settingsModel) {
            _appDbContext.Settings.Update(settingsModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除设置项
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _appDbContext.Settings.FindAsync(id);
            if (model == null)
                return false;
            _appDbContext.Settings.Remove(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}