using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Registration.Repositories;

/// <summary>
/// 挂号仓储实现 -- DataSource 抽象层，支持 Local/Remote 双模式
/// PRD: registration.md US-REG-001~006
/// </summary>
public class RegistrationRepository : IRegistrationRepository
{
    private readonly IRegistrationDataSource _dataSource;
    private readonly IRegistrationApi? _api;
    private readonly ILogger _logger;

    public RegistrationRepository(
        IRegistrationDataSource dataSource,
        ILogger<RegistrationRepository> logger,
        IRegistrationApi? api = null)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _api = api;
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input)
    {
        _logger.LogDebug("[REG-REPO] 创建挂号: PatientId={PatientId}, DoctorId={DoctorId}, Source={Source}",
            input.PatientId, input.DoctorId, input.Source);

        return await _dataSource.CreateAsync(input);
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id)
    {
        return await _dataSource.GetByIdAsync(id);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        if (_api is not null)
        {
            try
            {
                var response = await _api.GetListAsync(page, pageSize, keyword);
                if (response is { Success: true, Data: not null })
                {
                    return response.Data;
                }

                _logger.LogWarning("[REG-REPO] 分页查询失败: {Message}", response?.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REG-REPO] 分页查询异常");
            }

            return new PagedResult<RegistrationListDto>();
        }

        // Local 模式: 通过 DataSource 的 GetPagedAsync
        var (items, total) = await _dataSource.GetPagedAsync(page, pageSize, keyword);
        return new PagedResult<RegistrationListDto>
        {
            Items = items.Select(d => new RegistrationListDto
            {
                Id = d.Id,
                PatientId = d.PatientId,
                PatientName = d.PatientName,
                DoctorId = d.DoctorId,
                DoctorName = d.DoctorName,
                MedicalCaseId = d.MedicalCaseId,
                Source = d.Source,
                Status = d.Status,
                CreatedAt = d.CreatedAt
            }).ToList(),
            TotalCount = total,
            CurrentPage = page
        };
    }

    /// <inheritdoc/>
    public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null)
    {
        _logger.LogDebug("[REG-REPO] 获取等待队列: DoctorId={DoctorId}", doctorId);
        return await _dataSource.GetWaitingQueueAsync(doctorId);
    }

    /// <inheritdoc/>
    public async Task<Guid?> StartVisitAsync(Guid id)
    {
        _logger.LogInformation("[REG-REPO] 接诊: RegistrationId={Id}", id);
        return await _dataSource.StartVisitAsync(id);
    }

    /// <inheritdoc/>
    public async Task<bool> CancelAsync(Guid id)
    {
        _logger.LogInformation("[REG-REPO] 取消挂号: RegistrationId={Id}", id);
        return await _dataSource.CancelAsync(id);
    }
}
