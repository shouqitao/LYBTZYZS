using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 验方服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IFormulaService
    {
        /// <summary>
        /// 分页查询验方
        /// </summary>
        Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);

        /// <summary>
        /// 更新验方信息
        /// </summary>
        Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}