using Microsoft.Extensions.Diagnostics.HealthChecks;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.HealthChecks
{
    /// <summary>
    /// 用户模块健康检查
    /// </summary>
    public class UsersModuleHealthCheck : IHealthCheck
    {
        private readonly IUserRepository _userRepository;
        
        public UsersModuleHealthCheck(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 简单检查：能否访问数据库
                var userCount = await _userRepository.CountAsync();
                
                return HealthCheckResult.Healthy(
                    $"用户模块正常运行，当前用户数: {userCount}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "用户模块数据库访问失败", 
                    ex);
            }
        }
    }
}