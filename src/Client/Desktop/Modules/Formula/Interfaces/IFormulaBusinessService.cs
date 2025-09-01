using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Desktop.Formula.Interfaces;

namespace LYBT.Desktop.Formula.Interfaces;

/// <summary>
/// 验方业务服务接口 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、完整事务管理、业务规则处理
/// </summary>
public interface IFormulaBusinessService
{
    #region 事件定义

    /// <summary>
    /// 验方状态变更事件
    /// </summary>
    event EventHandler<FormulaStatusChangedEventArgs>? FormulaStatusChanged;

    /// <summary>
    /// 验方操作事件
    /// </summary>
    event EventHandler<FormulaOperationEventArgs>? FormulaOperation;

    /// <summary>
    /// 验方验证事件
    /// </summary>
    event EventHandler<FormulaValidationEventArgs>? FormulaValidation;

    #endregion

    #region 核心业务操作

    /// <summary>
    /// 创建验方
    /// </summary>
    Task<ServiceResult<FormulaDto>> CreateFormulaAsync(FormulaCreateDto createDto);

    /// <summary>
    /// 更新验方
    /// </summary>
    Task<ServiceResult<FormulaDto>> UpdateFormulaAsync(Guid id, FormulaUpdateDto updateDto);

    /// <summary>
    /// 删除验方
    /// </summary>
    Task<ServiceResult<bool>> DeleteFormulaAsync(Guid id);

    /// <summary>
    /// 启用验方
    /// </summary>
    Task<ServiceResult> EnableFormulaAsync(Guid id);

    /// <summary>
    /// 禁用验方
    /// </summary>
    Task<ServiceResult> DisableFormulaAsync(Guid id);

