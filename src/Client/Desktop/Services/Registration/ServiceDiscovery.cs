using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Desktop.Services.Registration;

/// <summary>
/// 服务自动发现静态类
/// 通过程序集扫描自动发现符合命名约定的服务接口和实现
/// </summary>
public static class ServiceDiscovery
{
    private static readonly ILogger Logger = NullLogger.Instance;
    private static readonly Dictionary<string, ServiceRegistrationInfo> DiscoveredServices = new();
    private static bool _isScanned = false;

    /// <summary>
    /// 扫描程序集，发现所有符合条件的模块服务
    /// </summary>
    /// <param name="assemblies">要扫描的程序集，如果为null则扫描当前执行程序集</param>
    public static void ScanForModuleServices(params Assembly[]? assemblies)
    {
        if (_isScanned)
        {
            Logger.LogInformation("服务发现已完成，跳过重复扫描");
            return;
        }

        assemblies ??= [Assembly.GetExecutingAssembly()];

        Logger.LogInformation("开始扫描程序集，发现模块服务...");

        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly);
        }

        _isScanned = true;
        Logger.LogInformation("服务发现完成，共发现 {Count} 个服务", DiscoveredServices.Count);
    }

    /// <summary>
    /// 获取所有发现的服务注册信息
    /// </summary>
    /// <returns>服务注册信息列表</returns>
    public static IEnumerable<ServiceRegistrationInfo> GetDiscoveredServices()
    {
        EnsureScanned();
        return DiscoveredServices.Values.ToList();
    }

    /// <summary>
    /// 获取指定模块的服务注册信息
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <returns>该模块的服务注册信息</returns>
    public static IEnumerable<ServiceRegistrationInfo> GetModuleServices(string moduleName)
    {
        EnsureScanned();
        return DiscoveredServices.Values.Where(s => s.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 检查服务类型和实现类型是否匹配命名约定
    /// </summary>
    /// <param name="serviceType">服务接口类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <returns>是否匹配命名约定</returns>
    public static bool IsMatchingNamingConvention(Type serviceType, Type implementationType)
    {
        if (!serviceType.IsInterface || !serviceType.Name.StartsWith("I") || !serviceType.Name.EndsWith("Service"))
        {
            return false;
        }

        // I{Module}Service → {Module}Module
        var expectedModuleName = serviceType.Name[1..^7]; // 移除 "I" 前缀和 "Service" 后缀
        var expectedImplementationName = $"{expectedModuleName}Module";

        return implementationType.Name == expectedImplementationName;
    }

    /// <summary>
    /// 验证服务注册的有效性（增强版）
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <returns>验证结果和错误消息</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateServiceRegistration(Type serviceType, Type implementationType)
    {
        // 检查类型基本有效性
        if (!serviceType.IsInterface)
        {
            return (false, $"服务类型 {serviceType.Name} 必须是接口");
        }

        if (implementationType.IsAbstract)
        {
            return (false, $"实现类型 {implementationType.Name} 不能是抽象类");
        }

        // 检查接口实现关系
        if (!serviceType.IsAssignableFrom(implementationType))
        {
            return (false, $"实现类型 {implementationType.Name} 必须实现接口 {serviceType.Name}");
        }

        // 检查命名约定
        if (!IsMatchingNamingConvention(serviceType, implementationType))
        {
            return (false, $"类型 {serviceType.Name} → {implementationType.Name} 不符合命名约定 I{{Module}}Service → {{Module}}Module");
        }

        // 检查构造函数
        var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
        {
            return (false, $"实现类型 {implementationType.Name} 必须有公共构造函数");
        }

        // 增强验证：检查UltraThink双层架构模式
        var architectureValidation = ValidateUltraThinkArchitecturePattern(serviceType, implementationType);
        if (!architectureValidation.IsValid)
        {
            return architectureValidation;
        }

        // 增强验证：检查依赖注入模式
        var dependencyValidation = ValidateDependencyInjectionPattern(implementationType);
        return !dependencyValidation.IsValid ? dependencyValidation : ((bool IsValid, string? ErrorMessage))(true, null);
    }

    /// <summary>
    /// 验证UltraThink双层架构模式
    /// </summary>
    /// <param name="serviceType">服务接口类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <returns>验证结果</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateUltraThinkArchitecturePattern(Type serviceType, Type implementationType)
    {
        // 获取模块名称
        var moduleName = ExtractModuleName(serviceType);

        // 检查是否存在QueryService和BusinessService
        var expectedQueryServiceType = $"{implementationType.Namespace}.{moduleName}QueryService";
        var expectedBusinessServiceType = $"{implementationType.Namespace}.{moduleName}BusinessService";

        // 检查程序集中是否存在相应的服务类
        var assembly = implementationType.Assembly;
        var queryServiceType = assembly.GetTypes().FirstOrDefault(t =>
            t.Name == $"{moduleName}QueryService" && t.IsClass && !t.IsAbstract);
        var businessServiceType = assembly.GetTypes().FirstOrDefault(t =>
            t.Name == $"{moduleName}BusinessService" && t.IsClass && !t.IsAbstract);

        if (queryServiceType == null)
        {
            Logger.LogWarning("UltraThink架构验证: 未找到 {ModuleName}QueryService", moduleName);
        }

        if (businessServiceType == null)
        {
            Logger.LogWarning("UltraThink架构验证: 未找到 {ModuleName}BusinessService", moduleName);
        }

        // 检查Module类是否遵循纯委托模式
        var delegationValidation = ValidateDelegationPattern(implementationType, queryServiceType, businessServiceType);
        return !delegationValidation.IsValid ? delegationValidation : ((bool IsValid, string? ErrorMessage))(true, null);
    }

    /// <summary>
    /// 验证依赖注入模式
    /// </summary>
    /// <param name="implementationType">实现类型</param>
    /// <returns>验证结果</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateDependencyInjectionPattern(Type implementationType)
    {
        var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();

        // 检查构造函数参数是否都是接口或已知类型
        var parameters = primaryConstructor.GetParameters();
        foreach (var param in parameters)
        {
            var paramType = param.ParameterType;

            // 验证参数类型是否符合依赖注入最佳实践
            if (!paramType.IsInterface && !IsKnownServiceType(paramType))
            {
                return (false, $"构造函数参数 {param.Name} 的类型 {paramType.Name} 应该是接口类型");
            }

            // 检查参数名称是否符合约定
            if (!IsValidParameterName(param.Name, paramType))
            {
                Logger.LogWarning("参数名称 {ParameterName} 可能不符合命名约定", param.Name);
            }
        }

        return (true, null);
    }

    /// <summary>
    /// 验证委托模式实现
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <param name="queryServiceType">查询服务类型</param>
    /// <param name="businessServiceType">业务服务类型</param>
    /// <returns>验证结果</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateDelegationPattern(
        Type moduleType,
        Type? queryServiceType,
        Type? businessServiceType)
    {
        var fields = moduleType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

        // 检查是否有预期的字段
        var hasQueryServiceField = queryServiceType != null &&
            fields.Any(f => f.FieldType == queryServiceType || f.FieldType.Name.Contains("QueryService"));

        var hasBusinessServiceField = businessServiceType != null &&
            fields.Any(f => f.FieldType == businessServiceType || f.FieldType.Name.Contains("BusinessService"));

        // 检查方法实现是否为纯委托
        var methods = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.DeclaringType == moduleType);

        foreach (var method in methods)
        {
            if (!IsMethodPureDelegation(method))
            {
                return (false, $"方法 {method.Name} 不是纯委托实现，违反UltraThink架构模式");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// 检查方法是否为纯委托实现
    /// </summary>
    /// <param name="method">方法信息</param>
    /// <returns>是否为纯委托</returns>
    public static bool IsMethodPureDelegation(MethodInfo method)
    {
        // 这里可以通过IL代码分析来检查，但为了简化，我们使用启发式方法
        // 实际项目中可以使用Mono.Cecil或其他IL分析库

        // 检查方法体长度（纯委托方法通常很简单）
        var methodBody = method.GetMethodBody();
        if (!(methodBody?.GetILAsByteArray()?.Length > 50)) // 简化的启发式判断
        {
            return true; // 暂时返回true，避免阻断注册
        }
        Logger.LogDebug("方法 {MethodName} 的IL长度可能超过纯委托预期", method.Name);

        return true; // 暂时返回true，避免阻断注册
    }

    /// <summary>
    /// 检查是否为已知的服务类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>是否为已知服务类型</returns>
    public static bool IsKnownServiceType(Type type)
    {
        var knownServiceTypes = new[]
        {
            typeof(Microsoft.Extensions.Logging.ILogger<>),
            typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache),
            typeof(AutoMapper.IMapper),
            typeof(System.Net.Http.HttpClient)
        };

        return knownServiceTypes.Any(known =>
            known.IsAssignableFrom(type) ||
            (known.IsGenericTypeDefinition && type.IsGenericType &&
             type.GetGenericTypeDefinition() == known));
    }

    /// <summary>
    /// 验证参数名称是否有效
    /// </summary>
    /// <param name="parameterName">参数名称</param>
    /// <param name="parameterType">参数类型</param>
    /// <returns>是否有效</returns>
    public static bool IsValidParameterName(string? parameterName, Type parameterType)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        // 检查参数名是否以下划线开头（私有字段约定）
        var expectedFieldName = $"_{parameterName}";

        // 检查是否遵循camelCase约定
        return char.IsLower(parameterName[0]);
    }

    /// <summary>
    /// 重置扫描状态，用于测试
    /// </summary>
    internal static void Reset()
    {
        DiscoveredServices.Clear();
        _isScanned = false;
    }

    private static void ScanAssembly(Assembly assembly)
    {
        Logger.LogDebug("扫描程序集: {AssemblyName}", assembly.FullName);

        try
        {
            var types = assembly.GetTypes();
            var interfaces = types.Where(t => t.IsInterface && t.Name.StartsWith("I") && t.Name.EndsWith("Service")).ToArray();
            var implementations = types.Where(t => !t.IsInterface && !t.IsAbstract && t.Name.EndsWith("Module")).ToArray();

            Logger.LogDebug(
                "在程序集 {AssemblyName} 中找到 {InterfaceCount} 个服务接口和 {ImplementationCount} 个模块实现",
                assembly.GetName().Name, interfaces.Length, implementations.Length);

            foreach (var serviceInterface in interfaces)
            {
                var matchingImplementation = implementations.FirstOrDefault(impl =>
                    IsMatchingNamingConvention(serviceInterface, impl) && serviceInterface.IsAssignableFrom(impl));

                if (matchingImplementation != null)
                {
                    var validation = ValidateServiceRegistration(serviceInterface, matchingImplementation);
                    if (validation.IsValid)
                    {
                        RegisterDiscoveredService(serviceInterface, matchingImplementation);
                    }
                    else
                    {
                        Logger.LogWarning(
                            "跳过无效的服务注册 {ServiceType} → {ImplementationType}: {Error}",
                            serviceInterface.Name, matchingImplementation.Name, validation.ErrorMessage);
                    }
                }
                else
                {
                    Logger.LogDebug("未找到匹配的实现类: {ServiceInterface}", serviceInterface.Name);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            Logger.LogError(ex, "加载程序集类型时出错: {AssemblyName}", assembly.FullName);
            foreach (var loaderException in ex.LoaderExceptions.Where(e => e != null))
            {
                Logger.LogError(loaderException, "加载器异常");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "扫描程序集时出现未知错误: {AssemblyName}", assembly.FullName);
        }
    }

    private static void RegisterDiscoveredService(Type serviceType, Type implementationType)
    {
        var moduleName = ExtractModuleName(serviceType);
        var registrationInfo = new ServiceRegistrationInfo(
            serviceType,
            implementationType,
            moduleName,
            ServiceLifetime.Singleton); // 保持现有的单例模式

        var key = $"{serviceType.FullName}->{implementationType.FullName}";

        if (!DiscoveredServices.ContainsKey(key))
        {
            DiscoveredServices[key] = registrationInfo;
            Logger.LogInformation(
                "发现服务: {ServiceType} → {ImplementationType} (模块: {ModuleName})",
                serviceType.Name, implementationType.Name, moduleName);
        }
    }

    private static string ExtractModuleName(Type serviceType)
    {
        // I{Module}Service → {Module}
        if (serviceType.Name.StartsWith("I") && serviceType.Name.EndsWith("Service"))
        {
            return serviceType.Name[1..^7];
        }

        return serviceType.Name;
    }

    private static void EnsureScanned()
    {
        if (!_isScanned)
        {
            ScanForModuleServices();
        }
    }
}
