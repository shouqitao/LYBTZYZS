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
        Assembly.Load("LYBT.Desktop.Services"),
        Assembly.Load("LYBT.Desktop.Shell"),
        Assembly.Load("LYBT.Desktop.Auth"),
        Assembly.Load("LYBT.Desktop.Users"),
        Assembly.Load("LYBT.Desktop.Patients"),
        Assembly.Load("LYBT.Desktop.MedicalCase"),
        Assembly.Load("LYBT.Desktop.Consultation"),
        Assembly.Load("LYBT.Desktop.Prescriptions"),
        Assembly.Load("LYBT.Desktop.Herbs"),
        Assembly.Load("LYBT.Desktop.Formula"),
        Assembly.Load("LYBT.Desktop.AdminWorkstation"),
        Assembly.Load("LYBT.Desktop.ClinicalWorkstation")
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
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Contain_DTO_Classes()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("Desktop")
            .Should()
            .NotHaveNameEndingWith("Dto")
            .And()
            .NotHaveNameEndingWith("DTO")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Desktop层包含DTO类（应使用Item/ViewState/Info）: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
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

        Assert.True(
            result.IsSuccessful,
            $"Desktop层直接使用了Entity类: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
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
    /// 禁止目录：Interfaces/、Mappings/、Services/
    /// 原因：接口统一在Shared.Interfaces，Mapping统一在Desktop.Services/Mapping，
    /// 业务服务统一在Desktop.Services/Business
    /// Issue #1113
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
            "LYBT.Desktop.Consultation",
            "LYBT.Desktop.Prescriptions",
            "LYBT.Desktop.Herbs",
            "LYBT.Desktop.Formula",
            "LYBT.Desktop.AdminWorkstation",
            "LYBT.Desktop.ClinicalWorkstation"
        };

        var forbiddenNamespaces = new[] { "Interfaces", "Mappings", "Services" };

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
    /// Desktop业务服务必须在统一位置实现
    /// </summary>
    /// <remarks>
    /// 业务服务统一在Desktop.Services/Business/实现
    /// 实现Shared.Interfaces.Services中定义的接口
    /// Issue #1113
    /// </remarks>
    [Fact]
    public void Desktop_Services_Should_Be_In_Correct_Location()
    {
        var serviceAssembly = Assembly.Load("LYBT.Desktop.Services");
        var serviceTypes = serviceAssembly.GetTypes()
            .Where(t => t.Namespace != null &&
                        t.Namespace.Contains("Business") &&
                        t.IsClass &&
                        !t.IsAbstract &&
                        t.Name.EndsWith("Service"))
            .ToList();

        // 验证：所有XxxService类都应该在Desktop.Services.Business命名空间
        foreach (var serviceType in serviceTypes)
        {
            Assert.True(
                serviceType.Namespace!.StartsWith("LYBT.Desktop.Services.Business"),
                $"服务 {serviceType.FullName} 不在正确的命名空间（应为LYBT.Desktop.Services.Business）");
        }

        // 验证：业务服务应实现Shared.Interfaces.Services中的接口
        var servicesWithInterfaces = serviceTypes
            .Where(t => t.GetInterfaces().Any(i => i.Namespace != null && i.Namespace.StartsWith("LYBT.Shared.Interfaces.Services")))
            .ToList();

        Assert.True(
            servicesWithInterfaces.Count > 0,
            "Desktop.Services.Business中应至少有一个服务实现Shared.Interfaces.Services接口");
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
}
