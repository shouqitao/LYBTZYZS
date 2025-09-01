using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Prescriptions.Interfaces;

/// <summary>
/// 处方业务服务接口 - UltraThink三层架构业务层
/// 职责：业务流程编排、完整事务管理、业务规则处理
/// </summary>
public interface IPrescriptionsBusinessService
{
    #region 事件定义

    /// <summary>
    /// 处方状态变更事件
    /// </summary>
    event EventHandler<PrescriptionStatusChangedEventArgs>? PrescriptionStatusChanged;

    /// <summary>
    /// 处方操作事件
    /// </summary>
    event EventHandler<PrescriptionOperationEventArgs>? PrescriptionOperation;

    /// <summary>
    /// 处方验证事件
    /// </summary>
    event EventHandler<PrescriptionValidationEventArgs>? PrescriptionValidation;

    #endregion

    #region 核心业务操作

    /// <summary>
    /// 创建处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionCreateDto createDto);

    /// <summary>
    /// 更新处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(Guid id, PrescriptionEditDto updateDto);

    /// <summary>
    /// 删除处方
    /// </summary>
    Task<ServiceResult<bool>> DeletePrescriptionAsync(Guid id);

    /// <summary>
    /// 复制处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> CopyPrescriptionAsync(Guid id, string newName);

    /// <summary>
    /// 从验方创建处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> CreateFromFormulaAsync(Guid formulaId, Guid patientId, Guid doctorId);

    #endregion

    #region 处方状态管理

    /// <summary>
    /// 完成处方
    /// </summary>
    Task<ServiceResult> CompletePrescriptionAsync(Guid id);

    /// <summary>
    /// 作废处方
    /// </summary>
    Task<ServiceResult> VoidPrescriptionAsync(Guid id, string reason);

    /// <summary>
    /// 重新激活处方
    /// </summary>
    Task<ServiceResult> ReactivatePrescriptionAsync(Guid id, string reason);

    /// <summary>
    /// 更新处方状态
    /// </summary>
    Task<ServiceResult> UpdateStatusAsync(Guid id, PrescriptionStatus status, string reason);

