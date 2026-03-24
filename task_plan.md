# Desktop 架构优化任务计划

**目标**: 修复 WPF Desktop 架构中的不合理之处，提升代码质量和可维护性

**创建时间**: 2026-03-19
**计划文档**: `docs/plans/2026-03-19-desktop-architecture-optimization-plan.md`

---

## 决策记录

| 编号 | 决策 | 状态 |
|------|------|------|
| ARCH-D01 | 抽象 UI 线程调度器，创建 IUiThreadDispatcher | 待执行 |
| ARCH-D02 | 修复 Models 层依赖方向 | **已取消** (IViewModelServices 已在 Contracts) |
| ARCH-D03 | 清理 ISessionManager 兼容方法 (注释"兼容性保留"的3个方法) | 待执行 |
| ARCH-D04 | 清理死代码文件 (ProblemDetails.cs) | 待执行 |

---

## Phases

### Phase 1: 抽象 UI 线程调度器 (P0)
**状态**: pending
**目标**: 解除 CoreViewModelBase 对 WPF Application.Dispatcher 的直接依赖

Tasks:
- [ ] Task 1: 创建 `IUiThreadDispatcher` 接口 (Contracts/Services)
- [ ] Task 2: 创建 `WpfUiThreadDispatcher` 实现 (Infrastructure/Services)
- [ ] Task 3: 注册 `IUiThreadDispatcher` 到 DI 容器 (Shell)
- [ ] Task 4: 更新 `IViewModelServices` 添加 `IUiThreadDispatcher` 属性
- [ ] Task 5: 重构 `CoreViewModelBase.RunOnUIThread` 委托给接口
- [ ] Task 6: 添加 `WpfUiThreadDispatcherTests` 单元测试

---

### Phase 2: 清理兼容方法 (P1)
**状态**: pending
**目标**: 移除 ISessionManager 中3个"兼容性保留"方法（无调用方）

Tasks:
- [ ] Task 7: 确认搜索无调用方后，从接口和实现中删除 SetCurrentUser / SetUserSession / ClearUserSession

---

### Phase 3: 清理死代码 (P2)
**状态**: pending
**目标**: 删除无引用的文件和空 XML 节点

Tasks:
- [ ] Task 8: 删除 ProblemDetails.cs（无引用），清理 Infrastructure.csproj 空 ItemGroup
- [ ] Task 9: 全量验证（编译 + 测试）

---

## 依赖关系

```
Task 1 (IUiThreadDispatcher 接口)
    |
Task 2 (WpfUiThreadDispatcher 实现)   Task 4 (IViewModelServices 更新)
    |                                       |
Task 3 (DI 注册)                       Task 5 (CoreViewModelBase 重构) <- 依赖 Task 3 + 4
    |
Task 6 (测试) <- 依赖 Task 2

Task 7, Task 8 (并行，独立任务)
    |
Task 9 (全量验证)
```

---

## 测试策略

每 Phase 完成后执行:
```bash
dotnet build LYBT.All.sln
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Architecture/
```
