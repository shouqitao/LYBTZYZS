using System.IO;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces
{
    /// <summary>
    /// 药材数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IHerbRepository
    {
        Task<PagedResult<HerbDetailDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 获取草药列表（返回HerbListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        Task<PagedResult<HerbListDto>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);
        Task<HerbDetailDto?> GetByIdAsync(Guid id);
        Task<HerbDetailDto> CreateAsync(HerbInputDto dto);
        Task<HerbDetailDto> UpdateAsync(HerbInputDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<HerbDetailDto>> SearchAsync(string keyword);

        // ========== Epic #1962: 批量导入/导出功能 ==========

        /// <summary>
        /// 批量导入药材数据
        /// </summary>
        Task<HerbBatchImportResultDto?> BatchImportAsync(Stream fileStream, string fileName);

        /// <summary>
        /// 下载药材导入模板
        /// </summary>
        Task<byte[]?> ExportTemplateAsync();

        /// <summary>
        /// 导出药材数据到Excel
        /// </summary>
        Task<byte[]?> ExportHerbsAsync(string? keyword = null);

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复 ==========

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        Task<HerbDetailDto?> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复已删除的药材
        /// </summary>
        Task<HerbDetailDto?> RestoreAsync(Guid id);
    }
}
