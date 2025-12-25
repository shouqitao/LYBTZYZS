namespace LYBT.Desktop.Herbs.Services;

// OpenSpec: standardize-service-layer - 重命名CommandHandler为Service
using LYBT.Desktop.Herbs.Contracts;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

/// <summary>
/// 中药Service实现
/// 无状态设计，统一返回类型，依赖Repository进行数据访问
/// </summary>
public class HerbService : IHerbService
{
    private readonly IHerbRepository _repository;
    private readonly ILogger<HerbService> _logger;

    public HerbService(
        IHerbRepository repository,
        ILogger<HerbService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[SVC]前缀
    public async Task<(bool success, HerbDetailDto? data, string? error)> CreateAsync(HerbInputDto input)
    {
        try
        {
            _logger.LogInformation("[SVC] Herb.Create started - Name={Name}", input.Name);

            var result = await _repository.CreateAsync(input);

            _logger.LogInformation("[SVC] Herb.Create completed - HerbId={HerbId}", result.Id);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Herb.Create failed - Name={Name}", input.Name);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建中药", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<(bool success, HerbDetailDto? data, string? error)> UpdateAsync(Guid id, HerbInputDto input)
    {
        try
        {
            _logger.LogInformation("[SVC] Herb.Update started - HerbId={HerbId}", id);

            var result = await _repository.UpdateAsync(input);

            _logger.LogInformation("[SVC] Herb.Update completed - HerbId={HerbId}", id);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Herb.Update failed - HerbId={HerbId}", id);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("更新中药", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<(bool success, string? error)> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[SVC] Herb.Delete started - HerbId={HerbId}", id);

            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                _logger.LogInformation("[SVC] Herb.Delete completed - HerbId={HerbId}", id);
                return (true, null);
            }
            else
            {
                _logger.LogWarning("[SVC] Herb.Delete → NotFound - HerbId={HerbId}", id);
                return (false, "删除中药失败，记录不存在或已被删除");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Herb.Delete failed - HerbId={HerbId}", id);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除中药", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<(bool success, HerbDetailDto? data, string? error)> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[SVC] Herb.GetById started - HerbId={HerbId}", id);

            var result = await _repository.GetByIdAsync(id);

            if (result != null)
            {
                _logger.LogDebug("[SVC] Herb.GetById completed - HerbId={HerbId}", id);
                return (true, result, null);
            }
            else
            {
                _logger.LogWarning("[SVC] Herb.GetById → NotFound - HerbId={HerbId}", id);
                return (false, null, "未找到指定的中药记录");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Herb.GetById failed - HerbId={HerbId}", id);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取中药详情", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[SVC] Herb.GetPaged started - Page={Page} PageSize={PageSize} Keyword={Keyword}", page, pageSize, keyword);

            var result = await _repository.GetPagedAsync(page, pageSize, keyword);

            _logger.LogDebug("[SVC] Herb.GetPaged completed - TotalCount={TotalCount}", result.TotalCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Herb.GetPaged failed - Page={Page} PageSize={PageSize}", page, pageSize);
            return new PagedResult<HerbListDto>([], 0, page, pageSize);
        }
    }
}
