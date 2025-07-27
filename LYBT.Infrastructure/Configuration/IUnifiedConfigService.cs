using LYBT.Infrastructure.Configuration.Dtos;
using LYBT.Common.Models;

namespace LYBT.Infrastructure.Configuration {

    /// <summary>
    /// 统一配置服务接口
    /// </summary>
    public interface IUnifiedConfigService {

        // ==================== 全局设置管理 ====================

        /// <summary>
        /// 获取全局设置
        /// </summary>
        Task<GlobalSettingsDto?> GetGlobalSettingsAsync();

        /// <summary>
        /// 更新全局设置
        /// </summary>
        Task<bool> UpdateGlobalSettingsAsync(GlobalSettingsDto globalSettingsDto, Guid updatedBy, string updatedByName);

        /// <summary>
        /// 初始化默认全局设置
        /// </summary>
        Task<bool> InitializeDefaultGlobalSettingsAsync();

        // ==================== 系统设置管理 ====================

        /// <summary>
        /// 获取设置值（泛型方法，自动转换类型）
        /// </summary>
        Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);

        /// <summary>
        /// 获取设置值（字符串）
        /// </summary>
        Task<string?> GetSettingAsync(string key, string? defaultValue = null);

        /// <summary>
        /// 设置配置值
        /// </summary>
        Task<bool> SetSettingAsync(string key, object value, string? description = null, string? group = null, Guid? updatedBy = null);

        /// <summary>
        /// 批量设置配置值
        /// </summary>
        Task<bool> SetSettingsAsync(Dictionary<string, object> settings, Guid? updatedBy = null);

        /// <summary>
        /// 删除设置
        /// </summary>
        Task<bool> DeleteSettingAsync(string key);

        /// <summary>
        /// 检查设置是否存在
        /// </summary>
        Task<bool> SettingExistsAsync(string key);

        /// <summary>
        /// 分页查询设置
        /// </summary>
        Task<PagedResult<SettingsDto>> GetSettingsAsync(string? group = null, string? keyword = null, int pageIndex = 1, int pageSize = 10);

        /// <summary>
        /// 根据分组获取所有设置
        /// </summary>
        Task<Dictionary<string, string>> GetSettingsByGroupAsync(string group);

        /// <summary>
        /// 创建设置
        /// </summary>
        Task<bool> CreateSettingAsync(SettingsCreateDto settingsCreateDto, Guid? createdBy = null);

        /// <summary>
        /// 更新设置
        /// </summary>
        Task<bool> UpdateSettingAsync(SettingsEditDto settingsEditDto, Guid? updatedBy = null);

        /// <summary>
        /// 获取所有设置分组
        /// </summary>
        Task<List<string>> GetSettingGroupsAsync();

        // ==================== 诊断目录管理 ====================

        /// <summary>
        /// 获取所有诊断目录
        /// </summary>
        Task<List<DiagnosisCatalogDto>> GetDiagnosisCatalogsAsync();

        /// <summary>
        /// 根据ID获取诊断目录
        /// </summary>
        Task<DiagnosisCatalogDto?> GetDiagnosisCatalogByIdAsync(Guid id);

        /// <summary>
        /// 分页查询诊断目录
        /// </summary>
        Task<PagedResult<DiagnosisCatalogDto>> GetDiagnosisCatalogsAsync(string? keyword = null, bool? isEnabled = null, int pageIndex = 1, int pageSize = 10);

        /// <summary>
        /// 创建诊断目录
        /// </summary>
        Task<bool> CreateDiagnosisCatalogAsync(DiagnosisCatalogDto diagnosisCatalogDto, Guid? createdBy = null);

        /// <summary>
        /// 更新诊断目录
        /// </summary>
        Task<bool> UpdateDiagnosisCatalogAsync(DiagnosisCatalogDto diagnosisCatalogDto, Guid? updatedBy = null);

        /// <summary>
        /// 删除诊断目录
        /// </summary>
        Task<bool> DeleteDiagnosisCatalogAsync(Guid id);

        /// <summary>
        /// 批量删除诊断目录
        /// </summary>
        Task<bool> DeleteDiagnosisCatalogsAsync(List<Guid> ids);

