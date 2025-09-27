using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LYBT.Module.Consultation.Health
{
    /// <summary>
    /// 诊疗模块健康检查
    /// </summary>
    public class ConsultationModuleHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // TODO: 实现实际的健康检查逻辑
            return Task.FromResult(HealthCheckResult.Healthy("诊疗模块运行正常"));
        }
    }
}