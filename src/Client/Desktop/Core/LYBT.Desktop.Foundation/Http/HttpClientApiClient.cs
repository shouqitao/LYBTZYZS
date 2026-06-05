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
using LYBT.Shared.Models.Contracts.Users;
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
    IApiClientRegistrations
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

    /// <summary>GET -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    private async Task<ApiResponse<T>> GetAndWrapAsync<T>(string url, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response);
        var data = await DeserializeAsync<T>(response, ct);
        return WrapSuccess(data!);
    }

    /// <summary>GET -> deserialize -> return raw T (local-only methods).</summary>
    private async Task<T> GetRawAsync<T>(string url, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response);
        return (await DeserializeAsync<T>(response, ct))!;
    }

    /// <summary>POST with JSON body -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    private async Task<ApiResponse<T>> PostAndWrapAsync<T>(string url, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        HttpResponseMessage response;
        if (body != null)
        {
            using var content = ToJsonContent(body);
            response = await client.PostAsync(url, content, ct);
        }
        else
        {
            response = await client.PostAsync(url, null, ct);
        }
        await EnsureSuccessOrThrowAsync(response);
        var data = await DeserializeAsync<T>(response, ct);
        return WrapSuccess(data!);
    }

    /// <summary>POST -> non-generic ApiResponse (void operations).</summary>
    private async Task<ApiResponse> PostVoidAsync(string url, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        HttpResponseMessage response;
        if (body != null)
        {
            using var content = ToJsonContent(body);
            response = await client.PostAsync(url, content, ct);
        }
        else
        {
            response = await client.PostAsync(url, null, ct);
        }
        await EnsureSuccessOrThrowAsync(response);
        return WrapSuccess();
    }

    /// <summary>POST -> return raw T (local-only methods).</summary>
    private async Task<T> PostRawAsync<T>(string url, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        HttpResponseMessage response;
        if (body != null)
        {
            using var content = ToJsonContent(body);
            response = await client.PostAsync(url, content, ct);
        }
        else
        {
            response = await client.PostAsync(url, null, ct);
        }
        await EnsureSuccessOrThrowAsync(response);
        return (await DeserializeAsync<T>(response, ct))!;
    }

    /// <summary>PUT with JSON body -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    private async Task<ApiResponse<T>> PutAndWrapAsync<T>(string url, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        HttpResponseMessage response;
        if (body != null)
        {
            using var content = ToJsonContent(body);
            response = await client.PutAsync(url, content, ct);
        }
        else
        {
            response = await client.PutAsync(url, null, ct);
        }
        await EnsureSuccessOrThrowAsync(response);
        var data = await DeserializeAsync<T>(response, ct);
        return WrapSuccess(data!);
    }

    /// <summary>PUT -> non-generic ApiResponse (void operations).</summary>
    private async Task<ApiResponse> PutVoidAsync(string url, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        HttpResponseMessage response;
        if (body != null)
        {
            using var content = ToJsonContent(body);
            response = await client.PutAsync(url, content, ct);
        }
        else
        {
            response = await client.PutAsync(url, null, ct);
        }
        await EnsureSuccessOrThrowAsync(response);
        return WrapSuccess();
    }

    /// <summary>DELETE -> non-generic ApiResponse.</summary>
    private async Task<ApiResponse> DeleteVoidAsync(string url, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.DeleteAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response);
        return WrapSuccess();
    }

    /// <summary>GET -> return HttpResponseMessage (file downloads). Caller disposes client.</summary>
    private async Task<HttpResponseMessage> GetResponseAsync(string url, CancellationToken ct = default)
    {
        var client = CreateClient();
        var response = await client.GetAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response);
        return response;
    }

    // ========================================================================
    // Auth helpers
    // ========================================================================

    /// <summary>
    /// Maps LocalWebAPI auth response (flat { Token, UserId, Username, Role }) to LoginResponse.
    /// LocalWebAPI returns a different shape than the remote API.
    /// </summary>
    private static LoginResponse MapToLoginResponse(JsonElement raw)
    {
        var response = new LoginResponse
        {
            Token = raw.TryGetProperty("Token", out var token) ? token.GetString() ?? string.Empty : string.Empty
        };

        // Local API returns flat structure; map to nested User
        response.User = new UserDetailDto
        {
            Id = raw.TryGetProperty("UserId", out var userId) && userId.ValueKind == JsonValueKind.String
                ? Guid.Parse(userId.GetString()!)
                : Guid.Empty,
            UserName = raw.TryGetProperty("Username", out var username) ? username.GetString() ?? string.Empty : string.Empty,
            Role = raw.TryGetProperty("Role", out var role) && role.ValueKind == JsonValueKind.String
                ? Enum.TryParse<UserRole>(role.GetString(), out var userRole) ? userRole : UserRole.Doctor
                : UserRole.Doctor
        };

        return response;
    }

    // ========================================================================
    // IApiClientAuth — Authentication endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<LoginResponse>> IApiClientAuth.LoginAsync(LoginRequest loginRequest)
    {
        var raw = await PostRawAsync<JsonElement>("/api/auth/login", loginRequest);
        return WrapSuccess(MapToLoginResponse(raw));
    }

    async Task<ApiResponse<LoginResponse>> IApiClientAuth.LoginWithAutoTokenAsync(AutoLoginRequest request)
    {
        var raw = await PostRawAsync<JsonElement>("/api/auth/auto-login", request);
        return WrapSuccess(MapToLoginResponse(raw));
    }

    async Task<ApiResponse> IApiClientAuth.LogoutAsync(LogoutRequest logoutRequest)
    {
        await PostVoidAsync("/api/auth/logout", logoutRequest);
        return WrapSuccess();
    }

    async Task<ApiResponse<LoginResponse>> IApiClientAuth.RefreshTokenAsync(RefreshTokenRequest request)
    {
        var raw = await PostRawAsync<JsonElement>("/api/auth/refresh", request);
        return WrapSuccess(MapToLoginResponse(raw));
    }

    async Task<ApiResponse<object>> IApiClientAuth.ValidateTokenFromHeaderAsync()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/api/auth/validate");
        await EnsureSuccessOrThrowAsync(response);
        var data = await DeserializeAsync<object>(response);
        return WrapSuccess(data!);
    }

    async Task<ApiResponse<ValidateTokenResponse>> IApiClientAuth.ValidateTokenAsync(ValidateTokenRequest request)
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/api/auth/validate");
        await EnsureSuccessOrThrowAsync(response);
        var raw = await DeserializeAsync<JsonElement>(response);

        var result = new ValidateTokenResponse
        {
            IsValid = raw.TryGetProperty("IsValid", out var isValid) && isValid.GetBoolean(),
            Username = raw.TryGetProperty("Username", out var username) ? username.GetString() : null,
            Role = raw.TryGetProperty("Role", out var role) ? role.GetString() : null
        };

        if (raw.TryGetProperty("UserId", out var userIdEl) && userIdEl.ValueKind == JsonValueKind.Number)
            result.UserId = userIdEl.GetInt32();

        return WrapSuccess(result);
    }

    async Task<ApiResponse<HealthCheckResponse>> IApiClientAuth.HealthCheckAsync()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/api/health");
        await EnsureSuccessOrThrowAsync(response);
        var raw = await DeserializeAsync<JsonElement>(response);

        var result = new HealthCheckResponse
        {
            Status = raw.TryGetProperty("status", out var status) ? status.GetString() ?? "Unknown" : "Unknown",
            Timestamp = raw.TryGetProperty("timestamp", out var ts) ? ts.GetDateTime() : DateTime.UtcNow
        };

        return WrapSuccess(result);
    }

    // ========================================================================
    // IApiClientUsers — User management endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<UserListDto>>> IApiClientUsers.GetUsersAsync(
        int page, int pageSize, string? keyword)
    {
        var url = $"/api/users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";

        using var client = CreateClient();
        var response = await client.GetAsync(url);
        await EnsureSuccessOrThrowAsync(response);
        var items = await DeserializeAsync<List<UserListDto>>(response) ?? [];
        var paged = new PagedResult<UserListDto>(items, items.Count, page, pageSize);
        return WrapSuccess(paged);
    }

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.GetUserByIdAsync(Guid id)
        => GetAndWrapAsync<UserDetailDto>($"/api/users/{id}");

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.CreateUserAsync(UserInputDto request)
        => PostAndWrapAsync<UserDetailDto>("/api/users", request);

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.UpdateUserAsync(Guid id, UserInputDto request)
        => PutAndWrapAsync<UserDetailDto>($"/api/users/{id}", request);

    Task<ApiResponse> IApiClientUsers.DeleteUserAsync(Guid id)
        => DeleteVoidAsync($"/api/users/{id}");

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.ChangeProfileAsync(Guid id, ChangeProfileDto request)
        => PutAndWrapAsync<UserDetailDto>($"/api/users/{id}/profile", request);

    Task<ApiResponse> IApiClientUsers.ChangePasswordAsync(Guid id, ChangePasswordRequest request)
        => PutVoidAsync($"/api/users/{id}/change-password", request);

    Task<ApiResponse<ResetPasswordResponseDto>> IApiClientUsers.ResetPasswordAsync(Guid id, ResetPasswordRequestDto request)
        => PostAndWrapAsync<ResetPasswordResponseDto>($"/api/users/{id}/reset-password", request);

    Task<ApiResponse<UserBatchImportResultDto>> IApiClientUsers.BatchImportAsync(UserBatchImportInputDto request)
        => PostAndWrapAsync<UserBatchImportResultDto>("/api/users/import", request);

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<UserDetailDto>($"/api/users/{id}/toggle-status");

    Task<ApiResponse<UserDetailDto>> IApiClientUsers.RestoreAsync(Guid id)
        => PostAndWrapAsync<UserDetailDto>($"/api/users/{id}/restore");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientUsers.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/users/batch-delete", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientUsers.BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/users/batch-enable", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientUsers.BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/users/batch-disable", request);

    Task<UserDetailDto> IApiClientUsers.GetCurrentUserAsync()
        => GetRawAsync<UserDetailDto>("/api/users/current");

    // ========================================================================
    // IApiClientPatients — Patient management endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<PatientListDto>>> IApiClientPatients.GetPatientsAsync(
        int page, int pageSize, string? keyword)
    {
        var url = $"/api/patients?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";

        using var client = CreateClient();
        var response = await client.GetAsync(url);
        await EnsureSuccessOrThrowAsync(response);
        var items = await DeserializeAsync<List<PatientListDto>>(response) ?? [];
        var paged = new PagedResult<PatientListDto>(items, items.Count, page, pageSize);
        return WrapSuccess(paged);
    }

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.GetPatientByIdAsync(Guid id)
        => GetAndWrapAsync<PatientDetailDto>($"/api/patients/{id}");

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.CreatePatientAsync(PatientInputDto request)
        => PostAndWrapAsync<PatientDetailDto>("/api/patients", request);

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.UpdatePatientAsync(Guid id, PatientInputDto request)
        => PutAndWrapAsync<PatientDetailDto>($"/api/patients/{id}", request);

    Task<ApiResponse> IApiClientPatients.DeletePatientAsync(Guid id)
        => DeleteVoidAsync($"/api/patients/{id}");

    Task<ApiResponse<PatientBatchImportResultDto>> IApiClientPatients.BatchImportAsync(PatientBatchImportInputDto request)
        => PostAndWrapAsync<PatientBatchImportResultDto>("/api/patients/import", request);

    Task<HttpResponseMessage> IApiClientPatients.ExportTemplateAsync()
        => GetResponseAsync("/api/patients/import-template");

    async Task<HttpResponseMessage> IApiClientPatients.ExportPatientsAsync(string? keyword)
    {
        var url = "/api/patients/export";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"?keyword={Uri.EscapeDataString(keyword)}";
        return await GetResponseAsync(url);
    }

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.RestoreAsync(Guid id)
        => PostAndWrapAsync<PatientDetailDto>($"/api/patients/{id}/restore");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientPatients.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/patients/batch-delete", request);

    Task<ApiResponse<PatientDetailDto>> IApiClientPatients.ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<PatientDetailDto>($"/api/patients/{id}/toggle-status");

    // ========================================================================
    // IApiClientHerbs — Herb management endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<HerbListDto>>> IApiClientHerbs.GetHerbsAsync(
        int page, int pageSize, string? keyword, string? category)
    {
        var url = $"/api/herbs?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"&category={Uri.EscapeDataString(category)}";

        using var client = CreateClient();
        var response = await client.GetAsync(url);
        await EnsureSuccessOrThrowAsync(response);
        var items = await DeserializeAsync<List<HerbListDto>>(response) ?? [];
        var paged = new PagedResult<HerbListDto>(items, items.Count, page, pageSize);
        return WrapSuccess(paged);
    }

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.GetHerbByIdAsync(Guid id)
        => GetAndWrapAsync<HerbDetailDto>($"/api/herbs/{id}");

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.CreateHerbAsync(HerbInputDto request)
        => PostAndWrapAsync<HerbDetailDto>("/api/herbs", request);

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.UpdateHerbAsync(Guid id, HerbInputDto request)
        => PutAndWrapAsync<HerbDetailDto>($"/api/herbs/{id}", request);

    Task<ApiResponse> IApiClientHerbs.DeleteHerbAsync(Guid id)
        => DeleteVoidAsync($"/api/herbs/{id}");

    Task<ApiResponse<HerbBatchImportResultDto>> IApiClientHerbs.BatchImportAsync(HerbBatchImportInputDto request)
        => PostAndWrapAsync<HerbBatchImportResultDto>("/api/herbs/batch-import", request);

    Task<HttpResponseMessage> IApiClientHerbs.ExportTemplateAsync()
        => GetResponseAsync("/api/herbs/import-template");

    async Task<HttpResponseMessage> IApiClientHerbs.ExportHerbsAsync(string? keyword)
    {
        var url = "/api/herbs/export";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"?keyword={Uri.EscapeDataString(keyword)}";
        return await GetResponseAsync(url);
    }

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<HerbDetailDto>($"/api/herbs/{id}/toggle-status");

    Task<ApiResponse<HerbDetailDto>> IApiClientHerbs.RestoreAsync(Guid id)
        => PostAndWrapAsync<HerbDetailDto>($"/api/herbs/{id}/restore");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientHerbs.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/herbs/batch-delete", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientHerbs.BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/herbs/batch-enable", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientHerbs.BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/herbs/batch-disable", request);

    Task<List<string>> IApiClientHerbs.GetCategoriesAsync()
        => GetRawAsync<List<string>>("/api/herbs/categories");

    // ========================================================================
    // IApiClientFormulas — Formula management endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<FormulaListDto>>> IApiClientFormulas.GetFormulasAsync(
        int page, int pageSize, string? keyword, string? category)
    {
        var url = $"/api/formulas?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"&category={Uri.EscapeDataString(category)}";

        using var client = CreateClient();
        var response = await client.GetAsync(url);
        await EnsureSuccessOrThrowAsync(response);
        var items = await DeserializeAsync<List<FormulaListDto>>(response) ?? [];
        var paged = new PagedResult<FormulaListDto>(items, items.Count, page, pageSize);
        return WrapSuccess(paged);
    }

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.GetFormulaByIdAsync(Guid id)
        => GetAndWrapAsync<FormulaDetailDto>($"/api/formulas/{id}");

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.CreateFormulaAsync(FormulaInputDto request)
        => PostAndWrapAsync<FormulaDetailDto>("/api/formulas", request);

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.UpdateFormulaAsync(Guid id, FormulaInputDto request)
        => PutAndWrapAsync<FormulaDetailDto>($"/api/formulas/{id}", request);

    Task<ApiResponse> IApiClientFormulas.DeleteFormulaAsync(Guid id)
        => DeleteVoidAsync($"/api/formulas/{id}");

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.CloneFormulaAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/formulas/{id}/clone");

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/formulas/{id}/toggle-status");

    Task<ApiResponse<FormulaDetailDto>> IApiClientFormulas.RestoreAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/formulas/{id}/restore");

    Task<ApiResponse<BatchOperationResultDto>> IApiClientFormulas.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/formulas/batch-delete", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientFormulas.BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/formulas/batch-enable", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientFormulas.BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/formulas/batch-disable", request);

    Task<ApiResponse<FormulaBatchImportResultDto>> IApiClientFormulas.BatchImportAsync(FormulaBatchImportInputDto request)
        => PostAndWrapAsync<FormulaBatchImportResultDto>("/api/formulas/batch-import", request);

    async Task<HttpResponseMessage> IApiClientFormulas.ExportFormulasAsync(string? category)
    {
        var url = "/api/formulas/export";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"?category={Uri.EscapeDataString(category)}";
        return await GetResponseAsync(url);
    }

    Task<HttpResponseMessage> IApiClientFormulas.ExportTemplateAsync()
        => GetResponseAsync("/api/formulas/import-template");

    Task<List<string>> IApiClientFormulas.GetCategoriesAsync()
        => GetRawAsync<List<string>>("/api/formulas/categories");

    // ========================================================================
    // IApiClientMedicalCases — Medical case endpoints (explicit implementation)
    // ========================================================================

    async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> IApiClientMedicalCases.GetMedicalCasesAsync(
        int page, int pageSize, string? keyword, bool includeAllDoctors)
    {
        var url = $"/api/medicalcases?page={page}&pageSize={pageSize}&includeAllDoctors={includeAllDoctors.ToString().ToLower()}";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseListDto>>(url);
    }

    async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> IApiClientMedicalCases.QueryMedicalCasesAsync(
        MedicalCaseQueryType queryType, Guid? patientId, Guid? doctorId, string? keyword,
        int pageIndex, int pageSize, bool includeAllDoctors, int? limit)
    {
        var url = $"/api/medicalcases/query?queryType={queryType}&pageIndex={pageIndex}&pageSize={pageSize}&includeAllDoctors={includeAllDoctors.ToString().ToLower()}";
        if (patientId.HasValue) url += $"&patientId={patientId.Value}";
        if (doctorId.HasValue) url += $"&doctorId={doctorId.Value}";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (limit.HasValue) url += $"&limit={limit.Value}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseListDto>>(url);
    }

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.GetMedicalCaseByIdAsync(Guid id)
        => GetAndWrapAsync<MedicalCaseDetailDto>($"/api/medicalcases/{id}");

    async Task<ApiResponse<List<PendingMedicalCaseDto>>> IApiClientMedicalCases.GetPendingCasesAsync(Guid? patientId)
    {
        var url = "/api/medicalcases/pending";
        if (patientId.HasValue) url += $"?patientId={patientId.Value}";
        return await GetAndWrapAsync<List<PendingMedicalCaseDto>>(url);
    }

    async Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> IApiClientMedicalCases.SearchMedicalCasesAsync(
        string? patientName, string? diagnosisKeyword, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        var url = $"/api/medicalcases/search?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(patientName)) url += $"&patientName={Uri.EscapeDataString(patientName)}";
        if (!string.IsNullOrWhiteSpace(diagnosisKeyword)) url += $"&diagnosisKeyword={Uri.EscapeDataString(diagnosisKeyword)}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:O}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:O}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseDetailDto>>(url);
    }

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.CreateMedicalCaseAsync(MedicalCaseInputDto request)
        => PostAndWrapAsync<MedicalCaseDetailDto>("/api/medicalcases", request);

    Task<ApiResponse> IApiClientMedicalCases.DeleteMedicalCaseAsync(Guid id)
        => DeleteVoidAsync($"/api/medicalcases/{id}");

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/medicalcases/{medicalCaseId}/prescription-flag", request);

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.CloseCaseAsync(Guid id)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/medicalcases/{id}/close");

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.SuspendAsync(Guid id, ConsultationInputDto? request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/medicalcases/{id}/suspend", request);

    async Task<ApiResponse> IApiClientMedicalCases.CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    {
        await PutVoidAsync($"/api/medicalcases/{id}/cancel", request);
        return WrapSuccess();
    }

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/medicalcases/{id}/status", request);

    Task<ApiResponse<MedicalCasePermissionDto>> IApiClientMedicalCases.GetPermissionsAsync(Guid id)
        => GetAndWrapAsync<MedicalCasePermissionDto>($"/api/medicalcases/{id}/permissions");

    Task<ApiResponse<MedicalCaseAuditLogPagedResultDto>> IApiClientMedicalCases.GetAuditLogsAsync(Guid id, int page, int pageSize)
        => GetAndWrapAsync<MedicalCaseAuditLogPagedResultDto>($"/api/medicalcases/{id}/audit-logs?page={page}&pageSize={pageSize}");

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.SaveAsync(Guid id, MedicalCaseInputDto request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/medicalcases/{id}", request);

    Task<ApiResponse<MedicalCaseDetailDto>> IApiClientMedicalCases.RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/medicalcases/{medicalCaseId}/print-completed", request);

    Task<ApiResponse<object>> IApiClientMedicalCases.AddPrintLogAsync(Guid medicalCaseId, PrintLogInputDto request)
        => PostAndWrapAsync<object>($"/api/medicalcases/{medicalCaseId}/print-logs", request);

    Task<ApiResponse<BatchOperationResultDto>> IApiClientMedicalCases.BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/medicalcases/batch-delete", request);

    Task<ApiResponse<List<MedicalCaseDetailDto>>> IApiClientMedicalCases.GetBatchDetailsAsync(BatchDetailQueryDto request)
        => PostAndWrapAsync<List<MedicalCaseDetailDto>>("/api/medicalcases/batch-details", request);

    // ========================================================================
    // IApiClientRegistrations — Registration endpoints (explicit implementation)
    // ========================================================================

    Task<ApiResponse<RegistrationDetailDto>> IApiClientRegistrations.CreateAsync(RegistrationInputDto request)
        => PostAndWrapAsync<RegistrationDetailDto>("/api/registrations", request);

    Task<ApiResponse<RegistrationDetailDto>> IApiClientRegistrations.GetByIdAsync(Guid id)
        => GetAndWrapAsync<RegistrationDetailDto>($"/api/registrations/{id}");

    async Task<ApiResponse<PagedResult<RegistrationListDto>>> IApiClientRegistrations.GetListAsync(
        int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId)
    {
        var url = $"/api/registrations?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:O}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:O}";
        if (patientId.HasValue) url += $"&patientId={patientId.Value}";
        if (doctorId.HasValue) url += $"&doctorId={doctorId.Value}";

        using var client = CreateClient();
        var response = await client.GetAsync(url);
        await EnsureSuccessOrThrowAsync(response);
        var items = await DeserializeAsync<List<RegistrationListDto>>(response) ?? [];
        var paged = new PagedResult<RegistrationListDto>(items, items.Count, page, pageSize);
        return WrapSuccess(paged);
    }

    async Task<ApiResponse<List<RegistrationListDto>>> IApiClientRegistrations.GetQueueAsync(Guid? doctorId)
    {
        var url = "/api/registrations/queue";
        if (doctorId.HasValue) url += $"?doctorId={doctorId.Value}";
        return await GetAndWrapAsync<List<RegistrationListDto>>(url);
    }

    async Task<ApiResponse<Guid>> IApiClientRegistrations.StartVisitAsync(Guid id)
    {
        using var client = CreateClient();
        var response = await client.PutAsync($"/api/registrations/{id}/start-visit", null);
        await EnsureSuccessOrThrowAsync(response);
        var result = await DeserializeAsync<Guid>(response);
        return WrapSuccess(result);
    }

    async Task<ApiResponse> IApiClientRegistrations.CancelAsync(Guid id)
    {
        await PutVoidAsync($"/api/registrations/{id}/cancel");
        return WrapSuccess();
    }

    async Task<List<RegistrationListDto>> IApiClientRegistrations.GetRegistrationsAsync(DateTime? date)
    {
        var url = "/api/registrations";
        if (date.HasValue) url += $"?date={date.Value:O}";
        return await GetRawAsync<List<RegistrationListDto>>(url);
    }

    Task<QuickVisitResultDto> IApiClientRegistrations.QuickVisitAsync(QuickVisitInputDto request)
        => PostRawAsync<QuickVisitResultDto>("/api/registrations/quick-visit", request);

    async Task IApiClientRegistrations.DeleteRegistrationAsync(Guid id)
    {
        using var client = CreateClient();
        var response = await client.DeleteAsync($"/api/registrations/{id}");
        await EnsureSuccessOrThrowAsync(response);
    }
}
