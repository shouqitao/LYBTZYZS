// ---------------------------------------------------------------------------
// RegistrationApiClient — Refit adapter for IApiClientRegistrations
// ---------------------------------------------------------------------------
// Delegates remote calls to IRegistrationApi (Refit-generated HTTP client).
// Local-only methods throw NotSupportedException in remote mode.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 挂号管理 API 客户端——包装 IRegistrationApi（Refit）以实现 IApiClientRegistrations。
/// </summary>
internal sealed class RegistrationApiClient : IApiClientRegistrations
{
    private readonly IRegistrationApi _api;

    /// <summary>
    /// 初始化 <see cref="RegistrationApiClient"/> 类的新实例。
    /// </summary>
    /// <param name="api">Refit-generated registration API client.</param>
    public RegistrationApiClient(IRegistrationApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request)
        => _api.CreateAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id)
        => _api.GetByIdAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? patientId = null,
        Guid? doctorId = null)
        => _api.GetListAsync(page, pageSize, keyword, startDate, endDate, patientId, doctorId);

    /// <inheritdoc />
    public Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(Guid? doctorId = null)
        => _api.GetQueueAsync(doctorId);

    /// <inheritdoc />
    public Task<ApiResponse<Guid>> StartVisitAsync(Guid id)
        => _api.StartVisitAsync(id);

    /// <inheritdoc/>
    public Task<ApiResponse<QuickVisitResultDto>> QuickVisitAsync(QuickVisitInputDto request)
        => _api.QuickVisitAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse> CancelAsync(Guid id)
        => _api.CancelAsync(id);
}
