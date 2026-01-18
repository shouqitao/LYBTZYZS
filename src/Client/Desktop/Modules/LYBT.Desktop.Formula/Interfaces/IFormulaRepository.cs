using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces
{
    /// <summary>
    /// 验方数据仓储接口 - RESTful设计
    /// List返回轻量ListDto，Detail返回完整DetailDto
    /// </summary>
    public interface IFormulaRepository
    {
        /// <summary>
        /// 分页查询验方列表（返回轻量级ListDto）
        /// </summary>
        Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);

        /// <summary>
        /// 根据ID获取验方详情（返回完整DetailDto）
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
        /// 删除验方（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索验方（基于关键词，返回ListDto）
        /// </summary>
        Task<List<FormulaListDto>> SearchAsync(string keyword);

        /// <summary>
        /// 克隆验方
        /// </summary>
        Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId);

        // OpenSpec: cleanup-formula-dead-code - 已删除GetPendingValidationFormulasAsync/ValidateFormulaHerbAsync
        // 原Issue #1349/#1348为FormulaValidationViewModel设计，该ViewModel已删除（OpenSpec: migrate-views-to-role-modules）

        #region 状态切换、恢复和批量操作

        /// <summary>
        /// 切换验方状态（启用/禁用）
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
    }
}
