using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Refit;

namespace LYBT.Shared.Interfaces.Api {

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
    public interface IAuthApi {

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
        [Post("/api/v1/auth/login")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>> LoginAsync([Body] LoginRequest loginRequest);

        /// <summary>
        /// 用户登出操作
        /// </summary>
        /// <returns>登出结果确认</returns>
        /// <remarks>
        /// <para>功能: 使当前JWT令牌失效，清理服务端会话状态</para>
        /// <para>操作: 令牌加入黑名单、清理缓存、记录登出日志</para>
        /// <para>安全: 防止令牌被恶意使用，确保会话完全终止</para>
        /// </remarks>
        [Post("/api/v1/auth/logout")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>> LogoutAsync();

        /// <summary>
        /// 获取当前认证用户信息
        /// </summary>
        /// <returns>当前用户详细信息</returns>
        /// <remarks>
        /// <para>功能: 根据JWT令牌获取当前登录用户的完整信息</para>
        /// <para>信息: 用户ID、用户名、显示名、角色、状态、最后登录时间</para>
        /// <para>缓存: 用户信息缓存10分钟，减少数据库查询</para>
        /// </remarks>
        [Get("/api/v1/auth/current-user")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDto>> GetCurrentUserAsync();

        /// <summary>
        /// 刷新JWT访问令牌
        /// </summary>
        /// <returns>新的JWT令牌对</returns>
        /// <remarks>
        /// <para>功能: 使用有效的刷新令牌获取新的访问令牌</para>
        /// <para>触发: 访问令牌即将过期时自动调用，保持用户会话连续</para>
        /// <para>安全: 刷新令牌单次使用，更新后旧令牌立即失效</para>
        /// </remarks>
        [Post("/api/v1/auth/refresh-token")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>> RefreshTokenAsync();

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="changePasswordRequest">密码修改请求 - 包含旧密码、新密码</param>
        /// <returns>密码修改结果</returns>
        /// <remarks>
        /// <para>功能: 验证旧密码后更新为新密码，强制重新登录</para>
        /// <para>验证: 旧密码验证、新密码强度检查、密码历史检查</para>
        /// <para>安全: 密码PBKDF2哈希存储、操作日志记录、会话失效</para>
        /// </remarks>
        [Post("/api/v1/auth/change-password")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>> ChangePasswordAsync([Body] ChangePasswordRequest changePasswordRequest);

        /// <summary>
        /// API服务健康状态检查
        /// </summary>
        /// <returns>服务状态响应字符串</returns>
        /// <remarks>
        /// <para>功能: 检查认证API服务的可用性和响应时间</para>
        /// <para>用途: 客户端连接测试、服务监控、网络诊断</para>
        /// <para>响应: 简单字符串响应，无需认证，用于快速连通性测试</para>
        /// </remarks>
        [Get("/api/v1/health/alive")]
        Task<string> HealthCheckAsync();
    }
}
