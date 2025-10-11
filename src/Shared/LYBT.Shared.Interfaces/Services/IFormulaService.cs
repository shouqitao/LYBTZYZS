using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 验方服务接口 - 简化版，包含基础CRUD和分类筛选
    /// </summary>
    public interface IFormulaService
    {
        /// <summary>
        /// 分页查询验方（Issue #1164: 扩展支持分类筛选）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="category">分类筛选（可选）</param>
        Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);

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

        /// <summary>
        /// 搜索验方 - 支持多条件搜索
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 克隆验方 - 复制验方并创建新实例
        /// </summary>
        Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId);
    }
}
