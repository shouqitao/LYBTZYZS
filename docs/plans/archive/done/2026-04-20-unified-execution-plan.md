# LYBTZYZS 统一执行计划

> **Created**: 2026-04-20
> **Status**: Ready for Execution
> **Merges**: `2026-03-31-service-split-refactoring.md` + `2026-04-11-post-refactoring-todo-plan.md`
> **Supersedes**: Both prior plans (to be archived upon execution start)

---

## 概览

| 层 | 内容 | 任务数 | 预估工时 |
|----|------|--------|----------|
| **Wave 0: 紧急修复** | 失败测试 + UI Bug + 数据丢失 | 3 | ~1.5h |
| **Wave 1: 架构拆分 (Server)** | AuthService/UserService 拆分 | 5 | ~9h |
| **Wave 2: Desktop 修复** | UI 绑定 + 数据流修复 | 5 | ~7h |
| **Wave 3: 体验增强** | 望闻问切 + 操作栏 + 价格计算 | 6 | ~12h |

**总计**: 19 任务，约 29.5 小时

---

## Wave 0: 紧急修复 (P0)

> 前置条件：无。可并行执行。

### W0-1: 修复 PendingQueue 测试失败

| 属性 | 值 |
|------|---|
| 问题 | `SelectPendingCaseAsync_...SkipsSuspend_NavigatesDirectly` 因未 Mock CommonDialogService 导致失败 |
| 修复 | Mock `IWorkspaceHost.CommonDialogService` → `ShowConfirmAsync` 返回 `true` |
| 文件 | `tests/LYBT.Tests.Desktop/PureLogic/Clinical/PendingQueueViewModelTests.cs` |
| 工时 | 15 min |
| 风险 | 低 — 仅测试修改 |
| 验证 | `dotnet test tests/LYBT.Tests.Desktop/ --filter "PendingQueueViewModelTests"` → 5/5 pass |

### W0-2: 修复 LoginView HasMessage 不触发

| 属性 | 值 |
|------|---|
| 问题 | `LoginViewModel.HasMessage` 的 PropertyChanged 永远不触发，用户看不到登录错误信息 |
| 修复 | 在 LoginViewModel 添加 `On_StatusMessageChanged`/`On_ErrorMessageChanged` 触发 `HasMessage` |
| 文件 | `src/Client/Desktop/Core/LYBT.Desktop.Foundation/ViewModels/CoreViewModelBase.cs` 或 `LoginViewModel.cs` |
| 工时 | 30 min |
| 风险 | 中 — 基类修改影响所有 VM，优先在 LoginViewModel 级别修复 |
| 验证 | 新测试 + 手动登录验证 |

### W0-3: 修复 HerbInputDto 缺失 Properties 字段

| 属性 | 值 |
|------|---|
| 问题 | HerbInputDto 缺少 Properties 字段，HerbMapper 忽略了该映射，导致药材创建/更新时数据丢失 |
| 修复 | 添加 Properties 字段 + 移除 `[MapperIgnoreSource(nameof(Properties))]` |
| 文件 | `HerbInputDto.cs`, `HerbMapper.cs` |
| 工时 | 30 min |
| 风险 | 中 — API 契约变更，需验证 Server + Desktop |
| 验证 | `dotnet test tests/LYBT.Tests.Server/ --filter "Herb"` + Desktop 测试 |

**Wave 0 验收门**: 全部测试通过 (`dotnet test tests/LYBT.Tests.Desktop/` + `dotnet test tests/LYBT.Tests.Server/ --filter "Herb"`)

---

## Wave 1: Server 架构拆分 (SRP)

> 前置条件：Wave 0 完成。任务按依赖顺序串行执行。

### W1-1: 拆分 AutoLoginService

| 属性 | 值 |
|------|---|
| 目标 | 从 AuthService (493行) 提取 AutoLogin 逻辑 |
| 新文件 | `LYBT.Module.Auth/Interfaces/IAutoLoginService.cs`, `LYBT.Module.Auth/Services/AutoLoginService.cs` |
| 修改 | `AuthService.cs`, `AuthModule.cs` |
| 工时 | 2h |
| 验证 | 新增 AutoLoginService 单元测试 + 全量 Auth 测试通过 |

