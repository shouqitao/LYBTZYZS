# 测试设计方案 - 架构测试综合文档

## 1. 模块概述

架构测试验证依赖关系、命名规范和层级约束。

| 模块 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| LYBT.ArchTests | 58 | 80 | +22 |
| LYBT.Server.ArchTests | ~10 | 30 | +20 |
| **总计** | **~68** | **110** | **+42** |

---

## 2. LYBT.ArchTests (+22)

### 2.1 层级依赖测试 (8个)

```
Modules_ShouldNotDependOnWebAPI
Shared_ShouldNotDependOnModules
Entities_ShouldNotDependOnServices
Repositories_ShouldOnlyDependOnEntities
Services_ShouldNotDependOnControllers
DTOs_ShouldNotDependOnEntities
Interfaces_ShouldNotDependOnImplementations
Desktop_ShouldNotDependOnServer
```

### 2.2 命名规范测试 (8个)

```
Services_ShouldEndWithService
Repositories_ShouldEndWithRepository
Controllers_ShouldEndWithController
ViewModels_ShouldEndWithViewModel
DTOs_ShouldEndWithDto
Validators_ShouldEndWithValidator
Handlers_ShouldEndWithHandler
Extensions_ShouldEndWithExtensions
```

### 2.3 接口规范测试 (6个)

```
Interfaces_ShouldStartWithI
Services_ShouldHaveInterface
Repositories_ShouldHaveInterface
Handlers_ShouldHaveInterface
Interfaces_ShouldBeInInterfacesFolder
Implementations_ShouldNotBeInInterfacesFolder
```

---

## 3. LYBT.Server.ArchTests (+20)

### 3.1 模块边界测试 (8个)

```
AuthModule_ShouldNotDependOnPatientsModule
PatientsModule_ShouldNotDependOnAuthModule
HerbsModule_ShouldNotDependOnPatientsModule
FormulaModule_CanDependOnHerbsModule
MedicalCaseModule_CanDependOnPatientsModule
UsersModule_ShouldNotDependOnMedicalCaseModule
SyncModule_CanDependOnAllDataModules
WebAPI_CanDependOnAllModules
```

### 3.2 数据库规范测试 (6个)

```
Entities_ShouldInheritFromBaseEntity
Entities_ShouldHaveIdProperty
Entities_ShouldHaveAuditProperties
Repositories_ShouldUseDbContext
DbContext_ShouldNotBeInjectedIntoServices
Repositories_ShouldBeRegisteredAsScoped
```

### 3.3 API 规范测试 (6个)

```
Controllers_ShouldHaveApiControllerAttribute
Controllers_ShouldHaveRouteAttribute
Actions_ShouldHaveHttpMethodAttribute
Actions_ShouldReturnActionResult
Controllers_ShouldUseAuthorizeAttribute
Controllers_ShouldInjectServices
```

---

## 4. 测试实现示例

### 4.1 层级依赖测试

```csharp
public class LayerDependencyTests
{
    [Fact]
    public void Modules_ShouldNotDependOnWebAPI()
    {
        var moduleAssemblies = Types.InAssemblies(GetModuleAssemblies());

        var result = moduleAssemblies
            .ShouldNot()
            .HaveDependencyOn("LYBT.WebAPI")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames?.FirstOrDefault());
    }

    [Fact]
    public void Shared_ShouldNotDependOnModules()
    {
        var sharedAssemblies = Types.InAssemblies(GetSharedAssemblies());

        var result = sharedAssemblies
            .ShouldNot()
            .HaveDependencyOnAny(GetModuleNamespaces())
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames?.FirstOrDefault());
    }

    private static Assembly[] GetModuleAssemblies()
    {
        return new[]
        {
            typeof(AuthModule).Assembly,
            typeof(PatientsModule).Assembly,
            typeof(HerbsModule).Assembly,
            typeof(FormulaModule).Assembly,
            typeof(UsersModule).Assembly,
            typeof(MedicalCaseModule).Assembly,
            typeof(SyncModule).Assembly
        };
    }

    private static string[] GetModuleNamespaces()
    {
        return new[]
        {
            "LYBT.Module.Auth",
            "LYBT.Module.Patients",
            "LYBT.Module.Herbs",
            "LYBT.Module.Formula",
            "LYBT.Module.Users",
            "LYBT.Module.MedicalCase",
            "LYBT.Module.Sync"
        };
    }
}
```

### 4.2 命名规范测试

```csharp
public class NamingConventionTests
{
    [Fact]
    public void Services_ShouldEndWithService()
    {
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespaceContaining("Services")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames?.FirstOrDefault());
    }

    [Fact]
    public void Repositories_ShouldEndWithRepository()
    {
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespaceContaining("Repositories")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames?.FirstOrDefault());
    }

    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        var result = Types.InCurrentDomain()
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames?.FirstOrDefault());
    }
}
```

### 4.3 模块边界测试

```csharp
public class ModuleBoundaryTests
{
    [Fact]
    public void AuthModule_ShouldNotDependOnPatientsModule()
    {
        var authAssembly = typeof(AuthModule).Assembly;

        var result = Types.InAssembly(authAssembly)
            .ShouldNot()
            .HaveDependencyOn("LYBT.Module.Patients")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames?.FirstOrDefault());
    }

    [Fact]
    public void FormulaModule_CanDependOnHerbsModule()
    {
        var formulaAssembly = typeof(FormulaModule).Assembly;

        // 这个测试验证 Formula 模块可以依赖 Herbs 模块
        // (因为方剂包含药材，这是合理的依赖)
        var result = Types.InAssembly(formulaAssembly)
            .That()
            .HaveDependencyOn("LYBT.Module.Herbs")
            .Should()
            .Exist()
            .GetResult();

        // 这个测试预期会找到依赖，所以成功
        result.IsSuccessful.Should().BeTrue();
    }
}
```

---

## 5. 验收标准

| 指标 | 目标 |
|------|------|
| LYBT.ArchTests 测试数 | 80 |
| LYBT.Server.ArchTests 测试数 | 30 |
| 总测试数 | 110 |
| 层级依赖覆盖 | 100% |
| 命名规范覆盖 | 100% |
| 模块边界覆盖 | 100% |

---

## 6. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | 层级依赖测试 (8个) | 1h |
| 2 | 命名规范测试 (8个) | 1h |
| 3 | 接口规范测试 (6个) | 0.5h |
| 4 | 模块边界测试 (8个) | 1h |
| 5 | 数据库规范测试 (6个) | 0.5h |
| 6 | API 规范测试 (6个) | 0.5h |
| 7 | 编译验证和修复 | 0.5h |
| **总计** | | **~5h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
