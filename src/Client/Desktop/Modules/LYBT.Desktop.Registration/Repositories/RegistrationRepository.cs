using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Registration.Repositories;

/// <summary>
/// 挂号仓储 - 通过 Refit IRegistrationApi 访问 WebAPI。
/// </summary>
public sealed class RegistrationRepository : IRegistrationRepository
{
    private readonly IRegistrationApi _api;
    private readonly ILocalRegistrationApi _localApi;
    private readonly IApiRouter _apiRouter;
    private readonly ILogger<RegistrationRepository> _logger;

    private bool IsOffline => _apiRouter.IsOffline;

    public RegistrationRepository(
        IRegistrationApi api,
        ILocalRegistrationApi localApi,
        IApiRouter apiRouter,
        ILogger<RegistrationRepository> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _localApi = localApi ?? throw new ArgumentNullException(nameof(localApi));
        _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Registration.Create - PatientId={PatientId}, DoctorId={DoctorId}, Source={Source}",
                    input.PatientId, input.DoctorId, input.Source);
                return await _localApi.CreateRegistrationAsync(input);
            }

            _logger.LogInformation("[REPO:Remote] Registration.Create - PatientId={PatientId}, DoctorId={DoctorId}, Source={Source}",
                input.PatientId, input.DoctorId, input.Source);

            var response = await _api.CreateAsync(input);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建挂号失败");

            _logger.LogInformation("[REPO:Remote] Registration.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Registration.Create failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Registration.GetById - Id={Id}", id);
                return await _localApi.GetRegistrationByIdAsync(id);
            }

            _logger.LogDebug("[REPO:Remote] Registration.GetById - Id={Id}", id);

            var response = await _api.GetByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Registration.GetById failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Registration.GetPaged - Page={Page} PageSize={PageSize}", page, pageSize);
                var registrations = await _localApi.GetRegistrationsAsync(date: null);
                return new PagedResult<RegistrationListDto>
                {
                    Items = registrations,
                    TotalCount = registrations.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }

            _logger.LogDebug("[REPO:Remote] Registration.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var response = await _api.GetListAsync(page, pageSize, keyword);
            if (response.Data == null)
                return new PagedResult<RegistrationListDto> { Items = [], TotalCount = 0, CurrentPage = page };

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Registration.GetPaged failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Registration.GetWaitingQueue - DoctorId={DoctorId}", doctorId);
                return await _localApi.GetQueueAsync(doctorId);
            }

            _logger.LogDebug("[REPO:Remote] Registration.GetWaitingQueue - DoctorId={DoctorId}", doctorId);

            var response = await _api.GetQueueAsync(doctorId);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Registration.GetWaitingQueue failed: {Message}", response.Message);
                return [];
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Registration.GetWaitingQueue failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Guid?> StartVisitAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Registration.StartVisit - Id={Id}", id);
                var medicalCaseId = await _localApi.StartVisitAsync(id);
                return medicalCaseId;
            }

            _logger.LogInformation("[REPO:Remote] Registration.StartVisit - Id={Id}", id);

            var response = await _api.StartVisitAsync(id);
            if (!response.Success)
            {
                _logger.LogWarning("[REPO:Remote] Registration.StartVisit failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] Registration.StartVisit completed - Id={Id}, MedicalCaseId={McId}",
                id, response.Data);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Registration.StartVisit failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CancelAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Registration.Cancel - Id={Id}", id);
                await _localApi.CancelAsync(id);
                return true;
            }

            _logger.LogInformation("[REPO:Remote] Registration.Cancel - Id={Id}", id);

            var response = await _api.CancelAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO:Remote] Registration.Cancel completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO:Remote] Registration.Cancel failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Registration.Cancel failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            return false;
        }
    }
}
