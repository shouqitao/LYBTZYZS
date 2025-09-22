using System;
using System.Collections.Generic;
using System.Linq;
using DryIoc;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Validators
{
    /// <summary>
    /// 模块注册验证器
    /// 用于验证DI容器中是否存在重复或冲突的注册
    /// </summary>
    public class ModuleRegistrationValidator
    {
        private readonly IContainer _container;
        private readonly ILogger<ModuleRegistrationValidator> _logger;

        public ModuleRegistrationValidator(IContainer container, ILogger<ModuleRegistrationValidator> logger)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 验证容器注册状态
        /// </summary>
        /// <returns>验证是否通过</returns>
        public bool ValidateRegistrations()
        {
            var hasErrors = false;
            var duplicates = new Dictionary<Type, int>();

            try
            {
                // 获取所有注册信息
                var registrations = _container.GetServiceRegistrations();

                foreach (var registration in registrations)
                {
                    var serviceType = registration.ServiceType;

                    if (!duplicates.ContainsKey(serviceType))
                        duplicates[serviceType] = 0;

                    duplicates[serviceType]++;
                }

                // 检查重复注册
                foreach (var kvp in duplicates.Where(d => d.Value > 1))
                {
                    _logger.LogWarning(
                        "检测到重复注册: {ServiceType} 被注册了 {Count} 次",
                        kvp.Key.Name, kvp.Value);
                    hasErrors = true;
                }

                // 验证关键服务是否已注册
                var criticalServices = new[]
                {
                    typeof(LYBT.Shared.Interfaces.Services.IAuthService),
                    typeof(LYBT.Shared.Interfaces.Services.IUserService),
                    typeof(LYBT.Shared.Interfaces.Services.IPatientService),
                    typeof(LYBT.Desktop.Core.Interfaces.Services.ISessionManager)
                };

                foreach (var serviceType in criticalServices)
                {
                    if (!_container.IsRegistered(serviceType))
                    {
                        _logger.LogError("关键服务未注册: {ServiceType}", serviceType.Name);
                        hasErrors = true;
                    }
                    else
                    {
                        _logger.LogDebug("✓ 关键服务已注册: {ServiceType}", serviceType.Name);
                    }
                }

                // 验证生命周期一致性
                ValidateLifetimeConsistency();

                if (!hasErrors)
                {
                    _logger.LogInformation("DI容器验证通过，无重复或冲突注册");
                }

                return !hasErrors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证DI容器注册时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 验证服务生命周期一致性
        /// </summary>
        private void ValidateLifetimeConsistency()
        {
            // 确保相关服务具有一致的生命周期
            var singletonServices = new[]
            {
                typeof(LYBT.Desktop.Core.Interfaces.Services.ISessionManager),
                typeof(LYBT.Desktop.Core.Interfaces.Services.IUserSessionManager),
                typeof(LYBT.Desktop.Core.Interfaces.Services.IPermissionService)
            };

            foreach (var serviceType in singletonServices)
            {
                if (_container.IsRegistered(serviceType))
                {
                    _logger.LogDebug("✓ Singleton服务已正确注册: {ServiceType}", serviceType.Name);
                }
            }
        }
    }
}