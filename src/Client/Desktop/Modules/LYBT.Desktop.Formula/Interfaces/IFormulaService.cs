using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces
{
    /// <summary>
    /// 配方Service接口
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: cleanup-formula-dead-code - 清理未使用的占位方法和FormulaValidation方法
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
        // OpenSpec: cleanup-formula-dead-code - 已删除PrintFormulaAsync/ViewUsageHistoryAsync占位方法
        // OpenSpec: cleanup-formula-dead-code - 已删除GetPendingValidationFormulasAsync/ValidateFormulaHerbAsync（FormulaValidationViewModel已删除）
    }
}
