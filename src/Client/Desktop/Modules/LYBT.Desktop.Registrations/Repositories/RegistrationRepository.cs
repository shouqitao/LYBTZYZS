using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Registrations.Repositories;

/// <summary>
/// 挂号仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class RegistrationRepository : ApiClientRepositoryBase<RegistrationListDto, RegistrationDetailDto>, IRegistrationRepository
{
    private readonly IApiClientRegistrations _registrations;

    public RegistrationRepository(
        IApiClientRegistrations registrations,
        ILogger<RegistrationRepository> logger)
        : base(logger)
    {
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
    }

    protected override string LogPrefix => "Registration";

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return await ExecuteAsync(
            async () =>
            {
                var response = await _registrations.CreateAsync(input, ct);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "创建挂号失败");

                Logger.LogInformation("[REPO] Registration.Create completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Create",
            LogLevel.Information);
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _registrations.GetByIdAsync(id, ct);
                return response.Data;
            },
            "GetById");
    }

    /// <inheritdoc/>
    public async Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _registrations.GetListAsync(page, pageSize, keyword, ct: ct);
                if (response.Data == null)
                    return new PagedResult<RegistrationListDto> { Items = [], TotalCount = 0, CurrentPage = page };

                return response.Data;
            },
            "GetPaged",
            "[REPO] Registration.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
            [page, pageSize, keyword]);
    }

    /// <inheritdoc/>
    public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _registrations.GetQueueAsync(doctorId, ct);
                if (!response.Success || response.Data == null)
                {
                    Logger.LogWarning("[REPO] Registration.GetWaitingQueue failed: {Message}", response.Message);
                    return [];
                }

                return response.Data;
            },
            "GetWaitingQueue");
    }

    /// <inheritdoc/>
    public async Task<Guid?> StartVisitAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync<Guid?>(
            async () =>
            {
                var response = await _registrations.StartVisitAsync(id, ct);
                if (!response.Success)
                {
                    // 抛出服务器业务消息（如 BR-001 重开现有医案），经 Service 映射后展示给用户
                    Logger.LogWarning("[REPO] Registration.StartVisit failed: {Message}", response.Message);
                    throw new InvalidOperationException(response.Message ?? "接诊失败");
                }

                Logger.LogInformation("[REPO] Registration.StartVisit completed - Id={Id}, MedicalCaseId={McId}",
                    id, response.Data);
                return response.Data;
            },
            "StartVisit",
            LogLevel.Information);
    }

    /// <inheritdoc/>
    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        await ExecuteAsync(
            async () =>
            {
                var response = await _registrations.CancelAsync(id, ct);
                if (!response.Success)
                    throw new InvalidOperationException(response.Message ?? "取消挂号失败");

                Logger.LogInformation("[REPO] Registration.Cancel completed - Id={Id}", id);
            },
            "Cancel",
            LogLevel.Information);
    }
}