    /// <summary>
    /// 克隆验方
    /// </summary>
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId, string newName, Guid userId);

    #endregion

    #region 验方药材管理

    /// <summary>
    /// 添加验方药材
    /// </summary>
    Task<ServiceResult<FormulaIngredientDto>> AddFormulaIngredientAsync(Guid formulaId, FormulaIngredientCreateDto ingredientDto);

    /// <summary>
    /// 更新验方药材
    /// </summary>
    Task<ServiceResult<FormulaIngredientDto>> UpdateFormulaIngredientAsync(Guid ingredientId, FormulaIngredientUpdateDto updateDto);

    /// <summary>
    /// 删除验方药材
    /// </summary>
    Task<ServiceResult<bool>> RemoveFormulaIngredientAsync(Guid ingredientId);

    /// <summary>
    /// 批量更新验方药材
    /// </summary>
    Task<ServiceResult<int>> BatchUpdateFormulaIngredientsAsync(Guid formulaId, List<FormulaIngredientDto> ingredients);

    /// <summary>
    /// 调整药材剂量
    /// </summary>
    Task<ServiceResult<FormulaIngredientDto>> AdjustIngredientDosageAsync(Guid ingredientId, decimal newDosage);

    #endregion

    #region 验方验证与检查

    /// <summary>
    /// 验证验方完整性
    /// </summary>
    Task<ServiceResult<FormulaValidationResultDto>> ValidateFormulaCompletenessAsync(Guid formulaId);

    /// <summary>
    /// 检查配伍禁忌
    /// </summary>
    Task<ServiceResult<FormulaCompatibilityResultDto>> CheckFormulaCompatibilityAsync(Guid formulaId);

    /// <summary>
    /// 检查剂量合理性
    /// </summary>
    Task<ServiceResult<DosageValidationResultDto>> ValidateFormulaDosageAsync(Guid formulaId);

    /// <summary>
    /// 验证验方名称可用性
    /// </summary>
    Task<ServiceResult<bool>> CheckNameAvailabilityAsync(string name, Guid? excludeFormulaId = null);

    /// <summary>
    /// 检查验方使用权限
    /// </summary>
    Task<ServiceResult<bool>> CheckFormulaUsagePermissionAsync(Guid formulaId, Guid userId);

    #endregion

    #region 验方使用与记录

    /// <summary>
    /// 记录验方使用
    /// </summary>
    Task<ServiceResult> RecordFormulaUsageAsync(Guid formulaId, FormulaUsageRecordDto usageRecord);

    /// <summary>
    /// 获取验方使用历史
    /// </summary>
    Task<ServiceResult<List<FormulaUsageHistoryDto>>> GetFormulaUsageHistoryAsync(Guid formulaId);

    /// <summary>
    /// 添加验方评价
    /// </summary>
    Task<ServiceResult<FormulaReviewDto>> AddFormulaReviewAsync(FormulaReviewCreateDto reviewDto);

    /// <summary>
    /// 更新验方评价
    /// </summary>
    Task<ServiceResult<FormulaReviewDto>> UpdateFormulaReviewAsync(Guid reviewId, FormulaReviewUpdateDto updateDto);

    /// <summary>
    /// 删除验方评价
    /// </summary>
    Task<ServiceResult<bool>> DeleteFormulaReviewAsync(Guid reviewId);

    #endregion

    #region 批量业务操作

    /// <summary>
    /// 批量更新验方状态
    /// </summary>
    Task<ServiceResult<FormulaBatchOperationResultDto>> BatchUpdateFormulaStatusAsync(List<Guid> formulaIds, bool isEnabled);

    /// <summary>
    /// 批量删除验方
    /// </summary>
    Task<ServiceResult<FormulaBatchOperationResultDto>> BatchDeleteFormulasAsync(List<Guid> formulaIds);

    /// <summary>
    /// 批量转移验方所有权
    /// </summary>
    Task<ServiceResult<FormulaBatchOperationResultDto>> BatchTransferFormulaOwnershipAsync(List<Guid> formulaIds, Guid newOwnerId);

    /// <summary>
    /// 批量复制验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> BatchCloneFormulasAsync(List<Guid> formulaIds, Guid targetUserId);

    #endregion

    #region 导入导出业务

    /// <summary>
    /// 导入验方
    /// </summary>
    Task<ServiceResult<FormulaImportResultDto>> ImportFormulasAsync(FormulaImportDto importDto);

    /// <summary>
    /// 导出验方
    /// </summary>
    Task<ServiceResult<FormulaExportResultDto>> ExportFormulasAsync(FormulaExportQueryDto exportQuery);

    /// <summary>
    /// 验证导入数据
    /// </summary>
    Task<ServiceResult<FormulaImportValidationResultDto>> ValidateImportDataAsync(FormulaImportDto importDto);

    /// <summary>
    /// 生成导出模板
    /// </summary>
    Task<ServiceResult<byte[]>> GenerateImportTemplateAsync();

    #endregion

    #region 智能推荐与分析

    /// <summary>
    /// 推荐相似验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> RecommendSimilarFormulasAsync(Guid formulaId, int limit = 5);

    /// <summary>
    /// 基于症状推荐验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> RecommendFormulasBySymptomAsync(List<string> symptoms, int limit = 10);

    /// <summary>
    /// 分析验方使用趋势
    /// </summary>
    Task<ServiceResult<FormulaUsageTrendDto>> AnalyzeFormulaUsageTrendAsync(Guid formulaId, int days = 30);

    /// <summary>
    /// 分析药材搭配模式
    /// </summary>
    Task<ServiceResult<IngredientCombinationAnalysisDto>> AnalyzeIngredientCombinationAsync(List<Guid> herbIds);

    #endregion

    #region 业务流程管理

    /// <summary>
    /// 提交验方审核
    /// </summary>
    Task<ServiceResult> SubmitFormulaForReviewAsync(Guid formulaId, string reviewNote);

    /// <summary>
    /// 审核验方
    /// </summary>
    Task<ServiceResult> ReviewFormulaAsync(Guid formulaId, FormulaReviewDecisionDto decision);

    /// <summary>
    /// 发布验方
    /// </summary>
    Task<ServiceResult> PublishFormulaAsync(Guid formulaId);

    /// <summary>
    /// 归档验方
    /// </summary>
    Task<ServiceResult> ArchiveFormulaAsync(Guid formulaId, string archiveReason);

    /// <summary>
    /// 恢复归档验方
    /// </summary>
    Task<ServiceResult> RestoreArchivedFormulaAsync(Guid formulaId);

    #endregion

    #region 权限与安全

    /// <summary>
    /// 设置验方访问权限
    /// </summary>
    Task<ServiceResult> SetFormulaPermissionAsync(Guid formulaId, FormulaPermissionDto permission);

    /// <summary>
    /// 检查验方操作权限
    /// </summary>
    Task<ServiceResult<bool>> CheckOperationPermissionAsync(Guid formulaId, Guid userId, string operation);

    /// <summary>
    /// 记录验方访问日志
    /// </summary>
    Task<ServiceResult> LogFormulaAccessAsync(Guid formulaId, Guid userId, string operation);

    #endregion

    #region 高级功能

    /// <summary>
    /// 生成验方二维码
    /// </summary>
    Task<ServiceResult<byte[]>> GenerateFormulaQRCodeAsync(Guid formulaId);

    /// <summary>
    /// 生成验方PDF报告
    /// </summary>
    Task<ServiceResult<byte[]>> GenerateFormulaPdfReportAsync(Guid formulaId);

    /// <summary>
    /// 分享验方
    /// </summary>
    Task<ServiceResult<FormulaShareTokenDto>> ShareFormulaAsync(Guid formulaId, FormulaShareOptionsDto shareOptions);

    /// <summary>
    /// 收藏验方
    /// </summary>
    Task<ServiceResult> FavoriteFormulaAsync(Guid formulaId, Guid userId);

    /// <summary>
    /// 取消收藏验方
    /// </summary>
    Task<ServiceResult> UnfavoriteFormulaAsync(Guid formulaId, Guid userId);

    #endregion
}

