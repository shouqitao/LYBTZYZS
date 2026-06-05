using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Registration.Repositories;

/// <summary>
/// 挂号仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class RegistrationRepository : IRegistrationRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<RegistrationRepository> _logger;

    public RegistrationRepository(
        IApiClient apiClient,
        ILogger<RegistrationRepository> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            _logger.LogInformation("[REPO] Registration.Create - PatientId={PatientId}, DoctorId={DoctorId}, Source={Source}",
                input.PatientId, input.DoctorId, input.Source);

            var response = await _apiClient.Registrations.CreateAsync(input);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建挂号失败");

            _logger.LogInformation("[REPO] Registration.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Registration.Create failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO] Registration.GetById - Id={Id}", id);

            var response = await _apiClient.Registrations.GetByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Registration.GetById failed - Id={Id}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[REPO] Registration.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var response = await _apiClient.Registrations.GetListAsync(page, pageSize, keyword);
            if (response.Data == null)
                return new PagedResult<RegistrationListDto> { Items = [], TotalCount = 0, CurrentPage = page };

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Registration.GetPaged failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null)
    {
        try
        {
            _logger.LogDebug("[REPO] Registration.GetWaitingQueue - DoctorId={DoctorId}", doctorId);

            var response = await _apiClient.Registrations.GetQueueAsync(doctorId);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO] Registration.GetWaitingQueue failed: {Message}", response.Message);
                return [];
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Registration.GetWaitingQueue failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Guid?> StartVisitAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO] Registration.StartVisit - Id={Id}", id);

            var response = await _apiClient.Registrations.StartVisitAsync(id);
            if (!response.Success)
            {
                _logger.LogWarning("[REPO] Registration.StartVisit failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO] Registration.StartVisit completed - Id={Id}, MedicalCaseId={McId}",
                id, response.Data);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Registration.StartVisit failed - Id={Id}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CancelAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO] Registration.Cancel - Id={Id}", id);

            var response = await _apiClient.Registrations.CancelAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO] Registration.Cancel completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO] Registration.Cancel failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Registration.Cancel failed - Id={Id}", id);
            return false;
        }
    }
}
