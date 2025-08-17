using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Auth;
using LYBT.Shared.Models.Common;

namespace LYBT.Desktop.Auth.Services.Interfaces
{
    /// <summary>
    /// Auth模块业务服务接口 - UltraThink四层架构标准
    /// 统一管理认证相关的所有业务逻辑，提供模块自包含的服务层
    /// </summary>
    public interface IAuthModuleService
    {
        #region 事件
        
        /// <summary>
        /// 登录状态变更事件
        /// </summary>
        event EventHandler<AuthStatusChangedEventArgs>? AuthStatusChanged;
        
        /// <summary>
        /// API连接状态变更事件
        /// </summary>
        event EventHandler<ApiConnectionChangedEventArgs>? ApiConnectionChanged;
        
        #endregion

        #region 登录认证

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="loginInfo">登录信息</param>
        /// <returns>登录结果</returns>
        Task<ServiceResult<LoginInfo>> LoginAsync(LoginInfo loginInfo);

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns>登出结果</returns>
        Task<ServiceResult> LogoutAsync();

        /// <summary>
        /// 检查当前登录状态
        /// </summary>
        /// <returns>是否已登录</returns>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        /// <returns>用户信息</returns>
        Task<ServiceResult<LoginInfo?>> GetCurrentUserAsync();

        /// <summary>
        /// 刷新令牌
        /// </summary>
        /// <returns>刷新结果</returns>
        Task<ServiceResult<LoginInfo>> RefreshTokenAsync();

        #endregion

        #region 会话管理

        /// <summary>
        /// 获取存储的Token
        /// </summary>
        /// <returns>访问令牌</returns>
        string? GetToken();

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        /// <returns>Token是否有效</returns>
        Task<ServiceResult<bool>> ValidateTokenAsync();

        /// <summary>
        /// 清除认证信息
        /// </summary>
        void ClearAuthInfo();

        /// <summary>
        /// 获取会话剩余时间
        /// </summary>
        /// <returns>剩余时间（分钟）</returns>
        int GetSessionRemainingMinutes();

        #endregion

        #region 凭据管理

        /// <summary>
        /// 保存用户凭据
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="rememberMe">是否记住</param>
        /// <returns>保存结果</returns>
        ServiceResult SaveCredentials(string username, string password, bool rememberMe);

        /// <summary>
        /// 加载保存的凭据
        /// </summary>
        /// <returns>保存的凭据</returns>
        ServiceResult<LoginInfo?> LoadSavedCredentials();

        /// <summary>
        /// 清除保存的凭据
        /// </summary>
        /// <returns>清除结果</returns>
        ServiceResult ClearSavedCredentials();

        /// <summary>
        /// 检查是否有保存的凭据
        /// </summary>
        /// <returns>是否有保存的凭据</returns>
        bool HasSavedCredentials();

        #endregion

        #region 系统连接

        /// <summary>
        /// 检查API连接状态
        /// </summary>
        /// <returns>API是否在线</returns>
        Task<ServiceResult<bool>> CheckApiConnectionAsync();

        /// <summary>
        /// 获取当前API状态
        /// </summary>
        /// <returns>API状态信息</returns>
        ServiceResult<ApiStatusInfo> GetApiStatus();

        /// <summary>
        /// 启动API连接监控
        /// </summary>
        void StartApiConnectionMonitoring();

        /// <summary>
        /// 停止API连接监控
        /// </summary>
        void StopApiConnectionMonitoring();

        #endregion

        #region 安全功能

        /// <summary>
        /// 验证登录信息格式
        /// </summary>
        /// <param name="loginInfo">登录信息</param>
        /// <returns>验证结果</returns>
        ServiceResult ValidateLoginInfo(LoginInfo loginInfo);

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        /// <returns>IP地址</returns>
        string GetClientIpAddress();

        /// <summary>
        /// 生成设备指纹
        /// </summary>
        /// <returns>设备指纹</returns>
        string GenerateDeviceFingerprint();

        /// <summary>
        /// 检查账户锁定状态
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>锁定状态</returns>
        Task<ServiceResult<AccountLockInfo>> CheckAccountLockStatusAsync(string username);

        #endregion

        #region 密码管理

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="oldPassword">旧密码</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>修改结果</returns>
        Task<ServiceResult> ChangePasswordAsync(string oldPassword, string newPassword);

        /// <summary>
        /// 重置密码请求
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="email">邮箱</param>
        /// <returns>重置结果</returns>
        Task<ServiceResult> RequestPasswordResetAsync(string username, string email);

        /// <summary>
        /// 验证密码强度
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns>强度等级和建议</returns>
        ServiceResult<PasswordStrengthInfo> ValidatePasswordStrength(string password);

        #endregion

        #region 多因子认证（预留）

        /// <summary>
        /// 发送验证码
        /// </summary>
        /// <param name="phoneNumber">手机号码</param>
        /// <returns>发送结果</returns>
        Task<ServiceResult> SendVerificationCodeAsync(string phoneNumber);

        /// <summary>
        /// 验证验证码
        /// </summary>
        /// <param name="phoneNumber">手机号码</param>
        /// <param name="code">验证码</param>
        /// <returns>验证结果</returns>
        Task<ServiceResult<bool>> VerifyCodeAsync(string phoneNumber, string code);

        #endregion
    }

    #region 事件参数

    /// <summary>
    /// 认证状态变更事件参数
    /// </summary>
    public class AuthStatusChangedEventArgs : EventArgs
    {
        public bool IsLoggedIn { get; set; }
        public string? Username { get; set; }
        public string? StatusMessage { get; set; }
        
        public AuthStatusChangedEventArgs(bool isLoggedIn, string? username = null, string? statusMessage = null)
        {
            IsLoggedIn = isLoggedIn;
            Username = username;
            StatusMessage = statusMessage;
        }
    }

    /// <summary>
    /// API连接状态变更事件参数
    /// </summary>
    public class ApiConnectionChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string StatusMessage { get; set; }
        public DateTime Timestamp { get; set; }

        public ApiConnectionChangedEventArgs(bool isConnected, string statusMessage)
        {
            IsConnected = isConnected;
            StatusMessage = statusMessage;
            Timestamp = DateTime.Now;
        }
    }

    #endregion

    #region 辅助信息类

    /// <summary>
    /// API状态信息
    /// </summary>
    public class ApiStatusInfo
    {
        public bool IsOnline { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime LastCheckTime { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string ServerVersion { get; set; } = string.Empty;
    }

    /// <summary>
    /// 账户锁定信息
    /// </summary>
    public class AccountLockInfo
    {
        public bool IsLocked { get; set; }
        public DateTime? LockTime { get; set; }
        public DateTime? UnlockTime { get; set; }
        public int FailedAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 密码强度信息
    /// </summary>
    public class PasswordStrengthInfo
    {
        public PasswordStrengthLevel Level { get; set; }
        public int Score { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public bool MeetsPolicy { get; set; }
    }

    /// <summary>
    /// 密码强度等级
    /// </summary>
    public enum PasswordStrengthLevel
    {
        VeryWeak = 1,
        Weak = 2,
        Medium = 3,
        Strong = 4,
        VeryStrong = 5
    }

    #endregion
}