namespace LYBT.Desktop.Herbs.Services;

// OpenSpec: standardize-service-layer - 重命名CommandHandler为Service
using LYBT.Desktop.Herbs.Contracts;
using LYBT.Desktop.Herbs.Interfaces;
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
    public async Task<(bool success, HerbDetailDto? data, string? error)> CreateAsync(HerbInputDto input)
    {
        try
        {
            _logger.LogInformation("[CMD] CreateHerb started: {Name}", input.Name);

            var result = await _repository.CreateAsync(input);

            _logger.LogInformation("[CMD] CreateHerb completed: {Id}", result.Id);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] CreateHerb failed: {Name}", input.Name);
            return (false, null, "创建中药失败，请重试");
        }
    }

    /// <inheritdoc/>
    public async Task<(bool success, HerbDetailDto? data, string? error)> UpdateAsync(Guid id, HerbInputDto input)
    {
        try
        {
            _logger.LogInformation("[CMD] UpdateHerb started: {Id}", id);

            var result = await _repository.UpdateAsync(input);

            _logger.LogInformation("[CMD] UpdateHerb completed: {Id}", id);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] UpdateHerb failed: {Id}", id);
            return (false, null, "更新中药失败，请重试");
        }
    }

    /// <inheritdoc/>
    public async Task<(bool success, string? error)> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[CMD] DeleteHerb started: {Id}", id);

            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                _logger.LogInformation("[CMD] DeleteHerb completed: {Id}", id);
                return (true, null);
            }
            else
            {
                _logger.LogWarning("[CMD] DeleteHerb failed - not found or already deleted: {Id}", id);
                return (false, "删除中药失败，记录不存在或已被删除");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] DeleteHerb failed: {Id}", id);
            return (false, "删除中药失败，请重试");
        }
    }

    /// <inheritdoc/>
    public async Task<(bool success, HerbDetailDto? data, string? error)> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[CMD] GetHerbById started: {Id}", id);

            var result = await _repository.GetByIdAsync(id);

            if (result != null)
            {
                _logger.LogDebug("[CMD] GetHerbById completed: {Id}", id);
                return (true, result, null);
            }
            else
            {
                _logger.LogWarning("[CMD] GetHerbById - not found: {Id}", id);
                return (false, null, "未找到指定的中药记录");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] GetHerbById failed: {Id}", id);
            return (false, null, "获取中药详情失败，请重试");
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[CMD] GetHerbsPaged started: Page={Page}, PageSize={PageSize}, Keyword={Keyword}", page, pageSize, keyword);

            var result = await _repository.GetPagedAsync(page, pageSize, keyword);

            _logger.LogDebug("[CMD] GetHerbsPaged completed: TotalCount={TotalCount}", result.TotalCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] GetHerbsPaged failed: Page={Page}, PageSize={PageSize}", page, pageSize);
            return new PagedResult<HerbListDto>([], 0, page, pageSize);
        }
    }
}
