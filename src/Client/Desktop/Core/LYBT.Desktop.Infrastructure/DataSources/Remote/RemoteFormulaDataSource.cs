using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程验方数据源 - 通过 API 访问服务端
/// </summary>
public class RemoteFormulaDataSource : IFormulaDataSource
{
    private readonly IFormulaApi _api;
    private readonly ILogger<RemoteFormulaDataSource> _logger;
    private readonly FormulaListToDetailMapper _listMapper = new();

    public RemoteFormulaDataSource(IFormulaApi api, ILogger<RemoteFormulaDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Formula.GetById - Id={Id}", id);

        try
        {
            var response = await _api.GetFormulaByIdAsync(id);
            if (response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] Formula.GetById - NotFound: {Id}", id);
                return null;
            }
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public Task<(List<FormulaDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        return GetPagedAsync(page, pageSize, keyword, null, ct);
    }

    public async Task<(List<FormulaDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? category,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Formula.GetPaged - Page={Page}, Category={Category}", page, category);

        try
        {
            var response = await _api.GetFormulasAsync(page, pageSize, keyword, category);
            if (response.Data == null)
            {
                return (new List<FormulaDetailDto>(), 0);
            }

            var items = response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.GetPaged failed");
            throw;
        }
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Formula.Create - Name={Name}", input.Name);

        try
        {
            var response = await _api.CreateFormulaAsync(input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建验方失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.Create failed");
            throw;
        }
    }

    public async Task<FormulaDetailDto> UpdateAsync(FormulaInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Formula.Update - Id={Id}", input.Id);

        try
        {
            var response = await _api.UpdateFormulaAsync(input.Id!.Value, input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新验方失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.Update failed - Id={Id}", input.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Formula.Delete - Id={Id}", id);

        try
        {
            var response = await _api.DeleteFormulaAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<FormulaDetailDto?> CloneAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Formula.Clone - Id={Id}", id);

        try
        {
            var response = await _api.CloneFormulaAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] Formula.Clone failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.Clone failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Formula.ToggleStatus - Id={Id}", id);

        try
        {
            var response = await _api.ToggleStatusAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.ToggleStatus failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<FormulaDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Formula.Restore - Id={Id}", id);

        try
        {
            var response = await _api.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] Formula.Restore failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<FormulaDetailDto?> GetWithHerbsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Formula.GetWithHerbs - Id={Id}", id);

        // GetFormulaByIdAsync 已包含药材组成
        return await GetByIdAsync(id, ct);
    }

    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Formula.BatchDelete - Count={Count}", ids.Count);

        try
        {
            var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量删除失败"
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }
    // OpenSpec: SYNC-D02 - 过渡态方法

    /// <inheritdoc />
    public async Task<BatchOperationResultDto> BatchImportAsync(List<FormulaImportItemDto> items, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Formula.BatchImport - Count={Count}", items.Count);

        try
        {
            var response = await _api.BatchImportAsync(new FormulaBatchImportInputDto
            {
                Formulas = items
            });

            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = items.Count,
                    FailureCount = items.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量导入失败"
                };
            }

            // 将 FormulaBatchImportResultDto 转换为 BatchOperationResultDto
            return new BatchOperationResultDto
            {
                TotalCount = response.Data.TotalCount,
                SuccessCount = response.Data.SuccessCount,
                FailureCount = response.Data.FailureCount,
                IsSuccess = response.Data.FailureCount == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.BatchImport failed");
            return new BatchOperationResultDto
            {
                TotalCount = items.Count,
                FailureCount = items.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<List<FormulaDetailDto>> GetPendingValidationAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Formula.GetPendingValidation");

        try
        {
            // 使用分页获取并在客户端过滤（远程 API 无专用端点）
            var response = await _api.GetFormulasAsync(1, 10000);
            if (response.Data == null)
            {
                return new List<FormulaDetailDto>();
            }

            return response.Data.Items
                .Where(f => f.ValidationStatus == FormulaValidationStatus.Draft)
                .Select(_listMapper.ToDetailDto)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.GetPendingValidation failed");
            return new List<FormulaDetailDto>();
        }
    }

    /// <inheritdoc />
    public async Task<List<FormulaDetailDto>> GetAllForExportAsync(string? keyword = null, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Formula.GetAllForExport - Keyword={Keyword}", keyword);

        try
        {
            var response = await _api.GetFormulasAsync(1, 10000, keyword);
            if (response.Data == null)
            {
                return new List<FormulaDetailDto>();
            }

            return response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Formula.GetAllForExport failed");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> ValidateHerbBindingsAsync(Guid formulaId, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Formula.ValidateHerbBindings - FormulaId={FormulaId}", formulaId);

        // 远程模式验证由服务端完成，客户端保守返回 true
        return Task.FromResult(true);
    }
}

/// <summary>
/// FormulaListDto -> FormulaDetailDto 映射器 (仅限 DTO 间转换, 无 Entity 依赖)
/// </summary>
[Mapper]
internal partial class FormulaListToDetailMapper
{
    [MapperIgnoreTarget(nameof(FormulaDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.CreatedBy))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Usage))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Property))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Source))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Remark))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Contraindications))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Herbs))]
    public partial FormulaDetailDto ToDetailDto(FormulaListDto listDto);
}
