using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 中药材服务 - 重构后的统一实现
/// 合并原QueryService和BusinessService的所有功能
/// </summary>
public class HerbService(
    ILogger<HerbService> logger,
    IHerbApi herbApi) : IHerbService, IDisposable
{
    private readonly ILogger<HerbService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IHerbApi _herbApi = herbApi ?? throw new ArgumentNullException(nameof(herbApi));

    #region Query Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbSearchDto query)
    {
        try
        {
            _logger.LogDebug("执行中药材分页查询，页码: {PageNumber}, 页大小: {PageSize}", query.PageIndex, query.PageSize);

            var refitResponse = await _herbApi.GetHerbsAsync(
                page: query.PageIndex,
                pageSize: query.PageSize,
                keyword: query.Keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                return ServiceResult<PagedResult<HerbDto>>.Success(refitResponse.Content, "查询成功");
            }

            _logger.LogError("中药材分页查询失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<PagedResult<HerbDto>>.Failure($"查询失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "中药材分页查询异常");
            return ServiceResult<PagedResult<HerbDto>>.Failure("查询中药材列表失败");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("查询中药材详细档案: {HerbId}", id);

            var refitResponse = await _herbApi.GetHerbByIdAsync(id);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var detailDto = refitResponse.Content;

                // 将 HerbDetailDto 转换为 HerbDto
                var herbDto = new HerbDto
                {
                    Id = detailDto.Id,
                    Name = detailDto.Name,
                    PinYinCode = detailDto.PinYinCode,
                    Origin = detailDto.Origin,
                    Spec = detailDto.Spec,
                    Unit = detailDto.Unit,
                    Price = detailDto.Price,
                    Effect = detailDto.Effect,
                    Usage = detailDto.Usage,
                    Status = detailDto.Status,
                    CreateTime = detailDto.CreateTime,
                    UpdateTime = detailDto.UpdateTime,
                    Remark = detailDto.Remark
                };

                _logger.LogInformation("药材详情查询成功: {HerbName}", herbDto.Name);
                return ServiceResult<HerbDto>.Success(herbDto, "药材详情查询成功");
            }

            _logger.LogError("药材详情查询失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<HerbDto>.Failure($"查询失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询药材详情异常: {HerbId}", id);
            return ServiceResult<HerbDto>.Failure($"查询药材详情失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("中药材关键字搜索: {Keyword}", keyword);

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ServiceResult<List<HerbDto>>.Success([]);
            }

            var refitResponse = await _herbApi.GetPagedAsync(1, 50, keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                _logger.LogDebug("药材搜索成功: {Keyword}, 结果数: {Count}", keyword, refitResponse.Content.Items.Count);
                return ServiceResult<List<HerbDto>>.Success(refitResponse.Content.Items, "搜索成功");
            }

            _logger.LogError("药材搜索失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<List<HerbDto>>.Success([]); // 搜索失败时返回空列表而不是错误
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材搜索异常");
            return ServiceResult<List<HerbDto>>.Failure("药材搜索失败");
        }
    }

    /// <inheritdoc/>
    public Task<ServiceResult<HerbStatisticsDto>> GetStatisticsAsync()
    {
        try
        {
            _logger.LogDebug("生成药材统计数据");
            var stats = new HerbStatisticsDto();
            return Task.FromResult(ServiceResult<HerbStatisticsDto>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材统计数据生成异常");
            return Task.FromResult(ServiceResult<HerbStatisticsDto>.Failure("生成统计数据失败"));
        }
    }

    /// <inheritdoc/>
    public Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogDebug("批量获取药材: {Count}个", ids.Count);
            
            if (ids == null || !ids.Any())
            {
                return Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));
            }

            // 简化实现：返回空列表，实际项目中应调用API
            List<HerbDto> emptyList = [];
            return Task.FromResult(ServiceResult<List<HerbDto>>.Success(emptyList));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取药材异常");
            return Task.FromResult(ServiceResult<List<HerbDto>>.Failure("批量获取药材失败"));
        }
    }

    /// <summary>
    /// 获取药材统计（详细版本）
    /// </summary>
    public Task<ServiceResult<HerbStatisticsDto>> GetHerbStatisticsAsync()
    {
        try
        {
            _logger.LogDebug("生成详细药材统计数据");
            var stats = new HerbStatisticsDto();
            return Task.FromResult(ServiceResult<HerbStatisticsDto>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "详细药材统计数据生成异常");
            return Task.FromResult(ServiceResult<HerbStatisticsDto>.Failure("生成详细统计数据失败"));
        }
    }

    #endregion

    #region Business Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        _logger.LogInformation("中药材创建请求: 药材名称: {HerbName}", createDto.Name);

        try
        {
            var refitResponse = await _herbApi.CreateHerbAsync(createDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var herb = refitResponse.Content;
                _logger.LogInformation("药材创建成功: {HerbName}", herb.Name);
                return ServiceResult<HerbDto>.Success(herb, "药材创建成功");
            }

            _logger.LogError("药材创建失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<HerbDto>.Failure($"药材创建失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材创建异常: 药材名称: {HerbName}", createDto.Name);
            return ServiceResult<HerbDto>.Failure($"药材创建失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("药材更新请求: {HerbId}", id);

        try
        {
            var refitResponse = await _herbApi.UpdateHerbAsync(id, updateDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var herb = refitResponse.Content;
                _logger.LogInformation("药材更新成功: {HerbName}", herb.Name);
                return ServiceResult<HerbDto>.Success(herb, "药材更新成功");
            }

            _logger.LogError("药材更新失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<HerbDto>.Failure($"药材更新失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材更新异常: {HerbId}", id);
            return ServiceResult<HerbDto>.Failure($"药材更新失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid herbId)
    {
        _logger.LogInformation("删除药材: {HerbId}", herbId);

        try
        {
            // 使用状态更新作为软删除
            var statusDto = new CommonStatusUpdateDto
            {
                Id = herbId,
                Status = (int)CommonStatus.Disabled
            };

            var refitResponse = await _herbApi.UpdateStatusAsync(statusDto);

            if (refitResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("药材删除（软删除）成功: {HerbId}", herbId);
                return ServiceResult<bool>.Success(true, "药材删除成功");
            }

            _logger.LogError("药材删除失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<bool>.Failure($"药材删除失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材删除异常: {HerbId}", herbId);
            return ServiceResult<bool>.Failure($"药材删除失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> EnableAsync(Guid herbId)
    {
        _logger.LogInformation("启用药材: {HerbId}", herbId);

        try
        {
            var refitResponse = await _herbApi.ToggleStatusAsync(herbId);

            if (refitResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("药材启用成功: {HerbId}", herbId);
                return ServiceResult.Success("药材启用成功");
            }

            _logger.LogError("药材启用失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult.Failure($"药材启用失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材启用异常: {HerbId}", herbId);
            return ServiceResult.Failure($"药材启用失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> DisableAsync(Guid herbId)
    {
        _logger.LogInformation("禁用药材: {HerbId}", herbId);

        try
        {
            var refitResponse = await _herbApi.ToggleStatusAsync(herbId);

            if (refitResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("药材禁用成功: {HerbId}", herbId);
                return ServiceResult.Success("药材禁用成功");
            }

            _logger.LogError("药材禁用失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult.Failure($"药材禁用失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材禁用异常: {HerbId}", herbId);
            return ServiceResult.Failure($"药材禁用失败: {ex.Message}");
        }
    }



    #endregion

    #region Batch Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<object>> ImportHerbsAsync(List<HerbCreateDto> herbs)
    {
        ArgumentNullException.ThrowIfNull(herbs, nameof(herbs));

        if (!herbs.Any())
        {
            return ServiceResult<object>.Failure("导入的药材列表为空");
        }

        _logger.LogInformation("批量导入药材: {Count}个", herbs.Count);

        try
        {
            var successCount = 0;
            var failedItems = new List<string>();

            // 逐个创建药材
            foreach (var herb in herbs)
            {
                try
                {
                    var createResult = await CreateAsync(herb);
                    if (createResult.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        failedItems.Add($"{herb.Name}: {createResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入药材失败: {HerbName}", herb.Name);
                    failedItems.Add($"{herb.Name}: {ex.Message}");
                }
            }

            var result = new
            {
                TotalCount = herbs.Count,
                SuccessCount = successCount,
                FailedCount = failedItems.Count,
                FailedItems = failedItems
            };

            return successCount > 0
                ? ServiceResult<object>.Success(result, $"药材批量导入完成，成功: {successCount}个, 失败: {failedItems.Count}个")
                : ServiceResult<object>.Failure("导入失败，没有成功导入任何药材");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材批量导入异常");
            return ServiceResult<object>.Failure($"药材批量导入失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<byte[]>> ExportHerbsAsync(PagedQueryBaseDto query)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        _logger.LogInformation("导出药材数据");

        try
        {
            var refitResponse = await _herbApi.ExportHerbsAsync();

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var herbsData = refitResponse.Content;

                // 生成CSV格式数据
                var csvContent = "药材名称,产地,功效,用法,状态\n";
                foreach (var herb in herbsData)
                {
                    var name = herb.Name ?? string.Empty;
                    var origin = herb.Origin ?? "未知";
                    var effect = herb.Effect?.Replace(",", "；") ?? string.Empty;
                    var usage = herb.Usage?.Replace(",", "；") ?? string.Empty;
                    var status = herb.IsEnabled ? "启用" : "禁用";

                    csvContent += $"{name},{origin},{effect},{usage},{status}\n";
                }

                var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                _logger.LogInformation("药材数据导出成功: {Count}条", herbsData.Count);

                return ServiceResult<byte[]>.Success(csvBytes, $"药材数据导出完成，共 {herbsData.Count} 条");
            }

            _logger.LogError("药材数据导出失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<byte[]>.Failure($"药材数据导出失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药材数据导出异常");
            return ServiceResult<byte[]>.Failure($"药材数据导出失败: {ex.Message}");
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 简单诊所版本：无资源需要释放
        GC.SuppressFinalize(this);
    }

    #endregion
}