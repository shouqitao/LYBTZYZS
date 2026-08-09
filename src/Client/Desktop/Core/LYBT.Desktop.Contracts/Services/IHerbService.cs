using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using System.Threading;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 药材Service接口
    /// </summary>
    public interface IHerbService : ICrudService<HerbListDto, HerbDetailDto, HerbInputDto>
    {
        #region 查询操作

        /// <summary>
        /// 分页查询药材（支持分类过滤）
        /// </summary>
        Task<CommandResult<PagedResult<HerbListDto>>> GetPagedAsync(
            int page, int pageSize, string? searchText = null, string? category = null, CancellationToken ct = default);

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除药材
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> herbIds, CancellationToken ct = default);

        /// <summary>
        /// 批量导入药材
        /// </summary>
        Task<CommandResult<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default);

        /// <summary>
        /// 导出药材模板
        /// </summary>
        Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        Task<CommandResult<byte[]>> ExportHerbsAsync(string? keyword, CancellationToken ct = default);

        #endregion
    }
}
