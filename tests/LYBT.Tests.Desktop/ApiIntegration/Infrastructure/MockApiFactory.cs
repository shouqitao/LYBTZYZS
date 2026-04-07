using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Common;
using NSubstitute;
using System.Net;

namespace LYBT.Tests.Desktop.ApiIntegration.Infrastructure;

/// <summary>
/// Factory for creating and configuring mock API clients
/// 
/// Provides fluent API for setting up various response scenarios:
/// - Success responses with data
/// - Error responses with specific status codes
/// - Network failures and timeouts
/// - Authentication scenarios
/// </summary>
public class MockApiFactory
{
    private readonly IAuthApi _authApi;
    private readonly IUserApi _userApi;
    private readonly IPatientApi _patientApi;
    private readonly IHerbApi _herbApi;
    private readonly IFormulaApi _formulaApi;
    private readonly IMedicalCaseApi _medicalCaseApi;
    private readonly ISyncApi _syncApi;
    private readonly IRegistrationApi _registrationApi;

    public MockApiFactory(
        IAuthApi authApi,
        IUserApi userApi,
        IPatientApi patientApi,
        IHerbApi herbApi,
        IFormulaApi formulaApi,
        IMedicalCaseApi medicalCaseApi,
        ISyncApi syncApi,
        IRegistrationApi registrationApi)
    {
        _authApi = authApi;
        _userApi = userApi;
        _patientApi = patientApi;
        _herbApi = herbApi;
        _formulaApi = formulaApi;
        _medicalCaseApi = medicalCaseApi;
        _syncApi = syncApi;
        _registrationApi = registrationApi;
    }

    #region Authentication API Setup

    /// <summary>
    /// Setup successful login response
    /// </summary>
    public MockApiFactory WithSuccessfulLogin<T>(T loginResponse) where T : class
    {
        _authApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromResult(new ApiResponse<T>
            {
                Success = true,
                Data = loginResponse,
                Message = "登录成功"
            }));
        return this;
    }

    /// <summary>
    /// Setup failed login response
    /// </summary>
    public MockApiFactory WithFailedLogin(string errorMessage = "用户名或密码错误")
    {
        _authApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = errorMessage
            }));
        return this;
    }

    /// <summary>
    /// Setup successful token refresh
    /// </summary>
    public MockApiFactory WithSuccessfulTokenRefresh<T>(T refreshResponse) where T : class
    {
        _authApi.RefreshTokenAsync(Arg.Any<RefreshTokenRequest>())
            .Returns(Task.FromResult(new ApiResponse<T>
            {
                Success = true,
                Data = refreshResponse,
                Message = "Token刷新成功"
            }));
        return this;
    }

    /// <summary>
    /// Setup failed token refresh
    /// </summary>
    public MockApiFactory WithFailedTokenRefresh(string errorMessage = "Token已过期")
    {
        _authApi.RefreshTokenAsync(Arg.Any<RefreshTokenRequest>())
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = errorMessage
            }));
        return this;
    }

    #endregion

    #region Generic API Response Setup

    /// <summary>
    /// Setup successful API response for any endpoint
    /// </summary>
    public MockApiFactory WithSuccessResponse<TApi, TResponse>(
        Func<TApi, Func<Task<TResponse>>> apiMethod,
        TResponse response,
        string message = "操作成功")
        where TApi : class
        where TResponse : class
    {
        var apiClient = GetApiClient<TApi>();
        var method = apiMethod(apiClient);
        
        method.Returns(Task.FromResult(new ApiResponse<TResponse>
        {
            Success = true,
            Data = response,
            Message = message
        }));
        
        return this;
    }

    /// <summary>
    /// Setup error response for any endpoint
    /// </summary>
    public MockApiFactory WithErrorResponse<TApi, TResponse>(
        Func<TApi, Func<Task<TResponse>>> apiMethod,
        string errorMessage = "操作失败",
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        where TApi : class
        where TResponse : class
    {
        var apiClient = GetApiClient<TApi>();
        var method = apiMethod(apiClient);
        
        method.Returns(Task.FromResult(new ApiResponse<TResponse>
        {
            Success = false,
            Message = errorMessage
        }));
        
        return this;
    }

    /// <summary>
    /// Setup network exception for any endpoint
    /// </summary>
    public MockApiFactory WithNetworkError<TApi, TResponse>(
        Func<TApi, Func<Task<TResponse>>> apiMethod)
        where TApi : class
        where TResponse : class
    {
        var apiClient = GetApiClient<TApi>();
        var method = apiMethod(apiClient);
        
        method.Returns(Task.FromException<TResponse>(
            new HttpRequestException("网络连接失败")));
        
        return this;
    }

    /// <summary>
    /// Setup timeout exception for any endpoint
    /// </summary>
    public MockApiFactory WithTimeoutError<TApi, TResponse>(
        Func<TApi, Func<Task<TResponse>>> apiMethod)
        where TApi : class
        where TResponse : class
    {
        var apiClient = GetApiClient<TApi>();
        var method = apiMethod(apiClient);
        
        method.Returns(Task.FromException<TResponse>(
            new TaskCanceledException("请求超时")));
        
        return this;
    }

    #endregion

    #region Specific API Methods

    /// <summary>
    /// Setup user API responses
    /// </summary>
    public UserApiConfigurator Users => new UserApiConfigurator(_userApi);

    /// <summary>
    /// Setup patient API responses
    /// </summary>
    public PatientApiConfigurator Patients => new PatientApiConfigurator(_patientApi);

    /// <summary>
    /// Setup herb API responses
    /// </summary>
    public HerbApiConfigurator Herbs => new HerbApiConfigurator(_herbApi);

    /// <summary>
    /// Setup formula API responses
    /// </summary>
    public FormulaApiConfigurator Formulas => new FormulaApiConfigurator(_formulaApi);

    /// <summary>
    /// Setup medical case API responses
    /// </summary>
    public MedicalCaseApiConfigurator MedicalCases => new MedicalCaseApiConfigurator(_medicalCaseApi);

    /// <summary>
    /// Setup sync API responses
    /// </summary>
    public SyncApiConfigurator Sync => new SyncApiConfigurator(_syncApi);

    /// <summary>
    /// Setup registration API responses
    /// </summary>
    public RegistrationApiConfigurator Registrations => new RegistrationApiConfigurator(_registrationApi);

    #endregion

    #region Helper Methods

    private TApi GetApiClient<TApi>() where TApi : class
    {
        return typeof(TApi) switch
        {
            Type t when t == typeof(IAuthApi) => (TApi)_authApi,
            Type t when t == typeof(IUserApi) => (TApi)_userApi,
            Type t when t == typeof(IPatientApi) => (TApi)_patientApi,
            Type t when t == typeof(IHerbApi) => (TApi)_herbApi,
            Type t when t == typeof(IFormulaApi) => (TApi)_formulaApi,
            Type t when t == typeof(IMedicalCaseApi) => (TApi)_medicalCaseApi,
            Type t when t == typeof(ISyncApi) => (TApi)_syncApi,
            Type t when t == typeof(IRegistrationApi) => (TApi)_registrationApi,
            _ => throw new ArgumentException($"Unsupported API type: {typeof(TApi)}")
        };
    }

    #endregion
}

