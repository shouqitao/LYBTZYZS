using System;
using System.Threading.Tasks;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// Auth模块纯委托层 - UltraThink三层架构统一入口
/// 职责：请求路由分发，委托给专业服务层
/// </summary>
public class AuthModule : IAuthenticationService, IAuthModule, IDisposable
{
    private readonly IAuthCoreService _coreService;
    private readonly IAuthQueryService _queryService;
    private readonly IAuthBusinessService _businessService;
    private bool _disposed = false;
    
    public AuthModule(
        IAuthCoreService coreService,
        IAuthQueryService queryService,
        IAuthBusinessService businessService)
    {
        _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        
        // 转发事件
        _businessService.AuthStatusChanged += (sender, args) => AuthStatusChanged?.Invoke(this, args);
        _businessService.ApiConnectionChanged += (sender, args) => ApiConnectionChanged?.Invoke(this, args);
        _businessService.SessionStatusChanged += (sender, args) => SessionStatusChanged?.Invoke(this, args);
        _businessService.SecurityEvent += (sender, args) => SecurityEvent?.Invoke(this, args);
        
        // 启动API连接监控
        _businessService.StartApiConnectionMonitoring();
    }
    
    #region IAuthenticationService 实现 - 委托给QueryService
    
    public bool IsLoggedIn => _queryService.IsLoggedIn;
    
    public string? GetToken() => _coreService.GetToken();
    
    public void ClearAuthInfo() => _coreService.ClearAuthenticationState();
    
