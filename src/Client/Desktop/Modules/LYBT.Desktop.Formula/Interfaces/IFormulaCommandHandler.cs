using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces
{
    /// <summary>
    /// 配方命令处理器接口
    /// Desktop层架构重构 Phase 1: 接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IFormulaCommandHandler
    {
        /// <summary>
        /// 保存配方
        /// Issue #2149: 优化双重映射，直接接收InputDto以提升性能
        /// </summary>
        Task<(bool success, FormulaDto? formula, string? errorMessage)> SaveFormulaAsync(
            FormulaDto currentFormula,
            string formulaName,
            string effect,
            string usage,
            string remark,
            bool isShared,
            List<FormulaHerbItemInputDto> herbInputDtos);

        /// <summary>
        /// 复制配方
        /// </summary>
        Task<(bool success, FormulaDto? formula, string? errorMessage)> CopyFormulaAsync(FormulaDto sourceFormula);

        /// <summary>
        /// 删除配方
        /// </summary>
        Task<(bool success, string? errorMessage)> DeleteFormulaAsync(Guid formulaId);

        /// <summary>
        /// 删除配方（简化版，Issue #1787: 兼容返回bool的调用）
        /// </summary>
        Task<bool> DeleteAsync(Guid formulaId);

        /// <summary>
        /// 创建配方（Issue #1787: 支持基本创建操作）
        /// </summary>
        Task<(bool success, FormulaDto? formula, string? errorMessage)> CreateAsync(FormulaInputDto createDto);

        /// <summary>
        /// 更新配方（Issue #1787: 支持基本更新操作）
        /// </summary>
        Task<(bool success, FormulaDto? formula, string? errorMessage)> UpdateAsync(FormulaInputDto updateDto);

        /// <summary>
        /// 打印配方
        /// </summary>
        Task<(bool success, string? errorMessage)> PrintFormulaAsync(FormulaDto formula);

        /// <summary>
        /// 分页查询配方（Issue #1787: 支持分页查询）
        /// </summary>
        Task<(bool success, PagedResult<FormulaDto>? data, string? errorMessage)> GetPagedAsync(
            int page, int pageSize, string? searchText = null);

        /// <summary>
        /// 根据ID获取配方（Issue #1787: 支持单个配方查询）
        /// </summary>
        Task<(bool success, FormulaDto? formula, string? errorMessage)> GetByIdAsync(Guid formulaId);

        /// <summary>
        /// 查看使用历史
        /// </summary>
        Task<(bool success, string? errorMessage)> ViewUsageHistoryAsync(Guid formulaId);

        /// <summary>
        /// 获取待校验的验方列表（Issue #1787: 支持FormulaValidationViewModel）
        /// </summary>
        Task<(bool success, List<FormulaDto>? data, string? errorMessage)> GetPendingValidationFormulasAsync();

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库（Issue #1787: 支持FormulaValidationViewModel）
        /// </summary>
        Task<(bool success, string? errorMessage)> ValidateFormulaHerbAsync(
            Guid formulaId, Guid herbItemId, Guid selectedHerbId);
    }
}
