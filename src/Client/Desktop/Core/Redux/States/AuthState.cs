using System;
using System.Collections.Immutable;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Redux.States
{
    /// <summary>
    /// 认证状态
    /// </summary>
    public record AuthState
    {
        public bool IsAuthenticated { get; init; }
        public bool IsLoading { get; init; }
        public UserInfo? CurrentUser { get; init; }
        public string? Token { get; init; }
        public DateTimeOffset? TokenExpiry { get; init; }
        public string? Error { get; init; }
        public ImmutableList<string> Permissions { get; init; } = ImmutableList<string>.Empty;

        /// <summary>
        /// 创建初始状态
        /// </summary>
        public static AuthState Initial => new()
        {
            IsAuthenticated = false,
            IsLoading = false,
            CurrentUser = null,
            Token = null,
            TokenExpiry = null,
            Error = null,
            Permissions = ImmutableList<string>.Empty
        };

        /// <summary>
        /// 检查Token是否过期
        /// </summary>
        public bool IsTokenExpired => TokenExpiry.HasValue && TokenExpiry.Value < DateTimeOffset.UtcNow;

        /// <summary>
        /// 检查是否有指定权限
        /// </summary>
        public bool HasPermission(string permission)
        {
            return Permissions.Contains(permission);
        }
    }

    /// <summary>
    /// 用户信息
    /// </summary>
    public record UserInfo
    {
        public Guid Id { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string RealName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string? Avatar { get; init; }
        public DateTimeOffset LastLoginTime { get; init; }
    }

    #region Auth Actions

    /// <summary>
    /// 登录请求Action
    /// </summary>
    public class LoginRequestAction : ActionBase<LoginRequest>
    {
        public LoginRequestAction(LoginRequest request)
            : base("AUTH/LOGIN_REQUEST", request) { }
    }

    /// <summary>
    /// 登录成功Action
    /// </summary>
    public class LoginSuccessAction : ActionBase<LoginResponse>
    {
        public LoginSuccessAction(LoginResponse response)
            : base("AUTH/LOGIN_SUCCESS", response) { }
    }

    /// <summary>
    /// 登录失败Action
    /// </summary>
    public class LoginFailureAction : ActionBase<string>
    {
        public LoginFailureAction(string error)
            : base("AUTH/LOGIN_FAILURE", error) { }
    }

    /// <summary>
    /// 登出Action
    /// </summary>
    public class LogoutAction : ActionBase
    {
        public LogoutAction() : base("AUTH/LOGOUT") { }
    }

    /// <summary>
    /// 刷新Token Action
    /// </summary>
    public class RefreshTokenAction : ActionBase
    {
        public RefreshTokenAction() : base("AUTH/REFRESH_TOKEN") { }
    }

    /// <summary>
    /// Token刷新成功Action
    /// </summary>
    public class RefreshTokenSuccessAction : ActionBase<string>
    {
        public RefreshTokenSuccessAction(string newToken)
            : base("AUTH/REFRESH_TOKEN_SUCCESS", newToken) { }
    }

    /// <summary>
    /// 更新权限Action
    /// </summary>
    public class UpdatePermissionsAction : ActionBase<ImmutableList<string>>
    {
        public UpdatePermissionsAction(ImmutableList<string> permissions)
            : base("AUTH/UPDATE_PERMISSIONS", permissions) { }
    }

    #endregion

    #region DTOs

    /// <summary>
    /// 登录请求
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// 登录响应
    /// </summary>
    public class LoginResponse
    {
        public UserInfo User { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset TokenExpiry { get; set; }
        public ImmutableList<string> Permissions { get; set; } = ImmutableList<string>.Empty;
    }

    #endregion

    /// <summary>
    /// 认证状态Reducer
    /// </summary>
    public class AuthReducer : IReducer<AuthState>
    {
        public AuthState Reduce(AuthState state, IAction action)
        {
            return action switch
            {
                LoginRequestAction _ => state with
                {
                    IsLoading = true,
                    Error = null
                },

                LoginSuccessAction success => state with
                {
                    IsAuthenticated = true,
                    IsLoading = false,
                    CurrentUser = success.Payload.User,
                    Token = success.Payload.Token,
                    TokenExpiry = success.Payload.TokenExpiry,
                    Permissions = success.Payload.Permissions,
                    Error = null
                },

                LoginFailureAction failure => state with
                {
                    IsAuthenticated = false,
                    IsLoading = false,
                    CurrentUser = null,
                    Token = null,
                    TokenExpiry = null,
                    Error = failure.Payload
                },

                LogoutAction _ => AuthState.Initial,

                RefreshTokenSuccessAction refresh => state with
                {
                    Token = refresh.Payload,
                    TokenExpiry = DateTimeOffset.UtcNow.AddHours(8) // 默认8小时
                },

                UpdatePermissionsAction update => state with
                {
                    Permissions = update.Payload
                },

                _ => state
            };
        }
    }
}
