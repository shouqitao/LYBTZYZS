using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpRegistrationRepository : IRegistrationRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<HttpRegistrationRepository> _logger;

    public HttpRegistrationRepository(IApiClient apiClient, ILogger<HttpRegistrationRepository> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input)
    {
        var response = await _apiClient.Registrations.CreateAsync(input);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Create registration failed");
        return response.Data;
    }

    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _apiClient.Registrations.GetByIdAsync(id);
        return response.Data;
    }

    public async Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        var response = await _apiClient.Registrations.GetListAsync(page, pageSize, keyword);
        if (response.Data == null)
            return new PagedResult<RegistrationListDto>();
        return response.Data;
    }

    public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null)
    {
        var response = await _apiClient.Registrations.GetQueueAsync(doctorId);
        if (!response.Success || response.Data == null)
            return [];
        return response.Data;
    }

    public async Task<Guid?> StartVisitAsync(Guid id)
    {
        var response = await _apiClient.Registrations.StartVisitAsync(id);
        if (!response.Success)
            return null;
        return response.Data;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var response = await _apiClient.Registrations.CancelAsync(id);
        return response.Success;
    }
}
