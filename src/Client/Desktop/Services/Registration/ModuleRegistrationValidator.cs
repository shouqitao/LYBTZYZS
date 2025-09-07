using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Services.Registration;

/// <summary>
/// 模块注册验证器
/// 在应用启动时验证所有预期的服务都已正确注册
/// </summary>
public class ModuleRegistrationValidator : IModuleServiceRegistrar
{
    private readonly ILogger<ModuleRegistrationValidator> _logger;
    private readonly List<string> _expectedModules = [
        "Auth", "User", "Patient", "Herb", "Formula",
        "Consultation", "Prescription", "MedicalCase"
    ];

    public ModuleRegistrationValidator(ILogger<ModuleRegistrationValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 注册指定模块的所有服务
    /// </summary>
    public void RegisterModuleServices<TModule>(IContainerRegistry registry) where TModule : class
    {
        var moduleType = typeof(TModule);
        var moduleName = ExtractModuleName(moduleType.Name);

        _logger.LogInformation("注册模块服务: {ModuleName}", moduleName);

        // 获取该模块的所有服务
        var moduleServices = ServiceDiscovery.GetModuleServices(moduleName);

        foreach (var service in moduleServices)
        {
            RegisterService(registry, service);
        }
    }

    /// <summary>
    /// 注册所有发现的模块服务
    /// </summary>
    public void RegisterAllDiscoveredServices(IContainerRegistry registry)
    {
        _logger.LogInformation("开始注册所有发现的模块服务");

        // 确保服务发现已完成
        ServiceDiscovery.ScanForModuleServices();

        var discoveredServices = ServiceDiscovery.GetDiscoveredServices();
        var serviceCount = 0;

        foreach (var service in discoveredServices)
        {
            try
            {
                RegisterService(registry, service);
                serviceCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册服务失败: {ServiceType} → {ImplementationType}",
                    service.ServiceType.Name, service.ImplementationType.Name);
                throw;
            }
        }

        _logger.LogInformation("自动注册完成，共注册 {Count} 个服务", serviceCount);

        // 验证所有预期模块都已注册
        ValidateExpectedModulesRegistered(discoveredServices);
    }

    /// <summary>
    /// 检查指定类型是否可以注册为服务
    /// </summary>
    public bool CanRegisterService(Type serviceType, Type implementationType)
    {
        var validation = ServiceDiscovery.ValidateServiceRegistration(serviceType, implementationType);
        return validation.IsValid;
    }

    /// <summary>
    /// 获取所有发现的服务注册信息
    /// </summary>
    public IEnumerable<ServiceRegistrationInfo> GetDiscoveredServices()
    {
        return ServiceDiscovery.GetDiscoveredServices();
    }

    /// <summary>
    /// 验证应用启动后所有服务都已正确注册
    /// </summary>
    /// <param name="containerProvider">容器提供器</param>
    /// <returns>验证结果</returns>
    public ValidationResult ValidateRegistrations(IContainerProvider containerProvider)
    {
        _logger.LogInformation("开始验证服务注册...");

        var result = new ValidationResult();
        var discoveredServices = ServiceDiscovery.GetDiscoveredServices();

        foreach (var service in discoveredServices)
        {
            try
            {
                // 尝试解析服务
                var instance = containerProvider.Resolve(service.ServiceType);
                if (instance != null)
                {
                    result.SuccessCount++;
                    _logger.LogDebug("服务解析成功: {ServiceType}", service.ServiceType.Name);
                }
                else
                {
                    result.FailedServices.Add($"服务 {service.ServiceType.Name} 解析为 null");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"服务 {service.ServiceType.Name} 解析失败: {ex.Message}";
                result.FailedServices.Add(errorMessage);
                _logger.LogError(ex, "服务解析失败: {ServiceType}", service.ServiceType.Name);
            }
        }

        result.TotalCount = discoveredServices.Count();

        if (result.IsAllSuccessful)
        {
            _logger.LogInformation("服务注册验证成功，所有 {Count} 个服务都可正常解析", result.SuccessCount);
        }
        else
        {
            _logger.LogWarning(
                "服务注册验证发现问题: {SuccessCount}/{TotalCount} 成功，{FailCount} 个失败",
                result.SuccessCount, result.TotalCount, result.FailedServices.Count);
        }

        return result;
    }

    /// <summary>
    /// 创建诊断报告
    /// </summary>
    /// <returns>诊断信息</returns>
    public string CreateDiagnosticReport()
    {
        var services = ServiceDiscovery.GetDiscoveredServices().ToList();
        var groupedByModule = services.GroupBy(s => s.ModuleName).OrderBy(g => g.Key);

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== 模块服务自动发现诊断报告 ===");
        report.AppendLine($"总计发现服务: {services.Count}");
        report.AppendLine($"涉及模块数量: {groupedByModule.Count()}");
        report.AppendLine();

        foreach (var moduleGroup in groupedByModule)
        {
            report.AppendLine($"模块: {moduleGroup.Key}");
            foreach (var service in moduleGroup.OrderBy(s => s.ServiceType.Name))
            {
                report.AppendLine($"  {service.ServiceType.Name} → {service.ImplementationType.Name} ({service.Lifetime})");
            }
            report.AppendLine();
        }

        return report.ToString();
    }

    private void RegisterService(IContainerRegistry registry, ServiceRegistrationInfo service)
    {
        _logger.LogDebug(
            "注册服务: {ServiceType} → {ImplementationType} ({Lifetime})",
            service.ServiceType.Name, service.ImplementationType.Name, service.Lifetime);

        switch (service.Lifetime)
        {
            case ServiceLifetime.Singleton:
                // 先注册实现类为单例
                registry.RegisterSingleton(service.ImplementationType);
                // 再注册接口到实现的映射
                registry.Register(service.ServiceType, container => container.Resolve(service.ImplementationType));
                break;

            case ServiceLifetime.Transient:
                registry.Register(service.ServiceType, service.ImplementationType);
                break;

            case ServiceLifetime.Scoped:
                registry.RegisterScoped(service.ServiceType, service.ImplementationType);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(service.Lifetime), service.Lifetime, "不支持的服务生命周期");
        }
    }

    private void ValidateExpectedModulesRegistered(IEnumerable<ServiceRegistrationInfo> discoveredServices)
    {
        var registeredModules = discoveredServices.Select(s => s.ModuleName).Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingModules = _expectedModules.Where(expected => !registeredModules.Contains(expected)).ToList();

        if (missingModules.Any())
        {
            _logger.LogWarning("以下预期模块未找到对应服务: {MissingModules}", string.Join(", ", missingModules));
        }
        else
        {
            _logger.LogInformation("所有预期模块都已发现对应服务");
        }
    }

    private static string ExtractModuleName(string typeName)
    {
        // {Module}Module → {Module}
        if (typeName.EndsWith("Module"))
        {
            return typeName[..^6];
        }
        return typeName;
    }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public List<string> FailedServices { get; set; } = [];

    public bool IsAllSuccessful => FailedServices.Count == 0 && TotalCount > 0;
    public int FailedCount => FailedServices.Count;
}
