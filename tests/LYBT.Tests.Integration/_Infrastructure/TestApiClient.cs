using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
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

namespace LYBT.Tests.Integration._Infrastructure;

/// <summary>
/// Test IApiClient implementation that wraps individual Refit API clients.
/// Used by integration tests to construct repositories with the new IApiClient-based constructors.
/// </summary>
public sealed class TestApiClient : IApiClient
{
    public IApiClientAuth Auth { get; }
    public IApiClientUsers Users { get; }
    public IApiClientPatients Patients { get; }
    public IApiClientHerbs Herbs { get; }
    public IApiClientFormulas Formulas { get; }
    public IApiClientMedicalCases MedicalCases { get; }
    public IApiClientRegistrations Registrations { get; }

    public TestApiClient(
        IApiClientAuth auth,
        IApiClientUsers users,
        IApiClientPatients patients,
        IApiClientHerbs herbs,
        IApiClientFormulas formulas,
        IApiClientMedicalCases medicalCases,
        IApiClientRegistrations registrations)
    {
        Auth = auth;
        Users = users;
        Patients = patients;
        Herbs = herbs;
        Formulas = formulas;
        MedicalCases = medicalCases;
        Registrations = registrations;
    }

    /// <summary>
    /// Creates a TestApiClient from an authenticated HttpClient using Refit.
    /// </summary>
    public static TestApiClient Create(HttpClient httpClient)
    {
        var settings = new Refit.RefitSettings
        {
            ContentSerializer = new Refit.SystemTextJsonContentSerializer(
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                })
        };

        return new TestApiClient(
            new TestAuthApiClient(Refit.RestService.For<IAuthApi>(httpClient, settings)),
            new TestUserApiClient(Refit.RestService.For<IUserApi>(httpClient, settings)),
            new TestPatientApiClient(Refit.RestService.For<IPatientApi>(httpClient, settings)),
            new TestHerbApiClient(Refit.RestService.For<IHerbApi>(httpClient, settings)),
            new TestFormulaApiClient(Refit.RestService.For<IFormulaApi>(httpClient, settings)),
            new TestMedicalCaseApiClient(Refit.RestService.For<IMedicalCaseApi>(httpClient, settings)),
            new TestRegistrationApiClient(Refit.RestService.For<IRegistrationApi>(httpClient, settings)));
    }
}

// Minimal adapter implementations for each sub-interface

internal sealed class TestAuthApiClient : IApiClientAuth
{
    private readonly IAuthApi _api;
    public TestAuthApiClient(IAuthApi api) => _api = api;
    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request) => _api.LoginAsync(request);
    public Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request) => _api.LoginWithAutoTokenAsync(request);
    public Task<ApiResponse> LogoutAsync(LogoutRequest request) => _api.LogoutAsync(request);
    public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request) => _api.RefreshTokenAsync(request);
    public Task<ApiResponse<object>> ValidateTokenFromHeaderAsync() => _api.ValidateTokenFromHeaderAsync();
    public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync(ValidateTokenRequest request) => _api.ValidateTokenAsync(request);
    public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync() => _api.HealthCheckAsync();
}

internal sealed class TestUserApiClient : IApiClientUsers
{
    private readonly IUserApi _api;
    public TestUserApiClient(IUserApi api) => _api = api;
    public Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(int page = 1, int pageSize = 20, string? keyword = null) => _api.GetUsersAsync(page, pageSize, keyword);
    public Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id) => _api.GetUserByIdAsync(id);
    public Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request) => _api.CreateUserAsync(request);
    public Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request) => _api.UpdateUserAsync(id, request);
    public Task<ApiResponse> DeleteUserAsync(Guid id) => _api.DeleteUserAsync(id);
    public Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request) => _api.ChangeProfileAsync(id, request);
    public Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request) => _api.ChangePasswordAsync(id, request);
    public Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request) => _api.ResetPasswordAsync(id, request);
    public Task<ApiResponse<UserBatchImportResultDto>> BatchImportAsync(UserBatchImportInputDto request) => _api.BatchImportAsync(request);
    public Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id) => _api.ToggleStatusAsync(id);
    public Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id) => _api.RestoreAsync(id);
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request) => _api.BatchDeleteAsync(request);
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request) => _api.BatchEnableAsync(request);
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request) => _api.BatchDisableAsync(request);
    public Task<UserDetailDto> GetCurrentUserAsync() => throw new NotSupportedException("Remote mode only");
}

