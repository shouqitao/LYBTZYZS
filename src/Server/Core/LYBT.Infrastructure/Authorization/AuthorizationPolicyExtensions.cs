using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Shared.Utilities.Security;

namespace LYBT.Infrastructure.Authorization
{
    /// <summary>
    /// 授权策略配置扩展方法
    /// 统一配置基于 RoleConstants 的授权策略，替代硬编码角色字符串
    /// </summary>
    public static class AuthorizationPolicyExtensions
    {
        /// <summary>
        /// 添加统一的角色授权策略
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRoleAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // 管理员策略
                options.AddPolicy(RoleHelper.Policies.AdminOnly, policy =>
                    policy.RequireRole(RoleHelper.Roles.Admin));

                // 医生策略（包含兼容性）
                options.AddPolicy(RoleHelper.Policies.DoctorOnly, policy =>
                    policy.RequireRole(RoleHelper.Roles.Doctor));

                // 医生或管理员策略
                options.AddPolicy(RoleHelper.Policies.DoctorOrAdmin, policy =>
                    policy.RequireRole(RoleHelper.Roles.Doctor, RoleHelper.Roles.Admin));

                // 兼容性策略：User 角色映射到 Doctor 策略
                options.AddPolicy("UserPolicy", policy =>
                    policy.RequireRole(RoleHelper.Roles.Doctor)); // User -> Doctor 映射

                // 默认策略：要求认证用户
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // 回退策略：未授权用户的处理
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }

        /// <summary>
        /// 添加 Claims 规范化服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddClaimsNormalization(this IServiceCollection services)
        {
            // Claims规范化现在由Shared.Utilities处理
            return services;
        }

        /// <summary>
        /// 配置完整的角色统一授权系统
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddUnifiedRoleAuthorization(this IServiceCollection services)
        {
            services.AddRoleAuthorizationPolicies();
            services.AddClaimsNormalization();

            return services;
        }
    }

    /// <summary>
    /// 授权属性扩展
    /// 提供类型安全的角色授权属性
    /// </summary>
    public static class AuthorizeRoles
    {
        /// <summary>
        /// 管理员授权属性
        /// </summary>
        public static readonly AuthorizeAttribute Admin = new(RoleHelper.Policies.AdminOnly);

        /// <summary>
        /// 医生授权属性
        /// </summary>
        public static readonly AuthorizeAttribute Doctor = new(RoleHelper.Policies.DoctorOnly);

        /// <summary>
        /// 医生或管理员授权属性
        /// </summary>
        public static readonly AuthorizeAttribute DoctorOrAdmin = new(RoleHelper.Policies.DoctorOrAdmin);

        /// <summary>
        /// 创建角色授权属性
        /// </summary>
        /// <param name="roles">角色列表</param>
        /// <returns>授权属性</returns>
        public static AuthorizeAttribute RequireRoles(params string[] roles)
        {
            var normalizedRoles = roles.Select(RoleHelper.NormalizeRole).ToArray();
            return new AuthorizeAttribute { Roles = string.Join(",", normalizedRoles) };
        }

        /// <summary>
        /// 创建策略授权属性
        /// </summary>
        /// <param name="policy">策略名称</param>
        /// <returns>授权属性</returns>
        public static AuthorizeAttribute RequirePolicy(string policy)
        {
            return new AuthorizeAttribute(policy);
        }
    }
}
