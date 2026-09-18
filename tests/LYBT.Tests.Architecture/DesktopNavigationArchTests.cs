// N7 架构守卫（设计定案先写测试）：
// 设计 SSOT: docs/compose/specs/desktop-navigation-viewmodel-design-2026-09-18.md §3.4
// 1) NavParams 契约键必须被目标 ViewModel 消费
// 2) ViewNames ⊆ ViewRoleAccess ∪ {Login, AccountSettings}
// 3) 角色 Home 所属模块 ∈ RequiredModules / GetAllModules()
using System.Reflection;
using System.Text.RegularExpressions;
using LYBT.Desktop.Contracts.Roles;
using Xunit;

namespace LYBT.Tests.Architecture;

/// <summary>
/// Desktop 导航架构守卫 — NavParams 契约 / ViewRoleAccess 覆盖 / RoleRequiredModules 对齐。
/// 设计：desktop-navigation-viewmodel-design-2026-09-18.md §2.2/§2.3/§3.4
/// </summary>
public class DesktopNavigationArchTests
{
    /// <summary>设计 §3.1 目标契约类（N1 落地后必须存在）</summary>
    private static readonly string[] RequiredNavContractTypeNames =
    [
        "MedicalCaseNav",
        "PatientManagementNav",
        "RegistrationListNav"
    ];

    /// <summary>契约扫描全集：目标工厂 + 遗留薄封装 + 审计/账户</summary>
    private static readonly string[] OptionalNavContractTypeNames =
    [
        "AuditLogNav",
        "AccountSettingsNav",
        "MedicalCaseNavigationParameters"
    ];