### W1-2: 创建 RefreshTokenRepository

| 属性 | 值 |
|------|---|
| 目标 | 替代 AuthService/AutoLoginService 中直接使用 AppDbContext |
| 新文件 | `IRefreshTokenRepository.cs`, `RefreshTokenRepository.cs`, `IAutoLoginTokenRepository.cs`, `AutoLoginTokenRepository.cs` |
| 修改 | `AuthService.cs`, `AutoLoginService.cs`, `AuthModule.cs` |
| 工时 | 2h |
| 依赖 | W1-1 |
| 验证 | AuthService 无直接 AppDbContext 注入 + 测试通过 |

### W1-3: 拆分 UserQueryService

| 属性 | 值 |
|------|---|
| 目标 | 从 UserService (587行) 提取查询逻辑 |
| 新文件 | `LYBT.Module.Users/Interfaces/IUserQueryService.cs`, `LYBT.Module.Users/Services/UserQueryService.cs` |
| 修改 | `UserService.cs`, `UsersModule.cs` |
| 工时 | 2h |
| 验证 | 新增 UserQueryService 单元测试 |

### W1-4: 拆分 UserPasswordService

| 属性 | 值 |
|------|---|
| 目标 | 从 UserService 提取密码逻辑 (ResetPassword, ValidatePassword, ChangePassword) |
| 新文件 | `IUserPasswordService.cs`, `UserPasswordService.cs` |
| 修改 | `UserService.cs` |
| 工时 | 2h |
| 依赖 | W1-3 |
| 验证 | 密码相关测试通过 |

### W1-5: 拆分 UserStatusService

| 属性 | 值 |
|------|---|
| 目标 | 从 UserService 提取状态管理 (ToggleStatus, Restore) |
| 新文件 | `IUserStatusService.cs`, `UserStatusService.cs` |
| 修改 | `UserService.cs` |
| 工时 | 1h |
| 依赖 | W1-3 |
| 验证 | 状态相关测试通过 |

**Wave 1 验收门**:
- `dotnet test tests/LYBT.Tests.Server/` → 全部通过
- AuthService < 300 行，UserService < 300 行
- 架构测试验证无服务直接注入 AppDbContext

---

## Wave 2: Desktop 修复与改进 (P1)

> 前置条件：Wave 0 完成。W2-1/W2-2/W2-5 可并行，W2-3/W2-4 依赖 W2-1。

### W2-1: 修复 IsEnabled 作用域

| 属性 | 值 |
|------|---|
| 问题 | MedicalCaseEditControl 的 IsEnabled 可能禁用整个控件而非单个字段 |
| 文件 | `MedicalCaseWorkspaceView.xaml`, `MedicalCaseEditControl.xaml` |
| 工时 | 1h |
| 验证 | 手动测试：单个字段可独立切换 |

### W2-2: 修复 EnterEditMode 绑定

| 属性 | 值 |
|------|---|
| 问题 | EnterEditMode 命令在某些导航状态下失败 |
| 文件 | `MedicalCaseWorkspaceView.xaml`, `MedicalCaseWorkspaceViewModel.cs` |
| 工时 | 1h |
| 验证 | ReadOnly → Edit → Editing 状态正确切换 |

### W2-3: 统一 Remark 数据源

| 属性 | 值 |
|------|---|
| 问题 | Remark 字段在 Consultation 和 MedicalCase 层级数据源不一致 |
| 文件 | `MedicalCaseEditControl.xaml`, `ConsultationItem.cs` |
| 工时 | 2h |
| 依赖 | W2-1 |

### W2-4: 添加验证错误显示

| 属性 | 值 |
|------|---|
| 目标 | 在 MedicalCaseEditControl 中显示验证错误 (INotifyDataErrorInfo) |
| 文件 | `MedicalCaseEditControl.xaml`, shared error styles |
| 工时 | 2h |
| 依赖 | W2-1 |

