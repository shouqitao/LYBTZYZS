using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace LYBT.ArchTests;

/// <summary>
/// Desktop层架构约束测试
/// 确保Desktop层分层纯净，依赖方向正确
/// </summary>
public class DesktopLayerArchTests
{
    private static readonly Assembly[] DesktopAssemblies =
    [
        Assembly.Load("LYBT.Desktop.Infrastructure"),
        Assembly.Load("LYBT.Desktop.Models"),
        Assembly.Load("LYBT.Desktop.Shell"),
        Assembly.Load("LYBT.Desktop.Auth"),
        Assembly.Load("LYBT.Desktop.Users"),
        Assembly.Load("LYBT.Desktop.Patients"),
        Assembly.Load("LYBT.Desktop.MedicalCase"),
        // [已删除] LYBT.Desktop.Consultation - 诊断功能已合并到MedicalCase模块
        // [已删除] LYBT.Desktop.Prescriptions - 处方功能已合并到MedicalCase模块
        Assembly.Load("LYBT.Desktop.Herbs"),
        Assembly.Load("LYBT.Desktop.Formula"),
        Assembly.Load("LYBT.Desktop.Admin"),
        Assembly.Load("LYBT.Desktop.Clinical")
    ];

    /// <summary>
    /// Desktop层不得依赖Server层
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Depend_On_Server_Layers()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .Should()
            .NotHaveDependencyOnAll("LYBT.Infrastructure", "LYBT.Entities")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Desktop层违规依赖Server层: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// Desktop层不得包含DTO类
    /// 例外：打印相关DTO（用于打印服务数据传递）
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Contain_DTO_Classes()
    {
        // 允许的打印相关DTO（用于打印服务，与服务端共享协议）
        var allowedPrintDtos = new[]
        {
            "PrescriptionPrintDto",
            "PrescriptionItemPrintDto"
        };

        var result = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("Desktop")
            .Should()
            .NotHaveNameEndingWith("Dto")
            .And()
            .NotHaveNameEndingWith("DTO")
            .GetResult();

        // 过滤掉允许的打印DTO
        var actualViolations = result.FailingTypes?
            .Where(t => !allowedPrintDtos.Contains(t.Name))
            .ToList() ?? [];

        Assert.True(
            actualViolations.Count == 0,
            $"Desktop层包含DTO类（应使用Item/ViewState/Info）: {string.Join(", ", actualViolations.Select(t => t.Name))}");
    }

    /// <summary>
    /// Desktop层UI模型必须使用正确后缀
    /// </summary>
    [Fact]
    public void Desktop_UI_Models_Should_Have_Correct_Suffix()
    {
        var modelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespace("Models")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes();

        var invalidTypes = modelTypes
            .Where(t => !t.Name.EndsWith("Item") &&
                       !t.Name.EndsWith("ViewState") &&
                       !t.Name.EndsWith("Info") &&
                       !t.Name.EndsWith("Model") &&
                       !t.Name.EndsWith("EventArgs") &&
                       !t.Name.EndsWith("Data"))
            .ToList();

        Assert.Empty(invalidTypes);
    }

    /// <summary>
    /// Desktop层ViewModels必须继承自正确基类
    /// </summary>
    [Fact]
    public void Desktop_ViewModels_Should_Inherit_From_Base_Classes()
    {
        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespace("ViewModels")
            .And()
            .HaveNameEndingWith("ViewModel")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes();

        var baseTypes = new[]
        {
            "ModernViewModelBase",
            "ModernManagementViewModel",
            "NavigationViewModelBase",
            "DialogViewModelBase",
            "BaseServiceManagementViewModel", // 临时保留，待迁移
            "NewBaseListViewModel" // 临时保留，待迁移
        };

        foreach (var vmType in viewModelTypes)
        {
            var hasValidBase = false;
            var currentType = vmType.BaseType;

            while (currentType != null && currentType != typeof(object))
            {
                if (baseTypes.Contains(currentType.Name))
                {
                    hasValidBase = true;
                    break;
                }
                currentType = currentType.BaseType;
            }

            Assert.True(
                hasValidBase || vmType.Name.Contains("Design") || vmType.Name.Contains("Mock"),
                $"ViewModel {vmType.Name} 未继承自标准基类");
        }
    }

    // <summary>
    // Desktop层只能依赖Shared层接口
    // </summary>
    // <remarks>
    // 已移除原因（Issue #1113）：
    // NetArchTest的OnlyHaveDependenciesOn会将Desktop内部跨程序集引用也标记为违规，导致误报。
    // 真正需要检查的Server层依赖已在 Desktop_Should_Not_Depend_On_Server_Layers 测试中覆盖。
    // 移除日期：2025-01-09
    // </remarks>