    private static readonly Regex NavKeyCallRegex = new(
        @"(?:GetValue(?:<[^>]+>)?|ContainsKey|TryGetValue(?:<[^>]+>)?)\s*\(\s*(?<key>""[^""]+""|[A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// NavParams 契约测试：Contracts 导航常量类的 const string 键必须在某个 ViewModel 中被 GetValue/ContainsKey/TryGetValue 消费。
    /// </summary>
    [Fact]
    public void NavParams_ContractKeys_ConsumedByTargetViewModel()
    {
        var contractsAssembly = Assembly.Load("LYBT.Desktop.Contracts");

        var missingRequired = RequiredNavContractTypeNames
            .Where(name => FindContractType(contractsAssembly, name) is null)
            .ToList();
        Assert.True(
            missingRequired.Count == 0,
            $"Contracts 导航常量类缺失（设计 §3.1 / N1）: {string.Join(", ", missingRequired)}");

        var contractTypes = RequiredNavContractTypeNames
            .Concat(OptionalNavContractTypeNames)
            .Select(name => FindContractType(contractsAssembly, name))
            .Where(t => t is not null)
            .Cast<Type>()
            .ToList();

        Assert.NotEmpty(contractTypes);

        // 生产键：契约类上的 public const string
        var producedKeys = new Dictionary<string, string>(StringComparer.Ordinal); // value → "Type.Field"
        var constValueByTypeName = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        foreach (var contractType in contractTypes)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in contractType
                         .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                         .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string)))
            {
                var value = field.GetRawConstantValue() as string;
                if (string.IsNullOrEmpty(value))
                    continue;

                map[field.Name] = value;
                producedKeys[value] = $"{contractType.Name}.{field.Name}";
            }

            constValueByTypeName[contractType.Name] = map;
        }

        Assert.NotEmpty(producedKeys);

        // 消费键：Desktop ViewModel / View code-behind 源码中的 GetValue/ContainsKey/TryGetValue 参数
        var desktopRoot = TryFindDesktopSrcDirectory();
        Assert.NotNull(desktopRoot);

        var consumedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in EnumerateNavConsumerSources(desktopRoot!))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in NavKeyCallRegex.Matches(text))
            {
                var raw = match.Groups["key"].Value.Trim();
                if (raw.Length == 0)
                    continue;

                if (raw.StartsWith('"') && raw.EndsWith('"') && raw.Length >= 2)
                {
                    consumedKeys.Add(raw[1..^1]);
                    continue;
                }

                // Foo.BarKey → 反射解析契约类 const 值
                var parts = raw.Split('.');
                if (parts.Length == 2)
                {
                    if (constValueByTypeName.TryGetValue(parts[0], out var fields) &&
                        fields.TryGetValue(parts[1], out var value))
                    {
                        consumedKeys.Add(value);
                    }
                }
                else if (parts.Length == 1)
                {
                    // 裸成员名：在已知契约类字段名中解析
                    foreach (var fields in constValueByTypeName.Values)
                    {
                        if (fields.TryGetValue(parts[0], out var value))
                        {
                            consumedKeys.Add(value);
                            break;
                        }
                    }
                }
            }
        }

        var unconsumed = producedKeys
            .Where(kv => !consumedKeys.Contains(kv.Key))
            .Select(kv => $"{kv.Value} (\"{kv.Key}\")")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unconsumed.Count == 0,
            $"导航参数契约键只写不读（设计 §3.4-1，禁止参数只写不读）:\n{string.Join("\n", unconsumed)}");
    }

    /// <summary>
    /// ViewRoleAccess 覆盖：ViewNames 全集 ⊆ ViewRoleAccess 键 ∪ {Login, AccountSettings}。
    /// Login 匿名放行、AccountSettings 保持未列入=放行（设计 §2.2）。
    /// </summary>
    [Fact]
    public void ViewRoleAccess_CoversAllRegisterForNavigationViews()
    {
        var infrastructure = Assembly.Load("LYBT.Desktop.Infrastructure");
        var viewNamesType = infrastructure.GetType("LYBT.Desktop.Infrastructure.Constants.ViewNames", throwOnError: false);
        Assert.NotNull(viewNamesType);

        var viewNameValues = viewNamesType!
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => f.GetRawConstantValue() as string)
            .Where(v => !string.IsNullOrEmpty(v))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(viewNameValues);

        var coordinatorType = infrastructure.GetType(
            "LYBT.Desktop.Infrastructure.Navigation.NavigationCoordinator", throwOnError: false);
        Assert.NotNull(coordinatorType);

        var accessField = coordinatorType!.GetField(
            "ViewRoleAccess", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(accessField);

        var accessDict = accessField!.GetValue(null) as System.Collections.IEnumerable;
        Assert.NotNull(accessDict);

        var accessKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in accessDict!)
        {
            var entryType = entry.GetType();
            var keyProp = entryType.GetProperty("Key")
                          ?? entryType.GetField("Key");
            if (keyProp is null)
                continue;

            var key = keyProp is PropertyInfo pi
                ? pi.GetValue(entry) as string
                : ((FieldInfo)keyProp).GetValue(entry) as string;
            if (!string.IsNullOrEmpty(key))
                accessKeys.Add(key!);
        }

        // 显式豁免表（设计 §2.2：Login 匿名放行；AccountSettings 未列入=放行）
        var explicitExemptions = new HashSet<string>(StringComparer.Ordinal)
        {
            "LoginView",
            "AccountSettingsView"
        };

        var uncovered = viewNameValues
            .Where(v => !accessKeys.Contains(v) && !explicitExemptions.Contains(v))
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            uncovered.Count == 0,
            $"ViewNames 未被 ViewRoleAccess 覆盖且不在豁免表（设计 §3.4-3）: {string.Join(", ", uncovered)}");
    }

    /// <summary>
    /// 角色 Home 所属模块必须出现在该角色 RequiredModules（或 GetAllModules()）。
    /// 设计 §2.3：懒加载仅用于次级业务页；Home 模块缺失会导致登录后首屏空白。
    /// </summary>
    [Fact]
    public void RoleRequiredModules_ContainHomeViewModule()
    {
        var infrastructure = Assembly.Load("LYBT.Desktop.Infrastructure");
        var contracts = Assembly.Load("LYBT.Desktop.Contracts");

        var viewToModuleMap = GetModuleLazyLoaderViewToModuleMap(infrastructure);
        Assert.NotEmpty(viewToModuleMap);

        var roleDefInterface = contracts.GetType("LYBT.Desktop.Contracts.Roles.IRoleDefinition", throwOnError: false);
        Assert.NotNull(roleDefInterface);

        var definitionTypes = infrastructure
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && roleDefInterface!.IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(definitionTypes);

        var violations = new List<string>();

        foreach (var defType in definitionTypes)
        {
            if (Activator.CreateInstance(defType) is not IRoleDefinition definition)
                continue;

            var homeView = definition.HomeViewName;
            if (string.IsNullOrEmpty(homeView))
            {
                violations.Add($"{defType.Name}: HomeViewName 为空");
                continue;
            }

            // Home 视图若不在懒加载映射中（Shell 直注册），无模块对齐要求
            if (!viewToModuleMap.TryGetValue(homeView!, out var module))
                continue;

            var modules = definition.GetAllModules().ToList();
            if (!modules.Contains(module))
            {
                violations.Add(
                    $"{defType.Name}: Home={homeView} → Module={module}，" +
                    $"但 GetAllModules=[{string.Join(", ", modules)}] 不含该模块（设计 §2.3 / N5）");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"角色 RequiredModules 未包含 Home 所属模块:\n{string.Join("\n", violations)}");
    }

    private static Type? FindContractType(Assembly contractsAssembly, string simpleName)
    {
        return contractsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == simpleName && t is { IsClass: true, IsPublic: true });
    }

    private static Dictionary<string, string> GetModuleLazyLoaderViewToModuleMap(Assembly infrastructure)
    {
        var loaderType = infrastructure.GetType(
            "LYBT.Desktop.Infrastructure.Navigation.ModuleLazyLoader", throwOnError: false);
        Assert.NotNull(loaderType);

        var mapField = loaderType!.GetField(
            "ViewToModuleMap", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(mapField);

        var raw = mapField!.GetValue(null);
        Assert.NotNull(raw);

        // ViewToModuleMap 声明为 IReadOnlyDictionary，运行时实例为 Dictionary
        if (raw is Dictionary<string, string> dict)
            return dict;

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var enumerable = (System.Collections.IEnumerable)raw!;
        foreach (var entry in enumerable)
        {
            var entryType = entry.GetType();
            var key = entryType.GetProperty("Key")!.GetValue(entry) as string;
            var value = entryType.GetProperty("Value")!.GetValue(entry) as string;
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                result[key!] = value!;
        }

        Assert.NotEmpty(result);
        return result;
    }

    private static IEnumerable<string> EnumerateNavConsumerSources(string desktopRoot)
    {
        var roots = new[]
        {
            Path.Combine(desktopRoot, "Modules"),
            Path.Combine(desktopRoot, "Roles"),
            Path.Combine(desktopRoot, "Shell"),
            Path.Combine(desktopRoot, "Core")
        };

        foreach (var root in roots.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                    continue;

                var isViewModel = file.Contains($"{Path.DirectorySeparatorChar}ViewModels{Path.DirectorySeparatorChar}") ||
                                  Path.GetFileName(file).EndsWith("ViewModel.cs", StringComparison.Ordinal);
                var isViewCodeBehind = file.EndsWith(".xaml.cs", StringComparison.Ordinal) &&
                                       file.Contains($"{Path.DirectorySeparatorChar}Views{Path.DirectorySeparatorChar}");

                if (isViewModel || isViewCodeBehind)
                    yield return file;
            }
        }
    }

    /// <summary>从测试程序集位置向上查找仓库根 → src/Client/Desktop</summary>
    private static string? TryFindDesktopSrcDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Client", "Desktop");
            if (File.Exists(Path.Combine(dir.FullName, "LYBTZYZS.sln")) && Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }
}
