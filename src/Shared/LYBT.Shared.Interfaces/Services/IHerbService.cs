using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Shared.Interfaces.Services
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
        Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新药材
        /// </summary>
        Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);

        /// <summary>
        /// 更新药材信息
        /// </summary>
        Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除药材（软删除）(Issue #1169)
        /// </summary>
        /// <param name="ids">药材ID列表</param>
        Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

        /// <summary>
        /// 搜索药材 - 支持多条件搜索
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 从Excel文件导入药材数据 (Issue #1166)
        /// </summary>
        Task<ServiceResult<ImportResultDto<HerbDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null);

        /// <summary>
        /// 导出药材数据到Excel (Issue #1166)
        /// </summary>
        Task<MemoryStream> ExportAsync(string? category = null);

        /// <summary>
        /// 生成药材导入模板 (Issue #1166)
        /// </summary>
        MemoryStream GenerateImportTemplate();
    }
}
