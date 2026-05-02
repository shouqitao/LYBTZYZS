using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using System.Threading;
using Refit;

namespace LYBT.Desktop.Herbs.Services
{
    /// <summary>
    /// 药材Remote Service实现
    /// 通过 IHerbRepository 调用远程API
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// </summary>
    public class RemoteHerbService : IHerbService
    {
        private readonly IHerbRepository _herbRepository;
        private readonly ILogger<RemoteHerbService> _logger;

        public RemoteHerbService(
            IHerbRepository herbRepository,
            ILogger<RemoteHerbService> logger)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基本CRUD操作

        /// <summary>
        /// 创建药材
        /// </summary>
        public async Task<CommandResult<HerbDetailDto>> CreateAsync(HerbInputDto createDto, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.Create started - Name={HerbName}", createDto.Name);

                var herb = await _herbRepository.CreateAsync(createDto);
                _logger.LogInformation("[SVC] Herb.Create completed - HerbId={HerbId}", herb.Id);
                return CommandResult<HerbDetailDto>.Succeeded(herb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.Create failed - Name={HerbName}", createDto.Name);
                return CommandResult<HerbDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建药材", ex));
            }
        }

        /// <summary>
        /// 更新药材
        /// </summary>
        public async Task<CommandResult<HerbDetailDto>> UpdateAsync(HerbInputDto updateDto, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.Update started - HerbId={HerbId}", updateDto.Id);

                var herb = await _herbRepository.UpdateAsync(updateDto);
                _logger.LogInformation("[SVC] Herb.Update completed - HerbId={HerbId}", herb.Id);
                return CommandResult<HerbDetailDto>.Succeeded(herb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.Update failed - HerbId={HerbId}", updateDto.Id);
                return CommandResult<HerbDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("更新药材", ex));
            }
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<CommandResult<bool>> DeleteAsync(Guid herbId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.Delete started - HerbId={HerbId}", herbId);

                var success = await _herbRepository.DeleteAsync(herbId);
                _logger.LogInformation("[SVC] Herb.Delete completed - HerbId={HerbId}, Success={Success}", herbId, success);
                return CommandResult<bool>.Succeeded(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.Delete failed - HerbId={HerbId}", herbId);
                return CommandResult<bool>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除药材", ex));
            }
        }

        /// <summary>
        /// 批量删除药材
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> herbIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.BatchDelete started - Count={Count}", herbIds.Count);

