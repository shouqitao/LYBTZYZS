# 架构测试补全规格

> 日期: 2026-06-29
> 状态: 草稿
> 优先级: HIGH
> 来源: solution-architecture-optimization.md [O1] H1/H2

## [S1] 问题

架构测试存在两个覆盖缺口：

| 缺口 | 位置 | 影响 |
|------|------|------|
| 无模块间引用检查 | ArchTests.cs P06 | Module.Users → Module.Registration 未被检测 |
| 8 个 Desktop 项目未纳入测试 | DesktopLayerArchTests.cs | 这些项目不受层依赖保护 |

## [S2] 目标

1. 补全模块间引用检查测试
2. 补全 8 个 Desktop 项目的层依赖测试
3. 确保所有架构违规被自动检测

## [S3] 模块间引用检查

### 测试代码

```csharp
// ArchTests.cs 新增
[Fact]
public void ServerModules_Should_Not_Reference_Other_ServerModules()
{
    var moduleAssemblies = new[]
    {
        typeof(LYBT.Module.Formula.FormulaModule).Assembly,
        typeof(LYBT.Module.Herbs.HerbsModule).Assembly,
        typeof(LYBT.Module.MedicalCase.MedicalCaseModule).Assembly,
        typeof(LYBT.Module.Patients.PatientsModule).Assembly,
        typeof(LYBT.Module.Registration.RegistrationModule).Assembly,
        typeof(LYBT.Module.Reports.ReportsModule).Assembly,
        typeof(LYBT.Module.Users.UsersModule).Assembly,
    };

    foreach (var module in moduleAssemblies)
    {
        var referencedModules = module.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("LYBT.Module.") == true 
                     && a.Name != module.GetName().Name)
            .Select(a => a.Name)
            .ToList();

        referencedModules.Should().BeEmpty(
            $"模块 {module.GetName().Name} 不应引用其他模块");
    }
}

[Fact]
public void ServerModules_Should_Not_Reference_WebAPI()
{
    var moduleAssemblies = new[]
    {
        typeof(LYBT.Module.Formula.FormulaModule).Assembly,
        typeof(LYBT.Module.Herbs.HerbsModule).Assembly,
        typeof(LYBT.Module.MedicalCase.MedicalCaseModule).Assembly,
        typeof(LYBT.Module.Patients.PatientsModule).Assembly,
        typeof(LYBT.Module.Registration.RegistrationModule).Assembly,
        typeof(LYBT.Module.Reports.ReportsModule).Assembly,
        typeof(LYBT.Module.Users.UsersModule).Assembly,
    };

    var webApiAssembly = typeof(LYBT.WebAPI.Controllers.HealthController).Assembly;

    foreach (var module in moduleAssemblies)
    {
        var references = module.GetReferencedAssemblies()
            .Any(a => a.FullName == webApiAssembly.FullName);

        references.Should().BeFalse(
            $"模块 {module.GetName().Name} 不应引用 WebAPI");
    }
}
```

## [S4] Desktop 层依赖测试补全

### 测试代码

```csharp
// DesktopLayerArchTests.cs 修改
private static readonly string[] DesktopAssemblies = new[]
{
    // 现有项目
    "LYBT.Desktop.Auth",
    "LYBT.Desktop.Users",
    "LYBT.Desktop.Patients",
    "LYBT.Desktop.Herbs",
    "LYBT.Desktop.Formula",
    "LYBT.Desktop.MedicalCase",
    
    // 新增项目
    "LYBT.Desktop.LocalData",
    "LYBT.Desktop.Navigation",
    "LYBT.Desktop.Printing",
    "LYBT.Desktop.CardReader",
    "LYBT.Desktop.Reports",
    "LYBT.Desktop.Registration",
    "LYBT.Desktop.Sysadmin",
    "LYBT.Desktop.Receptionist",
};

[Fact]
public void Desktop_Should_Not_Depend_On_Server_Layers()
{
    var serverAssemblies = new[]
    {
        "LYBT.Entities",
        "LYBT.Infrastructure",
    };

    foreach (var desktopAssembly in DesktopAssemblies)
    {
        var assembly = Assembly.Load(desktopAssembly);
        var references = assembly.GetReferencedAssemblies()
            .Where(a => serverAssemblies.Contains(a.Name))
            .Select(a => a.Name)
            .ToList();

        // 例外: LYBT.Desktop.LocalData 可以引用 LYBT.Entities
        if (desktopAssembly == "LYBT.Desktop.LocalData")
        {
            references.Should().NotContain("LYBT.Infrastructure",
                "LocalData 不应引用 Infrastructure");
            continue;
        }

        references.Should().BeEmpty(
            $"Desktop 项目 {desktopAssembly} 不应引用 Server 层");
    }
}

[Fact]
public void DesktopModules_Should_Not_Reference_Other_DesktopModules()
{
    // 例外: Registration 可以引用其他模块（协调者角色）
    var moduleAssemblies = new[]
    {
        "LYBT.Desktop.Auth",
        "LYBT.Desktop.Users",
        "LYBT.Desktop.Patients",
        "LYBT.Desktop.Herbs",
        "LYBT.Desktop.Formula",
        "LYBT.Desktop.MedicalCase",
        "LYBT.Desktop.Reports",
        "LYBT.Desktop.Sysadmin",
        "LYBT.Desktop.Receptionist",
    };

    foreach (var moduleAssembly in moduleAssemblies)
    {
        var assembly = Assembly.Load(moduleAssembly);
        var references = assembly.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("LYBT.Desktop.") == true
                     && a.Name != moduleAssembly
                     && !a.Name.StartsWith("LYBT.Desktop.Core"))
            .Select(a => a.Name)
            .ToList();

        references.Should().BeEmpty(
            $"Desktop 模块 {moduleAssembly} 不应引用其他 Desktop 模块");
    }
}
```

## [S5] 实施步骤

1. 在 `LYBT.Tests.Architecture/ArchTests.cs` 新增模块间引用检查测试
2. 在 `LYBT.Tests.Architecture/DesktopLayerArchTests.cs` 补全 8 个项目
3. 新增 Desktop 模块间引用检查测试
4. 运行 `dotnet test tests/LYBT.Tests.Architecture/` 验证

## [S6] 验收标准

1. 模块间引用检查测试覆盖所有 7 个 Server 模块
2. Desktop 层依赖测试覆盖所有 14 个项目
3. Desktop 模块间引用检查测试覆盖所有 9 个模块
4. 所有架构测试通过
5. 违规情况被自动检测并报告
