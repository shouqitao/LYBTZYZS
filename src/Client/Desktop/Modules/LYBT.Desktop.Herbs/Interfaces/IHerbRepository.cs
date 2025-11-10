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
        Task<PagedResult<HerbDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<HerbDto?> GetByIdAsync(Guid id);
        Task<HerbDto> CreateAsync(HerbInputDto dto);
        Task<HerbDto> UpdateAsync(HerbInputDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<HerbDto>> SearchAsync(string keyword);

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
    }
}
