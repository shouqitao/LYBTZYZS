using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formulas;

namespace LYBT.Desktop.Formula.Interfaces;

/// <summary>
/// 验方核心服务接口 - UltraThink三层架构核心层
/// 职责：API通信、数据验证、缓存管理、基础操作
/// </summary>
public interface IFormulaCoreService
{
    #region API通信层

    /// <summary>
    /// 调用创建验方API
    /// </summary>
    Task<ServiceResult<FormulaDto>> CallCreateFormulaApiAsync(FormulaCreateDto createDto);

    /// <summary>
    /// 调用更新验方API
    /// </summary>
    Task<ServiceResult<FormulaDto>> CallUpdateFormulaApiAsync(Guid id, FormulaUpdateDto updateDto);

    /// <summary>
    /// 调用删除验方API
    /// </summary>
    Task<ServiceResult<bool>> CallDeleteFormulaApiAsync(Guid id);

    /// <summary>
    /// 调用获取验方详情API
    /// </summary>
    Task<ServiceResult<FormulaDto>> CallGetFormulaByIdApiAsync(Guid id);

    /// <summary>
    /// 调用获取验方列表API
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> CallGetFormulasApiAsync();

    /// <summary>
    /// 调用分页查询验方API
    /// </summary>
    Task<ServiceResult<PagedResult<FormulaDto>>> CallGetPagedFormulasApiAsync(FormulaPagedQueryDto query);

    /// <summary>
    /// 调用搜索验方API
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> CallSearchFormulasApiAsync(FormulaSearchDto searchDto);

    /// <summary>
    /// 调用更新验方状态API
    /// </summary>
    Task<ServiceResult<bool>> CallUpdateFormulaStatusApiAsync(Guid id, bool isEnabled);

    /// <summary>
    /// 调用批量操作验方API
    /// </summary>
    Task<ServiceResult<FormulaBatchOperationResultDto>> CallBatchOperateFormulasApiAsync(FormulaBatchOperationDto operationDto);

    /// <summary>
    /// 调用验方统计API
    /// </summary>
    Task<ServiceResult<FormulaStatisticsDto>> CallGetFormulaStatisticsApiAsync();

    /// <summary>
    /// 调用验方导入API
    /// </summary>
    Task<ServiceResult<FormulaImportResultDto>> CallImportFormulasApiAsync(FormulaImportDto importDto);

    /// <summary>
    /// 调用验方导出API
    /// </summary>
    Task<ServiceResult<FormulaExportResultDto>> CallExportFormulasApiAsync(FormulaExportQueryDto exportQuery);

    #endregion

    #region 数据验证层

    /// <summary>
    /// 验证验方创建数据
    /// </summary>
    ServiceResult ValidateFormulaCreateData(FormulaCreateDto createDto);

    /// <summary>
    /// 验证验方更新数据
    /// </summary>
    ServiceResult ValidateFormulaUpdateData(FormulaUpdateDto updateDto);

    /// <summary>
    /// 验证验方ID
    /// </summary>
    ServiceResult ValidateFormulaId(Guid formulaId);

    /// <summary>
    /// 验证验方名称格式
    /// </summary>
    ServiceResult ValidateFormulaName(string name);

    /// <summary>
    /// 验证验方类型
    /// </summary>
    ServiceResult ValidateFormulaType(string type);

    /// <summary>
    /// 验证验方药材列表
    /// </summary>
    ServiceResult ValidateFormulaIngredients(List<FormulaIngredientDto> ingredients);

    /// <summary>
    /// 验证验方配伍
    /// </summary>
    ServiceResult ValidateFormulaCompatibility(FormulaDto formula);

    /// <summary>
    /// 验证批量操作数据
    /// </summary>
    ServiceResult ValidateBatchOperationData(List<Guid> formulaIds, string operation);

    /// <summary>
    /// 验证搜索参数
    /// </summary>
    ServiceResult ValidateSearchParameters(FormulaSearchDto searchDto);

    /// <summary>
    /// 验证分页参数
    /// </summary>
    ServiceResult ValidatePagedQueryParameters(FormulaPagedQueryDto query);

    #endregion

    #region 缓存管理层

    /// <summary>
    /// 获取缓存的验方
    /// </summary>
    Task<ServiceResult<FormulaDto?>> GetCachedFormulaAsync(Guid formulaId);

    /// <summary>
    /// 缓存验方数据
    /// </summary>
    Task SetFormulaCacheAsync(Guid formulaId, FormulaDto formula);

    /// <summary>
    /// 获取缓存的验方列表
    /// </summary>
    Task<ServiceResult<List<FormulaDto>?>> GetCachedFormulasAsync(string cacheKey);

    /// <summary>
    /// 缓存验方列表
    /// </summary>
    Task SetFormulasCacheAsync(string cacheKey, List<FormulaDto> formulas);

    /// <summary>
    /// 清除验方缓存
    /// </summary>
    void ClearFormulaCache(Guid? formulaId = null);

    /// <summary>
    /// 清除所有验方相关缓存
    /// </summary>
    void ClearAllFormulaCache();

    /// <summary>
    /// 获取缓存统计
    /// </summary>
    ServiceResult<FormulaCacheStatsDto> GetCacheStats();

    /// <summary>
    /// 预热缓存
    /// </summary>
    Task<ServiceResult> WarmupCacheAsync();

    #endregion

    #region 基础操作层

    /// <summary>
    /// 获取验方基础信息
    /// </summary>
    Task<ServiceResult<FormulaDto>> GetFormulaBasicInfoAsync(Guid formulaId);

    /// <summary>
    /// 获取验方详细信息
    /// </summary>
    Task<ServiceResult<FormulaDetailDto>> GetFormulaDetailAsync(Guid formulaId);

    /// <summary>
    /// 检查验方是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckFormulaExistsAsync(Guid formulaId);

    /// <summary>
    /// 检查验方名称是否可用
    /// </summary>
    Task<ServiceResult<bool>> CheckFormulaNameAvailableAsync(string name, Guid? excludeFormulaId = null);

    /// <summary>
    /// 获取验方的药材列表
    /// </summary>
    Task<ServiceResult<List<FormulaIngredientDto>>> GetFormulaIngredientsAsync(Guid formulaId);

    /// <summary>
    /// 检查验方权限
    /// </summary>
    Task<ServiceResult<bool>> CheckFormulaPermissionAsync(Guid formulaId, Guid userId, string operation);

    /// <summary>
    /// 记录验方操作日志
    /// </summary>
    Task LogFormulaOperationAsync(string operation, Guid formulaId, Guid? userId = null, object? additionalData = null);

    /// <summary>
    /// 生成验方缓存键
    /// </summary>
    string GenerateFormulaCacheKey(Guid formulaId);

    /// <summary>
    /// 生成验方列表缓存键
    /// </summary>
    string GenerateFormulasListCacheKey(string operation, object? parameters = null);

    #endregion
}

/// <summary>
/// 验方缓存统计DTO
/// </summary>
public class FormulaCacheStatsDto
{
    public int TotalCachedItems { get; set; }
    public double HitRate { get; set; }
    public int TotalHits { get; set; }
    public int TotalMisses { get; set; }
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// 验方批量操作DTO
/// </summary>
public class FormulaBatchOperationDto
{
    public List<Guid> FormulaIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 验方批量操作结果DTO
/// </summary>
public class FormulaBatchOperationResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailureReasons { get; set; } = new();
    public List<Guid> ProcessedIds { get; set; } = new();
    public DateTime OperationTime { get; set; } = DateTime.Now;
}