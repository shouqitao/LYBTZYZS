using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LYBT.Infrastructure.Options;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Helpers
{
    /// <summary>
    /// AuthService日志记录助手类 - UltraThink Helper模式
    /// 负责所有认证相关的日志记录：登录成功、失败、异常、用户操作等
    /// </summary>
    public class AuthLoggingHelper
    {
        private readonly ILogger<AuthLoggingHelper> _logger;
        private readonly AuthOptions _authOptions;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthLoggingHelper(
            ILogger<AuthLoggingHelper> logger,
            IOptions<AuthOptions> authOptions,
            SysAdminHandler sysAdminHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
            _sysAdminHandler = sysAdminHandler ?? throw new ArgumentNullException(nameof(sysAdminHandler));
        }

        #region 登录日志记录

        /// <summary>
        /// 记录登录成功日志
        /// </summary>
        public async Task LogSuccessfulLoginAsync(User user, LoginRequest dto)
        {
            if (!_authOptions.EnableDetailedLoginLogging)
                return;

            var content = "登录成功";
            if (!string.IsNullOrEmpty(dto.ClientIp))
            {
                content += $" | IP: {dto.ClientIp}";
            }
            if (!string.IsNullOrEmpty(dto.UserAgent))
            {
                content += $" | UA: {dto.UserAgent}";
            }

            await LogUserActionAsync(user.Id, user.RealName, ActionType.Login, content);
        }

        /// <summary>
        /// 记录登录失败日志
        /// </summary>
        public async Task LogFailedLoginAsync(Guid userId, string operatorName, string reason, LoginRequest dto)
        {
            if (!_authOptions.EnableDetailedLoginLogging)
                return;

            var content = $"登录失败: {reason}";
            if (!string.IsNullOrEmpty(dto.ClientIp))
            {
                content += $" | IP: {dto.ClientIp}";
            }
            if (!string.IsNullOrEmpty(dto.UserAgent))
            {
                content += $" | UA: {dto.UserAgent}";
            }

            await LogUserActionAsync(userId, operatorName, ActionType.Login, content);
        }

        /// <summary>
        /// 记录登录异常日志
        /// </summary>
        public async Task LogLoginExceptionAsync(string username, Exception ex, LoginRequest dto)
        {
            var content = $"登录异常: {ex.Message}";
            if (!string.IsNullOrEmpty(dto.ClientIp))
            {
                content += $" | IP: {dto.ClientIp}";
            }

            await LogUserActionAsync(Guid.Empty, username, ActionType.Login, content);
        }

        #endregion

        #region 登出日志记录

        /// <summary>
        /// 记录登出日志
        /// </summary>
        public async Task LogLogoutAsync(User user, LogoutRequest dto)
        {
            if (!_authOptions.EnableDetailedLoginLogging)
                return;

            var content = "用户登出";
            // UltraThink v2.0简化：LogoutRequest不包含ClientIp信息

            var operatorName = GetOperatorName(user, dto.Username);
            await LogUserActionAsync(user?.Id ?? Guid.Empty, operatorName, ActionType.Logout, content);
        }

        /// <summary>
        /// 记录强制登出日志
        /// </summary>
        public async Task LogForceLogoutAsync(Guid userId, string username, string reason, Guid operatorId)
        {
            var content = $"强制登出: {reason}";
            await LogUserActionAsync(userId, username, ActionType.Logout, content);
            
            _logger.LogWarning("管理员强制登出用户: {Username} ({UserId}), 原因: {Reason}, 操作者: {OperatorId}", 
                username, userId, reason, operatorId);
        }

        #endregion

        #region 密码管理日志记录

        /// <summary>
        /// 记录密码修改日志
        /// </summary>
        public async Task LogPasswordChangeAsync(Guid userId, string username, bool isSuccess, string? reason = null)
        {
            var content = isSuccess ? "密码修改成功" : $"密码修改失败: {reason}";
            await LogUserActionAsync(userId, username, ActionType.Update, content);
        }

        /// <summary>
        /// 记录系统管理员密码修改日志
        /// </summary>
        public async Task LogSysAdminPasswordChangeAsync(bool isSuccess, string? reason = null)
        {
            var content = isSuccess ? "系统管理员密码修改成功" : $"系统管理员密码修改失败: {reason}";
            await LogUserActionAsync(Guid.Empty, "系统管理员", ActionType.Update, content);
        }

        #endregion

        #region 令牌管理日志记录

        /// <summary>
        /// 记录令牌刷新日志
        /// </summary>
        public async Task LogTokenRefreshAsync(Guid userId, string username, bool isSuccess, string? reason = null)
        {
            var content = isSuccess ? "令牌刷新成功" : $"令牌刷新失败: {reason}";
            await LogUserActionAsync(userId, username, ActionType.Login, content);
        }

        /// <summary>
        /// 记录令牌验证日志
        /// </summary>
        public async Task LogTokenValidationAsync(string token, bool isValid, string? reason = null)
        {
            if (!_authOptions.EnableDetailedLoginLogging)
                return;

            var content = isValid ? "令牌验证成功" : $"令牌验证失败: {reason}";
            var tokenPreview = string.IsNullOrEmpty(token) ? "空" : $"{token.Substring(0, Math.Min(10, token.Length))}...";
            
            _logger.LogInformation("令牌验证 - Token: {TokenPreview}, 结果: {Content}", tokenPreview, content);
        }

        #endregion

        #region 账户安全日志记录

        /// <summary>
        /// 记录账户锁定日志
        /// </summary>
        public async Task LogAccountLockoutAsync(Guid userId, string username, string reason)
        {
            var content = $"账户锁定: {reason}";
            await LogUserActionAsync(userId, username, ActionType.Update, content);
            
            _logger.LogWarning("账户锁定 - 用户: {Username} ({UserId}), 原因: {Reason}", username, userId, reason);
        }

        /// <summary>
        /// 记录账户解锁日志
        /// </summary>
        public async Task LogAccountUnlockAsync(Guid userId, string username, Guid operatorId)
        {
            var content = "账户解锁";
            await LogUserActionAsync(userId, username, ActionType.Update, content);
            
            _logger.LogInformation("账户解锁 - 用户: {Username} ({UserId}), 操作者: {OperatorId}", username, userId, operatorId);
        }

        /// <summary>
        /// 记录可疑登录尝试
        /// </summary>
        public async Task LogSuspiciousLoginAttemptAsync(string username, string clientIp, string reason)
        {
            var content = $"可疑登录尝试: {reason} | IP: {clientIp}";
            await LogUserActionAsync(Guid.Empty, username, ActionType.Login, content);
            
            _logger.LogWarning("可疑登录尝试 - 用户: {Username}, IP: {ClientIp}, 原因: {Reason}", username, clientIp, reason);
        }

        #endregion

        #region 权限变更日志记录

        /// <summary>
        /// 记录权限变更日志
        /// </summary>
        public async Task LogPermissionChangeAsync(Guid userId, string username, string permissionChange, Guid operatorId)
        {
            var content = $"权限变更: {permissionChange}";
            await LogUserActionAsync(userId, username, ActionType.Update, content);
            
            _logger.LogInformation("权限变更 - 用户: {Username} ({UserId}), 变更: {PermissionChange}, 操作者: {OperatorId}", 
                username, userId, permissionChange, operatorId);
        }

        /// <summary>
        /// 记录角色变更日志
        /// </summary>
        public async Task LogRoleChangeAsync(Guid userId, string username, UserRole oldRole, UserRole newRole, Guid operatorId)
        {
            var content = $"角色变更: {oldRole} -> {newRole}";
            await LogUserActionAsync(userId, username, ActionType.Update, content);
            
            _logger.LogInformation("角色变更 - 用户: {Username} ({UserId}), 从 {OldRole} 变更为 {NewRole}, 操作者: {OperatorId}", 
                username, userId, oldRole, newRole, operatorId);
        }

        #endregion

        #region 核心日志方法

        /// <summary>
        /// 记录用户操作日志（核心方法）
        /// </summary>
        public async Task LogUserActionAsync(Guid userId, string operatorName, ActionType actionType, string content)
        {
            _logger.LogInformation("认证操作日志 - 操作者: {OperatorName} ({UserId}), 操作类型: {ActionType}, 内容: {Content}",
                operatorName, userId, actionType, content);
            
            await Task.CompletedTask;
            // 实际项目中可以在这里：
            // 1. 写入操作日志表
            // 2. 发送到日志系统
            // 3. 触发安全告警
        }

        /// <summary>
        /// 记录系统事件日志
        /// </summary>
        public async Task LogSystemEventAsync(string eventType, string description, object? data = null)
        {
            _logger.LogInformation("认证系统事件 - 类型: {EventType}, 描述: {Description}, 数据: {Data}", 
                eventType, description, data);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 记录安全告警
        /// </summary>
        public async Task LogSecurityAlertAsync(string alertType, string description, string? source = null)
        {
            _logger.LogWarning("安全告警 - 类型: {AlertType}, 描述: {Description}, 来源: {Source}", 
                alertType, description, source);
            
            await Task.CompletedTask;
            // 实际项目中可以在这里：
            // 1. 发送告警通知
            // 2. 写入安全日志
            // 3. 触发自动响应
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取操作员名称
        /// </summary>
        private string GetOperatorName(User? user, string username)
        {
            if (user != null)
            {
                return !string.IsNullOrEmpty(user.RealName) ? user.RealName : user.Username;
            }

            // 检查是否为系统管理员
            if (_sysAdminHandler.IsSysAdmin(username))
            {
                return "系统管理员";
            }

            return username;
        }

        /// <summary>
        /// 格式化客户端信息
        /// </summary>
        public string FormatClientInfo(string? clientIp, string? userAgent)
        {
            var info = "";
            if (!string.IsNullOrEmpty(clientIp))
            {
                info += $"IP: {clientIp}";
            }
            if (!string.IsNullOrEmpty(userAgent))
            {
                if (!string.IsNullOrEmpty(info)) info += " | ";
                info += $"UA: {userAgent}";
            }
            return info;
        }

        /// <summary>
        /// 脱敏处理敏感信息
        /// </summary>
        public string MaskSensitiveInfo(string input, int visibleLength = 4)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= visibleLength)
                return input;

            return input.Substring(0, visibleLength) + "***";
        }

        #endregion
    }
}