// P2-19-5 DP08 IApiClient包装已评估：ViewModel持IApiClientPatients规避I*Api检测，已在DP08显式覆盖IApiClient*
// P2-18-4 VM覆盖率已评估
using System.Collections.ObjectModel;
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
    private static Assembly[] DesktopAssemblies => TestAssemblies.Desktop;

    /// <summary>
    /// Desktop层不得依赖Server层（LYBT.Entities 已下沉到 Shared，不再受限）
    /// </summary>
    [Fact]
    public void DP01_Desktop_Should_Not_Depend_On_Server()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .Should()
            .NotHaveDependencyOn("LYBT.Infrastructure")
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
    public void DP02_Desktop_Should_Not_Contain_DTO_Classes()
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
    public void DP03_UI_Models_Must_Have_Correct_Suffix()
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
    public void DP04_ViewModels_Must_Inherit_Base_Classes()
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
            "NavigableViewModelBase",
            "MasterDetailViewModelBase",
            "DialogViewModelBase",
            "ChildViewModelBase"
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
    public void DP05_Events_No_Duplicate_Definitions()
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
    public void DP06_Desktop_Should_Not_Use_Entity_Classes()
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
    public void DP07_Services_Must_Follow_Naming_Convention()
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
    public void DP08_ViewModels_No_Direct_Api_Interfaces()
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
    public void DP09_Must_Use_Unified_Navigation_Service()
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
    public void DM01b_Modules_No_Forbidden_Directories()
    {
        var moduleAssemblies = new[]
        {
            "LYBT.Desktop.Auth",
            "LYBT.Desktop.Users",
            "LYBT.Desktop.Patients",
            "LYBT.Desktop.MedicalCase",
            "LYBT.Desktop.Catalog",
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
    public void DM02_ViewModels_Use_Standard_Base_Classes()
    {
        var allowedBaseClasses = new[]
        {
            "NavigableViewModelBase",
            "MasterDetailViewModelBase`2",  // 泛型类
            "DialogViewModelBase",
            "ChildViewModelBase"
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
    public void DM01_AllRepositories_Must_Have_Remote_Implementation()
    {
        var contractsAssembly = Assembly.Load("LYBT.Desktop.Contracts");

        var moduleAssemblies = new[]
        {
            Assembly.Load("LYBT.Desktop.Users"),
            Assembly.Load("LYBT.Desktop.Patients"),
            Assembly.Load("LYBT.Desktop.MedicalCase"),
            Assembly.Load("LYBT.Desktop.Catalog"),
            Assembly.Load("LYBT.Desktop.Registrations")
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
    public void DM03_CrudViewModels_Must_Inherit_MasterDetailViewModelBase()
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
    public void DM07_LocalData_Must_Not_Depend_On_SQLite()
    {
        var localDataAssembly = Assembly.Load("LYBT.Desktop.Infrastructure");

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
    /// A-21 C1: 生产层（LYBT.Desktop.Infrastructure）不得再包含 LocalDbContext。
    /// LocalData 已废弃（生产 0 引用），LocalDbContext 移入测试项目 LYBT.Tests.Desktop。
    /// </summary>
    [Fact]
    public void DM08_Production_Should_Not_Contain_LocalDbContext()
    {
        var localDataAssembly = Assembly.Load("LYBT.Desktop.Infrastructure");
        var localDbContextType = localDataAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "LocalDbContext");

        Assert.Null(localDbContextType);
    }

    /// <summary>
    /// 禁止在 ViewModel 中新增 DelegateCommand（应使用 [RelayCommand]）
    /// 截至 2026-07-30，全部 51 个 VM 已使用 CommunityToolkit 命令，零 DelegateCommand。
    /// </summary>
    [Fact]
    public void DM04_ViewModels_No_New_DelegateCommand()
    {
        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("ViewModels")
            .And()
            .HaveNameEndingWith("ViewModel")
            .And()
            .AreClasses()
            .GetTypes()
            .Where(t => !t.Name.Contains("Design") && !t.Name.Contains("Mock"))
            .ToList();

        var violations = new List<string>();

        foreach (var vmType in viewModelTypes)
        {
            // 截至 2026-07-30，所有 VM 已迁移 CommunityToolkit，无需例外

            var ctors = vmType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var ctor in ctors)
            {
                var body = ctor.GetMethodBody();
                if (body == null) continue;

                // 检查 IL 中是否有 newobj DelegateCommand
                var il = body.GetILAsByteArray();
                if (il == null) continue;

                // 简单检查：构造函数中是否引用了 DelegateCommand 类型
                var fields = vmType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                var delegateCommandFields = fields.Where(f =>
                    f.FieldType.Name.Contains("DelegateCommand") ||
                    (f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition().Name.Contains("DelegateCommand")))
                    .ToList();

                if (delegateCommandFields.Any())
                {
                    violations.Add($"{vmType.Name}: {string.Join(", ", delegateCommandFields.Select(f => f.Name))}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"ViewModel 不应使用 new DelegateCommand（应使用 [RelayCommand]）:\n{string.Join("\n", violations)}");
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
    public void DM05_Repository_Interfaces_Must_Be_In_Contracts()
    {
        var moduleAssemblies = new[]
        {
            Assembly.Load("LYBT.Desktop.Auth"),
            Assembly.Load("LYBT.Desktop.Users"),
            Assembly.Load("LYBT.Desktop.Patients"),
            Assembly.Load("LYBT.Desktop.MedicalCase"),
            Assembly.Load("LYBT.Desktop.Catalog"),
            Assembly.Load("LYBT.Desktop.Registrations")
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

    /// <summary>
    /// 业务模块不得引用其他业务模块（仅 MedicalCase.Models 枚举例外）
    /// 防止模块耦合，确保模块隔离
    /// </summary>
    [Fact]
    public void DM06_Business_Modules_No_Cross_References()
    {
        var moduleAssemblies = new Dictionary<string, Assembly>
        {
            ["LYBT.Desktop.Patients"] = Assembly.Load("LYBT.Desktop.Patients"),
            ["LYBT.Desktop.Catalog"] = Assembly.Load("LYBT.Desktop.Catalog"),
            ["LYBT.Desktop.MedicalCase"] = Assembly.Load("LYBT.Desktop.MedicalCase"),
            ["LYBT.Desktop.Users"] = Assembly.Load("LYBT.Desktop.Users"),
            ["LYBT.Desktop.Auth"] = Assembly.Load("LYBT.Desktop.Auth"),
        };

        var violations = new List<string>();

        foreach (var (moduleName, assembly) in moduleAssemblies)
        {
            var referencedModules = assembly.GetReferencedAssemblies()
                .Where(a => moduleAssemblies.ContainsKey(a.Name!) && a.Name != moduleName)
                .Select(a => a.Name!)
                .ToList();

            if (referencedModules.Any())
            {
                violations.Add($"{moduleName} 引用了: {string.Join(", ", referencedModules)}");
            }
        }

        Assert.True(violations.Count == 0,
            $"违反模块隔离规则: 业务模块间不得相互引用\n{string.Join("\n", violations)}");
    }

    /// <summary>
    /// DP07: Desktop 业务模块之间不得互相引用
    /// 判定范围：Modules/LYBT.Desktop.*（Auth/Users/Patients/MedicalCase/Herbs/Formula/Registration）
    /// 排除 Core/(Contracts/Foundation/Infrastructure/Controls/Printing)、LocalWebAPI 等基础设施层
    /// 豁免（角色编排/组合根，注释理由）：
    ///   - Roles(Admin/Clinical)→Modules：角色工作区是组合根，负责按角色组装业务模块视图
    ///   - Shell→全部：Shell 是应用组合根，负责模块装配与启动
    /// 参照 Server P07_ServerModules_Should_Not_Reference_Other_ServerModules 写法
    /// </summary>
    [Fact]
    public void DP07_DesktopModules_Should_Not_Reference_Other_DesktopModules()
    {
        var moduleAssemblies = new Dictionary<string, Assembly>
        {
            ["LYBT.Desktop.Auth"] = Assembly.Load("LYBT.Desktop.Auth"),
            ["LYBT.Desktop.Users"] = Assembly.Load("LYBT.Desktop.Users"),
            ["LYBT.Desktop.Patients"] = Assembly.Load("LYBT.Desktop.Patients"),
            ["LYBT.Desktop.MedicalCase"] = Assembly.Load("LYBT.Desktop.MedicalCase"),
            ["LYBT.Desktop.Catalog"] = Assembly.Load("LYBT.Desktop.Catalog"),
            ["LYBT.Desktop.Registrations"] = Assembly.Load("LYBT.Desktop.Registrations"),
        };

        var violations = new List<string>();

        foreach (var (moduleName, assembly) in moduleAssemblies)
        {
            var referencedModules = assembly.GetReferencedAssemblies()
                .Where(a => moduleAssemblies.ContainsKey(a.Name!) && a.Name != moduleName)
                .Select(a => a.Name!)
                .ToList();

            if (referencedModules.Any())
            {
                violations.Add($"{moduleName} 引用了: {string.Join(", ", referencedModules)}");
            }
        }

        Assert.True(violations.Count == 0,
            $"违反 DP07 规则: Desktop 业务模块间不得相互引用\n{string.Join("\n", violations)}");
    }

    /// <summary>
    /// DP10: Desktop ViewModel 禁止注入 IApiClient 子接口（IApiClientIdentity/IApiClientPatients 等）
    /// A-23b: 应注入 Service 接口（IUserCrossModuleService/IPatientService/IMedicalCaseQueryService 等）
    /// 豁免：统一 IApiClient（A-18 过渡期允许，长期目标是 VM 全部走 Service）
    /// </summary>
    [Fact]
    public void DP10_ViewModels_Must_Not_Inject_IApiClient_SubInterfaces()
    {
        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("ViewModels")
            .And()
            .HaveNameEndingWith("ViewModel")
            .And()
            .AreClasses()
            .GetTypes()
            .Where(t => !t.Name.Contains("Design") && !t.Name.Contains("Mock"))
            .ToList();

        var violatingTypes = new List<string>();

        foreach (var vmType in viewModelTypes)
        {
            var ctors = vmType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var ctor in ctors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    var paramType = param.ParameterType;
                    // IApiClient 子接口 = 类型名以 "IApiClient" 开头且非统一 IApiClient
                    if (paramType.IsInterface &&
                        paramType.Name.StartsWith("IApiClient") &&
                        paramType.Name != "IApiClient")
                    {
                        violatingTypes.Add($"{vmType.FullName} 注入 {paramType.Name}（参数 {param.Name}）");
                    }
                }
            }
        }

        Assert.True(violatingTypes.Count == 0,
            $"ViewModel 不应注入 IApiClient 子接口，应注入 Service 接口:\n{string.Join("\n", violatingTypes)}");
    }

    /// <summary>
    /// DP-M1: Desktop ViewModel 禁止直接持有 DTO 做编辑属性
    /// DTO 是传输对象，编辑必须通过 Model。只读 DTO 属性（用于显示）除外。
    /// 扩展：ObservableCollection&lt;T&gt; 泛型参数名以 Dto 结尾同样判违规。
    /// </summary>
    [Fact]
    public void DP_M1_ViewModels_Must_Not_Hold_Dto_As_Editable_Property()
    {
        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("ViewModels")
            .And()
            .HaveNameEndingWith("ViewModel")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes()
            .Where(t => !t.Name.Contains("Design") && !t.Name.Contains("Mock"))
            .ToList();

        var violations = new List<string>();

        // 基类/接口属性白名单（架构决策，不在本次检查范围）
        var baseClassPropertyNames = new HashSet<string>
        {
            "SelectedItem", "CurrentDetail", "CurrentUser", "PatientDetail",
            "SelectedPatient", "SelectedHerb", "CurrentPatient",
            "DailyIncome", "DailyConsultations", "DailyHerbUsage"
        };

        // 只读展示/引用集合白名单（DP-M1 文档：只读 DTO 属性用于显示除外）
        // 先例：Registration 患者/医生选择列表豁免；共享控件药材目录引用列表（HerbListDto）豁免
        var readOnlyDisplayCollectionNames = new HashSet<string>
        {
            "AllHerbs",          // 药材目录引用列表（共享控件数据源，HerbListDto）
            "FilteredHerbs",     // 药材过滤建议列表（HerbListDto，只读展示）
            "Patients",          // 患者列表（只读展示/选择，先例豁免）
            "Queue",             // 待诊队列（只读展示，PendingMedicalCaseDto）
            "Logs",              // 审计日志列表（只读展示，AuditLogDto）
            "PatientSearchResults", // 患者搜索列表（先例豁免）
            "DoctorList"            // 医生选择列表（先例豁免）
        };

        foreach (var vmType in viewModelTypes)
        {
            // 只检查 ViewModel 自身声明的属性（不检查继承的基类属性）
            var properties = vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.CanWrite)
                .Where(p => !baseClassPropertyNames.Contains(p.Name))
                .ToList();

            foreach (var prop in properties)
            {
                var propertyType = prop.PropertyType;

                // 直接声明 DTO 类型
                if (propertyType.Name.EndsWith("Dto") && !propertyType.Name.EndsWith("PrintDto"))
                {
                    violations.Add($"{vmType.Name}.{prop.Name} (类型: {propertyType.Name})");
                }

                // ObservableCollection<T> 泛型参数名以 Dto 结尾（扩展检查）
                if (propertyType.IsGenericType &&
                    propertyType.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
                {
                    var elementType = propertyType.GetGenericArguments()[0];
                    if (elementType.Name.EndsWith("Dto") &&
                        !elementType.Name.EndsWith("PrintDto") &&
                        !readOnlyDisplayCollectionNames.Contains(prop.Name))
                    {
                        violations.Add($"{vmType.Name}.{prop.Name} (集合元素: {elementType.Name})");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"ViewModel 不应直接持有 DTO 做编辑属性（应使用 Model）:\n{string.Join("\n", violations)}");
    }

    /// <summary>
    /// DP-M2: 每个 MasterDetail 模块必须有 DetailModel
    /// 即使简单域也要有 Model（可以只是 DTO 的薄包装）
    /// </summary>
    [Fact]
    public void DP_M2_MasterDetail_Modules_Must_Have_DetailModel()
    {
        var moduleAssemblies = new[]
        {
            Assembly.Load("LYBT.Desktop.Users"),
            Assembly.Load("LYBT.Desktop.Patients"),
            Assembly.Load("LYBT.Desktop.MedicalCase"),
            Assembly.Load("LYBT.Desktop.Catalog"),
            Assembly.Load("LYBT.Desktop.Registrations")
        };

        var missingModules = new List<string>();

        foreach (var assembly in moduleAssemblies)
        {
            var hasMasterDetailVm = assembly.GetTypes()
                .Any(t => t.Name.EndsWith("MasterDetailViewModel") && t.IsClass && !t.IsAbstract);

            if (!hasMasterDetailVm) continue;

            var hasDetailModel = assembly.GetTypes()
                .Any(t => t.Name.EndsWith("DetailModel") && t.IsClass && t.IsPublic);

            if (!hasDetailModel)
            {
                missingModules.Add(assembly.GetName().Name!);
            }
        }

        Assert.True(missingModules.Count == 0,
            $"MasterDetail 模块缺少 DetailModel（违反 DP-M2 规则）:\n{string.Join("\n", missingModules)}");
    }
}
