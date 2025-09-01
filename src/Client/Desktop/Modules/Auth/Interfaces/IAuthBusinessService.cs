using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Interfaces;

/// <summary>
/// 认证业务服务接口 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、凭据管理、会话管理、监控管理、事务处理
/// </summary>
public interface IAuthBusinessService
{
    #region 认证业务流程
    
    /// <summary>
    /// 执行完整登录流程 (验证 → API调用 → 状态更新 → 凭据保存)
    /// </summary>
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest);
    
    /// <summary>
    /// 执行完整登出流程 (API调用 → 状态清理 → 凭据清理 → 事件通知)
    /// </summary>
    Task<ServiceResult> LogoutAsync();
    
    /// <summary>
    /// 自动登录流程 (加载凭据 → 自动登录)
    /// </summary>
    Task<ServiceResult<LoginResponse>> AutoLoginAsync();
    
    /// <summary>
    /// Token刷新业务流程
    /// </summary>
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync();
    
    /// <summary>
    /// 静默重新认证 (Token过期时自动处理)
    /// </summary>
    Task<ServiceResult<bool>> SilentReauthenticationAsync();
    
    #endregion
    
    #region 凭据管理业务
    
    /// <summary>
    /// 保存用户凭据 (加密存储 + 安全验证)
    /// </summary>
    ServiceResult SaveCredentials(string username, string password, bool rememberMe);
    
    /// <summary>
    /// 加载保存的凭据 (解密 + 完整性验证)
    /// </summary>
    ServiceResult<LoginRequest?> LoadSavedCredentials();
    
    /// <summary>
    /// 清除保存的凭据 (安全清理)
    /// </summary>
    ServiceResult ClearSavedCredentials();
    
    /// <summary>
    /// 更新保存的凭据
    /// </summary>
    ServiceResult UpdateSavedCredentials(string username, string newPassword, bool rememberMe);
    
    /// <summary>
    /// 验证凭据有效性
    /// </summary>
    Task<ServiceResult<bool>> ValidateSavedCredentialsAsync();
    
    #endregion
    
    #region 会话管理业务
    
    /// <summary>
    /// 初始化用户会话
    /// </summary>
    Task<ServiceResult> InitializeSessionAsync(LoginResponse loginResponse);
    
    /// <summary>
    /// 扩展会话时间
    /// </summary>
    Task<ServiceResult> ExtendSessionAsync();
    
    /// <summary>
    /// 终止用户会话
    /// </summary>
    Task<ServiceResult> TerminateSessionAsync();
    
    /// <summary>
    /// 检查并处理会话过期
    /// </summary>
    Task<ServiceResult<bool>> HandleSessionExpiryAsync();
    
    /// <summary>
    /// 会话保活处理
    /// </summary>
    Task<ServiceResult> KeepSessionAliveAsync();
    
    #endregion
    
    #region 监控管理业务
    
    /// <summary>
    /// 启动API连接监控
    /// </summary>
    ServiceResult StartApiConnectionMonitoring();
    
    /// <summary>
    /// 停止API连接监控
    /// </summary>
    ServiceResult StopApiConnectionMonitoring();
    
    /// <summary>
    /// 启动会话监控
    /// </summary>
    ServiceResult StartSessionMonitoring();
    
    /// <summary>
    /// 停止会话监控
    /// </summary>
    ServiceResult StopSessionMonitoring();
    
    /// <summary>
    /// 处理连接状态变更
    /// </summary>
    Task<ServiceResult> HandleConnectionStatusChange(bool isConnected);
    
    #endregion
    
    #region 安全管理业务
    
    /// <summary>
    /// 执行安全检查流程
    /// </summary>
    Task<ServiceResult<SecurityCheckResultDto>> PerformSecurityCheckAsync();
    
    /// <summary>
    /// 处理安全威胁
    /// </summary>
    Task<ServiceResult> HandleSecurityThreatAsync(SecurityThreatDto threat);
    
    /// <summary>
    /// 锁定用户账户 (安全防护)
    /// </summary>
    Task<ServiceResult> LockUserAccountAsync(string reason);
    
    /// <summary>
    /// 解锁用户账户
    /// </summary>
    Task<ServiceResult> UnlockUserAccountAsync();
    
    #endregion
    
    #region 密码管理业务
    
    /// <summary>
    /// 修改密码业务流程
    /// </summary>
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
    
    /// <summary>
    /// 重置密码业务流程
    /// </summary>
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    
    /// <summary>
    /// 验证密码强度
    /// </summary>
    ServiceResult<PasswordStrengthDto> ValidatePasswordStrength(string password);
    
    #endregion
    
    #region 用户体验优化
    
    /// <summary>
    /// 预加载用户偏好设置
    /// </summary>
    Task<ServiceResult> PreloadUserPreferencesAsync();
    
    /// <summary>
    /// 优化登录体验 (自动填充、记住选择等)
    /// </summary>
    Task<ServiceResult<LoginExperienceDto>> OptimizeLoginExperienceAsync();
    
    /// <summary>
    /// 处理离线场景
    /// </summary>
    Task<ServiceResult<OfflineModeDto>> HandleOfflineScenarioAsync();
    
    #endregion
    
    #region 事件管理
    
    /// <summary>
    /// 认证状态变更事件
    /// </summary>
    event EventHandler<(bool IsLoggedIn, string? Username, string? Message)>? AuthStatusChanged;
    
    /// <summary>
    /// API连接状态变更事件  
    /// </summary>
    event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;
    
    /// <summary>
    /// 会话状态变更事件
    /// </summary>
    event EventHandler<SessionStatusChangedEventArgs>? SessionStatusChanged;
    
    /// <summary>
    /// 安全事件
    /// </summary>
    event EventHandler<SecurityEventArgs>? SecurityEvent;
    
    #endregion
    
    #region 诊断和故障排除
    
    /// <summary>
    /// 诊断认证问题
    /// </summary>
    Task<ServiceResult<AuthDiagnosticsDto>> DiagnoseAuthIssuesAsync();
    
    /// <summary>
    /// 修复认证状态不一致
    /// </summary>
    Task<ServiceResult> RepairAuthStateInconsistencyAsync();
    
    /// <summary>
    /// 清理损坏的认证数据
    /// </summary>
    ServiceResult CleanupCorruptedAuthData();
    
    #endregion
}