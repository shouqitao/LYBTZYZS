namespace LYBT.WebAPI.Extensions.ServiceCollection
{
    /// <summary>
    /// 授权策略相关的扩展方法
    /// </summary>
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// 添加基于角色的授权策略
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="policies">策略配置字典</param>
        /// <returns>配置后的服务集合</returns>
        public static IServiceCollection AddRoleBasedAuthorization(
            this IServiceCollection services,
            Dictionary<string, string[]>? policies = null)
        {
            services.AddAuthorization(options =>
            {
                // 使用提供的策略或默认策略
                var policyDefinitions = policies ?? GetDefaultPolicies();

                foreach (var (policyName, roles) in policyDefinitions)
                {
                    options.AddPolicy(policyName, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        if (roles != null && roles.Length > 0)
                        {
                            policy.RequireRole(roles);
                        }
                    });
                }
            });

            return services;
        }

        /// <summary>
        /// 添加单个授权策略
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="policyName">策略名称</param>
        /// <param name="roles">允许的角色列表</param>
        /// <returns>配置后的服务集合</returns>
        public static IServiceCollection AddAuthorizationPolicy(
            this IServiceCollection services,
            string policyName,
            params string[] roles)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(policyName, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    if (roles != null && roles.Length > 0)
                    {
                        policy.RequireRole(roles);
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// 获取默认的授权策略配置
        /// </summary>
        /// <returns>默认策略字典</returns>
        private static Dictionary<string, string[]> GetDefaultPolicies()
        {
            return new Dictionary<string, string[]>
            {
                ["AdminPolicy"] = new[] { "Admin" },
                ["DoctorPolicy"] = new[] { "Doctor" },
                ["DoctorOrAdminPolicy"] = new[] { "Doctor", "Admin" }
            };
        }

    }
}
