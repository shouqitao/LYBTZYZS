using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Repositories
{
    /// <summary>
    /// 验方数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IFormulaRepository
    {
        Task<PagedResult<FormulaDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<FormulaDto> GetByIdAsync(Guid id);
        Task<FormulaDto> CreateAsync(FormulaCreateDto dto);
        Task<FormulaDto> UpdateAsync(FormulaUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<FormulaDto>> SearchAsync(string keyword);
        Task<FormulaDto> CloneFormulaAsync(Guid formulaId);
    }
}
