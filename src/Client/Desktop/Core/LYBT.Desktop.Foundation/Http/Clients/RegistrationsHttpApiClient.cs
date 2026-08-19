// ---------------------------------------------------------------------------
// RegistrationsHttpApiClient — HttpClient adapter for IApiClientRegistrations
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientRegistrations (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using System.Net.Http;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式挂号 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class RegistrationsHttpApiClient : HttpApiClientBase, IApiClientRegistrations
{
    public RegistrationsHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    public Task<ApiResponse<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request)
        => PostAndWrapAsync<RegistrationDetailDto>("/api/v1/registrations", request);

    public Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id)
        => GetAndWrapAsync<RegistrationDetailDto>($"/api/v1/registrations/{id}");

    public async Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync(
        int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId)
    {
        var url = BuildPagedUrl("/api/v1/registrations", page, pageSize,
            ("keyword", keyword),
            ("startDate", startDate?.ToString("O")),
            ("endDate", endDate?.ToString("O")),
            ("patientId", patientId?.ToString()),
            ("doctorId", doctorId?.ToString()));
        return await GetPagedAndWrapAsync<RegistrationListDto>(url);
    }

    public async Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(Guid? doctorId)
    {
        var url = "/api/v1/registrations/queue";
        if (doctorId.HasValue) url += $"?doctorId={doctorId.Value}";
        return await GetAndWrapAsync<List<RegistrationListDto>>(url);
    }

    public async Task<ApiResponse<Guid>> StartVisitAsync(Guid id)
        => await SendAndWrapAsync<Guid>($"/api/v1/registrations/{id}/start-visit", HttpMethod.Put);

    public async Task<ApiResponse> CancelAsync(Guid id)
    {
        await PutVoidAsync($"/api/v1/registrations/{id}/cancel");
        return WrapSuccess();
    }
}