    public async Task<bool> CheckConnectionAsync()
    {
        var result = await _queryService.CheckConnectionAsync();
        return result.IsSuccess && result.Data;
    }
    
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var result = await _queryService.GetCurrentUserAsync();
        return result.IsSuccess ? result.Data : null;
    }
    
    #endregion
    
    #region 认证业务流程 - 委托给BusinessService
    
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
        => await _businessService.LoginAsync(loginRequest);
        
    public async Task<ServiceResult> LogoutAsync()
        => await _businessService.LogoutAsync();
        
    public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync()
        => await _businessService.RefreshTokenAsync();
        
    public async Task<ServiceResult<LoginResponse>> AutoLoginAsync()
        => await _businessService.AutoLoginAsync();
        
    public async Task<ServiceResult<bool>> SilentReauthenticationAsync()
        => await _businessService.SilentReauthenticationAsync();
    
    #endregion
    
    #region 会话管理 - 委托给QueryService和BusinessService
    
    public async Task<ServiceResult<bool>> ValidateTokenAsync()
        => await _queryService.IsAuthenticationValidAsync();
        
    public int GetSessionRemainingMinutes()
        => _queryService.GetSessionRemainingMinutes();
        
    public async Task<ServiceResult> InitializeSessionAsync(LoginResponse loginResponse)
        => await _businessService.InitializeSessionAsync(loginResponse);
        
    public async Task<ServiceResult> ExtendSessionAsync()
        => await _businessService.ExtendSessionAsync();
        
    public async Task<ServiceResult> TerminateSessionAsync()
        => await _businessService.TerminateSessionAsync();
        
    public async Task<ServiceResult<bool>> HandleSessionExpiryAsync()
        => await _businessService.HandleSessionExpiryAsync();
        
    public async Task<ServiceResult> KeepSessionAliveAsync()
        => await _businessService.KeepSessionAliveAsync();
    
    #endregion
    
    #region 凭据管理 - 委托给BusinessService
    
    public ServiceResult SaveCredentials(string username, string password, bool rememberMe)
        => _businessService.SaveCredentials(username, password, rememberMe);
        
    public ServiceResult<LoginRequest?> LoadSavedCredentials()
        => _businessService.LoadSavedCredentials();
        
    public ServiceResult ClearSavedCredentials()
        => _businessService.ClearSavedCredentials();
        
    public ServiceResult UpdateSavedCredentials(string username, string newPassword, bool rememberMe)
        => _businessService.UpdateSavedCredentials(username, newPassword, rememberMe);
        
    public async Task<ServiceResult<bool>> ValidateSavedCredentialsAsync()
        => await _businessService.ValidateSavedCredentialsAsync();
        
    public bool HasSavedCredentials()
        => _queryService.HasSavedCredentials();
    
    #endregion
    
    #region 监控管理 - 委托给BusinessService
    
    public ServiceResult StartApiConnectionMonitoring()
        => _businessService.StartApiConnectionMonitoring();
        
    public ServiceResult StopApiConnectionMonitoring()
        => _businessService.StopApiConnectionMonitoring();
        
    public ServiceResult StartSessionMonitoring()
        => _businessService.StartSessionMonitoring();
        
    public ServiceResult StopSessionMonitoring()
        => _businessService.StopSessionMonitoring();
        
    public async Task<ServiceResult> HandleConnectionStatusChange(bool isConnected)
        => await _businessService.HandleConnectionStatusChange(isConnected);
    
    #endregion
    
    #region 核心操作层接口 (CoreService) - 委托给CoreService
    
    public async Task<ServiceResult<LoginResponse>> CallLoginApiAsync(LoginRequest loginRequest)
        => await _coreService.CallLoginApiAsync(loginRequest);
        
    public async Task<ServiceResult> CallLogoutApiAsync()
        => await _coreService.CallLogoutApiAsync();
        
    public async Task<ServiceResult<LoginResponse>> CallRefreshTokenApiAsync()
        => await _coreService.CallRefreshTokenApiAsync();
        
    public async Task<ServiceResult<bool>> CheckApiHealthAsync()
        => await _coreService.CheckApiHealthAsync();
        
    public void SetToken(string token)
        => _coreService.SetToken(token);
        
    public void ClearToken()
        => _coreService.ClearToken();
        
    public ServiceResult ValidateToken(string? token)
        => _coreService.ValidateToken(token);
        
    public ServiceResult ValidateLoginRequest(LoginRequest loginRequest)
        => _coreService.ValidateLoginRequest(loginRequest);
        
    public ServiceResult ValidateUsername(string? username)
        => _coreService.ValidateUsername(username);
        
    public ServiceResult ValidatePassword(string? password)
        => _coreService.ValidatePassword(password);
        
    public ServiceResult<bool> ValidateAuthenticationState(UserDto? user, string? token)
        => _coreService.ValidateAuthenticationState(user, token);
        
    public void UpdateAuthenticationState(bool isAuthenticated, UserDto? user, LoginResponse? loginResponse)
        => _coreService.UpdateAuthenticationState(isAuthenticated, user, loginResponse);
        
    public void ClearAuthenticationState()
        => _coreService.ClearAuthenticationState();
        
    public ServiceResult<(bool IsAuthenticated, UserDto? User, LoginResponse? LoginResponse)> GetAuthenticationState()
        => _coreService.GetAuthenticationState();
        
    public async Task<ServiceResult> PreWarmAuthCacheAsync()
        => await _coreService.PreWarmAuthCacheAsync();
        
    public ServiceResult ClearAuthCache()
        => _coreService.ClearAuthCache();
    
    #endregion
    
    #region 查询服务层接口 (QueryService) - 委托给QueryService
    
    public ServiceResult<LoginStatusDto> GetLoginStatusDetails()
        => _queryService.GetLoginStatusDetails();
        
    public async Task<ServiceResult<bool>> IsAuthenticationValidAsync()
        => await _queryService.IsAuthenticationValidAsync();
        
    public ServiceResult<ApiConnectionStatusDto> GetApiConnectionStatus()
        => _queryService.GetApiConnectionStatus();
        
    public async Task<ServiceResult<ConnectionLatencyDto>> GetConnectionLatencyAsync()
        => await _queryService.GetConnectionLatencyAsync();
        
    public ServiceResult<SessionInfoDto> GetSessionInfo()
        => _queryService.GetSessionInfo();
        
    public ServiceResult<bool> IsSessionExpiringSoon(int warningMinutes = 10)
        => _queryService.IsSessionExpiringSoon(warningMinutes);
        
    public ServiceResult<DateTime?> GetTokenExpiryTime()
        => _queryService.GetTokenExpiryTime();
        
    public ServiceResult<SavedCredentialInfoDto> GetSavedCredentialInfo()
        => _queryService.GetSavedCredentialInfo();
        
    public ServiceResult<bool> ValidateSavedCredentials()
        => _queryService.ValidateSavedCredentials();
        
    public ServiceResult<AuthStatisticsDto> GetAuthStatistics()
        => _queryService.GetAuthStatistics();
        
    public ServiceResult<RecentLoginHistoryDto> GetRecentLoginHistory()
        => _queryService.GetRecentLoginHistory();
        
    public ServiceResult<bool> IsMonitoringActive()
        => _queryService.IsMonitoringActive();
        
    public ServiceResult<SecurityStatusDto> GetSecurityStatus()
        => _queryService.GetSecurityStatus();
        
    public async Task<ServiceResult<bool>> ShouldReauthenticate()
        => await _queryService.ShouldReauthenticate();
        
    public ServiceResult<AuthRiskLevelDto> GetAuthRiskLevel()
        => _queryService.GetAuthRiskLevel();
    
    #endregion
    
    #region 业务逻辑层接口完整实现 - 委托给BusinessService
    
    public async Task<ServiceResult<SecurityCheckResultDto>> PerformSecurityCheckAsync()
        => await _businessService.PerformSecurityCheckAsync();
        
    public async Task<ServiceResult> HandleSecurityThreatAsync(SecurityThreatDto threat)
        => await _businessService.HandleSecurityThreatAsync(threat);
        
    public async Task<ServiceResult> LockUserAccountAsync(string reason)
        => await _businessService.LockUserAccountAsync(reason);
        
    public async Task<ServiceResult> UnlockUserAccountAsync()
        => await _businessService.UnlockUserAccountAsync();
        
    public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
        => await _businessService.ChangePasswordAsync(changePasswordDto);
        
    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        => await _businessService.ResetPasswordAsync(resetPasswordDto);
        
    public ServiceResult<PasswordStrengthDto> ValidatePasswordStrength(string password)
        => _businessService.ValidatePasswordStrength(password);
        
    public async Task<ServiceResult> PreloadUserPreferencesAsync()
        => await _businessService.PreloadUserPreferencesAsync();
        
    public async Task<ServiceResult<LoginExperienceDto>> OptimizeLoginExperienceAsync()
        => await _businessService.OptimizeLoginExperienceAsync();
        
    public async Task<ServiceResult<OfflineModeDto>> HandleOfflineScenarioAsync()
        => await _businessService.HandleOfflineScenarioAsync();
        
    public async Task<ServiceResult<AuthDiagnosticsDto>> DiagnoseAuthIssuesAsync()
        => await _businessService.DiagnoseAuthIssuesAsync();
        
    public async Task<ServiceResult> RepairAuthStateInconsistencyAsync()
        => await _businessService.RepairAuthStateInconsistencyAsync();
        
    public ServiceResult CleanupCorruptedAuthData()
        => _businessService.CleanupCorruptedAuthData();
    
    #endregion
    
    #region 事件管理 - 转发BusinessService事件
    
    public event EventHandler<(bool IsLoggedIn, string? Username, string? Message)>? AuthStatusChanged;
    public event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;
    public event EventHandler<SessionStatusChangedEventArgs>? SessionStatusChanged;
    public event EventHandler<SecurityEventArgs>? SecurityEvent;
    
    #endregion
    
    #region IDisposable
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _businessService?.StopApiConnectionMonitoring();
        _businessService?.StopSessionMonitoring();
        
        if (_businessService is IDisposable businessDisposable)
            businessDisposable.Dispose();
            
        _disposed = true;
    }
    
    #endregion
}