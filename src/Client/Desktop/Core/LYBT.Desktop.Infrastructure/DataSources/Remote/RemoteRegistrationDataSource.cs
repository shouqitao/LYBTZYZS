using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程挂号数据源实现 - 调用 WebAPI
/// PRD: registration.md
/// </summary>
public class RemoteRegistrationDataSource : IRegistrationDataSource
{
    private readonly IRegistrationApi _api;
    private readonly ILogger<RemoteRegistrationDataSource> _logger;
    private readonly RegistrationListToDetailMapper _listMapper = new();

    public RemoteRegistrationDataSource(IRegistrationApi api, ILogger<RemoteRegistrationDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Registration.GetById - Id={Id}", id);

        try
        {
            var response = await _api.GetByIdAsync(id);
            if (response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] Registration.GetById - NotFound: {Id}", id);
                return null;
            }
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Registration.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<(List<RegistrationDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Registration.GetPaged - Page={Page}, Keyword={Keyword}", page, keyword);

        try
        {
            var response = await _api.GetListAsync(page, pageSize, keyword);
            if (response.Data == null)
            {
                return (new List<RegistrationDetailDto>(), 0);
            }

            var items = response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Registration.GetPaged failed");
            throw;
        }
    }

    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Registration.Create - PatientName={PatientName}", input.PatientName);

        try
        {
            var response = await _api.CreateAsync(input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建挂号失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Registration.Create failed");
            throw;
        }
    }

    public Task<RegistrationDetailDto> UpdateAsync(RegistrationInputDto input, CancellationToken ct = default)
    {
        // 挂号不支持更新操作，只有状态流转 (StartVisit/Cancel)
        throw new NotSupportedException("挂号记录不支持更新操作，请使用 StartVisitAsync 或 CancelAsync");
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // 挂号不支持删除，只支持取消
        throw new NotSupportedException("挂号记录不支持删除操作，请使用 CancelAsync");
    }

    public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Registration.GetWaitingQueue - DoctorId={DoctorId}", doctorId);

        try
        {
            var response = await _api.GetQueueAsync(doctorId);
            if (response.Data == null)
            {
                return new List<RegistrationListDto>();
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Registration.GetWaitingQueue failed");
            throw;
        }
    }

    public async Task<Guid?> StartVisitAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Registration.StartVisit - Id={Id}", id);

        try
        {
            var response = await _api.StartVisitAsync(id);

            if (!response.Success)
            {
                _logger.LogWarning("[RemoteDataSource] Registration.StartVisit failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Registration.StartVisit failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Registration.Cancel - Id={Id}", id);

        try
        {
            var response = await _api.CancelAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Registration.Cancel failed - Id={Id}", id);
            return false;
        }
    }
}

/// <summary>
/// RegistrationListDto -> RegistrationDetailDto 映射器 (仅限 DTO 间转换, 无 Entity 依赖)
/// </summary>
[Mapper]
internal partial class RegistrationListToDetailMapper
{
    [MapperIgnoreTarget(nameof(RegistrationDetailDto.Remark))]
    [MapperIgnoreTarget(nameof(RegistrationDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(RegistrationDetailDto.CreatedBy))]
    public partial RegistrationDetailDto ToDetailDto(RegistrationListDto listDto);
}
