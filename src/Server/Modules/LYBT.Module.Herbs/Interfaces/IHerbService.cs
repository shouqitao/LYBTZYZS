using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{

    /// <summary>
    /// 药材业务服务接口（简化版）
    /// 只提供基础的药材信息维护功能，不包含库存管理
    /// </summary>
    public interface IHerbService
    {

        /// <summary>
        /// 获取药材详情
        /// </summary>
        Task<HerbDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有药材列表
        /// </summary>
        Task<List<HerbDto>> GetListAsync();

        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<PaginatedResult<HerbDto>> GetPagedAsync(HerbPagedQueryDto query);

        /// <summary>
        /// 新增药材
        /// </summary>
        Task<HerbDto?> AddAsync(HerbCreateDto dto);

        /// <summary>
        /// 编辑药材信息
        /// </summary>
        Task<bool> UpdateAsync(HerbUpdateDto dto);

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索药材（根据名称、拼音码）
        /// </summary>
        Task<List<HerbDto>> SearchAsync(string keyword);

        /// <summary>
        /// 获取可用药材列表（状态为启用）
        /// </summary>
        Task<List<HerbDto>> GetAvailableHerbsAsync();

        /// <summary>
        /// 设置药材启用/禁用状态
        /// </summary>
        Task<bool> SetStatusAsync(Guid id, bool isActive);

        /// <summary>
        /// 批量导入药材
        /// </summary>
        Task<int> ImportAsync(List<HerbImportDto> dtos);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        Task<List<HerbDetailDto>> ExportAsync();

        // ==================== 库存管理功能 ====================

        /// <summary>
        /// 获取库存预警药材列表（库存量低于预警值的药材）
        /// </summary>
        Task<List<HerbStockWarningDto>> GetStockWarningListAsync();

        /// <summary>
        /// 获取库存统计信息
        /// </summary>
        Task<HerbStockStatisticsDto> GetStockStatisticsAsync();

        /// <summary>
        /// 更新药材库存量（用于Pharmacy模块调用）
        /// </summary>
        Task<bool> UpdateStockAsync(Guid id, decimal quantity, bool isIncrease);

        /// <summary>
        /// 批量更新库存量（用于盘点）
        /// </summary>
        Task<int> BatchUpdateStockAsync(List<HerbStockUpdateDto> updates);

        /// <summary>
        /// 设置库存预警值
        /// </summary>
        Task<bool> SetStockWarningLevelAsync(Guid id, decimal warningLevel, decimal maxStock);

        /// <summary>
        /// 获取即将过期的药材（30天内）
        /// </summary>
        Task<List<HerbExpiryWarningDto>> GetExpiryWarningListAsync(int days = 30);

        // ==================== 价格管理功能 ====================

        /// <summary>
        /// 更新药材价格（包括成本价、零售价、会员价）
        /// </summary>
        Task<bool> UpdatePriceAsync(HerbPriceUpdateDto dto);

        /// <summary>
        /// 批量更新价格
        /// </summary>
        Task<int> BatchUpdatePriceAsync(List<HerbPriceUpdateDto> updates);

        /// <summary>
        /// 设置特价促销
        /// </summary>
        Task<bool> SetSpecialPriceAsync(Guid id, decimal specialPrice, DateTime startTime, DateTime endTime);

        /// <summary>
        /// 取消特价促销
        /// </summary>
        Task<bool> CancelSpecialPriceAsync(Guid id);

        /// <summary>
        /// 获取当前特价药材列表
        /// </summary>
        Task<List<HerbDto>> GetSpecialPriceHerbsAsync();

        /// <summary>
        /// 获取价格历史记录
        /// </summary>
        Task<List<HerbPriceHistoryDto>> GetPriceHistoryAsync(Guid id);

        /// <summary>
        /// 按价格区间查询药材
        /// </summary>
        Task<List<HerbDto>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    }
}