                var result = await _herbRepository.BatchDeleteAsync(herbIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量删除操作失败");

                _logger.LogInformation("[SVC] Herb.BatchDelete completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.BatchDelete failed - Count={Count}", herbIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量删除药材", ex));
            }
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据ID获取药材
        /// </summary>
        public async Task<CommandResult<HerbDetailDto>> GetByIdAsync(Guid herbId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] Herb.GetById - HerbId={HerbId}", herbId);

                var herb = await _herbRepository.GetByIdAsync(herbId);
                if (herb == null)
                    return CommandResult<HerbDetailDto>.NotFound("药材不存在");

                return CommandResult<HerbDetailDto>.Succeeded(herb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.GetById failed - HerbId={HerbId}", herbId);
                return CommandResult<HerbDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取药材", ex));
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<CommandResult<PagedResult<HerbListDto>>> GetPagedAsync(
            int page, int pageSize, string? searchText = null, string? category = null, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] Herb.GetPaged - Page={Page}, PageSize={PageSize}, Search={Search}, Category={Category}",
                    page, pageSize, searchText, category);

                var result = await _herbRepository.GetPagedAsync(page, pageSize, searchText, category);
                return CommandResult<PagedResult<HerbListDto>>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.GetPaged failed - Page={Page}, Search={Search}", page, searchText);
                return CommandResult<PagedResult<HerbListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("分页查询药材", ex));
            }
        }

        /// <summary>
        /// 获取所有药材
        /// </summary>
        public async Task<CommandResult<List<HerbListDto>>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] Herb.GetAll started");

                // Get all herbs by requesting a large page
                var result = await _herbRepository.GetPagedAsync(1, 10000, null, null);
                var herbs = result.Items.ToList();

                _logger.LogDebug("[SVC] Herb.GetAll completed - Count={Count}", herbs.Count);
                return CommandResult<List<HerbListDto>>.Succeeded(herbs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.GetAll failed");
                return CommandResult<List<HerbListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取所有药材", ex));
            }
        }

        /// <summary>
        /// 搜索药材
        /// </summary>
        public async Task<CommandResult<List<HerbListDto>>> SearchAsync(string keyword, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] Herb.Search - Keyword={Keyword}", keyword);

                var herbs = await _herbRepository.SearchAsync(keyword);
                _logger.LogDebug("[SVC] Herb.Search completed - Count={Count}", herbs.Count);
                return CommandResult<List<HerbListDto>>.Succeeded(herbs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.Search failed - Keyword={Keyword}", keyword);
                return CommandResult<List<HerbListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("搜索药材", ex));
            }
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 切换药材状态
        /// </summary>
        public async Task<CommandResult<HerbDetailDto>> ToggleStatusAsync(Guid herbId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.ToggleStatus started - HerbId={HerbId}", herbId);

                var herb = await _herbRepository.ToggleStatusAsync(herbId);
                if (herb == null)
                    return CommandResult<HerbDetailDto>.NotFound("药材不存在");

                _logger.LogInformation("[SVC] Herb.ToggleStatus completed - HerbId={HerbId}, Status={Status}",
                    herbId, herb.Status);
                return CommandResult<HerbDetailDto>.Succeeded(herb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.ToggleStatus failed - HerbId={HerbId}", herbId);
                return CommandResult<HerbDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("切换药材状态", ex));
            }
        }

        /// <summary>
        /// 恢复已删除药材
        /// </summary>
        public async Task<CommandResult<HerbDetailDto>> RestoreAsync(Guid herbId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.Restore started - HerbId={HerbId}", herbId);

                var herb = await _herbRepository.RestoreAsync(herbId);
                if (herb == null)
                    return CommandResult<HerbDetailDto>.NotFound("药材不存在或未被删除");

                _logger.LogInformation("[SVC] Herb.Restore completed - HerbId={HerbId}", herbId);
                return CommandResult<HerbDetailDto>.Succeeded(herb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.Restore failed - HerbId={HerbId}", herbId);
                return CommandResult<HerbDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("恢复药材", ex));
            }
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量启用药材
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchEnableAsync(List<Guid> herbIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.BatchEnable started - Count={Count}", herbIds.Count);

                var result = await _herbRepository.BatchEnableAsync(herbIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量启用操作失败");

                _logger.LogInformation("[SVC] Herb.BatchEnable completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.BatchEnable failed - Count={Count}", herbIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量启用药材", ex));
            }
        }

        /// <summary>
        /// 批量禁用药材
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchDisableAsync(List<Guid> herbIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.BatchDisable started - Count={Count}", herbIds.Count);

                var result = await _herbRepository.BatchDisableAsync(herbIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量禁用操作失败");

                _logger.LogInformation("[SVC] Herb.BatchDisable completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.BatchDisable failed - Count={Count}", herbIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量禁用药材", ex));
            }
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<CommandResult<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.BatchImport started - Count={Count}", request.Herbs.Count);

                var result = await _herbRepository.BatchImportAsync(request);
                if (result == null)
                    return CommandResult<HerbBatchImportResultDto>.Failed("批量导入操作失败");

                _logger.LogInformation("[SVC] Herb.BatchImport completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<HerbBatchImportResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.BatchImport failed");
                return CommandResult<HerbBatchImportResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量导入药材", ex));
            }
        }

        /// <summary>
        /// 导出药材模板
        /// </summary>
        public async Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.ExportTemplate started");

                var data = await _herbRepository.ExportTemplateAsync();
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出模板操作失败");

                _logger.LogInformation("[SVC] Herb.ExportTemplate completed - Size={Size} bytes", data.Length);
                return CommandResult<byte[]>.Succeeded(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.ExportTemplate failed");
                return CommandResult<byte[]>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导出药材模板", ex));
            }
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<CommandResult<byte[]>> ExportHerbsAsync(string? keyword, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Herb.ExportHerbs started - Keyword={Keyword}", keyword);

                var data = await _herbRepository.ExportHerbsAsync(keyword);
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出药材数据操作失败");

                _logger.LogInformation("[SVC] Herb.ExportHerbs completed - Size={Size} bytes", data.Length);
                return CommandResult<byte[]>.Succeeded(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Herb.ExportHerbs failed - Keyword={Keyword}", keyword);
                return CommandResult<byte[]>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导出药材数据", ex));
            }
        }

        #endregion
    }
}