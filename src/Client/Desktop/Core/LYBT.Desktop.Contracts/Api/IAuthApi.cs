using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Auth;
namespace LYBT.Desktop.Contracts.Api
{

    /// <summary>
    /// 身份认证API客户端接口 - UltraThink统一API客户端标准
    /// </summary>
    /// <remarks>
    /// <para>功能范围: JWT身份认证、会话管理、密码操作、健康检查</para>
    /// <para>技术特性: Refit类型安全REST客户端、统一ApiResponse响应格式</para>
    /// <para>安全特性: JWT Bearer Token认证、8小时过期、Remember Me 30天</para>
    /// <para>架构定位: 前端WPF客户端与后端Web API的统一接口契约</para>
    /// </remarks>
    [Description("身份认证API客户端 - JWT认证、会话管理、安全操作")]
    public interface IAuthApi
    {

        /// <summary>
        /// 用户登录认证
        /// </summary>
        /// <param name="loginRequest">登录请求信息 - 包含用户名、密码、记住我选项</param>
        /// <returns>登录响应 - 包含JWT令牌、用户信息、过期时间</returns>
        /// <remarks>
        /// <para>功能: 验证用户凭据，生成JWT访问令牌和刷新令牌</para>
        /// <para>令牌: 访问令牌8小时有效期，刷新令牌30天(Remember Me)或1天</para>
        /// <para>安全: PBKDF2密码哈希验证、失败次数限制、IP地址记录</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/login")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>> LoginAsync([Refit.Body] LoginRequest loginRequest);

        /// <summary>
        /// 用户登出操作
        /// </summary>
        /// <param name="logoutRequest">登出请求信息</param>
        /// <returns>登出结果确认</returns>
        /// <remarks>
        /// <para>功能: 使当前JWT令牌失效，清理服务端会话状态</para>
        /// <para>操作: 令牌加入黑名单、清理缓存、记录登出日志</para>
        /// <para>安全: 防止令牌被恶意使用，确保会话完全终止</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/logout")]
        [Refit.Headers("Authorization: Bearer")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse> LogoutAsync([Refit.Body] LogoutRequest logoutRequest);

        /// <summary>
        /// 刷新访问令牌 - Issue #1838
        /// </summary>
        /// <param name="request">刷新令牌请求</param>
        /// <returns>新的令牌对（包含新的AccessToken和RefreshToken）</returns>
        /// <remarks>
        /// <para>功能: 使用RefreshToken获取新的AccessToken和RefreshToken</para>
        /// <para>安全: Token轮换机制，旧RefreshToken被撤销，新RefreshToken生成</para>
        /// <para>过期: AccessToken 15分钟，RefreshToken 7天</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/refresh")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>> RefreshTokenAsync([Refit.Body] RefreshTokenRequest request);


        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        /// <param name="changeSysAdminPassword">密码修改请求</param>
        /// <returns>密码修改结果</returns>
        /// <remarks>
        /// <para>功能: 修改系统管理员密码</para>
        /// <para>验证: 新密码强度检查，至少6位</para>
        /// <para>权限: 仅管理员角色可访问</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/changeSysAdminPassword")]
        [Refit.Headers("Authorization: Bearer")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse> ChangeSysAdminPasswordAsync([Refit.Body] ChangeSysAdminPassword changeSysAdminPassword);

        /// <summary>
        /// 验证Token (GET方法)
        /// </summary>
        /// <returns>验证结果包含token有效性和用户信息</returns>
        /// <remarks>
        /// <para>功能: 从Authorization header中获取Bearer Token进行验证</para>
        /// <para>返回: Token有效性、用户信息和过期时间</para>
        /// </remarks>
        [Refit.Get("/api/v1/auth/validate")]
        [Refit.Headers("Authorization: Bearer")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>> ValidateTokenFromHeaderAsync();

        /// <summary>
        /// 验证Token (POST方法)
        /// </summary>
        /// <param name="token">要验证的Token</param>
        /// <returns>验证结果</returns>
        /// <remarks>
        /// <para>功能: 验证指定的Token是否有效</para>
        /// <para>用途: 用于无法使用Header的场景</para>
        /// </remarks>
        /// <summary>
        /// 验证Token并返回详细信息 (POST方法) - Issue #1824
        /// </summary>
        /// <param name="request">Token验证请求</param>
        /// <returns>详细的验证结果</returns>
        /// <remarks>
        /// <para>功能: 验证指定的Token并返回用户信息和过期时间</para>
        /// <para>用途: Desktop客户端启动时的Token自动验证</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/validate")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<ValidateTokenResponse>> ValidateTokenAsync([Refit.Body] ValidateTokenRequest request);

        /// <summary>
        /// API服务健康状态检查
        /// </summary>
        /// <returns>健康检查响应</returns>
        /// <remarks>
        /// <para>功能: 检查API服务的可用性和响应时间</para>
        /// <para>用途: 客户端连接测试、服务监控、网络诊断</para>
        /// <para>响应: 返回服务状态信息，包含状态和时间戳，无需认证</para>
        /// </remarks>
        [Refit.Get("/api/v1/health")]
        Task<LYBT.Shared.Models.Contracts.Common.HealthCheckResponse> HealthCheckAsync();
    }
}
