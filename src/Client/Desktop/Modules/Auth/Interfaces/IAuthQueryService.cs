using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Interfaces;

/// <summary>
/// 认证查询服务接口 - UltraThink三层架构查询专业层
/// 职责：状态查询、连接检查、用户信息获取、监控数据查询
/// </summary>
public interface IAuthQueryService
{
    #region 认证状态查询
    
    /// <summary>
    /// 检查是否已登录
    /// </summary>
    bool IsLoggedIn { get; }
    
    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<ServiceResult<UserDto?>> GetCurrentUserAsync();
    
    /// <summary>
    /// 获取登录状态详情
    /// </summary>
    ServiceResult<LoginStatusDto> GetLoginStatusDetails();
    
    /// <summary>
    /// 检查认证状态是否有效
    /// </summary>
    Task<ServiceResult<bool>> IsAuthenticationValidAsync();
    
    #endregion
    
    #region 连接状态查询
    
    /// <summary>
    /// 检查API连接状态
    /// </summary>
    Task<ServiceResult<bool>> CheckConnectionAsync();
    
    /// <summary>
    /// 获取API连接状态详情
    /// </summary>
    ServiceResult<ApiConnectionStatusDto> GetApiConnectionStatus();
    
    /// <summary>
    /// 获取连接延迟信息
    /// </summary>
    Task<ServiceResult<ConnectionLatencyDto>> GetConnectionLatencyAsync();
    
    #endregion
    
    #region 会话信息查询
    
    /// <summary>
    /// 获取会话剩余时间
    /// </summary>
    ServiceResult<SessionInfoDto> GetSessionInfo();
    
    /// <summary>
    /// 计算会话剩余分钟数
    /// </summary>
    int GetSessionRemainingMinutes();
    
    /// <summary>
    /// 检查会话是否即将过期
    /// </summary>
    ServiceResult<bool> IsSessionExpiringSoon(int warningMinutes = 10);
    
    /// <summary>
    /// 获取Token过期时间
    /// </summary>
    ServiceResult<DateTime?> GetTokenExpiryTime();
    
    #endregion
    
    #region 凭据查询
    
    /// <summary>
    /// 检查是否有保存的凭据
    /// </summary>
    bool HasSavedCredentials();
    
    /// <summary>
    /// 加载保存的凭据信息（不含密码）
    /// </summary>
    ServiceResult<SavedCredentialInfoDto> GetSavedCredentialInfo();
    
    /// <summary>
    /// 验证保存的凭据是否完整
    /// </summary>
    ServiceResult<bool> ValidateSavedCredentials();
    
    #endregion
    
    #region 监控数据查询
    
    /// <summary>
    /// 获取认证统计信息
    /// </summary>
    ServiceResult<AuthStatisticsDto> GetAuthStatistics();
    
    /// <summary>
    /// 获取最近登录历史（本地缓存）
    /// </summary>
    ServiceResult<RecentLoginHistoryDto> GetRecentLoginHistory();
    
    /// <summary>
    /// 检查监控状态
    /// </summary>
    ServiceResult<bool> IsMonitoringActive();
    
    #endregion
    
    #region 安全状态查询
    
    /// <summary>
    /// 获取安全状态评估
    /// </summary>
    ServiceResult<SecurityStatusDto> GetSecurityStatus();
    
    /// <summary>
    /// 检查是否需要重新认证
    /// </summary>
    Task<ServiceResult<bool>> ShouldReauthenticate();
    
    /// <summary>
    /// 获取认证风险等级
    /// </summary>
    ServiceResult<AuthRiskLevelDto> GetAuthRiskLevel();
    
    #endregion
}