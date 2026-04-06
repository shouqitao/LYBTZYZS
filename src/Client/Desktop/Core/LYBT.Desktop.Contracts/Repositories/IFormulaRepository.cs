using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Contracts.Repositories;

/// <summary>
/// 验方数据仓储接口 (SYNC-D02)
/// List 返回轻量 ListDto，Detail 返回完整 DetailDto。
/// 远程模式和本地模式各有独立实现，由 DI 工厂根据 IConnectionModeProvider 选择。
/// </summary>
public interface IFormulaRepository
{
    /// <summary>
    /// 分页查询验方列表 (返回轻量级 ListDto，支持分类过滤)
    /// </summary>
    Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);

    /// <summary>
    /// 根据 ID 获取验方详情 (返回完整 DetailDto，含药材子项)
    /// </summary>
    Task<FormulaDetailDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建新验方
    /// </summary>
    Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto);

    /// <summary>
    /// 更新验方信息
    /// </summary>
    Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto);

    /// <summary>
    /// 删除验方 (软删除)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// 搜索验方 (基于关键词，返回 ListDto)
    /// </summary>
    Task<List<FormulaListDto>> SearchAsync(string keyword);

    /// <summary>
    /// 克隆验方
    /// </summary>
    Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId);

    // OpenSpec: cleanup-formula-dead-code - 已删除 GetPendingValidationFormulasAsync/ValidateFormulaHerbAsync

    #region 状态切换、恢复和批量操作

    /// <summary>
    /// 切换验方状态 (启用/禁用)
    /// </summary>
    Task<FormulaDetailDto?> ToggleStatusAsync(Guid id);

    /// <summary>
    /// 恢复已删除的验方
    /// </summary>
    Task<FormulaDetailDto?> RestoreAsync(Guid id);

    /// <summary>
    /// 批量删除验方
    /// </summary>
    Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids);

    /// <summary>
    /// 批量启用验方
    /// </summary>
    Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids);

    /// <summary>
    /// 批量禁用验方
    /// </summary>
    Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids);

    #endregion

    #region 批量导入/导出

    /// <summary>
    /// 批量导入验方数据
    /// </summary>
    Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 导出验方数据到Excel
    /// </summary>
    Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default);

    /// <summary>
    /// 下载验方导入模板
    /// </summary>
    Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default);

    #endregion
}
