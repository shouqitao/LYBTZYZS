using LYBT.Shared.Models;
using LYBT.Shared.Models.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces;

/// <summary>
/// 中药材模块主接口 - UltraThink三层架构纯委托层
/// 职责：统一服务入口，纯委托模式，无业务逻辑
/// </summary>
public interface IHerbModule
{
    #region 基础CRUD操作 (委托给CoreService)
    
    /// <summary>
    /// 根据ID获取中药材信息
    /// </summary>
    Task<ServiceResult<HerbDto>> GetHerbByIdAsync(Guid id);
    
    /// <summary>
    /// 获取所有中药材列表
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> GetAllHerbsAsync();
    
    /// <summary>
    /// 创建新的中药材
    /// </summary>
    Task<ServiceResult<HerbDto>> CreateHerbAsync(HerbCreateDto createDto);
    
    /// <summary>
    /// 更新中药材信息
    /// </summary>
    Task<ServiceResult<HerbDto>> UpdateHerbAsync(Guid id, HerbUpdateDto updateDto);
    
    /// <summary>
    /// 删除中药材
    /// </summary>
    Task<ServiceResult<bool>> DeleteHerbAsync(Guid id);
    
    #endregion
    
    #region 查询和搜索功能 (委托给QueryService)
    
    /// <summary>
    /// 搜索中药材
    /// </summary>
    Task<ServiceResult<PagedResult<HerbDto>>> SearchHerbsAsync(HerbSearchDto searchDto);
    
    /// <summary>
    /// 按分类筛选中药材
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> GetHerbsByCategoryAsync(string category);
    
    /// <summary>
    /// 获取中药材统计信息
    /// </summary>
    Task<ServiceResult<HerbStatisticsDto>> GetHerbStatisticsAsync();
    
    /// <summary>
    /// 获取价格趋势分析
    /// </summary>
    Task<ServiceResult<List<HerbPriceTrendDto>>> GetPriceTrendsAsync(Guid herbId, int days = 30);
    
    #endregion
    
    #region 业务逻辑功能 (委托给BusinessService)
    
    /// <summary>
    /// 检查药材配伍禁忌
    /// </summary>
    Task<ServiceResult<CompatibilityCheckResult>> CheckCompatibilityAsync(List<Guid> herbIds);
    
    /// <summary>
    /// 计算处方总价
    /// </summary>
    Task<ServiceResult<decimal>> CalculateFormulaPriceAsync(List<HerbDosageDto> herbDosages);
    
    /// <summary>
    /// 获取价格变更历史
    /// </summary>
    Task<ServiceResult<List<HerbPriceHistoryDto>>> GetPriceHistoryAsync(Guid herbId);
    
    /// <summary>
    /// 批量导入中药材数据
    /// </summary>
    Task<ServiceResult<HerbImportResultDto>> ImportHerbsFromExcelAsync(string filePath);
    
    /// <summary>
    /// 导出中药材数据到Excel
    /// </summary>
    Task<ServiceResult<string>> ExportHerbsToExcelAsync(HerbExportDto exportDto);
    
    #endregion
}