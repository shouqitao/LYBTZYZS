using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材服务接口 - 简化版，包含基础CRUD和分类筛选
    /// </summary>
    public interface IHerbService
    {
        /// <summary>
        /// 分页查询药材（Issue #1164: 扩展支持分类筛选）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="category">分类筛选（可选）</param>
        Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建新药材
        /// </summary>
        Task<Result<HerbDetailDto>> CreateAsync(HerbInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新药材信息
        /// </summary>
        Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 搜索药材 - 支持多条件搜索
        /// </summary>
        Task<Result<List<HerbDetailDto>>> SearchAsync(string keyword, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量导入药材
        /// </summary>
        /// <param name="herbs">药材DTO列表（≤10000条，BR-006）</param>
        /// <param name="strategy">重复处理策略（Skip/Update/Error）</param>
        Task<Result<HerbBatchImportResultDto>> BatchImportAsync(List<HerbInputDto> herbs, DuplicateStrategy strategy, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有药材数据用于导出
        /// </summary>
        /// <param name="category">分类筛选（可选）</param>
        Task<Result<List<HerbDetailDto>>> GetAllForExportAsync(string? category = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        /// <param name="id">药材ID</param>
        Task<Result<HerbDetailDto>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量删除药材（软删除）
        /// </summary>
        /// <param name="ids">药材ID列表</param>
        Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    }
}
