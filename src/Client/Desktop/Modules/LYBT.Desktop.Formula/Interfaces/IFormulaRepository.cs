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
        Task<PagedResult<FormulaDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<FormulaDto?> GetByIdAsync(Guid id);
        Task<FormulaDto> CreateAsync(FormulaInputDto dto);
        Task<FormulaDto> UpdateAsync(FormulaInputDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<FormulaDto>> SearchAsync(string keyword);
        Task<FormulaDto> CloneFormulaAsync(Guid formulaId);

        /// <summary>
        /// 获取待校验的验方列表 (Issue #1349)
        /// </summary>
        Task<List<FormulaDto>> GetPendingValidationFormulasAsync();

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        Task<bool> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId);
    }
}
