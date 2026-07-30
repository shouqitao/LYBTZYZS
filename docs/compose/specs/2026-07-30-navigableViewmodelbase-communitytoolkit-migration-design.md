---
feature: navigableViewModelBase-communitytoolkit-migration
status: delivered
updated: 2026-07-30
scope: Desktop ViewModel 基类迁移（Prism BindableBase → CommunityToolkit ObservableObject）
---

# NavigableViewModelBase CommunityToolkit Migration

## [S1] Problem

项目 AGENTS.md 定义标准为 CommunityToolkit.Mvvm（`[ObservableProperty]`/`[RelayCommand]`），但核心 ViewModel 基类 `NavigableViewModelBase` 仍继承自 Prism 的 `BindableBase`。这导致：

1. **技术栈半迁移**：约 20+ VM 用 CommunityToolkit，但基类仍是 Prism，形成混合架构
2. **新开发者困惑**：AGENTS.md 说用 CommunityToolkit，但基类代码是 Prism
3. **LoginViewModel 等 VM 被迫用 DelegateCommand**：因为基类是 BindableBase，无法自然使用 `[RelayCommand]`

## [S2] Scope

- **迁移目标**：`NavigableViewModelBase` 从 `Prism.BindableBase` → `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
- **影响范围**：所有继承 `NavigableViewModelBase` 的 ViewModel（约 20+ 个）
- **依赖链**：`NavigableViewModelBase` → `MasterDetailViewModelBase<TListDto, TDetailModel>` → 各业务 VM

## [S3] Key Decisions

1. **ObservableObject vs BindableBase**：两者都提供 `INotifyPropertyChanged`，但 ObservableObject 是 CommunityToolkit 的标准基类，支持 `[ObservableProperty]` 源生成器

2. **Prism 特性保留**：BindableBase 提供的 Prism 特性（如 `PropertyChanging`/`PropertyChanged` 虚方法）在 ObservableObject 中没有直接等价物。需要评估这些特性是否被使用，如果使用，需要替代方案

3. **ChildViewModelBase 已迁移**：`ChildViewModelBase` 已经继承 `ObservableObject`，说明迁移是可行的

4. **DelegateCommand 迁移**：基类迁移后，所有 VM 的 DelegateCommand 应改为 `[RelayCommand]` 或 `AsyncRelayCommand`

## [S4] Risk Assessment

- **HIGH**：影响 20+ VM，回归风险高
- **MEDIUM**：某些 VM 可能依赖 BindableBase 的 Prism 特性（需要逐一检查）
- **LOW**：ObservableObject 的 API 与 BindableBase 高度兼容

## [S5] Recommended Approach

1. **Phase 1：基类迁移** — 修改 `NavigableViewModelBase` 继承链，从 BindableBase → ObservableObject
2. **Phase 2：命令迁移** — 逐一将各 VM 的 DelegateCommand 改为 CommunityToolkit 命令
3. **Phase 3：架构测试更新** — 更新 DelegateCommand 限制测试的例外列表

## [S6] Out of Scope

- `MedicalCaseWorkspaceViewModel` 的 CanExecute 跨 VM 边界问题（单独评估）
- `BindableBase` 在 Model Item 类中的使用（`FormulaItem`、`PatientItem` 等，这些是 UI 绑定模型，非 ViewModel，保留合理）

## [S7] Acceptance Criteria

- `NavigableViewModelBase` 继承 `ObservableObject`
- 所有继承 VM 的命令使用 CommunityToolkit
- `dotnet build` 通过
- `dotnet test tests/LYBT.Tests.Desktop/` 通过
- 架构测试 `ViewModels_Should_Not_Use_New_DelegateCommand` 例外列表清零或最小化
