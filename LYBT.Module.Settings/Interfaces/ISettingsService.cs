using LYBT.Module.Settings.Dtos;

namespace LYBT.Module.Settings.Interfaces {

    /// <summary>
    /// 系统设置业务服务接口
    /// </summary>
    public interface ISettingsService {

        /// <summary>
        /// 根据ID获取设置项详情
        /// </summary>
        Task<SettingsDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取设置项列表
        /// </summary>
        Task<List<SettingsDto>> GetListAsync();

        /// <summary>
        /// 新增设置项
        /// </summary>
        Task<bool> AddAsync(SettingsCreateDto settingsCreateDto);

        /// <summary>
        /// 编辑设置项
        /// </summary>
        Task<bool> UpdateAsync(SettingsEditDto settingsEditDto);

        /// <summary>
        /// 删除设置项
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}