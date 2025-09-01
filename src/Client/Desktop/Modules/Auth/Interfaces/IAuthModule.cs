using System;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Interfaces;

/// <summary>
/// 认证模块统一接口 - UltraThink三层架构模块入口
/// 继承IAuthenticationService保持兼容性，同时提供完整的三层架构接口访问
/// </summary>
public interface IAuthModule : IAuthenticationService
{
    #region 核心操作层接口 (CoreService)
    
    // API通信操作
    Task<ServiceResult<LoginResponse>> CallLoginApiAsync(LoginRequest loginRequest);
    Task<ServiceResult> CallLogoutApiAsync();
    Task<ServiceResult<LoginResponse>> CallRefreshTokenApiAsync();
    Task<ServiceResult<bool>> CheckApiHealthAsync();
    
    // Token管理操作
    string? GetToken();
    void SetToken(string token);
    void ClearToken();
    ServiceResult ValidateToken(string? token);
    
    // 数据验证
    ServiceResult ValidateLoginRequest(LoginRequest loginRequest);
    ServiceResult ValidateUsername(string? username);
    ServiceResult ValidatePassword(string? password);
    ServiceResult<bool> ValidateAuthenticationState(UserDto? user, string? token);
    
    // 状态管理
    void UpdateAuthenticationState(bool isAuthenticated, UserDto? user, LoginResponse? loginResponse);
    void ClearAuthenticationState();
    ServiceResult<(bool IsAuthenticated, UserDto? User, LoginResponse? LoginResponse)> GetAuthenticationState();
    
    // 缓存优化
    Task<ServiceResult> PreWarmAuthCacheAsync();
    ServiceResult ClearAuthCache();
    
    #endregion
    
    #region 查询服务层接口 (QueryService)
    
    // 认证状态查询
    bool IsLoggedIn { get; }
    Task<ServiceResult<UserDto?>> GetCurrentUserAsync();
    ServiceResult<LoginStatusDto> GetLoginStatusDetails();
    Task<ServiceResult<bool>> IsAuthenticationValidAsync();
    
    // 连接状态查询  
    Task<ServiceResult<bool>> CheckConnectionAsync();
    ServiceResult<ApiConnectionStatusDto> GetApiConnectionStatus();
    Task<ServiceResult<ConnectionLatencyDto>> GetConnectionLatencyAsync();
    
    // 会话信息查询
    ServiceResult<SessionInfoDto> GetSessionInfo();
    int GetSessionRemainingMinutes();
    ServiceResult<bool> IsSessionExpiringSoon(int warningMinutes = 10);
    ServiceResult<DateTime?> GetTokenExpiryTime();
    
    // 凭据查询
    bool HasSavedCredentials();
    ServiceResult<SavedCredentialInfoDto> GetSavedCredentialInfo();
    ServiceResult<bool> ValidateSavedCredentials();
    
    // 监控数据查询
    ServiceResult<AuthStatisticsDto> GetAuthStatistics();
    ServiceResult<RecentLoginHistoryDto> GetRecentLoginHistory();
    ServiceResult<bool> IsMonitoringActive();
    
    // 安全状态查询
    ServiceResult<SecurityStatusDto> GetSecurityStatus();
    Task<ServiceResult<bool>> ShouldReauthenticate();
    ServiceResult<AuthRiskLevelDto> GetAuthRiskLevel();
    
    #endregion
    
    #region 业务逻辑层接口 (BusinessService)
    
    // 认证业务流程
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest);
    Task<ServiceResult> LogoutAsync();
    Task<ServiceResult<LoginResponse>> AutoLoginAsync();
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync();
    Task<ServiceResult<bool>> SilentReauthenticationAsync();
    
    // 凭据管理业务
    ServiceResult SaveCredentials(string username, string password, bool rememberMe);
    ServiceResult<LoginRequest?> LoadSavedCredentials();
    ServiceResult ClearSavedCredentials();
    ServiceResult UpdateSavedCredentials(string username, string newPassword, bool rememberMe);
    Task<ServiceResult<bool>> ValidateSavedCredentialsAsync();
    
    // 会话管理业务
    Task<ServiceResult> InitializeSessionAsync(LoginResponse loginResponse);
    Task<ServiceResult> ExtendSessionAsync();
    Task<ServiceResult> TerminateSessionAsync();
    Task<ServiceResult<bool>> HandleSessionExpiryAsync();
    Task<ServiceResult> KeepSessionAliveAsync();
    
    // 监控管理业务
    ServiceResult StartApiConnectionMonitoring();
    ServiceResult StopApiConnectionMonitoring();
    ServiceResult StartSessionMonitoring();
    ServiceResult StopSessionMonitoring();
    Task<ServiceResult> HandleConnectionStatusChange(bool isConnected);
    
    // 安全管理业务
    Task<ServiceResult<SecurityCheckResultDto>> PerformSecurityCheckAsync();
    Task<ServiceResult> HandleSecurityThreatAsync(SecurityThreatDto threat);
    Task<ServiceResult> LockUserAccountAsync(string reason);
    Task<ServiceResult> UnlockUserAccountAsync();
    
    // 密码管理业务
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    ServiceResult<PasswordStrengthDto> ValidatePasswordStrength(string password);
    
    // 用户体验优化
    Task<ServiceResult> PreloadUserPreferencesAsync();
    Task<ServiceResult<LoginExperienceDto>> OptimizeLoginExperienceAsync();
    Task<ServiceResult<OfflineModeDto>> HandleOfflineScenarioAsync();
    
    // 事件管理
    event EventHandler<(bool IsLoggedIn, string? Username, string? Message)>? AuthStatusChanged;
    event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;
    event EventHandler<SessionStatusChangedEventArgs>? SessionStatusChanged;
    event EventHandler<SecurityEventArgs>? SecurityEvent;
    
    // 诊断和故障排除
    Task<ServiceResult<AuthDiagnosticsDto>> DiagnoseAuthIssuesAsync();
    Task<ServiceResult> RepairAuthStateInconsistencyAsync();
    ServiceResult CleanupCorruptedAuthData();
    
    #endregion
}