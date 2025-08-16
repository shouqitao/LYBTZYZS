using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Shared
{
    /// <summary>
    /// 共享中药材服务接口
    /// 提供跨工作台的药材查询和管理功能
    /// </summary>
    public interface ISharedHerbService
    {
        /// <summary>
        /// 获取所有可用药材列表
        /// 用于处方开具时的药材选择
        /// </summary>
        /// <returns>可用药材列表</returns>
        Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync();

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        /// <param name="herbId">药材ID</param>
        /// <returns>药材详细信息</returns>
        Task<ServiceResult<HerbDto>> GetHerbByIdAsync(Guid herbId);

        /// <summary>
        /// 搜索药材
        /// 支持名称、拼音、别名搜索
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的药材列表</returns>
        Task<ServiceResult<List<HerbDto>>> SearchHerbsAsync(string keyword);

        /// <summary>
        /// 根据功效分类获取药材
        /// </summary>
        /// <param name="category">功效分类</param>
        /// <returns>该分类下的药材列表</returns>
        Task<ServiceResult<List<HerbDto>>> GetHerbsByCategoryAsync(string category);

        /// <summary>
        /// 获取常用药材列表
        /// 基于使用频率统计
        /// </summary>
        /// <param name="limit">返回数量，默认50</param>
        /// <returns>常用药材列表</returns>
        Task<ServiceResult<List<HerbDto>>> GetFrequentlyUsedHerbsAsync(int limit = 50);

        /// <summary>
        /// 检查药材价格
        /// </summary>
        /// <param name="herbIds">药材ID列表</param>
        /// <returns>药材价格信息</returns>
        Task<ServiceResult<Dictionary<Guid, decimal>>> GetHerbPricesAsync(List<Guid> herbIds);

        /// <summary>
        /// 获取药材配伍禁忌信息
        /// </summary>
        /// <param name="herbId">药材ID</param>
        /// <returns>配伍禁忌信息</returns>
        Task<ServiceResult<List<string>>> GetHerbContraindicationsAsync(Guid herbId);

        /// <summary>
        /// 批量获取药材信息
        /// </summary>
        /// <param name="herbIds">药材ID列表</param>
        /// <returns>药材信息列表</returns>
        Task<ServiceResult<List<HerbDto>>> GetHerbsByIdsAsync(List<Guid> herbIds);

        /// <summary>
        /// 获取药材分类列表
        /// </summary>
        /// <returns>药材分类</returns>
        Task<ServiceResult<List<string>>> GetHerbCategoriesAsync();

        /// <summary>
        /// 验证药材是否可用
        /// </summary>
        /// <param name="herbId">药材ID</param>
        /// <returns>是否可用</returns>
        Task<ServiceResult<bool>> ValidateHerbAvailabilityAsync(Guid herbId);

        /// <summary>
        /// 获取药材分页列表
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>分页的药材列表</returns>
        Task<ServiceResult<PagedResult<HerbDto>>> GetHerbsAsync(int page, int pageSize, string keyword = null);

        /// <summary>
        /// 创建新药材
        /// </summary>
        /// <param name="createDto">创建数据</param>
        /// <returns>创建结果</returns>
        Task<ServiceResult<HerbDto>> CreateHerbAsync(HerbCreateDto createDto);

        /// <summary>
        /// 更新药材信息
        /// </summary>
        /// <param name="id">药材ID</param>
        /// <param name="updateDto">更新数据</param>
        /// <returns>更新结果</returns>
        Task<ServiceResult<HerbDto>> UpdateHerbAsync(Guid id, HerbUpdateDto updateDto);

        /// <summary>
        /// 切换药材状态
        /// </summary>
        /// <param name="id">药材ID</param>
        /// <returns>操作结果</returns>
        /// <summary>
        /// 分页查询药材列表（带查询条件）
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>分页药材列表</returns>
        Task<ServiceResult<PagedResult<HerbDto>>> GetHerbsAsync(HerbPagedQueryDto queryDto);

        /// <summary>
        /// 根据ID获取单个药材信息
        /// </summary>
        /// <param name="id">药材ID</param>
        /// <returns>药材信息</returns>
        Task<ServiceResult<HerbDto>> GetHerbAsync(Guid id);

        /// <summary>
        /// 删除药材
        /// </summary>
        /// <param name="id">药材ID</param>
        /// <returns>删除结果</returns>
        Task<ServiceResult> DeleteHerbAsync(Guid id);

        /// <summary>
        /// 获取药材统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<ServiceResult<HerbStatisticsDto>> GetHerbStatisticsAsync();

        /// <summary>
        /// 批量调整药材价格
        /// </summary>
        /// <param name="herbIds">药材ID列表</param>
        /// <param name="adjustmentType">调整类型（percentage/fixed）</param>
        /// <param name="adjustmentValue">调整值</param>
        /// <returns>批量调整结果</returns>
        Task<ServiceResult<int>> BatchAdjustPriceAsync(List<Guid> herbIds, string adjustmentType, decimal adjustmentValue);

        Task<ServiceResult<bool>> ToggleHerbStatusAsync(Guid id);

        /// <summary>
        /// 获取药材产地列表
        /// </summary>
        /// <returns>产地列表</returns>
        Task<ServiceResult<List<string>>> GetHerbOriginsAsync();
    }
}