    /// <summary>
    /// 批量更新处方状态
    /// </summary>
    Task<ServiceResult<PrescriptionBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> prescriptionIds, PrescriptionStatus status);

    #endregion

    #region 处方项目管理

    /// <summary>
    /// 添加处方项目
    /// </summary>
    Task<ServiceResult<PrescriptionItemDto>> AddPrescriptionItemAsync(Guid prescriptionId, PrescriptionItemCreateDto itemDto);

    /// <summary>
    /// 更新处方项目
    /// </summary>
    Task<ServiceResult<PrescriptionItemDto>> UpdatePrescriptionItemAsync(Guid itemId, PrescriptionItemUpdateDto updateDto);

    /// <summary>
    /// 删除处方项目
    /// </summary>
    Task<ServiceResult<bool>> RemovePrescriptionItemAsync(Guid itemId);

    /// <summary>
    /// 批量更新处方项目
    /// </summary>
    Task<ServiceResult<int>> BatchUpdatePrescriptionItemsAsync(Guid prescriptionId, List<PrescriptionItemDto> items);

    /// <summary>
    /// 调整项目剂量
    /// </summary>
    Task<ServiceResult<PrescriptionItemDto>> AdjustItemQuantityAsync(Guid itemId, decimal newQuantity);

    #endregion

    #region 价格计算与折扣

    /// <summary>
    /// 计算处方总价格
    /// </summary>
    Task<ServiceResult<decimal>> CalculateTotalPriceAsync(Guid prescriptionId);

    /// <summary>
    /// 计算单剂价格
    /// </summary>
    Task<ServiceResult<decimal>> CalculateSingleDosePriceAsync(Guid prescriptionId);

    /// <summary>
    /// 应用折扣
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> ApplyDiscountAsync(Guid prescriptionId, decimal discountRate, string reason);

    /// <summary>
    /// 移除折扣
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> RemoveDiscountAsync(Guid prescriptionId);

    /// <summary>
    /// 批量计算处方价格
    /// </summary>
    Task<ServiceResult<PrescriptionBatchPriceDto>> CalculateBatchPricesAsync(List<Guid> prescriptionIds);

    /// <summary>
    /// 更新价格
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> UpdatePriceAsync(Guid prescriptionId);

    #endregion

    #region 业务验证与检查

    /// <summary>
    /// 验证处方完整性
    /// </summary>
    Task<ServiceResult<PrescriptionValidationResult>> ValidatePrescriptionCompletenessAsync(Guid prescriptionId);

    /// <summary>
    /// 检查配伍禁忌
    /// </summary>
    Task<ServiceResult<CompatibilityCheckResult>> CheckIngredientCompatibilityAsync(Guid prescriptionId);

    /// <summary>
    /// 检查剂量合理性
    /// </summary>
    Task<ServiceResult<DosageValidationResult>> ValidateDosageAsync(Guid prescriptionId);

    /// <summary>
    /// 检查处方权限
    /// </summary>
    Task<ServiceResult<bool>> CheckPrescriptionPermissionAsync(Guid prescriptionId, Guid userId);

    /// <summary>
    /// 检查是否可修改
    /// </summary>
    Task<ServiceResult<bool>> CanModifyAsync(Guid prescriptionId);

    /// <summary>
    /// 检查是否可删除
    /// </summary>
    Task<ServiceResult<bool>> CanDeleteAsync(Guid prescriptionId);

    /// <summary>
    /// 检查是否可作废
    /// </summary>
    Task<ServiceResult<bool>> CanVoidAsync(Guid prescriptionId);

    #endregion

    #region 处方使用与记录

    /// <summary>
    /// 记录处方使用
    /// </summary>
    Task<ServiceResult> RecordPrescriptionUsageAsync(Guid prescriptionId, PrescriptionUsageRecordDto usageRecord);

    /// <summary>
    /// 获取处方使用历史
    /// </summary>
    Task<ServiceResult<List<PrescriptionUsageHistoryDto>>> GetUsageHistoryAsync(Guid prescriptionId);

    /// <summary>
    /// 标记处方为已打印
    /// </summary>
    Task<ServiceResult> MarkAsPrintedAsync(Guid prescriptionId);

    /// <summary>
    /// 标记处方为已发放
    /// </summary>
    Task<ServiceResult> MarkAsDispensedAsync(Guid prescriptionId, PrescriptionDispenseDto dispenseInfo);

    #endregion

    #region 批量业务操作

    /// <summary>
    /// 批量删除处方
    /// </summary>
    Task<ServiceResult<PrescriptionBatchOperationResultDto>> BatchDeletePrescriptionsAsync(List<Guid> prescriptionIds);

    /// <summary>
    /// 批量作废处方
    /// </summary>
    Task<ServiceResult<PrescriptionBatchOperationResultDto>> BatchVoidPrescriptionsAsync(List<Guid> prescriptionIds, string reason);

    /// <summary>
    /// 批量复制处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> BatchCopyPrescriptionsAsync(List<Guid> prescriptionIds, Guid targetPatientId);

    /// <summary>
    /// 批量转移处方
    /// </summary>
    Task<ServiceResult<PrescriptionBatchOperationResultDto>> BatchTransferPrescriptionsAsync(List<Guid> prescriptionIds, Guid newDoctorId);

    #endregion

    #region 导入导出业务

    /// <summary>
    /// 导入处方
    /// </summary>
    Task<ServiceResult<PrescriptionImportResultDto>> ImportPrescriptionsAsync(PrescriptionImportDto importDto);

    /// <summary>
    /// 导出处方
    /// </summary>
    Task<ServiceResult<PrescriptionExportResultDto>> ExportPrescriptionsAsync(PrescriptionExportQueryDto exportQuery);

    /// <summary>
    /// 验证导入数据
    /// </summary>
    Task<ServiceResult<PrescriptionImportValidationResultDto>> ValidateImportDataAsync(PrescriptionImportDto importDto);

    /// <summary>
    /// 生成导入模板
    /// </summary>
    Task<ServiceResult<byte[]>> GenerateImportTemplateAsync();

    #endregion

    #region 智能推荐与分析

    /// <summary>
    /// 推荐相似处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> RecommendSimilarPrescriptionsAsync(Guid prescriptionId, int limit = 5);

    /// <summary>
    /// 基于症状推荐处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> RecommendPrescriptionsBySymptomAsync(List<string> symptoms, int limit = 10);

    /// <summary>
    /// 分析处方使用趋势
    /// </summary>
    Task<ServiceResult<PrescriptionUsageTrendDto>> AnalyzePrescriptionUsageTrendAsync(Guid prescriptionId, int days = 30);

    /// <summary>
    /// 分析药材搭配模式
    /// </summary>
    Task<ServiceResult<IngredientCombinationAnalysisDto>> AnalyzeIngredientCombinationAsync(List<Guid> herbIds);

    #endregion

    #region 业务流程管理

    /// <summary>
    /// 提交处方审核
    /// </summary>
    Task<ServiceResult> SubmitPrescriptionForReviewAsync(Guid prescriptionId, string reviewNote);

    /// <summary>
    /// 审核处方
    /// </summary>
    Task<ServiceResult> ReviewPrescriptionAsync(Guid prescriptionId, PrescriptionReviewDecisionDto decision);

    /// <summary>
    /// 发布处方
    /// </summary>
    Task<ServiceResult> PublishPrescriptionAsync(Guid prescriptionId);

    /// <summary>
    /// 归档处方
    /// </summary>
    Task<ServiceResult> ArchivePrescriptionAsync(Guid prescriptionId, string archiveReason);

    /// <summary>
    /// 恢复归档处方
    /// </summary>
    Task<ServiceResult> RestoreArchivedPrescriptionAsync(Guid prescriptionId);

    #endregion

    #region 高级功能

    /// <summary>
    /// 生成处方二维码
    /// </summary>
    Task<ServiceResult<byte[]>> GeneratePrescriptionQRCodeAsync(Guid prescriptionId);

    /// <summary>
    /// 生成处方PDF报告
    /// </summary>
    Task<ServiceResult<byte[]>> GeneratePrescriptionPdfReportAsync(Guid prescriptionId);

    /// <summary>
    /// 获取打印信息
    /// </summary>
    Task<ServiceResult<PrescriptionPrintInfoDto>> GetPrintInfoAsync(Guid prescriptionId);

    /// <summary>
    /// 分享处方
    /// </summary>
    Task<ServiceResult<PrescriptionShareTokenDto>> SharePrescriptionAsync(Guid prescriptionId, PrescriptionShareOptionsDto shareOptions);

    /// <summary>
    /// 收藏处方
    /// </summary>
    Task<ServiceResult> FavoritePrescriptionAsync(Guid prescriptionId, Guid userId);

    /// <summary>
    /// 取消收藏处方
    /// </summary>
    Task<ServiceResult> UnfavoritePrescriptionAsync(Guid prescriptionId, Guid userId);

    #endregion
}

