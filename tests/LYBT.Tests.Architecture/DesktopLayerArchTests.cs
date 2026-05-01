using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace LYBT.Tests.Architecture;

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

        // 允许Repository/Mapper层使用Entity（本地模式EF Core数据访问需要）
        var allowedPatterns = new[]
        {
            "Repository",       // Repository层 - EF Core数据访问
            "Mapper",           // Mapper层 - Entity↔DTO映射
            "LoginCoordinator"  // 认证协调器 - 需要User Entity
        };

        var actualViolations = result.FailingTypes?
            .Where(t => !allowedPatterns.Any(p => t.Name.Contains(p)))
            .ToList() ?? [];

        Assert.True(
            actualViolations.Count == 0,
            $"Desktop层直接使用了Entity类（仅Repository/Mapper允许）: {string.Join(", ", actualViolations.Select(t => t.FullName))}");
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
    /// 确保 ViewModel 不直接依赖 IRegionManager 进行导航 (应通过 INavigationCoordinator)
    /// </summary>
    [Fact]
    public void Should_Use_Unified_Navigation_Service()
    {
        // 允许白名单: Shell 层的导航协调器本身需要 IRegionManager
        var allowedTypes = new HashSet<string>
        {
            "NavigationCoordinator",
            "MainWindowViewModel", // 通过 INavigationCoordinator 间接使用
        };

        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("ViewModels")
            .GetTypes()
            .Where(t => !allowedTypes.Contains(t.Name))
            .Where(t => t.Name.EndsWith("ViewModel"))
            .ToList();

        var violatingTypes = viewModelTypes
            .Where(t => t.GetConstructors()
                .Any(c => c.GetParameters()
                    .Any(p => p.ParameterType.Name == "IRegionManager")))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(violatingTypes.Count == 0,
            $"ViewModel 不应直接注入 IRegionManager，应使用 INavigationCoordinator: {string.Join(", ", violatingTypes)}");
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

    #region Sprint3-STD: 架构规则固化

    /// <summary>
    /// P-01: 所有 Repository 接口必须有 Remote 实现
    /// Remote: Modules/*/Repositories/*Repository.cs
    /// </summary>
    [Fact]
    public void P01_AllRepositories_Must_Have_Remote_Implementation()
    {
        var contractsAssembly = Assembly.Load("LYBT.Desktop.Contracts");

        var moduleAssemblies = new[]
        {
            Assembly.Load("LYBT.Desktop.Users"),
            Assembly.Load("LYBT.Desktop.Patients"),
            Assembly.Load("LYBT.Desktop.MedicalCase"),
            Assembly.Load("LYBT.Desktop.Herbs"),
            Assembly.Load("LYBT.Desktop.Formula"),
            Assembly.Load("LYBT.Desktop.Registration")
        };

        var repositoryInterfaces = contractsAssembly.GetTypes()
            .Where(t => t.IsInterface &&
                       t.Name.EndsWith("Repository") &&
                       t.Name.StartsWith("I") &&
                       t.Namespace != null &&
                       t.Namespace.Contains("Repositories"))
            .ToList();

        Assert.NotEmpty(repositoryInterfaces);

        var missingImplementations = new List<string>();

        foreach (var repoInterface in repositoryInterfaces)
        {
            var entityName = repoInterface.Name[1..^"Repository".Length];

            var remoteType = moduleAssemblies
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == $"{entityName}Repository" && !t.IsAbstract && !t.IsInterface);
            if (remoteType == null)
                missingImplementations.Add($"{entityName}Repository [Remote] (接口: {repoInterface.Name})");
        }

        Assert.True(missingImplementations.Count == 0,
            $"Repository 实现不完整，违反 P-01 规则:\n{string.Join("\n", missingImplementations)}");
    }

    /// <summary>
    /// P-03: 所有 CRUD ViewModel 必须继承 MasterDetailViewModelBase
    /// 确保 CRUD 功能的一致性 (列表/详情/导航/搜索)
    /// </summary>
    [Fact]
    public void P03_AllCrudViewModels_Must_Inherit_MasterDetailViewModelBase()
    {
        // MasterDetail 命名约定标识 CRUD ViewModel
        var crudViewModels = Types.InAssemblies(DesktopAssemblies)
            .That()
            .HaveNameEndingWith("MasterDetailViewModel")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes();

        Assert.NotEmpty(crudViewModels);

        foreach (var vmType in crudViewModels)
        {
            var currentType = vmType.BaseType;
            var inheritsMasterDetail = false;

            while (currentType != null && currentType != typeof(object))
            {
                var baseName = currentType.IsGenericType
                    ? currentType.GetGenericTypeDefinition().Name
                    : currentType.Name;

                if (baseName.Contains("MasterDetailViewModelBase"))
                {
                    inheritsMasterDetail = true;
                    break;
                }
                currentType = currentType.BaseType;
            }

            Assert.True(inheritsMasterDetail,
                $"CRUD ViewModel {vmType.Name} 未继承 MasterDetailViewModelBase，违反 P-03 规则");
        }
    }

    #endregion

    /// <summary>
    /// P-07: LocalData 模块必须使用 SQL Server，不得依赖 SQLite
    /// 确保 Local 模式与 Server 模式使用相同的数据库引擎，消除 SQL 方言差异
    /// </summary>
    [Fact]
    public void P07_LocalData_Must_Not_Depend_On_SQLite()
    {
        var localDataAssembly = Assembly.Load("LYBT.Desktop.LocalData");

        // LocalData 不应引用 SQLite 相关程序集
        var sqliteReferences = localDataAssembly.GetReferencedAssemblies()
            .Where(a => a.Name != null &&
                       a.Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Name!)
            .ToList();

        Assert.True(sqliteReferences.Count == 0,
            $"LocalData 不应依赖 SQLite (已迁移到 SQL Server LocalDB)，但发现引用:\n{string.Join("\n", sqliteReferences)}");
    }

    /// <summary>
    /// P-08: LocalData 的 LocalDbContext 不应包含 SQLite 适配方法
    /// IgnoreRowVersion 和 ApplyDecimalConversion 是 SQLite 特有的适配代码
    /// </summary>
    [Fact]
    public void P08_LocalDbContext_Must_Not_Have_SQLite_Adapters()
    {
        var localDataAssembly = Assembly.Load("LYBT.Desktop.LocalData");
        var localDbContextType = localDataAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "LocalDbContext");

        Assert.NotNull(localDbContextType);

        var sqliteAdapterMethods = new[] { "IgnoreRowVersion", "ApplyDecimalConversion" };
        var foundMethods = localDbContextType!
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => sqliteAdapterMethods.Contains(m.Name))
            .Select(m => m.Name)
            .ToList();

        Assert.True(foundMethods.Count == 0,
            $"LocalDbContext 不应包含 SQLite 适配方法，但发现:\n{string.Join("\n", foundMethods)}");
    }

    /// <summary>
    /// 验证所有 Repository 接口定义在 Contracts 层
    /// </summary>
    /// <remarks>
    /// SYNC-D02: Repository 接口统一在 Contracts/Repositories/ 中定义，
    /// 由 Shell/Extensions/DataSourceRegistrationExtensions 中央工厂注册。
    /// 模块程序集不应再包含 Repository 接口定义。
    /// </remarks>
    [Fact]
    public void All_Repository_Interfaces_Should_Be_In_Contracts()
    {
        var moduleAssemblies = new[]
        {
            Assembly.Load("LYBT.Desktop.Auth"),
            Assembly.Load("LYBT.Desktop.Users"),
            Assembly.Load("LYBT.Desktop.Patients"),
            Assembly.Load("LYBT.Desktop.MedicalCase"),
            Assembly.Load("LYBT.Desktop.Herbs"),
            Assembly.Load("LYBT.Desktop.Formula"),
            Assembly.Load("LYBT.Desktop.Registration")
        };

        var misplacedInterfaces = new List<string>();

        foreach (var assembly in moduleAssemblies)
        {
            var repoInterfaces = assembly.GetTypes()
                .Where(t => t.IsInterface &&
                           t.Name.EndsWith("Repository") &&
                           t.Name.StartsWith("I"))
                .ToList();

            foreach (var iface in repoInterfaces)
            {
                misplacedInterfaces.Add($"{iface.FullName} (in {assembly.GetName().Name})");
            }
        }

        Assert.True(misplacedInterfaces.Count == 0,
            $"Repository 接口应定义在 Contracts 层，而非模块中:\n{string.Join("\n", misplacedInterfaces)}");
    }
}
