using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Herbs.Services.Interfaces
{
    /// <summary>
    /// Herb模块核心业务服务接口
    /// UltraThink模块化架构：模块内部服务，不依赖外部SharedServices
    /// </summary>
    public interface IHerbModuleService
    {
        #region 基础CRUD操作
        
        /// <summary>
        /// 分页查询中药材
        /// </summary>
        Task<ServiceResult<PagedResult<HerbInfo>>> GetPagedAsync(PagedQueryBaseDto query);
        
        /// <summary>
        /// 根据ID获取中药材
        /// </summary>
        Task<ServiceResult<HerbInfo>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 创建中药材
        /// </summary>
        Task<ServiceResult<HerbInfo>> CreateAsync(HerbCreateInfo createInfo);
        
        /// <summary>
        /// 更新中药材
        /// </summary>
        Task<ServiceResult<HerbInfo>> UpdateAsync(HerbUpdateInfo updateInfo);
        
        /// <summary>
        /// 删除中药材（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
        
        #endregion
        
        #region 业务特定操作
        
        /// <summary>
        /// 搜索中药材
        /// </summary>
        Task<ServiceResult<PagedResult<HerbInfo>>> SearchHerbsAsync(PagedQueryBaseDto request);
        
        /// <summary>
        /// 根据名称获取中药材
        /// </summary>
        Task<ServiceResult<HerbInfo>> GetByNameAsync(string name);
        
        /// <summary>
        /// 验证中药材数据
        /// </summary>
        Task<ServiceResult> ValidateAsync(HerbInfo herbInfo);
        
        /// <summary>
        /// 检查中药材名称是否已被使用
        /// </summary>
        Task<ServiceResult<bool>> IsNameExistsAsync(string name, Guid? excludeId = null);
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 启用中药材
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用中药材
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);
        
        /// <summary>
        /// 批量更新状态
        /// </summary>
        Task<ServiceResult<int>> BatchUpdateStatusAsync(IEnumerable<Guid> ids, bool isEnabled, string reason = "");
        
        #endregion
        
        #region 库存管理
        
        /// <summary>
        /// 更新库存
        /// </summary>
        Task<ServiceResult> UpdateStockAsync(Guid id, decimal newStock, string reason = "");
        
        /// <summary>
        /// 批量更新库存
        /// </summary>
        Task<ServiceResult<int>> BatchUpdateStockAsync(IEnumerable<(Guid Id, decimal Stock)> stockUpdates, string reason = "");
        
        /// <summary>
        /// 获取库存不足的中药材
        /// </summary>
        Task<ServiceResult<IEnumerable<HerbInfo>>> GetLowStockHerbsAsync(decimal threshold = 10);
        
        #endregion
        
        #region 分类和统计
        
        /// <summary>
        /// 获取中药材分类列表
        /// </summary>
        Task<ServiceResult<IEnumerable<string>>> GetCategoriesAsync();
        
        /// <summary>
        /// 根据分类获取中药材
        /// </summary>
        Task<ServiceResult<PagedResult<HerbInfo>>> GetByCategoryAsync(string category, PagedQueryBaseDto query);
        
        /// <summary>
        /// 获取中药材统计信息
        /// </summary>
        Task<ServiceResult<HerbStatisticsInfo>> GetStatisticsAsync();
        
        /// <summary>
        /// 获取热门中药材（常用药材）
        /// </summary>
        Task<ServiceResult<IEnumerable<HerbInfo>>> GetPopularHerbsAsync(int count = 10);
        
        #endregion
        
        #region 导入导出功能
        
        /// <summary>
        /// 导入中药材数据
        /// </summary>
        Task<ServiceResult<IEnumerable<HerbInfo>>> ImportAsync(string filePath);
        
        /// <summary>
        /// 导出中药材数据
        /// </summary>
        Task<ServiceResult> ExportAsync(IEnumerable<Guid> herbIds, string filePath);
        
        /// <summary>
        /// 生成导入模板
        /// </summary>
        Task<ServiceResult> GenerateImportTemplateAsync(string filePath);
        
        #endregion
    }
    
    /// <summary>
    /// 中药材统计信息
    /// </summary>
    public class HerbStatisticsInfo
    {
        public int TotalCount { get; set; }
        public int EnabledCount { get; set; }
        public int DisabledCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalValue { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
        public Dictionary<string, decimal> CategoryValues { get; set; } = new();
        public DateTime LastUpdateTime { get; set; }
        public string? MostExpensiveHerb { get; set; }
        public string? MostPopularCategory { get; set; }
    }
}