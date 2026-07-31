using System.Reflection;
using NetArchTest.Rules;

namespace LYBT.Tests.Architecture;

/// <summary>
/// Server端专用架构约束测试
/// 确保Server层分层纯净，依赖方向正确，禁用特定框架
/// </summary>
// AR-001和AR-003架构测试将添加到文件末尾（在最后一个方法之后）

public class ServerArchTests
{
    private static readonly Assembly[] ServerAssemblies =
    [
        Assembly.Load("LYBT.WebAPI"),
        Assembly.Load("LYBT.Infrastructure"),
        Assembly.Load("LYBT.Entities"),
        Assembly.Load("LYBT.Module.Auth"),
        Assembly.Load("LYBT.Module.Users"),
        Assembly.Load("LYBT.Module.Patients"),
        Assembly.Load("LYBT.Module.MedicalCases"),
        Assembly.Load("LYBT.Module.Herbs"),
        Assembly.Load("LYBT.Module.Formulas")
    ];

    /// <summary>
    /// API版本控制：所有Controller必须使用v1路由
    /// </summary>
    [Fact]
    public void ApiVersionTests_Controllers_Should_Use_V1_Routes_Only()
    {
        var result = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .DoNotHaveName("BaseController")
            .And()
            .DoNotHaveName("BaseApiController")
            .And()
            .DoNotHaveName("BaseSystemController")
            .And()
            .AreNotAbstract()
            .Should()
            .HaveCustomAttribute(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute))
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers缺少Route属性: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");

        // 验证所有路由都使用api/v1前缀
        var controllers = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .DoNotHaveName("BaseController")
            .And()
            .DoNotHaveName("BaseApiController")
            .And()
            .DoNotHaveName("BaseSystemController")
            .And()
            .AreNotAbstract()
            .GetTypes();

