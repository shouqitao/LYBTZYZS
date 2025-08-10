using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Infrastructure.Configuration.Dtos;
using LYBT.Shared.Models.Common;
using Refit;
using LYBT.WPF.Client.Core.Models;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 系统设置API服务接口
    /// </summary>
    public interface ISystemSettingsApiService
    {
        /// <summary>
        /// 获取全局设置
        /// </summary>
        /// <returns>全局设置</returns>
        [Get("/api/UnifiedConfig/global-settings")]
        Task<Refit.ApiResponse<GlobalSettingsDto>> GetGlobalSettingsAsync();

        /// <summary>
        /// 更新全局设置
        /// </summary>
        /// <param name="globalSettingsDto">全局设置对象</param>
        /// <returns>更新结果</returns>
        [Put("/api/UnifiedConfig/global-settings")]
        Task<Refit.ApiResponse<object>> UpdateGlobalSettingsAsync([Body] GlobalSettingsDto globalSettingsDto);

        /// <summary>
        /// 获取设置值
        /// </summary>
        /// <param name="key">设置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>设置值</returns>
        [Get("/api/UnifiedConfig/settings/{key}")]
        Task<Refit.ApiResponse<object>> GetSettingAsync(string key, [Query] string? defaultValue = null);

        /// <summary>
        /// 设置配置值
        /// </summary>
        /// <param name="request">设置请求</param>
        /// <returns>设置结果</returns>
        [Post("/api/UnifiedConfig/settings")]
        Task<Refit.ApiResponse<object>> SetSettingAsync([Body] SetSettingRequest request);

        /// <summary>
        /// 批量设置配置值
        /// </summary>
        /// <param name="settings">设置字典</param>
        /// <returns>设置结果</returns>
        [Post("/api/UnifiedConfig/settings/batch")]
        Task<Refit.ApiResponse<object>> SetSettingsAsync([Body] Dictionary<string, object> settings);

        /// <summary>
        /// 分页查询设置
        /// </summary>
        /// <param name="group">设置分组</param>
        /// <param name="keyword">关键词</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>分页设置结果</returns>
        [Get("/api/UnifiedConfig/settings")]
        Task<Refit.ApiResponse<PagedData<SettingsDto>>> GetSettingsAsync(
            [Query] string? group = null,
            [Query] string? keyword = null,
            [Query] int pageIndex = 1,
            [Query] int pageSize = 10);

        /// <summary>
        /// 根据分组获取所有设置
        /// </summary>
        /// <param name="group">设置分组</param>
        /// <returns>设置字典</returns>
        [Get("/api/UnifiedConfig/settings/group/{group}")]
        Task<Refit.ApiResponse<Dictionary<string, string>>> GetSettingsByGroupAsync(string group);

        /// <summary>
        /// 删除设置
        /// </summary>
        /// <param name="key">设置键</param>
        /// <returns>删除结果</returns>
        [Delete("/api/UnifiedConfig/settings/{key}")]
        Task<Refit.ApiResponse<object>> DeleteSettingAsync(string key);

        /// <summary>
        /// 刷新所有配置缓存
        /// </summary>
        /// <returns>刷新结果</returns>
        [Post("/api/UnifiedConfig/cache/refresh-all")]
        Task<Refit.ApiResponse<object>> RefreshAllCacheAsync();

        /// <summary>
        /// 刷新设置缓存
        /// </summary>
        /// <returns>刷新结果</returns>
        [Post("/api/UnifiedConfig/cache/refresh-settings")]
        Task<Refit.ApiResponse<object>> RefreshSettingCacheAsync();
    }

    /// <summary>
    /// 设置配置请求模型
    /// </summary>
    public class SetSettingRequest
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Group { get; set; }
    }
}