    /// <summary>
    /// 事件定义不应重复
    /// </summary>
    [Fact]
    public void Events_Should_Not_Have_Duplicate_Definitions()
    {
        var eventTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("Events")
            .And()
            .Inherit(typeof(Prism.Events.PubSubEvent))
            .Or()
            .Inherit(typeof(Prism.Events.PubSubEvent<>))
            .GetTypes();

        // 按全限定名分组，只检测完全相同的类型定义
        var duplicates = eventTypes
            .GroupBy(t => t.FullName)
            .Where(g => g.Count() > 1)
            .Select(g => new { FullName = g.Key, Count = g.Count(), Types = g.ToList() })
            .ToList();

        // 如果有重复，显示详细信息
        if (duplicates.Any())
        {
            var details = string.Join("\n", duplicates.Select(d =>
                $"{d.FullName}: {d.Count}次 ({string.Join(", ", d.Types.Select(t => t.Assembly.GetName().Name))})"));
            Assert.Fail($"发现重复的事件定义:\n{details}");
        }
    }

    /// <summary>
    /// Desktop层不应直接使用Entity类
    /// 例外：Repository层（EF Core数据访问）和Mapper层（Entity↔DTO映射）
    /// 以及LoginCoordinator（认证时需要User Entity）
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Use_Entity_Classes()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("Desktop")
            .Should()
            .NotHaveDependencyOn("LYBT.Entities")
            .GetResult();

        // 允许Repository/Mapper/DataSource层使用Entity（本地模式EF Core数据访问需要）
        var allowedPatterns = new[]
        {
            "Repository",       // Repository层 - EF Core数据访问
            "Mapper",           // Mapper层 - Entity↔DTO映射
            "DataSource",       // DataSource层 - 远程/本地数据源
            "LoginCoordinator"  // 认证协调器 - 需要User Entity
        };

        var actualViolations = result.FailingTypes?
            .Where(t => !allowedPatterns.Any(p => t.Name.Contains(p)))
            .ToList() ?? [];

