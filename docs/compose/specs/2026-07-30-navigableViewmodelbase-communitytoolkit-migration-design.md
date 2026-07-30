---
feature: navigableViewModelBase-communitytoolkit-migration
status: invalid-superseded
updated: 2026-07-30
scope: Desktop ViewModel 基类迁移（Prism BindableBase → CommunityToolkit ObservableObject）
---

# ~~NavigableViewModelBase CommunityToolkit Migration~~

> **⚠️ 此 spec 基于错误前提，已作废。**
>
> 审计发现 `NavigableViewModelBase` **已经**继承 `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`（非 Prism `BindableBase`）。LoginViewModel 等 VM 使用 `DelegateCommand` 是 VM 自身选择，不是基类限制。截至 2026-07-30，全部 51 个 ViewModel 已使用 CommunityToolkit 命令，零 `DelegateCommand` 使用。
>
> 详见 `docs/compose/specs/2026-07-30-mvvm-audit-correction.md`。

## [S1] Problem

~~项目 AGENTS.md 定义标准为 CommunityToolkit.Mvvm（`[ObservableProperty]`/`[RelayCommand]`），但核心 ViewModel 基类 `NavigableViewModelBase` 仍继承自 Prism 的 `BindableBase`。~~

**实际状态**：`NavigableViewModelBase` 已继承 `ObservableObject`，所有 51 个 VM 已使用 CommunityToolkit 命令。此 spec 的前提假设不成立。
