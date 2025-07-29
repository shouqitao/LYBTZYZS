using LYBT.Module.Billing.Interfaces;
using LYBT.Module.Billing.Repositories;
using LYBT.Module.Billing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Billing {

    /// <summary>
    /// 费用结算模块服务注册
    /// </summary>
    public static class BillingModule {

        /// <summary>
        /// 注册费用结算相关服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IBillingRepository, BillingRepository>();
            services.AddScoped<IBillingService, BillingService>();
        }
    }
}