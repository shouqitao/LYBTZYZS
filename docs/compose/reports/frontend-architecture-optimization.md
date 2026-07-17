---
feature: frontend-architecture-optimization
status: delivered
specs:
  - docs/compose/specs/2026-07-17-frontend-architecture-optimization-design.md
plans:
  - docs/compose/plans/2026-07-17-frontend-architecture-optimization.md
branch: master
commits: 8b3a7432c
---

# 前端 WPF 架构全面优化 — Final Report

## What Was Built

通过 3 轮渐进式优化，使前端 WPF 架构的文档、代码模式、职责边界达到一致且高质量的状态。优化覆盖 `src/Client/Desktop/` 全部项目，不改变运行时行为，仅提升代码质量和文档准确性。

**Round 1 — 文档一致性**: 更新了 `DESKTOP_ARCHITECTURE_STANDARD.md`（Mapperly、CTK MVVM、当前基类）、`Desktop/README.md`（补充模块和角色）、`Desktop/AGENTS.md`（移除 AutoMapper 依赖）、各模块 README（修正映射器引用）。确认 `PatientViewState` 和 `IPatientCommandHandler` 已被移除。

**Round 2 — ViewModel 体系统一**: 迁移了 `SystemSettingsViewModel`、`PendingQueueViewModel`、`CardReaderViewModel` 从 `DelegateCommand` 到 `[RelayCommand]`。更新了架构测试基类白名单（移除旧基类，添加 `CoreViewModelBase`/`NavigableViewModelBase`/`MasterDetailViewModelBase`/`ChildViewModelBase`）。新增 DelegateCommand 禁止测试（含 CanExecute 例外）。

**Round 3 — Shell 拆分 + 模块治理**: 评估 `ServiceCollectionExtensions.cs`（~200行，8个方法，结构合理无需拆分）。新增模块边界隔离测试。评估 4 个薄包装 View（~16行，合法设计）。确认 Shell README 已是最新。

## Architecture

### 文档体系

所有架构文档现在与代码完全对齐:
- 技术栈: Riok.Mapperly (编译期映射) + CommunityToolkit.Mvvm (`[RelayCommand]`)
- 基类: `CoreViewModelBase` → `NavigableViewModelBase` → `MasterDetailViewModelBase<TListItem, TDetail>` → `DialogViewModelBase` → `ChildViewModelBase`
- 映射: Mapperly `[Mapper]` + `[MapProperty]`（非 AutoMapper Profile）

### 命令模式

| 场景 | 模式 | 示例 |
|------|------|------|
| 标准 VM | `[RelayCommand]` | `SystemSettingsViewModel.SaveAsync()` |
| CanExecute 跨 VM 边界 | `DelegateCommand` + `RaiseCanExecuteChanged` | `MedicalCaseCommandsViewModel` (ChildViewModelBase) |
| CanExecute 外部状态 | `DelegateCommand` + 手动刷新 | `LoginViewModel`, `MedicalCaseWorkspaceViewModel` |
| UI 控件 Code-Behind | `DelegateCommand` | `SearchBox`, `BaseDetailContainer` |
| 服务层 | `DelegateCommand` | `MenuManager` (ICommand 属性) |

### 架构测试

83 个架构测试全部通过，包括:
- VM 基类白名单验证
- DelegateCommand 禁止测试（含 ChildViewModelBase 和 CanExecute 例外）
- 模块边界隔离测试（业务模块不得相互引用）
- Repository 接口位置验证
- P-01/P-03/P-06/P-07/P-08 分层规则

## Verification

| 验证项 | 结果 |
|--------|------|
| `dotnet build LYBTZYZS.sln` | 0 errors, 0 warnings |
| `dotnet test LYBT.Tests.Architecture/` | 83 pass, 1 skip (pre-existing) |
| `grep "AutoMapper" *.md` | 仅 "is forbidden" / "替代" 上下文 |
| `grep "UnifiedViewModelBase" *.cs` | 0 结果 |
| `grep "new DelegateCommand" *.cs` | 仅 ChildViewModelBase / UI 控件 / 服务层 |

## Journey Log

- [lesson] DelegateCommand with CanExecute on external state (parent VM state) cannot be cleanly migrated to [RelayCommand] — the source generator's CanExecute property observation doesn't work across VM boundaries. Whitelist these cases in architecture tests.
- [lesson] `LYBT.Desktop.Models` assembly was removed from the project but still referenced in architecture tests — always verify assembly existence when tests fail with `FileNotFoundException`.
- [pivot] Decided not to split `ServiceCollectionExtensions.cs` — at ~200 lines with 8 focused methods, it's at the threshold but well-organized. Splitting would add complexity without benefit.

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/specs/2026-07-17-frontend-architecture-optimization-design.md` | Design spec | Problem analysis and strategy |
| `docs/compose/plans/2026-07-17-frontend-architecture-optimization.md` | Implementation plan | 15 tasks across 3 rounds |
| `src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md` | Architecture standard | Updated: Mapperly, CTK MVVM, current base classes |
| `tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs` | Architecture tests | Updated whitelist, added DelegateCommand ban test |