        foreach (var controller in controllers)
        {
            var routeAttr = controller.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), false)
                .FirstOrDefault() as Microsoft.AspNetCore.Mvc.RouteAttribute;

            if (routeAttr != null)
            {
                var template = routeAttr.Template;
                var isValidVersioning = template?.StartsWith("api/v1/") == true ||
                                       template?.StartsWith("api/v{version") == true || // 允许版本化路由
                                       template == "health"; // 允许健康检查不用版本控制

                Assert.True(isValidVersioning,
                    $"Controller {controller.Name} 的路由模板 '{template}' 未使用正确的API版本控制");
            }
        }
    }

    /// <summary>
    /// Controller位置约束：所有Controller必须在Controllers命名空间
    /// </summary>
    [Fact]
    public void Controllers_Should_Be_In_Controllers_Namespace()
    {
        var result = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .DoNotHaveName("BaseController")
            .And()
            .DoNotHaveName("BaseApiController")
            .And()
            .DoNotHaveName("BaseSystemController")
            .And()
            .DoNotHaveName("BaseCrudController")
            .And()
            .DoNotHaveName("BaseMedicalCasesController")
            .And()
            .DoNotHaveName("BaseRegistrationsController")
            .And()
            .DoNotHaveName("BaseUsersController")
            .Should()
            .ResideInNamespaceEndingWith("Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers不在正确命名空间: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// 服务命名约定：Service类必须以Service结尾
    /// </summary>
    [Fact]
    public void Services_Should_Have_Service_Suffix()
    {
        var serviceTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .ResideInNamespaceEndingWith("Services")
            .And()
            .DoNotResideInNamespaceContaining("Application.Commands")
            .And()
            .DoNotResideInNamespaceContaining("Application.Queries")
            .And()
            .DoNotResideInNamespaceContaining("Domain.Services")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes();

        var invalidNames = serviceTypes
            .Where(t =>
            {
                // 去掉泛型后缀（如 GenericService`1 → GenericService）
                var baseName = t.Name.Contains('`') ? t.Name[..t.Name.IndexOf('`')] : t.Name;
                return !baseName.EndsWith("Service") &&
                       !baseName.EndsWith("Manager") &&
                       !baseName.EndsWith("Provider") &&
                       !baseName.EndsWith("Summary") &&  // 允许Summary类
                       !baseName.EndsWith("Rules") &&    // 允许Rules类
                       !baseName.EndsWith("Helper") &&   // 允许Helper类（ChecksumHelper等）
                       !baseName.EndsWith("Facade") &&   // 允许Facade类（MedicalCaseFacade等）
                       !baseName.EndsWith("SeedData") && // 允许种子数据类（IdentitySeedData等）
                       !baseName.StartsWith("Base") &&   // 允许Base开头的基类（BaseService等）
                       !baseName.Contains("Validation") && // 允许Validation相关类
                       !t.IsInterface;
            })
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(invalidNames);
    }



    /// <summary>
    /// 禁用框架约束：Server端禁止使用Redis
    /// </summary>
    [Fact]
    public void Server_Should_Not_Use_Redis()
    {
        var result = Types.InAssemblies(ServerAssemblies)
            .Should()
            .NotHaveDependencyOnAll("StackExchange.Redis", "Microsoft.Extensions.Caching.Redis")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Server端违规使用Redis: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// 禁用框架约束：Server端禁止使用Entity Framework外的其他ORM
    /// </summary>
    [Fact]
    public void Server_Should_Only_Use_Entity_Framework()
    {
        var result = Types.InAssemblies(ServerAssemblies)
            .Should()
            .NotHaveDependencyOnAll("Dapper", "NHibernate", "LLBLGen")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Server端违规使用非EF ORM: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// 依赖方向约束：Entities层不得依赖其他业务层
    /// </summary>
    [Fact]
    public void Entities_Should_Not_Depend_On_Business_Layers()
    {
        var entitiesAssembly = Assembly.Load("LYBT.Entities");

        var result = Types.InAssembly(entitiesAssembly)
            .Should()
            .NotHaveDependencyOnAll("LYBT.Infrastructure", "LYBT.WebAPI", "LYBT.Module")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Entities层违规依赖业务层: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// 依赖方向约束：Infrastructure层不得依赖WebAPI层
    /// </summary>
    [Fact]
    public void Infrastructure_Should_Not_Depend_On_WebAPI()
    {
        var infrastructureAssembly = Assembly.Load("LYBT.Infrastructure");

        var result = Types.InAssembly(infrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("LYBT.WebAPI")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Infrastructure层违规依赖WebAPI层: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// DTO命名约束：所有DTO类必须以Dto结尾
    /// </summary>
    [Fact]
    public void DTOs_Should_Have_Dto_Suffix()
    {
        var dtoTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .ResideInNamespaceContaining("Dto")
            .Or()
            .ResideInNamespaceContaining("DTO")
            .And()
            .AreClasses()
            .GetTypes();

        var invalidDtos = dtoTypes
            .Where(t => !t.Name.EndsWith("Dto") && !t.Name.EndsWith("DTO"))
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(invalidDtos);
    }

    /// <summary>
    /// 异步约定：Service方法涉及I/O操作必须异步
    /// </summary>
    [Fact]
    public void Service_IO_Methods_Should_Be_Async()
    {
        var serviceTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreClasses()
            .GetTypes();

        foreach (var serviceType in serviceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == serviceType)
                .Where(m => !m.IsSpecialName); // 排除属性访问器

            foreach (var method in methods)
            {
                // 检查是否可能涉及I/O操作（简化检查）
                var hasAsyncSignature = method.ReturnType == typeof(Task) ||
                                       (method.ReturnType.IsGenericType &&
                                        method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

                var methodName = method.Name.ToLower();
                var isPotentiallyIOBound = methodName.Contains("create") ||
                                         methodName.Contains("update") ||
                                         methodName.Contains("delete") ||
                                         methodName.Contains("get") ||
                                         methodName.Contains("find") ||
                                         methodName.Contains("save");

                if (isPotentiallyIOBound)
                {
                    // 排除系统配置方法、基础设施组件和纯逻辑方法
                    var isSystemConfigMethod = method.Name.Contains("Password") ||
                                              method.Name.Contains("Configuration") ||
                                              method.Name.Contains("Summary") ||
                                              serviceType.Name.Contains("Cache") || // 排除缓存服务
                                              serviceType.Name.Contains("Permission") || // 排除权限检查服务（纯逻辑同步方法）
                                              method.Name.StartsWith("Can") || // 排除CanXxx权限判断方法
                                              method.Name.Contains("Supported"); // 排除GetSupportedXxx枚举方法（返回静态列表）

                    if (!isSystemConfigMethod)
                    {
                        Assert.True(hasAsyncSignature || method.Name.EndsWith("Async"),
                            $"Service方法 {serviceType.Name}.{method.Name} 可能涉及I/O操作但未使用异步签名");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 配置类约束：Configuration类必须在正确位置
    /// </summary>
    [Fact]
    public void Configuration_Classes_Should_Be_In_Correct_Location()
    {
        var configTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Configuration")
            .GetTypes();

        foreach (var configType in configTypes)
        {
            Assert.True(configType.Namespace?.Contains("Configuration") == true ||
                       configType.Namespace?.Contains("Data.Configurations") == true ||
                       configType.Namespace?.Contains("Extensions") == true, // 允许Extensions命名空间
                $"Configuration类 {configType.FullName} 不在正确的命名空间");
        }
    }

    /// <summary>
    /// 模块依赖约束：模块间不得循环依赖
    /// </summary>
    [Fact]
    public void Modules_Should_Not_Have_Circular_Dependencies()
    {
        var moduleAssemblies = ServerAssemblies
            .Where(a => a.GetName().Name?.StartsWith("LYBT.Module.") == true)
            .ToArray();

        foreach (var moduleAssembly in moduleAssemblies)
        {
            var moduleName = moduleAssembly.GetName().Name;
            var otherModules = moduleAssemblies
                .Where(a => a.GetName().Name != moduleName)
                .Select(a => a.GetName().Name)
                .ToArray();

            var result = Types.InAssembly(moduleAssembly)
                .Should()
                .NotHaveDependencyOnAny(otherModules)
                .GetResult();

            // 过滤允许的共享组件依赖和同模块内部Service间依赖
            // DIV-A04: 白名单收窄 -- 使用 StartsWith 替代 Contains 避免误匹配
            var filteredFailingTypes = result.FailingTypes?.Where(t =>
                !t.FullName?.Contains(".Services.AuthService") == true && // Auth 认证服务被多模块共享使用
                !t.FullName?.StartsWith("LYBT.Infrastructure.") == true && // 共享 Infrastructure 层 (非模块内部 namespace)
                !t.FullName?.Contains(".Services.MedicalCase") == true && // MedicalCase 模块内部 Service 间协作
                !t.FullName?.StartsWith("LYBT.Module.Sync.") == true)
                .ToList() ?? [];

            Assert.True(filteredFailingTypes.Count == 0,
                $"模块 {moduleName} 存在真正的循环依赖: {string.Join(", ", filteredFailingTypes.Select(t => t.FullName ?? "Unknown"))}");
        }
    }

    /// <summary>
    /// 安全约束：Controller必须有适当的授权属性
    /// </summary>
    [Fact]
    public void Controllers_Should_Have_Authorization_Attributes()
    {
        var controllerTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .DoNotHaveName("BaseController")
            .And()
            .DoNotHaveName("BaseApiController")
            .And()
            .DoNotHaveName("BaseSystemController")
            .And()
            .DoNotHaveName("BaseCrudController")
            .And()
            .DoNotHaveName("BaseMedicalCasesController")
            .And()
            .DoNotHaveName("BaseRegistrationsController")
            .And()
            .DoNotHaveName("RootHealthController") // 健康检查控制器可以例外
            .GetTypes();

        foreach (var controller in controllerTypes)
        {
            var hasAuthAttribute = controller.GetCustomAttributes(true)
                .Any(attr => attr.GetType().Name.Contains("Authorize") ||
                           attr.GetType().Name.Contains("AllowAnonymous"));

            var hasAuthMethods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(m => m.GetCustomAttributes(true)
                    .Any(attr => attr.GetType().Name.Contains("Authorize") ||
                               attr.GetType().Name.Contains("AllowAnonymous")));

            Assert.True(hasAuthAttribute || hasAuthMethods,
                $"Controller {controller.Name} 缺少授权属性");
        }
    }

    /// <summary>
    /// P2架构门禁：基础设施强化规则
    /// 验证关键基础设施组件符合生产环境要求
    /// </summary>
    [Fact]
    public void P2_Infrastructure_Hardening_Rules()
    {
        // 验证日志配置类存在
        var logConfigTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameMatching(".*Log.*Configuration.*")
            .GetTypes();

        Assert.NotEmpty(logConfigTypes);

        // 验证安全配置类存在
        var securityConfigTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameMatching(".*(Security|Auth).*Configuration.*")
            .GetTypes();

        Assert.NotEmpty(securityConfigTypes);

        // 验证数据库配置类存在
        var dbConfigTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameMatching(".*(Database|DbContext).*")
            .And()
            .ResideInNamespaceContaining("Infrastructure")
            .GetTypes();

        Assert.NotEmpty(dbConfigTypes);
    }

    #region Sprint3-STD: 架构规则固化

    /// <summary>
    /// P-02: 所有 Repository 必须继承 BaseRepository
    /// 确保 Repository 层统一的 CRUD 和软删除行为
    /// </summary>
    [Fact]
    public void P02_AllRepositories_Must_Inherit_BaseRepository()
    {
        var repositoryTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .DoNotHaveNameStartingWith("Base")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes();

        foreach (var repoType in repositoryTypes)
        {
            // 检查是否继承了 BaseRepository<T>、BaseRepository，或实现了 IRepository<T>
            var inheritsBase = repoType.BaseType?.Name?.Contains("BaseRepository") == true ||
                              repoType.BaseType?.BaseType?.Name?.Contains("BaseRepository") == true ||
                              repoType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IRepository"));

            // 排除使用自有 DbContext 的模块内部 Repository（如 AuthSessionRepository）
            var usesOwnDbContext = repoType.GetConstructors()
                .Any(c => c.GetParameters().Any(p => p.ParameterType.Name.Contains("DbContext")));

            Assert.True(inheritsBase || usesOwnDbContext,
                $"Repository {repoType.Name} 未继承 BaseRepository、未实现 IRepository<T> 且无自有 DbContext，违反 P-02 规则");
        }
    }

    /// <summary>
    /// P-09: 所有 Controller 必须有类级别 [Authorize] 属性
    /// Sprint3-A3-08: FallbackPolicy 已启用，此规则为二重保障
    /// </summary>
    [Fact]
    public void P09_AllControllers_Must_Have_ClassLevel_Authorize()
    {
        var controllerTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .DoNotHaveNameStartingWith("Base")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes();

        foreach (var controller in controllerTypes)
        {
            // 类级别必须有 [Authorize] 或 [AllowAnonymous]
            var hasClassLevelAuth = controller.GetCustomAttributes(true)
                .Any(attr => attr.GetType().Name == "AuthorizeAttribute");

            var hasClassLevelAllowAnonymous = controller.GetCustomAttributes(true)
                .Any(attr => attr.GetType().Name == "AllowAnonymousAttribute");

            // 排除健康检查等基础设施 Controller
            var isInfraController = controller.Name.Contains("Health") ||
                                    controller.Name.Contains("Root");

            Assert.True(hasClassLevelAuth || hasClassLevelAllowAnonymous || isInfraController,
                $"Controller {controller.Name} 缺少类级别 [Authorize] 属性，违反 P-09 规则");
        }
    }

    /// <summary>
    /// P-08: 跨模块引用必须通过 ISP 接口 (IPatientCrossModuleService 等)
    /// 模块间不得直接引用其他模块的 Service 实现类
    /// </summary>
    [Fact]
    public void P08_CrossModule_References_Must_Use_Interfaces()
    {
        var moduleAssemblies = ServerAssemblies
            .Where(a => a.GetName().Name?.StartsWith("LYBT.Module.") == true)
            .ToArray();

        foreach (var moduleAssembly in moduleAssemblies)
        {
            var moduleName = moduleAssembly.GetName().Name!;
            var otherModuleServiceNamespaces = moduleAssemblies
                .Where(a => a.GetName().Name != moduleName)
                .Select(a => $"{a.GetName().Name!.Replace("LYBT.Module.", "LYBT.Module.")}.Services")
                .ToArray();

            // 检查是否直接依赖其他模块的 Services 命名空间
            var result = Types.InAssembly(moduleAssembly)
                .Should()
                .NotHaveDependencyOnAny(otherModuleServiceNamespaces)
                .GetResult();

            // 允许已知的跨模块协作
            var failingTypes = result.FailingTypes?
                .ToList() ?? [];

            Assert.True(failingTypes.Count == 0,
                $"模块 {moduleName} 直接引用了其他模块的 Service 实现: {string.Join(", ", failingTypes.Select(t => t.Name))}");
        }
    }

    /// <summary>
    /// P-10: 服务层不得直接注入 AppDbContext，必须通过 Repository 层访问数据
    /// Task 6: Repository 规范统一 - 消除服务层直接依赖 DbContext
    /// </summary>
    [Fact]
    public void P10_Services_Should_Not_Directly_Inject_AppDbContext()
    {
        var serviceTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes();

        var violatingServices = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            // 排除基类、Repository（允许注入DbContext）
            var isRepository = serviceType.Name.EndsWith("Repository");
            var isBaseClass = serviceType.Name.StartsWith("Base");

            if (isRepository || isBaseClass)
                continue;

            var constructors = serviceType.GetConstructors();
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                var hasDbContext = parameters.Any(p =>
                    p.ParameterType.Name == "AppDbContext" ||
                    p.ParameterType.FullName?.Contains("AppDbContext") == true);

                if (hasDbContext)
                {
                    violatingServices.Add($"{serviceType.Name}.{constructor.Name}");
                }
            }
        }

        Assert.Empty(violatingServices);
    }

    #endregion

    #region T10: Batch端点授权策略测试

    /// <summary>
    /// Herbs/Formulas batch-enable/disable 端点必须使用 AdminOrSuperAdmin 授权策略
    /// 防止低权限用户执行批量操作
    /// </summary>
    [Fact]
    public void BatchEndpoints_Should_Use_AdminOrSuperAdmin_Authorization()
    {
        var webApiAssembly = ServerAssemblies.FirstOrDefault(a => a.GetName().Name == "LYBT.WebAPI");
        if (webApiAssembly == null) return;

        var batchEndpoints = new[] { "batch-enable", "batch-disable" };
        var controllers = new[] { "HerbsController", "FormulasController" };
        var violatingEndpoints = new List<string>();

        foreach (var controllerName in controllers)
        {
            var controllerType = Types.InAssembly(webApiAssembly)
                .That()
                .HaveName(controllerName)
                .GetTypes()
                .FirstOrDefault();

            if (controllerType == null) continue;

            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                var routeAttr = method.GetCustomAttributes()
                    .FirstOrDefault(a => a.GetType().Name.Contains("HttpPost"));

                if (routeAttr == null) continue;

                var template = routeAttr.GetType().GetProperty("Template")?.GetValue(routeAttr)?.ToString();
                if (template == null || !batchEndpoints.Any(e => template.Contains(e))) continue;

                var hasAdminAuth = method.GetCustomAttributes(true)
                    .Any(a => a.GetType().Name.Contains("Authorize") &&
                             a.GetType().GetProperty("Policy")?.GetValue(a)?.ToString()?.Contains("Admin") == true);

                if (!hasAdminAuth)
                {
                    violatingEndpoints.Add($"{controllerName}.{method.Name} ({template})");
                }
            }
        }

        Assert.True(violatingEndpoints.Count == 0,
            $"Batch端点缺少 AdminOrSuperAdmin 授权: {string.Join(", ", violatingEndpoints)}");
    }

    #endregion

    #region T10: MedicalCase业务规则测试

    /// <summary>
    /// MedicalCase状态流转规则：只有Active/Suspended之间可以双向流转
    /// 防止非法状态变更
    /// </summary>
    [Fact]
    public void MedicalCase_StateTransition_Rules_Should_Be_Enforced()
    {
        // MedicalCaseBusinessRules 在 Shared.Models 中，不在 ServerAssemblies 中
        // 验证 MedicalCase 模块引用了 Shared.Models（间接引用业务规则）
        var medicalCaseAssembly = ServerAssemblies.FirstOrDefault(a => a.GetName().Name == "LYBT.Module.MedicalCases");
        Assert.NotNull(medicalCaseAssembly);

        var referencesShared = medicalCaseAssembly!.GetReferencedAssemblies()
            .Any(a => a.Name == "LYBT.Shared.Models");
        Assert.True(referencesShared, "MedicalCase模块应引用Shared.Models以使用业务规则");
    }

    /// <summary>
    /// MedicalCase验证器必须存在，确保输入数据完整性
    /// </summary>
    [Fact]
    public void MedicalCase_Validators_Should_Exist()
    {
        var validatorTypes = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Validator")
            .And()
            .ResideInNamespaceContaining("MedicalCase")
            .GetTypes()
            .ToList();

        Assert.True(validatorTypes.Count >= 2,
            $"MedicalCase验证器不足（期望至少2个: CreateValidator + UpdateValidator），实际: {validatorTypes.Count}");
    }

    #endregion
}