/// <summary>
/// 处方批量操作结果DTO
/// </summary>
public class PrescriptionBatchOperationResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<Guid> SuccessfulIds { get; set; } = new();
    public List<Guid> FailedIds { get; set; } = new();
}

/// <summary>
/// 配伍检查结果DTO
/// </summary>
public class CompatibilityCheckResult
{
    public bool IsCompatible { get; set; }
    public List<string> ContraindicationWarnings { get; set; } = new();
    public List<string> InteractionWarnings { get; set; } = new();
    public List<string> RecommendedAdjustments { get; set; } = new();
    public double CompatibilityScore { get; set; }
}

/// <summary>
/// 剂量验证结果DTO
/// </summary>
public class DosageValidationResult
{
    public bool IsValid { get; set; }
    public List<string> DosageWarnings { get; set; } = new();
    public List<string> SafetyAlerts { get; set; } = new();
    public decimal TotalDosage { get; set; }
    public string DosageUnit { get; set; } = string.Empty;
}

/// <summary>
/// 处方使用记录DTO
/// </summary>
public class PrescriptionUsageRecordDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UsageType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime UsageTime { get; set; }
}

/// <summary>
/// 处方发放DTO
/// </summary>
public class PrescriptionDispenseDto
{
    public Guid PharmacistId { get; set; }
    public string PharmacistName { get; set; } = string.Empty;
    public DateTime DispenseTime { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// 处方使用趋势DTO
/// </summary>
public class PrescriptionUsageTrendDto
{
    public Guid PrescriptionId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public List<UsageTrendDataPoint> TrendData { get; set; } = new();
    public double TrendSlope { get; set; }
    public string TrendDirection { get; set; } = string.Empty;
}

/// <summary>
/// 使用趋势数据点
/// </summary>
public class UsageTrendDataPoint
{
    public DateTime Date { get; set; }
    public int UsageCount { get; set; }
}

/// <summary>
/// 药材组合分析DTO
/// </summary>
public class IngredientCombinationAnalysisDto
{
    public List<Guid> AnalyzedIngredients { get; set; } = new();
    public List<string> CommonCombinations { get; set; } = new();
    public List<string> CompatibilityAlerts { get; set; } = new();
    public double CompatibilityScore { get; set; }
    public List<string> RecommendedAdditions { get; set; } = new();
}

/// <summary>
/// 处方审核决策DTO
/// </summary>
public class PrescriptionReviewDecisionDto
{
    public bool IsApproved { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
    public List<string> RequiredChanges { get; set; } = new();
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
}

/// <summary>
/// 处方分享选项DTO
/// </summary>
public class PrescriptionShareOptionsDto
{
    public bool AllowCopy { get; set; }
    public bool AllowEdit { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<string> AllowedEmails { get; set; } = new();
    public string? ShareNote { get; set; }
}

/// <summary>
/// 处方分享令牌DTO
/// </summary>
public class PrescriptionShareTokenDto
{
    public string ShareToken { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// 处方导入结果DTO
/// </summary>
public class PrescriptionImportResultDto
{
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<PrescriptionDto> ImportedPrescriptions { get; set; } = new();
}

/// <summary>
/// 处方导出结果DTO
/// </summary>
public class PrescriptionExportResultDto
{
    public int TotalCount { get; set; }
    public string FileName { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// 处方导入验证结果DTO
/// </summary>
public class PrescriptionImportValidationResultDto
{
    public bool IsValid { get; set; }
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}