/// <summary>
/// Base configurator for API-specific setup methods
/// </summary>
public abstract class ApiConfiguratorBase<TApi> where TApi : class
{
    protected readonly TApi _api;

    protected ApiConfiguratorBase(TApi api)
    {
        _api = api;
    }

    /// <summary>
    /// Setup successful response for a specific method
    /// </summary>
    protected void SetupSuccess<TResponse>(
        Func<TApi, Func<Task<TResponse>>> method,
        TResponse response,
        string message = "操作成功")
    {
        method(_api).Returns(Task.FromResult(new ApiResponse<TResponse>
        {
            Success = true,
            Data = response,
            Message = message
        }));
    }

    /// <summary>
    /// Setup error response for a specific method
    /// </summary>
    protected void SetupError<TResponse>(
        Func<TApi, Func<Task<TResponse>>> method,
        string errorMessage = "操作失败")
    {
        method(_api).Returns(Task.FromResult(new ApiResponse<TResponse>
        {
            Success = false,
            Message = errorMessage
        }));
    }
}

/// <summary>
/// User API configurator
/// </summary>
public class UserApiConfigurator : ApiConfiguratorBase<IUserApi>
{
    public UserApiConfigurator(IUserApi api) : base(api) { }

    public UserApiConfigurator ReturnsUsers<T>(T usersResponse)
    {
        SetupSuccess(api => api.GetUsersAsync, usersResponse);
        return this;
    }

    public UserApiConfigurator FailsToGetUsers(string error = "获取用户列表失败")
    {
        SetupError(api => api.GetUsersAsync, error);
        return this;
    }
}

/// <summary>
/// Patient API configurator
/// </summary>
public class PatientApiConfigurator : ApiConfiguratorBase<IPatientApi>
{
    public PatientApiConfigurator(IPatientApi api) : base(api) { }

    public PatientApiConfigurator ReturnsPatients<T>(T patientsResponse)
    {
        SetupSuccess(api => api.GetPatientsAsync, patientsResponse);
        return this;
    }
}

/// <summary>
/// Herb API configurator
/// </summary>
public class HerbApiConfigurator : ApiConfiguratorBase<IHerbApi>
{
    public HerbApiConfigurator(IHerbApi api) : base(api) { }

    public HerbApiConfigurator ReturnsHerbs<T>(T herbsResponse)
    {
        SetupSuccess(api => api.GetHerbsAsync, herbsResponse);
        return this;
    }
}

/// <summary>
/// Formula API configurator
/// </summary>
public class FormulaApiConfigurator : ApiConfiguratorBase<IFormulaApi>
{
    public FormulaApiConfigurator(IFormulaApi api) : base(api) { }

    public FormulaApiConfigurator ReturnsFormulas<T>(T formulasResponse)
    {
        SetupSuccess(api => api.GetFormulasAsync, formulasResponse);
        return this;
    }
}

/// <summary>
/// Medical Case API configurator
/// </summary>
public class MedicalCaseApiConfigurator : ApiConfiguratorBase<IMedicalCaseApi>
{
    public MedicalCaseApiConfigurator(IMedicalCaseApi api) : base(api) { }

    public MedicalCaseApiConfigurator ReturnsMedicalCases<T>(T casesResponse)
    {
        SetupSuccess(api => api.GetMedicalCasesAsync, casesResponse);
        return this;
    }
}

/// <summary>
/// Sync API configurator
/// </summary>
public class SyncApiConfigurator : ApiConfiguratorBase<ISyncApi>
{
    public SyncApiConfigurator(ISyncApi api) : base(api) { }

    public SyncApiConfigurator ReturnsSyncMetadata<T>(T metadataResponse)
    {
        SetupSuccess(api => api.GetMetadataAsync, metadataResponse);
        return this;
    }
}

/// <summary>
/// Registration API configurator
/// </summary>
public class RegistrationApiConfigurator : ApiConfiguratorBase<IRegistrationApi>
{
    public RegistrationApiConfigurator(IRegistrationApi api) : base(api) { }

    public RegistrationApiConfigurator ReturnsRegistrations<T>(T registrationsResponse)
    {
        SetupSuccess(api => api.GetRegistrationsAsync, registrationsResponse);
        return this;
    }
}
