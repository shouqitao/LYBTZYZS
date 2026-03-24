# Desktop Architecture Optimization Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复 WPF Desktop 层中 3 个具体问题：(1) CoreViewModelBase 对 WPF Dispatcher 的直接依赖，(2) ISessionManager 中无调用方的兼容方法，(3) 死代码清理。

**Architecture:** 采用接口抽象隔离 WPF 运行时依赖，使 ViewModel 单元测试不再需要真实 WPF Application；同步清理确认无调用方的兼容方法和死代码文件。注：Phase 2（依赖方向修复）经代码确认 IViewModelServices 已在 Contracts 层，无需执行。

**Tech Stack:** .NET 8 + WPF + Prism.DryIoc 8.1.97 + CommunityToolkit.Mvvm + NSubstitute（测试）

---

## 前置信息

### 关键文件路径

| 文件 | 说明 |
|------|------|
| `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IViewModelServices.cs` | ViewModel服务聚合接口（已在Contracts） |
| `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/CoreViewModelBase.cs` | ViewModel基类，含RunOnUIThread问题 |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ViewModelServices.cs` | IViewModelServices实现 |
| `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISessionManager.cs` | 含兼容方法接口（SetCurrentUser等） |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs` | ISessionManager实现 |
| `src/Client/Desktop/Core/LYBT.Desktop.Models/Http/ProblemDetails.cs` | 无引用死代码文件 |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj` | 含空ItemGroup待清理 |

### 测试命令

```bash
# 编译检查
dotnet build LYBT.All.sln

# Desktop 单元测试
dotnet test tests/LYBT.Tests.Desktop/ --logger "console;verbosity=normal"

# 架构测试
dotnet test tests/LYBT.Tests.Architecture/ --logger "console;verbosity=normal"
```

### 当前代码现状

**RunOnUIThread 现状** (`CoreViewModelBase.cs:261-290`):
```csharp
protected void RunOnUIThread(Action action)
{
    if (Application.Current?.Dispatcher == null)
    {
        action();
        return;
    }
    if (Application.Current.Dispatcher.CheckAccess())
        action();
    else
        Application.Current.Dispatcher.Invoke(action);
}