internal sealed class TestPatientApiClient : IApiClientPatients
{
    private readonly IPatientApi _api;
    public TestPatientApiClient(IPatientApi api) => _api = api;
    public Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync(int page = 1, int pageSize = 20, string? keyword = null) => _api.GetPatientsAsync(page, pageSize, keyword);
    public Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id) => _api.GetPatientByIdAsync(id);
    public Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request) => _api.CreatePatientAsync(request);
    public Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request) => _api.UpdatePatientAsync(id, request);
    public Task<ApiResponse> DeletePatientAsync(Guid id) => _api.DeletePatientAsync(id);
    public Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request) => _api.BatchImportAsync(request);
    public Task<HttpResponseMessage> ExportTemplateAsync() => _api.ExportTemplateAsync();
    public Task<HttpResponseMessage> ExportPatientsAsync(string? keyword = null) => _api.ExportPatientsAsync(keyword);
    public Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id) => _api.RestoreAsync(id);
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request) => _api.BatchDeleteAsync(request);
    public Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id) => _api.ToggleStatusAsync(id);
}

internal sealed class TestHerbApiClient : IApiClientHerbs
{
    private readonly IHerbApi _api;
    public TestHerbApiClient(IHerbApi api) => _api = api;
    public Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null) => _api.GetHerbsAsync(page, pageSize, keyword, category);
    public Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id) => _api.GetHerbByIdAsync(id);
    public Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request) => _api.CreateHerbAsync(request);
    public Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request) => _api.UpdateHerbAsync(id, request);
    public Task<ApiResponse> DeleteHerbAsync(Guid id) => _api.DeleteHerbAsync(id);
    public Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request) => _api.BatchImportAsync(request);
    public Task<HttpResponseMessage> ExportTemplateAsync() => _api.ExportTemplateAsync();
    public Task<HttpResponseMessage> ExportHerbsAsync(string? keyword = null) => _api.ExportHerbsAsync(keyword);
    public Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id) => _api.ToggleStatusAsync(id);
    public Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id) => _api.RestoreAsync(id);
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request) => _api.BatchDeleteAsync(request);
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request) => _api.BatchEnableAsync(request);
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request) => _api.BatchDisableAsync(request);
    public Task<List<string>> GetCategoriesAsync() => throw new NotSupportedException("Remote mode only");
}

internal sealed class TestFormulaApiClient : IApiClientFormulas
{
    private readonly IFormulaApi _api;
    public TestFormulaApiClient(IFormulaApi api) => _api = api;
    public Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null) => _api.GetFormulasAsync(page, pageSize, keyword, category);
    public Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id) => _api.GetFormulaByIdAsync(id);
    public Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request) => _api.CreateFormulaAsync(request);
    public Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request) => _api.UpdateFormulaAsync(id, request);
    public Task<ApiResponse> DeleteFormulaAsync(Guid id) => _api.DeleteFormulaAsync(id);
    public Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id) => _api.CloneFormulaAsync(id);
    public Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id) => _api.ToggleStatusAsync(id);
    public Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id) => _api.RestoreAsync(id);
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request) => _api.BatchDeleteAsync(request);
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request) => _api.BatchEnableAsync(request);
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request) => _api.BatchDisableAsync(request);
    public Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request) => _api.BatchImportAsync(request);
    public Task<HttpResponseMessage> ExportFormulasAsync(string? category = null) => _api.ExportFormulasAsync(category);
    public Task<HttpResponseMessage> ExportTemplateAsync() => _api.ExportTemplateAsync();
    public Task<List<string>> GetCategoriesAsync() => throw new NotSupportedException("Remote mode only");
}