/// <summary>
/// 验方验证结果DTO
/// </summary>
public class FormulaValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 验方配伍检查结果DTO
/// </summary>
public class FormulaCompatibilityResultDto
{
    public bool IsCompatible { get; set; }
    public List<string> ContraindicationWarnings { get; set; } = new();
    public List<string> InteractionWarnings { get; set; } = new();
    public List<string> RecommendedAdjustments { get; set; } = new();
}

/// <summary>
/// 剂量验证结果DTO
/// </summary>
public class DosageValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> DosageWarnings { get; set; } = new();
    public List<string> SafetyAlerts { get; set; } = new();
    public decimal TotalDosage { get; set; }
    public string DosageUnit { get; set; } = string.Empty;
}

/// <summary>
/// 验方使用记录DTO
/// </summary>
public class FormulaUsageRecordDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UsageType { get; set; } = string.Empty;
    public Guid? PrescriptionId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// 验方使用趋势DTO
/// </summary>
public class FormulaUsageTrendDto
{
    public Guid FormulaId { get; set; }
    public string FormulaName { get; set; } = string.Empty;
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
/// 验方审核决策DTO
/// </summary>
public class FormulaReviewDecisionDto
{
    public bool IsApproved { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
    public List<string> RequiredChanges { get; set; } = new();
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
}

/// <summary>
/// 验方权限DTO
/// </summary>
public class FormulaPermissionDto
{
    public Guid FormulaId { get; set; }
    public bool IsPublic { get; set; }
    public List<Guid> AllowedUsers { get; set; } = new();
    public List<string> AllowedRoles { get; set; } = new();
    public bool AllowCopy { get; set; }
    public bool AllowEdit { get; set; }
    public bool AllowShare { get; set; }
}

/// <summary>
/// 验方分享选项DTO
/// </summary>
public class FormulaShareOptionsDto
{
    public bool AllowCopy { get; set; }
    public bool AllowEdit { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<string> AllowedEmails { get; set; } = new();
    public string? ShareNote { get; set; }
}

/// <summary>
/// 验方分享令牌DTO
/// </summary>
public class FormulaShareTokenDto
{
    public string ShareToken { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// 验方导入验证结果DTO
/// </summary>
public class FormulaImportValidationResultDto
{
    public bool IsValid { get; set; }
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}