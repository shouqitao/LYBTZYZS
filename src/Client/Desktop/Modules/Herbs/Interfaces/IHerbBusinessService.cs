using LYBT.Shared.Models;
using LYBT.Shared.Models.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces;

/// <summary>
/// 中药材业务服务接口 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、复杂业务逻辑、事务管理、业务规则验证
/// </summary>
public interface IHerbBusinessService
{
    #region 中药材业务管理
    
    /// <summary>
    /// 创建新的中药材 (包含完整业务流程)
    /// </summary>
    Task<ServiceResult<HerbDto>> CreateHerbAsync(HerbCreateDto createDto);
    
    /// <summary>
    /// 更新中药材信息 (包含业务验证和历史记录)
    /// </summary>
    Task<ServiceResult<HerbDto>> UpdateHerbAsync(Guid id, HerbUpdateDto updateDto);
    
    /// <summary>
    /// 删除中药材 (软删除，保留历史记录)
    /// </summary>
    Task<ServiceResult<bool>> DeleteHerbAsync(Guid id);
    
    /// <summary>
    /// 批量更新中药材价格
    /// </summary>
    Task<ServiceResult<List<HerbPriceUpdateResultDto>>> BatchUpdatePricesAsync(List<HerbPriceUpdateDto> priceUpdates);
    
    /// <summary>
    /// 恢复已删除的中药材
    /// </summary>
    Task<ServiceResult<HerbDto>> RestoreDeletedHerbAsync(Guid id);
    
    #endregion
    
    #region 配伍检查和验证
    
    /// <summary>
    /// 检查药材配伍禁忌
    /// </summary>
    Task<ServiceResult<CompatibilityCheckResult>> CheckCompatibilityAsync(List<Guid> herbIds);
    
    /// <summary>
    /// 验证处方中的药材组合
    /// </summary>
    Task<ServiceResult<PrescriptionValidationResult>> ValidatePrescriptionHerbsAsync(List<HerbDosageDto> herbDosages);
    
    /// <summary>
    /// 获取配伍建议
    /// </summary>
    Task<ServiceResult<List<CompatibilitySuggestionDto>>> GetCompatibilitySuggestionsAsync(List<Guid> herbIds);
    
    /// <summary>
    /// 检查单个药材的使用注意事项
    /// </summary>
    Task<ServiceResult<HerbUsagePrecautionDto>> CheckHerbUsagePrecautionsAsync(Guid herbId);
    
    #endregion
    
    #region 价格管理业务
    
    /// <summary>
    /// 计算处方总价格 (包含折扣和优惠)
    /// </summary>
    Task<ServiceResult<PrescriptionPriceCalculationDto>> CalculateFormulaPriceAsync(List<HerbDosageDto> herbDosages, Guid? patientId = null);
    
    /// <summary>
    /// 更新药材价格 (包含历史记录和通知)
    /// </summary>
    Task<ServiceResult<HerbPriceUpdateResultDto>> UpdateHerbPriceAsync(Guid herbId, decimal newPrice, string reason);
    
    /// <summary>
    /// 应用价格策略 (VIP折扣、批量优惠等)
    /// </summary>
    Task<ServiceResult<decimal>> ApplyPricingPolicyAsync(decimal originalPrice, Guid? patientId, int quantity);
    
    /// <summary>
    /// 生成价格变更通知
    /// </summary>
    Task<ServiceResult<bool>> NotifyPriceChangesAsync(List<HerbPriceUpdateDto> priceChanges);
    
    #endregion
    
    #region 导入导出业务
    
    /// <summary>
    /// 从Excel批量导入中药材数据 (包含验证和冲突处理)
    /// </summary>
    Task<ServiceResult<HerbImportResultDto>> ImportHerbsFromExcelAsync(string filePath, bool overwriteExisting = false);
    
    /// <summary>
    /// 导出中药材数据到Excel (包含格式化和模板应用)
    /// </summary>
    Task<ServiceResult<string>> ExportHerbsToExcelAsync(HerbExportDto exportDto);
    
    /// <summary>
    /// 验证导入数据格式
    /// </summary>
    Task<ServiceResult<HerbImportValidationDto>> ValidateImportDataAsync(string filePath);
    
    /// <summary>
    /// 生成导入模板文件
    /// </summary>
    Task<ServiceResult<string>> GenerateImportTemplateAsync(string templateType);
    
    #endregion
    
    #region 业务流程管理
    
    /// <summary>
    /// 处理中药材审核流程
    /// </summary>
    Task<ServiceResult<bool>> ProcessHerbApprovalAsync(Guid herbId, HerbApprovalDto approvalDto);
    
    /// <summary>
    /// 同步中药材数据到外部系统
    /// </summary>
    Task<ServiceResult<bool>> SyncHerbDataToExternalSystemAsync(List<Guid> herbIds);
    
    /// <summary>
    /// 归档长期未使用的药材
    /// </summary>
    Task<ServiceResult<List<Guid>>> ArchiveUnusedHerbsAsync(int unusedDays = 365);
    
    /// <summary>
    /// 重建中药材索引 (性能优化)
    /// </summary>
    Task<ServiceResult<bool>> RebuildHerbIndexAsync();
    
    #endregion
    
    #region 智能推荐和分析
    
    /// <summary>
    /// 基于症状智能推荐药材
    /// </summary>
    Task<ServiceResult<List<HerbRecommendationDto>>> RecommendHerbsForSymptomsAsync(List<string> symptoms, Guid? patientId = null);
    
    /// <summary>
    /// 分析药材使用模式
    /// </summary>
    Task<ServiceResult<HerbUsagePatternDto>> AnalyzeHerbUsagePatternsAsync(Guid? doctorId = null, int days = 90);
    
    /// <summary>
    /// 生成药材采购建议
    /// </summary>
    Task<ServiceResult<List<HerbPurchaseSuggestionDto>>> GeneratePurchaseSuggestionsAsync(int forecastDays = 30);
    
    /// <summary>
    /// 优化处方配方建议
    /// </summary>
    Task<ServiceResult<PrescriptionOptimizationDto>> OptimizePrescriptionAsync(List<HerbDosageDto> currentFormula);
    
    #endregion
}