internal sealed class TestMedicalCaseApiClient : IApiClientMedicalCases
{
    private readonly IMedicalCaseApi _api;
    public TestMedicalCaseApiClient(IMedicalCaseApi api) => _api = api;
    public Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync(int page = 1, int pageSize = 20, string? keyword = null, bool includeAllDoctors = false) => _api.GetMedicalCasesAsync(page, pageSize, keyword);
    public Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync(MedicalCaseQueryType queryType = MedicalCaseQueryType.All, Guid? patientId = null, Guid? doctorId = null, string? keyword = null, int pageIndex = 1, int pageSize = 20, bool includeAllDoctors = false, int? limit = null) => _api.QueryMedicalCasesAsync(queryType, patientId, doctorId, keyword, pageIndex, pageSize, includeAllDoctors, limit);
    public Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id) => _api.GetMedicalCaseByIdAsync(id);
    public Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId = null) => _api.GetPendingCasesAsync(patientId);
    public Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync(string? patientName = null, string? diagnosisKeyword = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20) => _api.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
    public Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request) => _api.CreateMedicalCaseAsync(request);
    public Task<ApiResponse> DeleteMedicalCaseAsync(Guid id) => _api.DeleteMedicalCaseAsync(id);
    public Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request) => _api.SetPrescriptionFlagAsync(medicalCaseId, request);
    public Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id) => _api.CloseCaseAsync(id);
    public Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(Guid id, ConsultationInputDto? request = null) => _api.SuspendAsync(id, request);
    public async Task<ApiResponse> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request = null)
    {
        await _api.CancelMedicalCaseAsync(id, request);
        return new ApiResponse { Success = true };
    }
    public Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request) => _api.UpdateStatusAsync(id, request);
    public Task<ApiResponse<MedicalCasePermissionDto>> GetPermissionsAsync(Guid id) => _api.GetPermissionsAsync(id);
    public Task<ApiResponse<MedicalCaseAuditLogPagedResultDto>> GetAuditLogsAsync(Guid id, int page = 1, int pageSize = 20) => _api.GetAuditLogsAsync(id, page, pageSize);
    public Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(Guid id, MedicalCaseInputDto request) => _api.SaveAsync(id, request);
    public Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request) => _api.RecordPrintCompletedAsync(medicalCaseId, request);
    public Task<ApiResponse<object>> AddPrintLogAsync(Guid medicalCaseId, PrintLogInputDto request) => _api.AddPrintLogAsync(medicalCaseId, request);
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request) => _api.BatchDeleteAsync(request);
    public Task<ApiResponse<List<MedicalCaseDetailDto>>> GetBatchDetailsAsync(BatchDetailQueryDto request) => _api.GetBatchDetailsAsync(request);
}

internal sealed class TestRegistrationApiClient : IApiClientRegistrations
{
    private readonly IRegistrationApi _api;
    public TestRegistrationApiClient(IRegistrationApi api) => _api = api;
    public Task<ApiResponse<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request) => _api.CreateAsync(request);
    public Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id) => _api.GetByIdAsync(id);
    public Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync(int page = 1, int pageSize = 20, string? keyword = null, DateTime? startDate = null, DateTime? endDate = null, Guid? patientId = null, Guid? doctorId = null) => _api.GetListAsync(page, pageSize, keyword);
    public Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(Guid? doctorId = null) => _api.GetQueueAsync(doctorId);
    public Task<ApiResponse<Guid>> StartVisitAsync(Guid id) => _api.StartVisitAsync(id);
    public async Task<ApiResponse> CancelAsync(Guid id)
    {
        await _api.CancelAsync(id);
        return new ApiResponse { Success = true };
    }
    public Task<List<RegistrationListDto>> GetRegistrationsAsync(DateTime? date = null) => throw new NotSupportedException("Remote mode only");
    public Task<QuickVisitResultDto> QuickVisitAsync(QuickVisitInputDto request) => throw new NotSupportedException("Remote mode only");
    public Task DeleteRegistrationAsync(Guid id) => throw new NotSupportedException("Remote mode only");
}