protected Task RunOnUIThreadAsync(Func<Task> action)
{
    if (Application.Current?.Dispatcher == null)
        return action();
    return Application.Current.Dispatcher.InvokeAsync(action).Task;
}
```

**兼容方法现状** (`ISessionManager.cs:48,58,68`):
```csharp
void SetCurrentUser(UserDetailDto user, string token);  // 兼容性保留
void SetUserSession(UserDetailDto user, string token);  // SetSession 的别名，兼容性保留
void ClearUserSession();                                // ClearSession 的别名，兼容性保留
```
搜索确认：这3个方法在Desktop层无任何调用方（`.SetCurrentUser(`、`.SetUserSession(`、`.ClearUserSession(` 均无搜索结果）。

**RunOnUIThread 调用方**：仅 `SyncViewModel.cs:413` 一处调用 `RunOnUIThread`，`RunOnUIThreadAsync` 无外部调用。

---

## Task 1: 创建 IUiThreadDispatcher 接口

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IUiThreadDispatcher.cs`

**Step 1: 创建接口文件**

```csharp
using System;
using System.Threading.Tasks;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// UI线程调度器抽象接口。
/// 隔离 ViewModel 对 WPF Dispatcher 的直接依赖，使 ViewModel 可在测试环境中运行。
/// </summary>
public interface IUiThreadDispatcher
{
    /// <summary>
    /// 在UI线程上同步执行 action。如果当前已在UI线程，直接执行；否则切换到UI线程执行。
    /// </summary>
    void RunOnUIThread(Action action);

    /// <summary>
    /// 在UI线程上异步执行 action。
    /// </summary>
    Task RunOnUIThreadAsync(Func<Task> action);

    /// <summary>
    /// 当前是否在UI线程上。
    /// </summary>
    bool IsUIThread { get; }
}
```

**Step 2: 验证编译**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Contracts/LYBT.Desktop.Contracts.csproj
```

期望结果：`Build succeeded`

**Step 3: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IUiThreadDispatcher.cs
git commit -m "feat(desktop): add IUiThreadDispatcher interface to Contracts"
```

---

## Task 2: 创建 WpfUiThreadDispatcher 实现

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/WpfUiThreadDispatcher.cs`

**Step 1: 创建实现文件**

```csharp
using System;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 基于 WPF Dispatcher 的 UI 线程调度器实现。
/// </summary>
public sealed class WpfUiThreadDispatcher : IUiThreadDispatcher
{
    /// <inheritdoc/>
    public bool IsUIThread => Application.Current?.Dispatcher?.CheckAccess() ?? true;

    /// <inheritdoc/>
    public void RunOnUIThread(Action action)
    {
        if (Application.Current?.Dispatcher == null)
        {
            action();
            return;
        }

        if (Application.Current.Dispatcher.CheckAccess())
            action();
        else
            Application.Current.Dispatcher.Invoke(action);
    }

    /// <inheritdoc/>
    public Task RunOnUIThreadAsync(Func<Task> action)
    {
        if (Application.Current?.Dispatcher == null)
            return action();

        return Application.Current.Dispatcher.InvokeAsync(action).Task;
    }
}
```

**Step 2: 验证编译**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj
```

期望结果：`Build succeeded`

**Step 3: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/WpfUiThreadDispatcher.cs
git commit -m "feat(desktop): implement WpfUiThreadDispatcher in Infrastructure"
```

---

## Task 3: 注册 IUiThreadDispatcher 到 DI 容器

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**Step 1: 找到注册位置**

在 `ServiceCollectionExtensions.cs` 中找到 `RegisterFoundationServices()` 方法（注册基础服务的地方），添加一行注册。

搜索定位：
```bash
grep -n "INavigationCoordinator\|INotificationService\|RegisterFoundationServices" src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs | head -10
```

**Step 2: 添加注册**

在 `RegisterFoundationServices` 或 `RegisterPresentationServices` 方法中找到合适位置，添加：

```csharp
containerRegistry.RegisterSingleton<IUiThreadDispatcher, WpfUiThreadDispatcher>();
```

注意：需要添加对应的 using：
```csharp
using LYBT.Desktop.Infrastructure.Services;
```

**Step 3: 验证编译**

```bash
dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
```

期望结果：`Build succeeded`

**Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(desktop): register IUiThreadDispatcher in DI container"
```

---

## Task 4: 更新 IViewModelServices 接口，添加 IUiThreadDispatcher

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IViewModelServices.cs`

**Step 1: 查看当前接口内容**

当前 `IViewModelServices` 包含 7 个属性：LoggerFactory, EventAggregator, RegionManager, SessionManager, UserNotificationService, CommonDialogService, RoleRegistry。

**Step 2: 添加 IUiThreadDispatcher 属性**

在接口末尾（`IRoleRegistry RoleRegistry { get; }` 之后，`}` 之前）添加：

```csharp
/// <summary>
/// UI线程调度器，隔离 ViewModel 对 WPF Dispatcher 的直接依赖
/// </summary>
IUiThreadDispatcher UiThreadDispatcher { get; }
```

**Step 3: 更新 ViewModelServices 实现**

在 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ViewModelServices.cs` 中：

1. 添加属性：
```csharp
public IUiThreadDispatcher UiThreadDispatcher { get; }
```

2. 在构造函数参数中添加：
```csharp
IUiThreadDispatcher uiThreadDispatcher
```

3. 在构造函数体中赋值：
```csharp
UiThreadDispatcher = uiThreadDispatcher ?? throw new ArgumentNullException(nameof(uiThreadDispatcher));
```

**Step 4: 验证编译**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj
```

期望结果：`Build succeeded` 或编译错误指向需要更新的调用方。

**Step 5: 如有编译错误，找到 ViewModelServices 的注册位置**

搜索注册点：
```bash
grep -n "ViewModelServices" src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
```

找到后在该注册处添加 `IUiThreadDispatcher` 参数。

**Step 6: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IViewModelServices.cs
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ViewModelServices.cs
git commit -m "feat(desktop): add IUiThreadDispatcher to IViewModelServices"
```

---

## Task 5: 重构 CoreViewModelBase，使用 IUiThreadDispatcher

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/CoreViewModelBase.cs`

**Step 1: 移除 WPF Application 引用**

在 `CoreViewModelBase.cs` 中找到并删除（或注释）WPF 相关的 using：
- 查找 `using System.Windows;`

**Step 2: 替换 RunOnUIThread 实现**

将第 261-277 行（`protected void RunOnUIThread`）替换为：

```csharp
/// <summary>
/// 在UI线程上同步执行 action。委托给 IUiThreadDispatcher 实现，解耦 WPF 依赖。
/// </summary>
protected void RunOnUIThread(Action action)
    => Services.UiThreadDispatcher.RunOnUIThread(action);
```

**Step 3: 替换 RunOnUIThreadAsync 实现**

将第 282-290 行（`protected Task RunOnUIThreadAsync`）替换为：

```csharp
/// <summary>
/// 在UI线程上异步执行 action。委托给 IUiThreadDispatcher 实现，解耦 WPF 依赖。
/// </summary>
protected Task RunOnUIThreadAsync(Func<Task> action)
    => Services.UiThreadDispatcher.RunOnUIThreadAsync(action);
```

**Step 4: 验证编译**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Models/LYBT.Desktop.Models.csproj
```

期望结果：`Build succeeded`

**Step 5: 验证 SyncViewModel 仍正常编译**（唯一调用方）

```bash
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Sync/LYBT.Desktop.Sync.csproj
```

期望结果：`Build succeeded`

**Step 6: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/CoreViewModelBase.cs
git commit -m "refactor(desktop): decouple CoreViewModelBase from WPF Dispatcher via IUiThreadDispatcher"
```

---

## Task 6: 为 IUiThreadDispatcher 添加测试

**Files:**
- Create: `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Services/WpfUiThreadDispatcherTests.cs`

**Step 1: 编写单元测试**

```csharp
using FluentAssertions;
using LYBT.Desktop.Infrastructure.Services;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Services;

/// <summary>
/// WpfUiThreadDispatcher 单元测试。
/// 注：WPF Dispatcher 测试在无UI线程时走回退路径（null检查）。
/// </summary>
public class WpfUiThreadDispatcherTests
{
    private readonly WpfUiThreadDispatcher _sut = new();

    [Fact]
    public void RunOnUIThread_WhenDispatcherNull_ExecutesActionDirectly()
    {
        // Arrange - 测试环境无WPF Application，走回退路径
        var executed = false;

        // Act
        _sut.RunOnUIThread(() => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task RunOnUIThreadAsync_WhenDispatcherNull_ExecutesActionDirectly()
    {
        // Arrange
        var executed = false;

        // Act
        await _sut.RunOnUIThreadAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void IsUIThread_WhenNoWpfApplication_ReturnsTrue()
    {
        // Arrange & Act
        var result = _sut.IsUIThread;

        // Assert - 无WPF Application时，默认认为在UI线程
        result.Should().BeTrue();
    }
}
```

**Step 2: 运行测试，确认通过**

```bash
dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/ --filter "WpfUiThreadDispatcherTests" -v
```

期望结果：`3 passed`

**Step 3: 提交**

```bash
git add tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Services/WpfUiThreadDispatcherTests.cs
git commit -m "test(desktop): add WpfUiThreadDispatcher unit tests"
```

---

## Task 7: 清理 ISessionManager 兼容方法

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISessionManager.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs`

**Step 1: 确认无调用方**

执行搜索确认（期望无结果）：
```bash
grep -rn "\.SetCurrentUser\|\.SetUserSession\|\.ClearUserSession" src/Client/Desktop/ --include="*.cs"
grep -rn "\.SetCurrentUser\|\.SetUserSession\|\.ClearUserSession" tests/ --include="*.cs"
```

期望结果：无任何搜索结果（这3个方法无外部调用方）。

**Step 2: 从 ISessionManager 接口删除兼容方法**

删除以下代码块（`ISessionManager.cs` 中）：

删除 `SetCurrentUser` 方法声明（约第44-49行）：
```csharp
// 删除这段
/// <summary>
/// 设置当前用户
/// </summary>
void SetCurrentUser(UserDetailDto user, string token);
```

删除 `SetUserSession` 方法声明（约第54-58行）：
```csharp
// 删除这段
/// <summary>
/// 设置用户会话（SetSession 的别名，兼容性保留）
/// </summary>
void SetUserSession(UserDetailDto user, string token);
```

删除 `ClearUserSession` 方法声明（约第63-68行）：
```csharp
// 删除这段
/// <summary>
/// 清除用户会话（ClearSession 的别名，兼容性保留）
/// </summary>
void ClearUserSession();
```

**Step 3: 从 SessionManager 实现中删除兼容方法**

删除以下代码块（`SessionManager.cs` 中）：

```csharp
// 删除 SetCurrentUser 实现（约第35-47行）
public void SetCurrentUser(UserDetailDto user, string token)
{
    // ... 整个方法体
}

// 删除别名方法（约第48,58行）
public void SetUserSession(UserDetailDto user, string token) => SetSession(user, token);
public void ClearUserSession() => ClearSession();
```

**Step 4: 验证编译**

```bash
dotnet build LYBT.All.sln
```

期望结果：`Build succeeded`（若有调用方会在此暴露，按错误提示修复）。

**Step 5: 运行测试**

```bash
dotnet test tests/LYBT.Tests.Desktop/ --logger "console;verbosity=minimal"
```

期望结果：所有测试通过，无失败。

**Step 6: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISessionManager.cs
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs
git commit -m "refactor(desktop): remove deprecated compat methods from ISessionManager"
```

---

## Task 8: 清理死代码文件

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Models/Http/ProblemDetails.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj`（删除空ItemGroup）

**Step 1: 确认 ProblemDetails.cs 无引用**

```bash
grep -rn "ProblemDetails" src/Client/Desktop/ --include="*.cs"
```

期望结果：仅 `ProblemDetails.cs` 自身文件包含该类名，无外部引用。

**Step 2: 删除 ProblemDetails.cs**

```bash
rm src/Client/Desktop/Core/LYBT.Desktop.Models/Http/ProblemDetails.cs
```

如果删除后 `Http/` 目录为空，也删除该目录：
```bash
# 检查目录是否为空
ls src/Client/Desktop/Core/LYBT.Desktop.Models/Http/
# 如果为空则删除
rmdir src/Client/Desktop/Core/LYBT.Desktop.Models/Http/
```

**Step 3: 清理 Infrastructure.csproj 中的空 ItemGroup**

在 `LYBT.Desktop.Infrastructure.csproj` 中找到并删除（约第75-77行）：
```xml
<!-- 删除这段空的ItemGroup -->
<ItemGroup Label="Core_New Dependencies">
</ItemGroup>
```

**Step 4: 验证编译**

```bash
dotnet build LYBT.All.sln
```

期望结果：`Build succeeded`

**Step 5: 提交**

```bash
git add -A
git commit -m "chore(desktop): remove dead code ProblemDetails.cs and empty ItemGroup"
```

---

## Task 9: 全量验证

**Step 1: 全量编译**

```bash
dotnet build LYBT.All.sln
```

期望：`0 Error(s)`

**Step 2: 全量测试**

```bash
dotnet test tests/LYBT.Tests.Desktop/ --logger "console;verbosity=normal"
dotnet test tests/LYBT.Tests.Architecture/ --logger "console;verbosity=normal"
```

期望：所有测试通过，无新增失败

**Step 3: 手动验证检查点**

确认以下代码模式已消失：
```bash
# 确认没有直接引用 Application.Current.Dispatcher（在ViewModel基类中）
grep -n "Application.Current.Dispatcher" src/Client/Desktop/Core/LYBT.Desktop.Models/ -r
```

期望：无结果（或仅在非ViewModel文件中存在）

**Step 4: 架构测试确认**

```bash
dotnet test tests/LYBT.Tests.Architecture/ -v
```

期望：76个架构测试全部通过

---

## 执行顺序总结

```
Task 1 → Task 2 → Task 3 (并行: Task 4)
                              |
                         Task 5 (依赖 Task 3+4)
                              |
                         Task 6 (测试，依赖 Task 2)
                              |
                    Task 7 (并行: Task 8)
                              |
                         Task 9 (全量验证)
```

**实际最短路径**（串行执行）：Task 1 → 2 → 4 → 5 → 3 → 6 → 7 → 8 → 9

---

## 修正说明

经代码验证，以下架构分析报告中的部分问题已不成立：

| 原报告问题 | 实际情况 | 处理 |
|-----------|---------|------|
| "IViewModelServices 定义在 Infrastructure" | 已在 Contracts 层 (`Services/IViewModelServices.cs`) | Phase 2 取消 |
| "ErrorHandlingServiceExtensions 死代码" | 文件已不存在 | 无需处理 |
| "[COMPAT] 标记搜索无结果" | 代码中未使用 `[COMPAT]` 标记，而是注释"兼容性保留" | 按实际位置处理 |

---

## 变更记录

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-03-19 | v1.0 | 初始实施计划（基于代码实际调研） |
