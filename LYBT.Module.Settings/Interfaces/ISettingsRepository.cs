using LYBT.Models.Settings;

namespace LYBT.Module.Settings.Interfaces {

    /// <summary>
    /// 系统设置仓储接口，定义数据操作方法
    /// </summary>
    public interface ISettingsRepository {

        /// <summary>
        /// 根据设置项ID获取设置
        /// </summary>
        Task<SettingsModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有设置项列表
        /// </summary>
        Task<List<SettingsModel>> GetListAsync();

        /// <summary>
        /// 新增设置项
        /// </summary>
        Task<bool> AddAsync(SettingsModel settingsModel);

        /// <summary>
        /// 更新设置项
        /// </summary>
        Task<bool> UpdateAsync(SettingsModel settingsModel);

        /// <summary>
        /// 删除设置项
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}