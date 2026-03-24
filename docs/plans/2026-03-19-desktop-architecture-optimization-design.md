# Desktop 架构优化设计

**日期**: 2026-03-19
**版本**: v1.0
**状态**: 设计阶段

---

## 背景

通过 WPF 架构专家分析，Desktop 层架构总体良好（评分 7.9/10），但存在 1 个 Critical 问题和若干中低风险问题需要修复。

---

## 架构决策

| 编号 | 决策 | 说明 |
|------|------|------|
| ARCH-D01 | 抽象 UI 线程调度器 | 将 CoreViewModelBase.RunOnUIThread 抽象为 IUiThreadDispatcher 接口，解除对 WPF 的直接依赖 |
| ARCH-D02 | 修复 Models 层依赖方向 | 将 IViewModelServices 移动到 Contracts 层，消除 Models 对 Infrastructure 的引用 |
| ARCH-D03 | 清理兼容代码 | 制定计划移除标记为 [COMPAT] 的过期方法 |
| ARCH-D04 | 统一服务命名规范 | Service = 业务服务, Manager = 资源管理, Coordinator = 流程编排 |

---

## Phase 1: 抽象 UI 线程调度器 (P0 - Critical)

### 问题

`CoreViewModelBase.RunOnUIThread()` 直接引用 `Application.Current.Dispatcher`，违反 MVVM 原则。

### 设计方案

```csharp
// Contracts/Services/IUiThreadDispatcher.cs
public interface IUiThreadDispatcher
{
    void RunOnUIThread(Action action);
    Task RunOnUIThreadAsync(Func<Task> action);
    bool IsUIThread { get; }
}

// Infrastructure/Services/WpfUiThreadDispatcher.cs
public class WpfUiThreadDispatcher : IUiThreadDispatcher
{
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

    public Task RunOnUIThreadAsync(Func<Task> action)
    {
        // 实现...
    }

    public bool IsUIThread => Application.Current?.Dispatcher?.CheckAccess() ?? true;
}
```

### 修改范围

1. `LYBT.Desktop.Contracts` - 新增 IUiThreadDispatcher 接口
2. `LYBT.Desktop.Infrastructure` - 新增 WpfUiThreadDispatcher 实现
3. `LYBT.Desktop.Models` - 修改 CoreViewModelBase，注入 IUiThreadDispatcher
4. `LYBT.Desktop.Infrastructure` - 修改 IViewModelServices，添加 IUiThreadDispatcher
5. `LYBT.Desktop.Shell` - 注册 IUiThreadDispatcher 到 DI 容器

---

## Phase 2: 修复 Models 层依赖方向 (P1 - High)

### 问题

`LYBT.Desktop.Models` 引用了 `LYBT.Desktop.Infrastructure`，导致依赖方向反转。

### 分析

Models -> Infrastructure 的具体依赖:
1. `CoreViewModelBase` 使用 `IViewModelServices`（定义在 Infrastructure）
2. `IViewModelServices` 聚合了 7 个服务接口

### 设计方案

**选项 A: 移动 IViewModelServices 到 Contracts (推荐)**

将 `IViewModelServices` 接口从 Infrastructure 移动到 Contracts 层:

```
Contracts/
  Services/
    IViewModelServices.cs  (从 Infrastructure 移动)
    INavigationCoordinator.cs
    IDialogService.cs
    ...

Infrastructure/
  Services/
    ViewModelServices.cs   (实现保留在 Infrastructure)
```

修改项目引用:
- `Models.csproj`: 移除 Infrastructure 引用，添加 Contracts 引用
- 其他项目: 无需修改

---

## Phase 3: 清理兼容代码 (P1)

### 问题

`ISessionManager` 等接口中存在标记为 [COMPAT] 的过期方法。

### 清理计划

1. 搜索所有 [COMPAT] 标记代码
2. 检查每个方法的引用情况
3. 对无引用的方法：直接删除
4. 对有引用的方法：
   - 更新调用方使用新 API
   - 然后删除旧方法

### 标记列表 (待验证)

| 接口/类 | 方法 | 状态 |
|---------|------|------|
| ISessionManager | SetCurrentUser | [COMPAT] |
| ISessionManager | SetUserSession | [COMPAT] |
| ISessionManager | ClearUserSession | [COMPAT] |

---

## Phase 4: 代码清理 (P2/P3)

### 死代码清单

| 文件 | 位置 | 操作 |
|------|------|------|
| ProblemDetails.cs | Models/Http/ | 删除 |
| ErrorHandlingServiceExtensions.cs | Shell/Extensions/ | 删除 |
| 空 ItemGroup | Infrastructure.csproj:75-77 | 删除 |

### 命名规范

统一服务命名:
- **Service**: 业务逻辑服务 (PatientService, MedicalCaseService)
- **Manager**: 资源/状态管理 (SessionManager, TokenManager)
- **Coordinator**: 流程编排 (NavigationCoordinator, LoginCoordinator)
- **Provider**: 数据/配置提供 (IConnectionModeProvider)

---

## 测试策略

每 Phase 完成后执行:

```bash
# 编译检查
dotnet build src/Client/Desktop/LYBT.Desktop.sln

# 单元测试
dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Models.Tests/
dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/

# 集成测试
dotnet test tests/LYBT.Tests.Desktop/
```

---

## 风险评估

| Phase | 风险 | 缓解措施 |
|-------|------|----------|
| Phase 1 | 影响所有 ViewModel | 保持 RunOnUIThread 方法签名不变，仅内部实现改为调用接口 |
| Phase 2 | 项目引用变更 | 逐步移动接口，确保编译通过 |
| Phase 3 | 误删仍在使用的方法 | 使用 find_referencing_symbols 确认无引用后再删除 |
| Phase 4 | 无 | 低风险 |

---

## 变更记录

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-03-19 | v1.0 | 初始设计 |
