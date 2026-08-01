// ---------------------------------------------------------------------------
// HttpClientApiClient — LocalWebAPI IApiClient implementation
// ---------------------------------------------------------------------------
// Single class implementing ALL IApiClient sub-interfaces using
// IHttpClientFactory + System.Text.Json for LocalWebAPI mode.
//
// This is the counterpart to RefitApiClient (Remote mode).
// Routes to LocalWebAPI controllers via /api/ prefix (no version).
// LocalWebAPI returns raw DTOs; this class wraps them in ApiResponse<T>.
//
// NOTE: Many sub-interfaces share method names (e.g. BatchDeleteAsync,
// ToggleStatusAsync, RestoreAsync). Explicit interface implementation is
// used for ALL interface methods to avoid ambiguity.
// ---------------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using System.Text.Json;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Diagnostics;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// LocalWebAPI mode API client — uses IHttpClientFactory to call local ASP.NET Core endpoints.
/// Implements <see cref="IApiClient"/> and all sub-interfaces directly (no adapter classes).
/// Uses explicit interface implementation for all methods to avoid ambiguity across
/// sub-interfaces that share method names.
/// </summary>
/// <remarks>
/// <para>Serialization: System.Text.Json with PascalCase (PropertyNamingPolicy = null)
/// to match LocalWebAPI's default JSON format.</para>
/// <para>Error handling: non-2xx responses are read and thrown as HttpRequestException
/// with a user-friendly message.</para>
/// <para>Response wrapping: LocalWebAPI returns raw DTOs; each method wraps the result
/// in <see cref="ApiResponse{T}"/> to satisfy the unified interface contract.</para>
/// </remarks>
public sealed class HttpClientApiClient : IApiClient,
    IApiClientAuth, IApiClientUsers, IApiClientPatients,
    IApiClientHerbs, IApiClientFormulas, IApiClientMedicalCases,
    IApiClientRegistrations, IApiClientReports,
    IApiClientDeploy, IApiClientDiagnostics
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// JSON serialization options matching LocalWebAPI format:
    /// PascalCase naming, case-insensitive deserialization.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="HttpClientApiClient"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating named HttpClient instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClientFactory"/> is null.</exception>
    public HttpClientApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    // ========================================================================
    // IApiClient properties — return this (class implements all sub-interfaces)
    // ========================================================================

    /// <inheritdoc />
    public IApiClientAuth Auth => this;

    /// <inheritdoc />
    public IApiClientUsers Users => this;

    /// <inheritdoc />
    public IApiClientPatients Patients => this;

    /// <inheritdoc />
    public IApiClientHerbs Herbs => this;

    /// <inheritdoc />
    public IApiClientFormulas Formulas => this;

    /// <inheritdoc />
    public IApiClientMedicalCases MedicalCases => this;

    /// <inheritdoc />
    public IApiClientRegistrations Registrations => this;

    /// <inheritdoc />
    public IApiClientReports Reports => this;

    /// <inheritdoc />
    public IApiClientDeploy Deploy => this;

    /// <inheritdoc />
    public IApiClientDiagnostics Diagnostics => this;

    // ========================================================================
    // Base HTTP helpers (private)
    // ========================================================================

    private HttpClient CreateClient() => _httpClientFactory.CreateClient();

    private static StringContent ToJsonContent<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct = default)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// 反序列化 LocalWebAPI 响应并解包 ApiResponse&lt;T&gt; 信封（与 Remote/Refit 契约一致）。
    /// 兼容两种情况：LocalWebAPI 返回信封时取 Data；极端情况返回裸 T 时直接反序列化。
    /// </summary>
    private static async Task<ApiResponse<T>> DeserializeEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken ct = default)
    {
        var json = await response.Content.ReadAsStringAsync(ct);

        try
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(json, JsonOptions);
            if (envelope != null)
                return envelope;
        }
        catch (JsonException)
        {
            // 不是信封格式，回退裸反序列化
        }

        var raw = JsonSerializer.Deserialize<T>(json, JsonOptions);
        return ApiResponse<T>.CreateSuccess(raw!);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorContent = await response.Content.ReadAsStringAsync();
        var message = !string.IsNullOrWhiteSpace(errorContent)
            ? errorContent
            : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static ApiResponse<T> WrapSuccess<T>(T data, string message = "操作成功")
        => ApiResponse<T>.CreateSuccess(data, message);

    private static ApiResponse WrapSuccess(string message = "操作成功")
        => ApiResponse.CreateSuccess(null, message);

    /// <summary>Build URL with pagination + optional filter parameters.</summary>
    private static string BuildPagedUrl(string baseUrl, int page, int pageSize, params (string Key, string? Value)[] filters)
    {
        var sb = new StringBuilder($"{baseUrl}?page={page}&pageSize={pageSize}");
        foreach (var (key, value) in filters)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            sb.Append('&');
            sb.Append(key);
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
        }
        return sb.ToString();
    }

    /// <summary>Build URL with conditional query parameters.</summary>
    private static string BuildQueryString(string baseUrl, params (string Key, string? Value)[] parameters)
    {
        var sb = new StringBuilder(baseUrl);
        var first = !baseUrl.Contains('?');
        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            sb.Append(first ? '?' : '&');
            sb.Append(key);
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
            first = false;
        }
        return sb.ToString();
    }

    /// <summary>Unified HTTP request execution with response handling.</summary>
    private async Task<HttpResponseMessage> SendAsync(string url, HttpMethod method, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        HttpResponseMessage response;
        if (body != null)
        {
            using var content = ToJsonContent(body);
            response = method.Method.ToUpperInvariant() switch
            {
                "POST" => await client.PostAsync(url, content, ct),
                "PUT" => await client.PutAsync(url, content, ct),
                "PATCH" => await client.PatchAsync(url, content, ct),
                _ => throw new ArgumentException($"Unsupported HTTP method: {method.Method}")
            };
        }
        else
        {
            response = method.Method.ToUpperInvariant() switch
            {
                "GET" => await client.GetAsync(url, ct),
                "POST" => await client.PostAsync(url, null, ct),
                "PUT" => await client.PutAsync(url, null, ct),
                "DELETE" => await client.DeleteAsync(url, ct),
                _ => throw new ArgumentException($"Unsupported HTTP method: {method.Method}")
            };
        }
        await EnsureSuccessOrThrowAsync(response);
        return response;
    }

    /// <summary>GET -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    private async Task<ApiResponse<T>> GetAndWrapAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await SendAsync(url, HttpMethod.Get, ct: ct);
        return await DeserializeEnvelopeAsync<T>(response, ct);
    }

    /// <summary>GET -> deserialize -> return raw T (local-only methods).</summary>
    private async Task<T> GetRawAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await SendAsync(url, HttpMethod.Get, ct: ct);
        var envelope = await DeserializeEnvelopeAsync<T>(response, ct);
        return envelope.Data ?? default!;
    }

    /// <summary>POST with JSON body -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    private Task<ApiResponse<T>> PostAndWrapAsync<T>(string url, object? body = null, CancellationToken ct = default)
        => SendAndWrapAsync<T>(url, HttpMethod.Post, body, ct);

    /// <summary>Unified void HTTP request -> ApiResponse.</summary>
    private async Task<ApiResponse> SendVoidAsync(string url, HttpMethod method, object? body = null, CancellationToken ct = default)
    {
        await SendAsync(url, method, body, ct);
        return WrapSuccess();
    }

    /// <summary>POST -> non-generic ApiResponse (void operations).</summary>
    private Task<ApiResponse> PostVoidAsync(string url, object? body = null, CancellationToken ct = default)
        => SendVoidAsync(url, HttpMethod.Post, body, ct);

    /// <summary>POST -> return raw T (local-only methods).</summary>
    private async Task<T> PostRawAsync<T>(string url, object? body = null, CancellationToken ct = default)
    {
        var response = await SendAsync(url, HttpMethod.Post, body, ct);
        var envelope = await DeserializeEnvelopeAsync<T>(response, ct);
        return envelope.Data ?? default!;
    }

    /// <summary>PUT with JSON body -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    private Task<ApiResponse<T>> PutAndWrapAsync<T>(string url, object? body = null, CancellationToken ct = default)
        => SendAndWrapAsync<T>(url, HttpMethod.Put, body, ct);

    /// <summary>PUT -> non-generic ApiResponse (void operations).</summary>
    private Task<ApiResponse> PutVoidAsync(string url, object? body = null, CancellationToken ct = default)
        => SendVoidAsync(url, HttpMethod.Put, body, ct);

    /// <summary>DELETE -> non-generic ApiResponse.</summary>
    private Task<ApiResponse> DeleteVoidAsync(string url, CancellationToken ct = default)
        => SendVoidAsync(url, HttpMethod.Delete, ct: ct);

    /// <summary>HTTP request -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    private async Task<ApiResponse<T>> SendAndWrapAsync<T>(string url, HttpMethod method, object? body = null, CancellationToken ct = default)
    {
        var response = await SendAsync(url, method, body, ct);
        return await DeserializeEnvelopeAsync<T>(response, ct);
    }

    /// <summary>GET -> server-side pagination envelope -> wrap in ApiResponse&lt;PagedResult&lt;T&gt;&gt;.</summary>
    private Task<ApiResponse<PagedResult<T>>> GetPagedAndWrapAsync<T>(string url, CancellationToken ct = default)
        => GetAndWrapAsync<PagedResult<T>>(url, ct);

    /// <summary>GET -> return HttpResponseMessage (file downloads). Caller disposes response.</summary>
    private async Task<HttpResponseMessage> GetResponseAsync(string url, CancellationToken ct = default)
    {
        var client = CreateClient();
        var response = await client.GetAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response);
        return response;
    }

    // ========================================================================
    // IApiClientAuth — Authentication endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<LoginResponse>> IApiClientAuth.LoginAsync(LoginRequest loginRequest)
        => await PostAndWrapAsync<LoginResponse>("/api/v1/auth/login", loginRequest);

    async Task<ApiResponse<LoginResponse>> IApiClientAuth.LoginWithAutoTokenAsync(AutoLoginRequest request)
        => await PostAndWrapAsync<LoginResponse>("/api/v1/auth/auto-login", request);

    async Task<ApiResponse> IApiClientAuth.LogoutAsync(LogoutRequest logoutRequest)
    {
        await PostVoidAsync("/api/v1/auth/logout", logoutRequest);
        return WrapSuccess();
    }

    async Task<ApiResponse<LoginResponse>> IApiClientAuth.RefreshTokenAsync(RefreshTokenRequest request)
        => await PostAndWrapAsync<LoginResponse>("/api/v1/auth/refresh", request);

    async Task<ApiResponse<ValidateTokenResponse>> IApiClientAuth.ValidateTokenAsync()
        => await GetAndWrapAsync<ValidateTokenResponse>("/api/v1/auth/validate");

    async Task<ApiResponse<HealthCheckResponse>> IApiClientAuth.HealthCheckAsync()
        => await GetAndWrapAsync<HealthCheckResponse>("/api/v1/health");

    // ========================================================================
    // IApiClientUsers — User management endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<UserListDto>>> IApiClientUsers.GetUsersAsync(
        int page, int pageSize, string? keyword)
    {
        var url = BuildPagedUrl("/api/v1/users", page, pageSize, ("keyword", keyword));
        return await GetPagedAndWrapAsync<UserListDto>(url);
    }

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.GetUserByIdAsync(Guid id)
        => GetAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}");

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.CreateUserAsync(UserInputDto request)
        => PostAndWrapAsync<UserDetailDto>("/api/v1/users", request);

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.UpdateUserAsync(Guid id, UserInputDto request)
        => PutAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}", request);

    Task<ApiResponse> IApiClientUsers.DeleteUserAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/users/{id}");

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.ChangeProfileAsync(Guid id, ChangeProfileDto request)
        => PutAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/profile", request);

    Task<ApiResponse> IApiClientUsers.ChangePasswordAsync(Guid id, ChangePasswordRequest request)
        => PutVoidAsync($"/api/v1/users/{id}/change-password", request);

    Task<ApiResponse<ResetPasswordResponseDto>> IApiClientUsers.ResetPasswordAsync(Guid id, ResetPasswordRequestDto request)
        => PostAndWrapAsync<ResetPasswordResponseDto>($"/api/v1/users/{id}/reset-password", request);

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/toggle-status");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientUsers.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-delete", request);

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.RestoreAsync(Guid id)
        => PostAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/restore");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientUsers.BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-enable", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientUsers.BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-disable", request);

    Task<UserDetailDto> IApiClientUsers.GetCurrentUserAsync()
        => GetRawAsync<UserDetailDto>("/api/v1/users/current");

    // ========================================================================
    // IApiClientPatients — Patient management endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<PatientListDto>>> IApiClientPatients.GetPatientsAsync(
        int page, int pageSize, string? keyword)
    {
        var url = BuildPagedUrl("/api/v1/patients", page, pageSize, ("keyword", keyword));
        return await GetPagedAndWrapAsync<PatientListDto>(url);
    }

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.GetPatientByIdAsync(Guid id)
        => GetAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}");

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.CreatePatientAsync(PatientInputDto request)
        => PostAndWrapAsync<PatientDetailDto>("/api/v1/patients", request);

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.UpdatePatientAsync(Guid id, PatientInputDto request)
        => PutAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}", request);

    Task<ApiResponse> IApiClientPatients.DeletePatientAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/patients/{id}");

    Task<ApiResponse<PatientBatchImportResultDto>> IApiClientPatients.BatchImportAsync(PatientBatchImportInputDto request)
        => PostAndWrapAsync<PatientBatchImportResultDto>("/api/v1/patients/import", request);

    Task<HttpResponseMessage> IApiClientPatients.ExportTemplateAsync()
        => GetResponseAsync("/api/v1/patients/import-template");

    async Task<HttpResponseMessage> IApiClientPatients.ExportPatientsAsync(string? keyword)
    {
        var url = "/api/v1/patients/export";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"?keyword={Uri.EscapeDataString(keyword)}";
        return await GetResponseAsync(url);
    }

    Task<ApiResponse<BatchOperationResultDto>> IApiClientPatients.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/patients/batch-delete", request);

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}/toggle-status");

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.RestoreAsync(Guid id)
        => PostAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}/restore");

    // ========================================================================
    // IApiClientHerbs — Herb management endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<HerbListDto>>> IApiClientHerbs.GetHerbsAsync(
        int page, int pageSize, string? keyword, string? category)
    {
        var url = BuildPagedUrl("/api/v1/herbs", page, pageSize, ("keyword", keyword), ("category", category));
        return await GetPagedAndWrapAsync<HerbListDto>(url);
    }

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.GetHerbByIdAsync(Guid id)
        => GetAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}");

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.CreateHerbAsync(HerbInputDto request)
        => PostAndWrapAsync<HerbDetailDto>("/api/v1/herbs", request);

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.UpdateHerbAsync(Guid id, HerbInputDto request)
        => PutAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}", request);

    Task<ApiResponse> IApiClientHerbs.DeleteHerbAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/herbs/{id}");

    Task<ApiResponse<HerbBatchImportResultDto>> IApiClientHerbs.BatchImportAsync(HerbBatchImportInputDto request)
        => PostAndWrapAsync<HerbBatchImportResultDto>("/api/v1/herbs/batch-import", request);

    Task<HttpResponseMessage> IApiClientHerbs.ExportTemplateAsync()
        => GetResponseAsync("/api/v1/herbs/import-template");

    async Task<HttpResponseMessage> IApiClientHerbs.ExportHerbsAsync(string? keyword)
    {
        var url = "/api/v1/herbs/export";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"?keyword={Uri.EscapeDataString(keyword)}";
        return await GetResponseAsync(url);
    }

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}/toggle-status");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientHerbs.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-delete", request);

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.RestoreAsync(Guid id)
        => PostAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}/restore");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientHerbs.BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-enable", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientHerbs.BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-disable", request);

    Task<List<string>> IApiClientHerbs.GetCategoriesAsync()
        => GetRawAsync<List<string>>("/api/v1/herbs/categories");

    // ========================================================================
    // IApiClientFormulas — Formula management endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<FormulaListDto>>> IApiClientFormulas.GetFormulasAsync(
        int page, int pageSize, string? keyword, string? category)
    {
        var url = BuildPagedUrl("/api/v1/formulas", page, pageSize, ("keyword", keyword), ("category", category));
        return await GetPagedAndWrapAsync<FormulaListDto>(url);
    }

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.GetFormulaByIdAsync(Guid id)
        => GetAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}");

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.CreateFormulaAsync(FormulaInputDto request)
        => PostAndWrapAsync<FormulaDetailDto>("/api/v1/formulas", request);

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.UpdateFormulaAsync(Guid id, FormulaInputDto request)
        => PutAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}", request);

    Task<ApiResponse> IApiClientFormulas.DeleteFormulaAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/formulas/{id}");

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.CloneFormulaAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/clone");

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/toggle-status");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientFormulas.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-delete", request);

    Task<ApiResponse<FormulaBatchImportResultDto>> IApiClientFormulas.BatchImportAsync(FormulaBatchImportInputDto request)
        => PostAndWrapAsync<FormulaBatchImportResultDto>("/api/v1/formulas/batch-import", request);

    async Task<HttpResponseMessage> IApiClientFormulas.ExportFormulasAsync(string? category)
    {
        var url = "/api/v1/formulas/export";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"?category={Uri.EscapeDataString(category)}";
        return await GetResponseAsync(url);
    }

    Task<HttpResponseMessage> IApiClientFormulas.ExportTemplateAsync()
        => GetResponseAsync("/api/v1/formulas/import-template");

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.RestoreAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/restore");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientFormulas.BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-enable", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientFormulas.BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-disable", request);

    Task<ApiResponse<List<FormulaListDto>>> IApiClientFormulas.GetPendingValidationAsync()
        => GetAndWrapAsync<List<FormulaListDto>>("/api/v1/formulas/pending-validation");

    Task<ApiResponse<FormulaHerbItemDto>> IApiClientFormulas.ValidateHerbAsync(
        Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto request)
        => PostAndWrapAsync<FormulaHerbItemDto>($"/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate", request);

    Task<List<string>> IApiClientFormulas.GetCategoriesAsync()
        => GetRawAsync<List<string>>("/api/v1/formulas/categories");

    // ========================================================================
    // IApiClientMedicalCases — Medical case endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> IApiClientMedicalCases.GetMedicalCasesAsync(
        int page, int pageSize, string? keyword, bool includeAllDoctors)
    {
        var url = $"/api/v1/medicalcases?page={page}&pageSize={pageSize}&includeAllDoctors={includeAllDoctors.ToString().ToLower()}";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseListDto>>(url);
    }

    async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> IApiClientMedicalCases.QueryMedicalCasesAsync(
        MedicalCaseQueryType queryType, Guid? patientId, Guid? doctorId, string? keyword,
        int pageIndex, int pageSize, bool includeAllDoctors, int? limit)
    {
        var url = $"/api/v1/medicalcases/query?queryType={queryType}&pageIndex={pageIndex}&pageSize={pageSize}&includeAllDoctors={includeAllDoctors.ToString().ToLower()}";
        if (patientId.HasValue) url += $"&patientId={patientId.Value}";
        if (doctorId.HasValue) url += $"&doctorId={doctorId.Value}";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (limit.HasValue) url += $"&limit={limit.Value}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseListDto>>(url);
    }

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.GetMedicalCaseByIdAsync(Guid id)
        => GetAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}");

    async Task<ApiResponse<List<PendingMedicalCaseDto>>> IApiClientMedicalCases.GetPendingCasesAsync(Guid? patientId)
    {
        var url = "/api/v1/medicalcases/pending";
        if (patientId.HasValue) url += $"?patientId={patientId.Value}";
        return await GetAndWrapAsync<List<PendingMedicalCaseDto>>(url);
    }

    async Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> IApiClientMedicalCases.SearchMedicalCasesAsync(
        string? patientName, string? diagnosisKeyword, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        var url = $"/api/v1/medicalcases/search?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(patientName)) url += $"&patientName={Uri.EscapeDataString(patientName)}";
        if (!string.IsNullOrWhiteSpace(diagnosisKeyword)) url += $"&diagnosisKeyword={Uri.EscapeDataString(diagnosisKeyword)}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:O}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:O}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseDetailDto>>(url);
    }

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.CreateMedicalCaseAsync(MedicalCaseInputDto request)
        => PostAndWrapAsync<MedicalCaseDetailDto>("/api/v1/medicalcases", request);

    Task<ApiResponse> IApiClientMedicalCases.DeleteMedicalCaseAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/medicalcases/{id}");

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{medicalCaseId}/prescription-flag", request);

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.CloseCaseAsync(Guid id)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}/close");

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.SuspendAsync(Guid id, ConsultationInputDto? request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}/suspend", request);

    async Task<ApiResponse> IApiClientMedicalCases.CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    {
        await PutVoidAsync($"/api/v1/medicalcases/{id}/cancel", request);
        return WrapSuccess();
    }

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}/status", request);

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.SaveAsync(Guid id, MedicalCaseInputDto request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientMedicalCases.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/medicalcases/batch-delete", request);

    Task<ApiResponse<MedicalCasePermissionsDto>> IApiClientMedicalCases.GetPermissionsAsync(Guid id)
        => GetAndWrapAsync<MedicalCasePermissionsDto>($"/api/v1/medicalcases/{id}/permissions");

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.RecordPrintAsync(Guid id, RecordPrintRequest request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}/print-completed", request);

    Task<ApiResponse<PagedResult<AuditLogDto>>> IApiClientMedicalCases.GetAuditLogsAsync(Guid id, int page, int pageSize)
        => GetAndWrapAsync<PagedResult<AuditLogDto>>($"/api/v1/medicalcases/{id}/audit-logs?page={page}&pageSize={pageSize}");

    // ========================================================================
    // IApiClientRegistrations — Registration endpoints (explicit implementation)
    // ========================================================================

    Task<ApiResponse<RegistrationDetailDto>> IApiClientRegistrations.CreateAsync(RegistrationInputDto request)
        => PostAndWrapAsync<RegistrationDetailDto>("/api/v1/registrations", request);

    Task<ApiResponse<RegistrationDetailDto>> IApiClientRegistrations.GetByIdAsync(Guid id)
        => GetAndWrapAsync<RegistrationDetailDto>($"/api/v1/registrations/{id}");

    async Task<ApiResponse<PagedResult<RegistrationListDto>>> IApiClientRegistrations.GetListAsync(
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

    async Task<ApiResponse<List<RegistrationListDto>>> IApiClientRegistrations.GetQueueAsync(Guid? doctorId)
    {
        var url = "/api/v1/registrations/queue";
        if (doctorId.HasValue) url += $"?doctorId={doctorId.Value}";
        return await GetAndWrapAsync<List<RegistrationListDto>>(url);
    }

    async Task<ApiResponse<Guid>> IApiClientRegistrations.StartVisitAsync(Guid id)
        => await SendAndWrapAsync<Guid>($"/api/v1/registrations/{id}/start-visit", HttpMethod.Put);

    async Task<ApiResponse> IApiClientRegistrations.CancelAsync(Guid id)
    {
        await PutVoidAsync($"/api/v1/registrations/{id}/cancel");
        return WrapSuccess();
    }

    async Task<List<RegistrationListDto>> IApiClientRegistrations.GetRegistrationsAsync(DateTime? date)
    {
        var url = "/api/v1/registrations";
        if (date.HasValue) url += $"?date={date.Value:O}";
        return await GetRawAsync<List<RegistrationListDto>>(url);
    }

    Task<QuickVisitResultDto> IApiClientRegistrations.QuickVisitAsync(QuickVisitInputDto request)
        => PostRawAsync<QuickVisitResultDto>("/api/v1/registrations/quick-visit", request);

    async Task IApiClientRegistrations.DeleteRegistrationAsync(Guid id)
    {
        await SendAsync($"/api/v1/registrations/{id}", HttpMethod.Delete);
    }

    // ========================================================================
    // IApiClientReports — Report endpoints (explicit implementation)
    // ========================================================================

    Task<ApiResponse<DailyIncomeDto>> IApiClientReports.GetDailyIncomeAsync(DateTime? startDate, DateTime? endDate)
        => GetAndWrapAsync<DailyIncomeDto>($"/api/v1/reports/daily/income?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

    Task<ApiResponse<DailyConsultationDto>> IApiClientReports.GetDailyConsultationsAsync(DateTime? startDate, DateTime? endDate)
        => GetAndWrapAsync<DailyConsultationDto>($"/api/v1/reports/daily/consultations?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

    Task<ApiResponse<DailyHerbUsageDto>> IApiClientReports.GetDailyHerbUsageAsync(DateTime? startDate, DateTime? endDate)
        => GetAndWrapAsync<DailyHerbUsageDto>($"/api/v1/reports/daily/herbs?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

    // ========================================================================
    // IApiClientDeploy — Deploy endpoints (Local mode not supported)
    // ========================================================================

    Task<ApiResponse<object>> IApiClientDeploy.UploadAsync(MultipartFormDataContent content)
        => Task.FromResult(ApiResponse<object>.CreateFail("本地模式不支持部署更新"));

    Task<ApiResponse<object>> IApiClientDeploy.RestartAsync()
        => Task.FromResult(ApiResponse<object>.CreateFail("本地模式不支持部署更新"));

    // ========================================================================
    // IApiClientDiagnostics — Diagnostics endpoints (explicit implementation)
    // ========================================================================

    Task<ApiResponse<object>> IApiClientDiagnostics.GetLoggingStatusAsync()
        => GetAndWrapAsync<object>("/api/v1/diagnostics/logging/status");

    Task<ApiResponse<object>> IApiClientDiagnostics.EnableDebugModeAsync(EnableDebugModeRequest request)
        => PostAndWrapAsync<object>("/api/v1/diagnostics/logging/debug/enable", request);

    Task<ApiResponse<object>> IApiClientDiagnostics.DisableDebugModeAsync()
        => PostAndWrapAsync<object>("/api/v1/diagnostics/logging/debug/disable", new object());

    Task<ApiResponse<object>> IApiClientDiagnostics.SetLoggingLevelAsync(SetLoggingLevelRequest request)
        => PostAndWrapAsync<object>("/api/v1/diagnostics/logging/level", request);
}
