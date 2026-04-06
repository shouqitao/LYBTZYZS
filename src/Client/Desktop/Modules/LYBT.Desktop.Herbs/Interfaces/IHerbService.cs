using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using System.Threading;
using Refit;

namespace LYBT.Desktop.Herbs.Interfaces
{
    /// <summary>
    /// 药材Service接口
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: dto-architecture-specification - 统一使用HerbDetailDto
    /// </summary>
    public interface IHerbService
    {
        #region 基本CRUD操作

        /// <summary>
        /// 创建药材
        /// </summary>
        Task<CommandResult<HerbDetailDto>> CreateAsync(HerbInputDto createDto, CancellationToken ct = default);

        /// <summary>
        /// 更新药材
        /// </summary>
        Task<CommandResult<HerbDetailDto>> UpdateAsync(HerbInputDto updateDto, CancellationToken ct = default);

        /// <summary>
        /// 删除药材
        /// </summary>
        Task<CommandResult<bool>> DeleteAsync(Guid herbId, CancellationToken ct = default);

        /// <summary>
        /// 批量删除药材
        /// OpenSpec: optimize-batch-operations Phase 2
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> herbIds, CancellationToken ct = default);

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据ID获取药材
        /// </summary>
        Task<CommandResult<HerbDetailDto>> GetByIdAsync(Guid herbId, CancellationToken ct = default);

        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<CommandResult<PagedResult<HerbListDto>>> GetPagedAsync(
            int page, int pageSize, string? searchText = null, string? category = null, CancellationToken ct = default);

        /// <summary>
        /// 获取所有药材
        /// </summary>
        Task<CommandResult<List<HerbListDto>>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 搜索药材
        /// </summary>
        Task<CommandResult<List<HerbListDto>>> SearchAsync(string keyword, CancellationToken ct = default);

        #endregion

        #region 状态管理

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        Task<CommandResult<HerbDetailDto>> ToggleStatusAsync(Guid herbId, CancellationToken ct = default);

        /// <summary>
        /// 恢复已删除药材
        /// </summary>
        Task<CommandResult<HerbDetailDto>> RestoreAsync(Guid herbId, CancellationToken ct = default);

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量启用药材
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchEnableAsync(List<Guid> herbIds, CancellationToken ct = default);

        /// <summary>
        /// 批量禁用药材
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDisableAsync(List<Guid> herbIds, CancellationToken ct = default);

        /// <summary>
        /// 批量导入药材
        /// </summary>
        Task<CommandResult<HerbBatchImportResultDto>> BatchImportAsync(StreamPart file, CancellationToken ct = default);

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