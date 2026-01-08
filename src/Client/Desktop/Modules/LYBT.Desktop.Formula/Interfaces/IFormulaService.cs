using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces
{
    /// <summary>
    /// 配方Service接口
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// 提供配方CRUD和业务操作的统一处理
    /// </summary>
    public interface IFormulaService
    {
        /// <summary>
        /// 保存配方
        /// Issue #2149: 优化双重映射，直接接收InputDto以提升性能
        /// </summary>
        Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> SaveFormulaAsync(
            FormulaDetailDto currentFormula,
            string formulaName,
            string effect,
            string usage,
            string property,
            string category,
            string remark,
            bool isShared,
            List<FormulaHerbItemInputDto> herbInputDtos);

        /// <summary>
        /// 复制配方
        /// </summary>
        Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> CopyFormulaAsync(FormulaDetailDto sourceFormula);

        /// <summary>
        /// 删除配方
        /// </summary>
        Task<(bool success, string? errorMessage)> DeleteFormulaAsync(Guid formulaId);

        // OpenSpec: simplify-desktop-data-layer - 已删除DeleteAsync/CreateAsync/UpdateAsync，ViewModel直接使用Repository

        /// <summary>
        /// 打印配方
        /// </summary>
        Task<(bool success, string? errorMessage)> PrintFormulaAsync(FormulaDetailDto formula);

        // OpenSpec: simplify-desktop-data-layer - 已删除GetPagedAsync/GetByIdAsync，ViewModel直接使用Repository

        /// <summary>
        /// 查看使用历史
        /// </summary>
        Task<(bool success, string? errorMessage)> ViewUsageHistoryAsync(Guid formulaId);

        /// <summary>
        /// 获取待校验的验方列表
        /// </summary>
        /// <remarks>Issue #1787原为FormulaValidationViewModel设计，该ViewModel已删除（OpenSpec: migrate-views-to-role-modules）</remarks>
        Task<(bool success, List<FormulaDetailDto>? data, string? errorMessage)> GetPendingValidationFormulasAsync();

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库
        /// </summary>
        /// <remarks>Issue #1787原为FormulaValidationViewModel设计，该ViewModel已删除（OpenSpec: migrate-views-to-role-modules）</remarks>
        Task<(bool success, string? errorMessage)> ValidateFormulaHerbAsync(
            Guid formulaId, Guid herbItemId, Guid selectedHerbId);
    }
}
