using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces
{
    /// <summary>
    /// 验方数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IFormulaRepository
    {
        Task<PagedResult<FormulaDetailDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 获取验方列表（返回FormulaListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        Task<PagedResult<FormulaListDto>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);
        Task<FormulaDetailDto?> GetByIdAsync(Guid id);
        Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto);
        Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<FormulaDetailDto>> SearchAsync(string keyword);
        Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId);

        /// <summary>
        /// 获取待校验的验方列表 (Issue #1349)
        /// </summary>
        Task<List<FormulaDetailDto>> GetPendingValidationFormulasAsync();

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        Task<bool> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId);

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复 ==========

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        Task<FormulaDetailDto?> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复已删除的验方
        /// </summary>
        Task<FormulaDetailDto?> RestoreAsync(Guid id);

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

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
    }
}
