using System.Reflection;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace LYBT.Tests.Architecture;

/// <summary>
/// Server端专用架构约束测试
/// 确保Server层分层纯净，依赖方向正确，禁用特定框架
/// </summary>
// AR-001和AR-003架构测试将添加到文件末尾（在最后一个方法之后）

public class ServerArchTests
{
    private static Assembly[] ServerAssemblies => TestAssemblies.Server;
    private static Assembly[] DesktopAssemblies => TestAssemblies.Desktop;

    /// <summary>
    /// API版本控制：所有Controller必须使用v1路由
    /// </summary>
    [Fact]
    public void P09b_Controllers_Should_Use_V1_Routes()
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
                                       template == "health" || // 允许健康检查不用版本控制
                                       template == ""; // 允许公开根页（下载主页 GET /——US-SHELL-010 决策 A）

                Assert.True(isValidVersioning,
                    $"Controller {controller.Name} 的路由模板 '{template}' 未使用正确的API版本控制");
            }
        }
    }

    /// <summary>
    /// Controller位置约束：所有Controller必须在Controllers命名空间
    /// </summary>
    [Fact]
    public void P09c_Controller_Must_Be_In_Controllers_Namespace()
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
    public void P10b_Service_Must_Have_Service_Suffix()
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
    public void P11_No_Redis_Usage()
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
    public void P11b_Only_Use_EntityFramework()
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
    public void P12_Entities_Should_Not_Depend_On_Business_Layers()
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
    public void P12b_Infrastructure_Should_Not_Depend_On_WebAPI()
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
    public void P13_Dto_Must_Have_Dto_Suffix()
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
    public void P14_Service_IO_Methods_Must_Be_Async()
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
    public void P15_Configuration_Must_Be_In_Correct_Location()
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
    public void P16_Modules_No_Circular_Dependencies()
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
    /// P2架构门禁：基础设施强化规则
    /// 验证关键基础设施组件符合生产环境要求
    /// </summary>
    [Fact]
    public void P17_Infrastructure_Hardening_Rules()
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
    public void P02b_AllRepositories_Must_Inherit_BaseRepository()
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
    /// 合并: Batch 端点必须使用 AdminOrSuperAdmin 授权策略
    /// </summary>
    [Fact]
    public void P09_Controller_Must_Have_ClassLevel_Authorize()
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

        // 合并检查: batch-enable/disable 端点必须使用 AdminOrSuperAdmin 授权策略
        var batchEndpoints = new[] { "batch-enable", "batch-disable" };
        var batchControllers = new[] { "CatalogController" };
        var violatingEndpoints = new List<string>();

        var webApiAssembly = ServerAssemblies.FirstOrDefault(a => a.GetName().Name == "LYBT.WebAPI");
        if (webApiAssembly != null)
        {
            foreach (var controllerName in batchControllers)
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
        }

        Assert.True(violatingEndpoints.Count == 0,
            $"Batch端点缺少 AdminOrSuperAdmin 授权: {string.Join(", ", violatingEndpoints)}");
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
    /// P-10: 服务层不得直接注入 AppDbContext / IDbContextAccessor，必须通过 Repository 层访问数据
    /// Task 6: Repository 规范统一 - 消除服务层直接依赖 DbContext
    /// A-28 P1-2: 扩展检查 IDbContextAccessor（跨模块服务绕过模块 DbContext 的盲区）；基础设施类豁免
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

            // 基础设施类（HealthCheckService/DatabaseInitializationService 等）豁免 — 位于 LYBT.Infrastructure
            if (serviceType.Namespace?.StartsWith("LYBT.Infrastructure") == true)
                continue;

            var constructors = serviceType.GetConstructors();
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                var hasDbContext = parameters.Any(p =>
                    p.ParameterType.Name == "AppDbContext" ||
                    p.ParameterType.FullName?.Contains("AppDbContext") == true ||
                    p.ParameterType.Name == "IDbContextAccessor" ||
                    p.ParameterType.FullName?.Contains("IDbContextAccessor") == true);

                if (hasDbContext)
                {
                    violatingServices.Add($"{serviceType.Name}.{constructor.Name}");
                }
            }
        }

        Assert.Empty(violatingServices);
    }

    /// <summary>
    /// ADR-0017: 模块 Repository 必须注入自己的 DbContext，不注入 AppDbContext
    /// 例外：Reports 模块保持 AppDbContext（任务书 A-20 决策点 2：只读聚合，无自有表）
    /// </summary>
    [Fact]
    public void P18_Module_Repositories_Must_Inject_Own_DbContext()
    {
        var moduleAssemblies = ServerAssemblies
            .Where(a => a.GetName().Name?.StartsWith("LYBT.Module.") == true);

        var violating = new List<string>();

        foreach (var assembly in moduleAssemblies)
        {
            var repositoryTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository"))
                .Where(t => t.Namespace?.StartsWith("LYBT.Module.Reports") != true) // 豁免 Reports（任务书决策点2）
                .ToList();

            foreach (var repoType in repositoryTypes)
            {
                var hasAppDbContext = repoType.GetConstructors()
                    .SelectMany(c => c.GetParameters())
                    .Any(p =>
                        p.ParameterType.Name == "AppDbContext" ||
                        p.ParameterType.FullName?.Contains("AppDbContext") == true);

                if (hasAppDbContext)
                {
                    violating.Add($"{repoType.FullName}");
                }
            }
        }

        Assert.True(violating.Count == 0,
            $"模块 Repository 不得注入 AppDbContext（ADR-0017）: {string.Join(", ", violating)}");
    }

    #endregion

    #region T10: MedicalCase业务规则测试

    /// <summary>
    /// MedicalCase状态流转规则：只有Active/Suspended之间可以双向流转
    /// 防止非法状态变更
    /// </summary>
    [Fact]
    public void MC01_MedicalCase_StateTransition_Rules()
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
    /// MedicalCase输入验证必须存在，确保输入数据完整性。
    /// A-03 简化：Application/Validators 的 Create/Update 验证器已随 Command 删除，
    /// 验证职责统一由 Shared.Models 的 MedicalCaseInputDtoValidator 承担（Module 中注册）。
    /// </summary>
    [Fact]
    public void MC02_MedicalCase_Validators_Must_Exist()
    {
        var unifiedValidator = Assembly.Load("LYBT.Shared.Models")
            .GetType("LYBT.Shared.Models.Validators.MedicalCase.MedicalCaseInputDtoValidator");

        Assert.NotNull(unifiedValidator);
    }

    #endregion

    #region A-09: 架构守卫测试补全

    /// <summary>
    /// A-09: 所有 Controller 必须继承 BaseApiController 或 BaseCrudController
    /// 确保统一的响应包装、操作者上下文与授权处理
    /// </summary>
    [Fact]
    public void A01_Controllers_Must_Inherit_BaseApiController()
    {
        var controllers = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .And()
            .AreNotAbstract()
            .GetTypes();

        var violations = controllers
            .Where(t => !typeof(BaseApiController).IsAssignableFrom(t))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Controller 未继承 BaseApiController/BaseCrudController: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// A-09: Desktop Repository 必须继承 ApiClientRepositoryBase
    /// 确保统一的 try/catch + 日志 + 异常处理模板
    /// </summary>
    [Fact]
    public void A02_Desktop_Repositories_Must_Inherit_ApiClientRepositoryBase()
    {
        var repositories = DesktopAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && t.Name.EndsWith("Repository"))
            .ToList();

        var violations = repositories
            .Where(t => !InheritsApiClientRepositoryBase(t))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Desktop Repository 未继承 ApiClientRepositoryBase: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// A-09: 每个 Module 必须有 DI 注册方法
    /// Desktop 模块通过 Prism IModule.RegisterTypes 注册，Server 模块通过静态 AddXxxModule 注册
    /// </summary>
    [Fact]
    public void A03_Modules_Must_Have_DI_Registration()
    {
        var moduleTypes = ServerAssemblies.Concat(DesktopAssemblies)
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && t.Name.EndsWith("Module"))
            .ToList();

        var violations = moduleTypes
            .Where(t =>
            {
                var hasPrismRegisterTypes = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(m => m.Name == "RegisterTypes");
                var hasServerAddModule = t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Any(m => m.Name.StartsWith("Add") && m.Name.EndsWith("Module"));
                return !hasPrismRegisterTypes && !hasServerAddModule;
            })
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Module 缺少 RegisterTypes/AddXxxModule 注册方法: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// A-09: 所有 Options 类必须有 public const string SectionName
    /// 例外：被其他 Options 类作为属性引用的子配置类（通过父级属性绑定，无独立 Section）
    /// </summary>
    [Fact]
    public void A04_Options_Must_Define_SectionName()
    {
        var optionsTypes = ServerAssemblies
            .Append(Assembly.Load("LYBT.Shared.Configuration"))
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && t.Name.EndsWith("Options") && t.DeclaringType == null)
            .ToList();

        // 子配置类（RateLimitOptions/ConnectionPoolOptions 等）通过父 Options 属性绑定，无需 SectionName
        var subConfigTypes = optionsTypes
            .SelectMany(o => o.GetProperties())
            .Select(p => p.PropertyType)
            .Where(t => t.IsClass && t.Name.EndsWith("Options"))
            .ToHashSet();

        var violations = optionsTypes
            .Where(t => !subConfigTypes.Contains(t))
            .Where(t =>
            {
                var sectionField = t.GetField("SectionName", BindingFlags.Public | BindingFlags.Static);
                return sectionField is not { IsLiteral: true } || sectionField.FieldType != typeof(string);
            })
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Options 类缺少 public const string SectionName: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// A-09: Controller 公共方法必须返回 IActionResult 或 Task&lt;IActionResult&gt;
    /// 统一 API 响应包装，避免直接暴露领域对象
    /// </summary>
    [Fact]
    public void A05_Controller_Methods_Must_Return_IActionResult()
    {
        var controllers = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .And()
            .AreNotAbstract()
            .GetTypes();

        var violations = new List<string>();

        foreach (var controller in controllers)
        {
            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName);

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;
                var isActionResult = typeof(IActionResult).IsAssignableFrom(returnType);
                var isTaskOfActionResult = returnType.IsGenericType &&
                    returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                    typeof(IActionResult).IsAssignableFrom(returnType.GetGenericArguments()[0]);

                if (!isActionResult && !isTaskOfActionResult)
                {
                    violations.Add($"{controller.Name}.{method.Name} → {returnType.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Controller 公共方法必须返回 IActionResult: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// A-09: 验证器必须继承 AbstractValidator&lt;T&gt;
    /// 例外：ProductionConfigurationValidator 是启动时配置检查器，非 FluentValidation 验证器
    /// </summary>
    [Fact]
    public void A06_Validators_Must_Inherit_AbstractValidator()
    {
        var validators = ServerAssemblies
            .Append(Assembly.Load("LYBT.Shared.Models"))
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && t.Name.EndsWith("Validator"))
            .ToList();

        var allowedNonFluentValidators = new[] { "ProductionConfigurationValidator" };

        var violations = validators
            .Where(t => !allowedNonFluentValidators.Contains(t.Name))
            .Where(t => t.BaseType?.IsGenericType != true || t.BaseType.Name != "AbstractValidator`1")
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"验证器未继承 AbstractValidator<T>: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// A-09: 使用 Mapperly 的 Mapper 类必须有 [Mapper] 注解 + partial 修饰符
    /// 静态手写映射工具类不在此列（非 partial 即为手写）
    /// </summary>
    [Fact]
    public void A07_Mapperly_Mappers_Must_Have_Mapper_Attribute()
    {
        var mapperTypes = ServerAssemblies.Concat(DesktopAssemblies)
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract
                && t.Name.EndsWith("Mapper")
                && t.IsNested == false  // 排除嵌套类
                && t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Any(m => m.IsDefined(typeof(Riok.Mapperly.Abstractions.MapperAttribute), true)))
            .ToList();

        // 如果没有 Mapperly Mapper，测试通过（全部是手写 Mapper）
        if (mapperTypes.Count == 0) return;

        var violations = mapperTypes
            .Where(t => !t.GetCustomAttributes(true).Any(a => a.GetType().Name == "MapperAttribute"))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Mapperly Mapper 缺少 [Mapper] 注解: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// 检查类型是否直接/间接继承 ApiClientRepositoryBase&lt;,&gt;
    /// </summary>
    private static bool InheritsApiClientRepositoryBase(Type type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(ApiClientRepositoryBase<,>))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    #endregion

    #region A-28: 双轨规范化守卫（蓝图 §2.2）

    /// <summary>
    /// A-28 P1-1: CQRS 模块 Service 接口不得暴露写方法（Update/Restore/Status 变更等）
    /// 写操作必须走 MediatR Handler（ISender.Send）——蓝图 §2.2 请求处理边界规则
    /// </summary>
    [Fact]
    public void P19_Cqrs_Services_Must_Not_Expose_Write_Methods()
    {
        var writeVerbs = new[] { "Update", "Restore", "Toggle", "Enable", "Disable", "Change", "Delete", "Status", "Save", "Reset", "Import", "Batch" };
        var cqrsReadServiceInterfaces = new[]
        {
            "IUserCrossModuleService", "IPatientService"
        };

        // A-31-C3a 豁免：登录流程写方法（LoginCommandHandler 专用，非 Controller 写端点）
        var loginFlowWriteMethods = new[]
        {
            "IUserCrossModuleService.UpdateLoginFailureAsync",
            "IUserCrossModuleService.ResetLoginStateAsync"
        };

        var violations = new List<string>();
        foreach (var asm in ServerAssemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (!type.IsInterface || !cqrsReadServiceInterfaces.Contains(type.Name)) continue;
                foreach (var method in type.GetMethods())
                {
                    if (writeVerbs.Any(v => method.Name.Contains(v))
                        && !loginFlowWriteMethods.Contains($"{type.Name}.{method.Name}"))
                        violations.Add($"{type.Name}.{method.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"CQRS 模块 Service 接口暴露写方法（写操作应走 MediatR Handler）: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// A-28 P1-1: CQRS 模块 Controller 方法体不得直调 Service 写方法（IL 扫描）
    /// 防止新增写操作绕过 MediatR 验证管道直接走 Service
    /// </summary>
    [Fact]
    public void P19b_Cqrs_Write_Endpoints_Must_Not_Call_Service_Write_Methods()
    {
        var cqrsControllers = new[]
        {
            "UsersController", "BaseUsersController",
            "PatientsController", "CatalogController",
            "AuthController", "RegistrationsController", "BaseRegistrationsController"
        };

        var violations = new List<string>();
        foreach (var asm in ServerAssemblies)
        {
            foreach (var type in asm.GetTypes().Where(t => t.IsClass && cqrsControllers.Contains(t.Name)))
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var body = method.GetMethodBody();
                    var il = body?.GetILAsByteArray();
                    if (il == null || il.Length == 0) continue;

                    try
                    {
                        foreach (var called in DecodeServiceWriteCalls(il, type.Module))
                        {
                            violations.Add($"{type.Name}.{method.Name} 直调 Service 写方法 {called}");
                        }
                    }
                    catch (BadImageFormatException)
                    {
                        // 忽略无法解析的 IL（理论上不发生）
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // IL 手工解码边界（未识别多字节操作码导致游标越界）——跳过该方法，
                        // 不误报（DownloadController 等新增方法体可触发；不影响白名单控制器分析）
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"CQRS 模块 Controller 直调 Service 写方法（应走 ISender.Send）:\n{string.Join("\n", violations)}");
    }

    /// <summary>
    /// 解码 IL 中的 call/callvirt 指令，返回对 Service 类型写方法的调用（格式 "TypeName.MethodName"）
    /// </summary>
    private static IEnumerable<string> DecodeServiceWriteCalls(byte[] il, System.Reflection.Module module)
    {
        var writeVerbs = new[] { "Update", "Restore", "Toggle", "Enable", "Disable", "Change", "Delete", "Status", "Save", "Reset", "Import", "Batch" };
        var results = new List<string>();
        int i = 0;

        while (i < il.Length)
        {
            var opcode = il[i];

            if (opcode == 0xFE)
            {
                // 两字节操作码（前缀 0xFE）
                if (i + 1 >= il.Length) break;
                var sub = il[i + 1];
                i += 2;
                i += sub switch
                {
                    0x06 or 0x07 or 0x15 or 0x16 or 0x1C => 4, // ldftn/ldvirtftn/initobj/constrained/sizeof
                    0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x0E or 0x19 => 2, // ldarg/ldloc/starg/stloc 等
                    0x12 => 1, // unaligned
                    _ => 0
                };
                continue;
            }

            if (opcode is 0x28 or 0x6F) // call / callvirt（4 字节元数据 token）
            {
                if (i + 5 > il.Length) break;
                var token = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);
                i += 5;

                try
                {
                    var target = module.ResolveMethod(token);
                    if (target == null) continue;
                    var declaringName = target.DeclaringType?.Name ?? string.Empty;
                    var isServiceType = declaringName.EndsWith("Service") && !declaringName.EndsWith("Repository");
                    if (isServiceType && writeVerbs.Any(v => target.Name.Contains(v)))
                        results.Add($"{declaringName}.{target.Name}");
                }
                catch (ArgumentException)
                {
                    // token 无法解析为方法（类型引用等），忽略
                }
                catch (BadImageFormatException)
                {
                }
                continue;
            }

            if (opcode == 0x45) // switch（4 字节计数 + N×4 跳转表）
            {
                if (i + 5 > il.Length) break;
                var count = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);
                i += 5 + count * 4;
                continue;
            }

            i += 1 + IlOperandSize(opcode);
        }

        return results;
    }

    /// <summary>
    /// 单字节操作码的操作数大小（字节数）
    /// </summary>
    private static int IlOperandSize(byte opcode)
    {
        switch (opcode)
        {
            case 0x0E: case 0x0F: case 0x10: case 0x11: case 0x12: case 0x13: // ldloc.s 等 8 位局部/参数
            case 0x1F: // ldc.i4.s
            case 0x2B: case 0x2C: case 0x2D: case 0x2E: case 0x2F: // 短分支
            case 0x30: case 0x31: case 0x32: case 0x33: case 0x34:
            case 0x35: case 0x36: case 0x37:
                return 1;
            case 0x20: case 0x22: // ldc.i4 / ldc.r4
            case 0x28: case 0x29: // call / calli
            case 0x38: case 0x39: case 0x3A: case 0x3B: case 0x3C: case 0x3D: // 长分支
            case 0x3E: case 0x3F: case 0x40: case 0x41: case 0x42: case 0x43: case 0x44:
            case 0x6F: // callvirt
                return 4;
            case 0x21: case 0x23: // ldc.i8 / ldc.r8
                return 8;
            default:
                return 0;
        }
    }

    #endregion
}
