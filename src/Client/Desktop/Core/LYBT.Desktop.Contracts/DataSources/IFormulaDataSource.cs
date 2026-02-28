using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 验方数据源接口
/// </summary>
public interface IFormulaDataSource : IDataSourceBase<FormulaDetailDto, FormulaInputDto>
{
    /// <summary>
    /// 克隆验方（深拷贝，包含药材组成）
    /// </summary>
    Task<FormulaDetailDto?> CloneAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 切换验方状态（启用/禁用）
    /// </summary>
    Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 恢复已删除的验方
    /// </summary>
    Task<FormulaDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 获取验方详情（包含药材组成）
    /// </summary>
    Task<FormulaDetailDto?> GetWithHerbsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 分页获取验方列表（带分类过滤）
    /// </summary>
    Task<(List<FormulaDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? category,
        CancellationToken ct = default);

    // Sprint 4 X2 扩展方法
    // OpenSpec: SYNC-D02 - 过渡态方法

    /// <summary>
    /// T4-X2-19/21: 批量导入验方 (延迟绑定，标记为 Draft)
    /// </summary>
    Task<BatchOperationResultDto> BatchImportAsync(List<FormulaImportItemDto> items, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-20: 获取待验证验方列表 (ValidationStatus == Draft)
    /// </summary>
    Task<List<FormulaDetailDto>> GetPendingValidationAsync(CancellationToken ct = default);

    /// <summary>
    /// T4-X2-22: 获取导出数据
    /// </summary>
    Task<List<FormulaDetailDto>> GetAllForExportAsync(string? keyword = null, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-19: 验证验方药材绑定
    /// </summary>
    Task<bool> ValidateHerbBindingsAsync(Guid formulaId, CancellationToken ct = default);

    /// <summary>
    /// T5-P2-37: 批量启用/禁用验方
    /// </summary>
    Task<BatchOperationResultDto> BatchToggleStatusAsync(List<Guid> ids, bool enable, CancellationToken ct = default);

    /// <summary>
    /// T5-P2-38: 获取导入模板列定义（主表）
    /// </summary>
    string[] GetImportTemplateColumns();

    /// <summary>
    /// T5-P2-38: 获取导入模板列定义（药材明细）
    /// </summary>
    string[] GetImportTemplateHerbColumns();
}
