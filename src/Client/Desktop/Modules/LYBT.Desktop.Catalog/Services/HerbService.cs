using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.Catalog.Services
{
    /// <summary>
    /// 药材Remote Service实现
    /// 通过 IHerbRepository 调用远程API
    /// </summary>
    public class HerbService : CrudServiceBase<HerbListDto, HerbDetailDto, HerbInputDto>, IHerbService
    {
        private readonly IHerbRepository _herbRepository;

        public HerbService(
            IHerbRepository herbRepository,
            ILogger<HerbService> logger)
            : base(logger, "Herb")
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
        }

        #region Core 实现

        protected override async Task<HerbDetailDto> CreateCoreAsync(HerbInputDto input, CancellationToken ct)
            => await _herbRepository.CreateAsync(input);

        protected override async Task<HerbDetailDto> UpdateCoreAsync(HerbInputDto input, CancellationToken ct)
            => await _herbRepository.UpdateAsync(input);

        protected override async Task DeleteCoreAsync(Guid id, CancellationToken ct)
            => await _herbRepository.DeleteAsync(id);

        protected override async Task<HerbDetailDto?> GetByIdCoreAsync(Guid id, CancellationToken ct)
            => await _herbRepository.GetByIdAsync(id);

        protected override async Task<PagedResult<HerbListDto>> GetPagedCoreAsync(int page, int pageSize, string? keyword, CancellationToken ct)
            => await _herbRepository.GetPagedAsync(page, pageSize, keyword, null);

        protected override async Task<List<HerbListDto>> SearchCoreAsync(string keyword, CancellationToken ct)
            => await _herbRepository.SearchAsync(keyword);

        protected override async Task<HerbDetailDto?> ToggleStatusCoreAsync(Guid id, CancellationToken ct)
            => await _herbRepository.ToggleStatusAsync(id);

        #endregion

        #region 带分类的分页查询

        /// <summary>
        /// 分页查询药材（支持分类过滤）
        /// </summary>
        public async Task<CommandResult<PagedResult<HerbListDto>>> GetPagedAsync(
            int page, int pageSize, string? searchText = null, string? category = null, CancellationToken ct = default)
        {
            return await ExecuteAsync<PagedResult<HerbListDto>>("Herb.GetPaged", async () =>
            {
                var result = await _herbRepository.GetPagedAsync(page, pageSize, searchText, category);
                return CommandResult<PagedResult<HerbListDto>>.Succeeded(result);
            });
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除药材
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> herbIds, CancellationToken ct = default)
        {
            return await ExecuteAsync<BatchOperationResultDto>("Herb.BatchDelete", async () =>
            {
                var result = await _herbRepository.BatchDeleteAsync(herbIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量删除操作失败");
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            });
        }

        /// <summary>
        /// 批量启用/禁用药材
        /// </summary>
        public override async Task<CommandResult<BatchOperationResultDto>> BatchSetStatusAsync(List<Guid> ids, CommonStatus status, CancellationToken ct = default)
        {
            return await ExecuteAsync<BatchOperationResultDto>("Herb.BatchSetStatus", async () =>
            {
                var result = await _herbRepository.BatchSetStatusAsync(ids, status, ct);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed(
                        status == CommonStatus.Enabled ? "批量启用药材失败" : "批量禁用药材失败");
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            });
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<CommandResult<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default)
        {
            return await ExecuteAsync<HerbBatchImportResultDto>("Herb.BatchImport", async () =>
            {
                var result = await _herbRepository.BatchImportAsync(request);
                if (result == null)
                    return CommandResult<HerbBatchImportResultDto>.Failed("批量导入操作失败");
                return CommandResult<HerbBatchImportResultDto>.Succeeded(result);
            });
        }

        /// <summary>
        /// 导出药材模板
        /// </summary>
        public async Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync<byte[]>("Herb.ExportTemplate", async () =>
            {
                var data = await _herbRepository.ExportTemplateAsync();
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出模板操作失败");
                return CommandResult<byte[]>.Succeeded(data);
            });
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<CommandResult<byte[]>> ExportHerbsAsync(string? keyword, CancellationToken ct = default)
        {
            return await ExecuteAsync<byte[]>("Herb.ExportHerbs", async () =>
            {
                var data = await _herbRepository.ExportHerbsAsync(keyword);
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出药材数据操作失败");
                return CommandResult<byte[]>.Succeeded(data);
            });
        }

        #endregion
    }
}