        /// <summary>
        /// 获取常用诊断
        /// </summary>
        Task<List<DiagnosisCatalogDto>> GetCommonDiagnosisAsync();

        /// <summary>
        /// 根据层级获取诊断目录
        /// </summary>
        Task<List<DiagnosisCatalogDto>> GetDiagnosisCatalogsByLevelAsync(int level);

        /// <summary>
        /// 根据父级ID获取子级诊断目录
        /// </summary>
        Task<List<DiagnosisCatalogDto>> GetDiagnosisCatalogsByParentIdAsync(Guid parentId);

        // ==================== 治疗目录管理 ====================

        /// <summary>
        /// 获取所有治疗目录
        /// </summary>
        Task<List<TreatmentCatalogDto>> GetTreatmentCatalogsAsync();

        /// <summary>
        /// 根据ID获取治疗目录
        /// </summary>
        Task<TreatmentCatalogDto?> GetTreatmentCatalogByIdAsync(Guid id);

        /// <summary>
        /// 分页查询治疗目录
        /// </summary>
        Task<PagedResult<TreatmentCatalogDto>> GetTreatmentCatalogsAsync(string? keyword = null, bool? isEnabled = null, int pageIndex = 1, int pageSize = 10);

        /// <summary>
        /// 创建治疗目录
        /// </summary>
        Task<bool> CreateTreatmentCatalogAsync(TreatmentCatalogDto treatmentCatalogDto, Guid? createdBy = null);

        /// <summary>
        /// 更新治疗目录
        /// </summary>
        Task<bool> UpdateTreatmentCatalogAsync(TreatmentCatalogDto treatmentCatalogDto, Guid? updatedBy = null);

        /// <summary>
        /// 删除治疗目录
        /// </summary>
        Task<bool> DeleteTreatmentCatalogAsync(Guid id);

        /// <summary>
        /// 批量删除治疗目录
        /// </summary>
        Task<bool> DeleteTreatmentCatalogsAsync(List<Guid> ids);

        /// <summary>
        /// 获取常用治疗项目
        /// </summary>
        Task<List<TreatmentCatalogDto>> GetCommonTreatmentsAsync();

        /// <summary>
        /// 根据层级获取治疗目录
        /// </summary>
        Task<List<TreatmentCatalogDto>> GetTreatmentCatalogsByLevelAsync(int level);

        /// <summary>
        /// 根据父级ID获取子级治疗目录
        /// </summary>
        Task<List<TreatmentCatalogDto>> GetTreatmentCatalogsByParentIdAsync(Guid parentId);

        // ==================== 枚举映射管理 ====================

        /// <summary>
        /// 获取枚举映射列表
        /// </summary>
        Task<List<EnumMappingDto>> GetEnumMappingsAsync(string enumTypeName);

        /// <summary>
        /// 获取所有枚举类型
        /// </summary>
        Task<List<string>> GetEnumTypesAsync();

        /// <summary>
        /// 刷新枚举映射缓存
        /// </summary>
        Task<bool> RefreshEnumMappingCacheAsync();

        // ==================== 缓存管理 ====================

        /// <summary>
        /// 刷新设置缓存
        /// </summary>
        Task<bool> RefreshSettingCacheAsync();

        /// <summary>
        /// 刷新诊断目录缓存
        /// </summary>
        Task<bool> RefreshDiagnosisCatalogCacheAsync();

        /// <summary>
        /// 刷新治疗目录缓存
        /// </summary>
        Task<bool> RefreshTreatmentCatalogCacheAsync();

        /// <summary>
        /// 刷新所有配置缓存
        /// </summary>
        Task<bool> RefreshAllCacheAsync();

        // ==================== 配置导入导出 ====================

        /// <summary>
        /// 导出系统配置
        /// </summary>
        Task<byte[]> ExportConfigurationAsync();

        /// <summary>
        /// 导入系统配置
        /// </summary>
        Task<bool> ImportConfigurationAsync(byte[] configData, Guid? importedBy = null);

        /// <summary>
        /// 备份配置数据
        /// </summary>
        Task<bool> BackupConfigurationAsync(string backupPath);

        /// <summary>
        /// 还原配置数据
        /// </summary>
        Task<bool> RestoreConfigurationAsync(string backupPath, Guid? restoredBy = null);
    }
}