### W2-5: UserEditControl 添加 Remark 字段

| 属性 | 值 |
|------|---|
| 问题 | 用户编辑界面缺少 Remark 字段 |
| 文件 | `UserEditControl.xaml`, `UserEditControl.xaml.cs` |
| 工时 | 1h |
| 验证 | 用户编辑 → Remark 可见可编辑 → 保存后持久化 |

**Wave 2 验收门**: `dotnet test tests/LYBT.Tests.Desktop/` → 全部通过 + 手动 UI 验证

---

## Wave 3: 体验增强 (P2)

> 前置条件：Wave 2 完成。按依赖关系分组执行。

### W3-1: 望闻问切诊断分区

| 属性 | 值 |
|------|---|
| 描述 | 按望闻问切四诊分组显示诊断字段 |
| 工时 | 2h |
| 依赖 | W2-1, W2-3 |

### W3-2: 处方操作引导

| 属性 | 值 |
|------|---|
| 描述 | 添加处方操作提示 (验方导入、历史复制等) |
| 工时 | 2h |

### W3-3: 底部操作栏

| 属性 | 值 |
|------|---|
| 描述 | MedicalCaseWorkspaceView 持久化底部操作栏 (保存/完成/打印/挂起) |
| 工时 | 2h |
| 依赖 | W2-2 |

### W3-4: 实时价格计算

| 属性 | 值 |
|------|---|
| 描述 | 处方药材实时总价计算显示 |
| 工时 | 2h |

### W3-5: 完整性检查指示器

| 属性 | 值 |
|------|---|
| 描述 | 必填字段填写状态可视化 |
| 工时 | 2h |
| 依赖 | W2-4 |

### W3-6: 常用术语快速选择

| 属性 | 值 |
|------|---|
| 描述 | 常用中医诊断术语下拉/自动完成 |
| 工时 | 2h |
| 依赖 | W3-1 |

**Wave 3 验收门**: 全部测试通过 + 手动冒烟测试 (登录 → 选患者 → 建案 → 诊断 → 处方 → 完成)

---

## 依赖图

```
Wave 0 (并行)
  W0-1 ─┐
  W0-2 ─┤
  W0-3 ─┘
         │
    ┌────┴────┐
    ▼         ▼
Wave 1      Wave 2
(串行)      (部分并行)
W1-1         W2-1 ──→ W2-3
 │           W2-1 ──→ W2-4
W1-2          W2-2
 │           W2-5 (独立)
W1-3 ──┬─ W1-4
  │    └─ W1-5
    │         │
    └────┬────┘
         ▼
       Wave 3
       W3-1 (← W2-1, W2-3)
       W3-2 (独立)
       W3-3 (← W2-2)
       W3-4 (独立)
       W3-5 (← W2-4)
       W3-6 (← W3-1)
```

## 文件冲突矩阵

| 文件 | 任务 | 执行顺序 |
|------|------|----------|
| `MedicalCaseEditControl.xaml` | W2-1, W2-3, W2-4, W3-1, W3-5, W3-6 | 严格串行 |
| `MedicalCaseWorkspaceView.xaml` | W2-1, W2-2, W3-3 | 严格串行 |
| `AuthService.cs` | W1-1, W1-2 | 严格串行 |
| `UserService.cs` | W1-3, W1-4, W1-5 | 严格串行 |

## 最终验收标准

- [ ] `dotnet build LYBTZYZS.sln` — 0 errors
- [ ] `dotnet test LYBTZYZS.sln` — 全部通过 (2021+ tests, 0 failures)
- [ ] AuthService < 300 行，UserService < 300 行
- [ ] 无服务直接使用 AppDbContext (架构测试验证)
- [ ] 手动冒烟测试通过：完整临床工作流
- [ ] 无新增编译器警告

---

**计划创建**: 2026-04-20
**合并自**: `2026-03-31-service-split-refactoring.md` + `2026-04-11-post-refactoring-todo-plan.md`
**预计总工时**: ~29.5 小时