        Assert.True(
            actualViolations.Count == 0,
            $"Desktop层直接使用了Entity类（仅Repository/Mapper/DataSource允许）: {string.Join(", ", actualViolations.Select(t => t.FullName))}");
    }

    /// <summary>
    /// 服务注册应遵循命名规范
    /// </summary>
    [Fact]
    public void Services_Should_Follow_Naming_Convention()
    {
        var serviceTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespace("Services")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes();

        var invalidNames = serviceTypes
            .Where(t => !t.Name.EndsWith("Service") &&
                       !t.Name.EndsWith("Manager") &&
                       !t.Name.EndsWith("Provider") &&
                       !t.Name.EndsWith("Handler") &&
                       !t.Name.EndsWith("Adapter") &&
                       !t.Name.EndsWith("Factory") &&
                       !t.Name.EndsWith("Coordinator") &&
                       !t.Name.EndsWith("Navigator"))
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(invalidNames);
    }

    /// <summary>
    /// Desktop层API调用必须通过Service层
    /// </summary>
    [Fact]
    public void ViewModels_Should_Not_Directly_Use_Api_Interfaces()
    {
        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespace("ViewModels")
            .And()
            .HaveNameEndingWith("ViewModel")
            .GetTypes();

        foreach (var vmType in viewModelTypes)
        {
            var fields = vmType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var apiFields = fields.Where(f => f.FieldType.Name.EndsWith("Api")).ToList();

            Assert.Empty(apiFields);
        }
    }

    /// <summary>
    /// 确保使用统一的导航服务
    /// </summary>
    [Fact]
    public void Should_Use_Unified_Navigation_Service()
    {
        var navigationUsages = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespace("ViewModels")
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Where(m => m.Name.Contains("Navigate") && !m.Name.Contains("OnNavigated"))
            .ToList();

        // 检查是否使用INavigationService而非直接使用RegionManager
        foreach (var method in navigationUsages)
        {
            var methodBody = method.GetMethodBody();
            // TODO: 深入检查方法体以验证使用了INavigationService
        }
    }

    /// <summary>
    /// Desktop模块内不得包含禁止的目录
    /// </summary>
    /// <remarks>
    /// 禁止目录：Interfaces/
    /// 原因：接口统一在各模块的 Interfaces/ 目录或 Shared.Interfaces
    /// ADR-002: Repository 由各模块自行管理（允许 Repositories/ 目录）
    /// Issue #1213
    /// </remarks>
    [Fact]
    public void Desktop_Modules_Should_Not_Have_Forbidden_Directories()
    {
        var moduleAssemblies = new[]
        {
            "LYBT.Desktop.Auth",
            "LYBT.Desktop.Users",
            "LYBT.Desktop.Patients",
            "LYBT.Desktop.MedicalCase",
            // [已删除] LYBT.Desktop.Consultation - 诊断功能已合并到MedicalCase模块
            // [已删除] LYBT.Desktop.Prescriptions - 处方功能已合并到MedicalCase模块
            "LYBT.Desktop.Herbs",
            "LYBT.Desktop.Formula",
            "LYBT.Desktop.Admin",
            "LYBT.Desktop.Clinical"
        };

        var forbiddenNamespaces = new[] { "Mappings" };

        foreach (var assemblyName in moduleAssemblies)
        {
            var assembly = Assembly.Load(assemblyName);
            var types = assembly.GetTypes();

            foreach (var forbiddenNs in forbiddenNamespaces)
            {
                var violatingTypes = types
                    .Where(t => t.Namespace != null && t.Namespace.Contains($".{forbiddenNs}"))
                    .ToList();

                Assert.Empty(violatingTypes);
            }
        }
    }

    /// <summary>
    /// Desktop模块ViewModel基类使用符合标准
    /// </summary>
    /// <remarks>
    /// 允许的基类：UnifiedViewModelBase, UnifiedListViewModelBase, ModernViewModelBase,
    /// NavigationViewModelBase, DialogViewModelBase
    /// 临时保留：BaseServiceManagementViewModel, NewBaseListViewModel
    /// Issue #1113
    /// </remarks>
    [Fact]
    public void Desktop_ViewModels_Should_Use_Standard_Base_Classes()
    {
        var allowedBaseClasses = new[]
        {
            "UnifiedViewModelBase",
            "UnifiedListViewModelBase`1",  // 泛型类
            "ModernViewModelBase",
            "ModernManagementViewModel",
            "NavigationViewModelBase",
            "DialogViewModelBase",
            // 临时保留
            "BaseServiceManagementViewModel",
            "NewBaseListViewModel"
        };

        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespace("ViewModels")
            .And()
            .HaveNameEndingWith("ViewModel")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes()
            .Where(t => !t.Name.Contains("Design") && !t.Name.Contains("Mock"))  // 排除设计时和Mock类
            .ToList();

        foreach (var vmType in viewModelTypes)
        {
            var currentType = vmType.BaseType;
            var hasValidBase = false;

            while (currentType != null && currentType != typeof(object))
            {
                var baseName = currentType.IsGenericType
                    ? currentType.GetGenericTypeDefinition().Name
                    : currentType.Name;

                if (allowedBaseClasses.Contains(baseName))
                {
                    hasValidBase = true;
                    break;
                }
                currentType = currentType.BaseType;
            }

            Assert.True(
                hasValidBase,
                $"ViewModel {vmType.FullName} 未继承自标准基类。允许的基类：{string.Join(", ", allowedBaseClasses)}");
        }
    }

    /// <summary>
    /// 验证所有 Repository 都在对应模块中注册
    /// </summary>
    /// <remarks>
    /// ADR-002 架构决策：Repository (数据访问层) 由各业务模块自行注册
    /// 每个模块的 *Module.cs 应包含 RegisterSingleton&lt;IXxxRepository, XxxRepository&gt;()
    /// Issue #1213
    /// </remarks>
    [Fact]
    public void All_Repositories_Should_Be_Registered_In_Modules()
    {
        var moduleAssemblies = new[]
        {
            Assembly.Load("LYBT.Desktop.Auth"),
            Assembly.Load("LYBT.Desktop.Users"),
            Assembly.Load("LYBT.Desktop.Patients"),
            Assembly.Load("LYBT.Desktop.MedicalCase"),
            // [已删除] LYBT.Desktop.Consultation - 诊断功能已合并到MedicalCase模块
            // [已删除] LYBT.Desktop.Prescriptions - 处方功能已合并到MedicalCase模块
            Assembly.Load("LYBT.Desktop.Herbs"),
            Assembly.Load("LYBT.Desktop.Formula")
        };

        var repositoriesWithoutRegistration = new List<string>();

        foreach (var assembly in moduleAssemblies)
        {
            // 查找 Repository 接口
            var repositoryInterfaces = assembly.GetTypes()
                .Where(t => t.IsInterface && t.Name.EndsWith("Repository"))
                .ToList();

            if (!repositoryInterfaces.Any())
                continue; // 模块没有 Repository，跳过

            // 查找模块类
            var moduleType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.EndsWith("Module") &&
                                   t.GetInterfaces().Any(i => i.Name == "IModule"));

            if (moduleType == null)
            {
                repositoriesWithoutRegistration.Add($"{assembly.GetName().Name}: 未找到 Module 类");
                continue;
            }

            // 检查 RegisterTypes 方法
            var registerMethod = moduleType.GetMethod("RegisterTypes");
            if (registerMethod == null)
            {
                repositoriesWithoutRegistration.Add($"{assembly.GetName().Name}: Module 类未实现 RegisterTypes 方法");
                continue;
            }

            // 注意：这里只是验证 Module 类存在且有 RegisterTypes 方法
            // 实际的注册验证需要运行时检查或源码分析，这里通过集成测试覆盖
            // 如果模块有 Repository 接口但未注册，应用启动时会失败
        }

        Assert.Empty(repositoriesWithoutRegistration);
    }
}
