using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LYBT.Module.Users.HealthChecks
{
    public class UsersModuleHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Users module is healthy"));
        }
    }
}
