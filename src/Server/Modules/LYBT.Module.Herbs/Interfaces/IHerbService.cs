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
        Task<Result<PagedResult<HerbDetailDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);

        /// <summary>
        /// 分页查询药材列表（返回HerbListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        Task<Result<PagedResult<HerbListDto>>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        Task<Result<HerbDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新药材
        /// </summary>
        Task<Result<HerbDetailDto>> CreateAsync(HerbInputDto dto);

        /// <summary>
        /// 更新药材信息
        /// </summary>
        Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto);

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索药材 - 支持多条件搜索
        /// </summary>
        Task<Result<List<HerbDetailDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 从Excel文件导入药材数据 (Issue #1166)
        /// </summary>
        Task<Result<ImportResultDto<HerbDetailDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null);

        /// <summary>
        /// 导出药材数据到Excel (Issue #1166)
        /// </summary>
        Task<MemoryStream> ExportAsync(string? category = null);

        /// <summary>
        /// 生成药材导入模板 (Issue #1166)
        /// </summary>
        MemoryStream GenerateImportTemplate();

        // ========== Epic #1962: 新增批量导入/导出和引用检查方法 ==========

        /// <summary>
        /// 批量导入药材（Epic #1962 Task 2.2）
        /// Desktop层负责Excel解析，Server层接收DTO列表
        /// </summary>
        /// <param name="herbs">药材DTO列表（≤10000条，BR-006）</param>
        /// <param name="strategy">重复处理策略（Skip/Update/Error）</param>
        Task<Result<HerbBatchImportResultDto>> BatchImportAsync(List<HerbInputDto> herbs, DuplicateStrategy strategy);

        /// <summary>
        /// 获取所有药材数据用于导出（Epic #1962 Task 3.1）
        /// Desktop层负责Excel生成，Server层返回JSON数据
        /// </summary>
        /// <param name="category">分类筛选（可选）</param>
        Task<Result<List<HerbDetailDto>>> GetAllForExportAsync(string? category = null);

        /// <summary>
        /// 检查药材是否被处方引用（Epic #1962 Task 4.2）
        /// </summary>
        /// <param name="herbId">药材ID</param>
        Task<Result<HerbReferenceCheckDto>> CheckReferenceAsync(Guid herbId);

        /// <summary>
        /// 批量检查药材引用关系（Epic #1962 Task 4.2）
        /// </summary>
        /// <param name="herbIds">药材ID列表（≤100条，BR-006）</param>
        Task<Result<List<HerbReferenceCheckDto>>> BatchCheckReferenceAsync(List<Guid> herbIds);

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法 ==========

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        /// <param name="id">药材ID</param>
        Task<Result<HerbDetailDto>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复软删除的药材
        /// </summary>
        /// <param name="id">药材ID</param>
        Task<Result<HerbDetailDto>> RestoreAsync(Guid id);

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量更新药材状态
        /// </summary>
        /// <param name="ids">药材ID列表</param>
        /// <param name="status">目标状态</param>
        Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status);

        /// <summary>
        /// 批量删除药材（软删除）
        /// </summary>
        /// <param name="ids">药材ID列表</param>
        Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);
    }
}
