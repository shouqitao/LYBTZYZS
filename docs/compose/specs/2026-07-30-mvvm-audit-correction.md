---
feature: mvvm-audit-correction
status: delivered
updated: 2026-07-30
scope: 修正 MVVM 基类架构的错误分析，同步文档与代码
---

# MVVM 架构审计修正

## [S1] Problem

2026-07-30 的架构审计错误地认为 `NavigableViewModelBase` 继承自 Prism `BindableBase`，并据此创建了迁移 spec。实际审计发现：

1. **`NavigableViewModelBase` 已继承 `ObservableObject`**（CommunityToolkit.Mvvm，非 Prism）
2. **全部 51 个 ViewModel 已使用 CommunityToolkit 命令**，零 `DelegateCommand` 使用
3. **多份文档仍声称使用 `DelegateCommand`**，与代码实际不符

## [S2] Root Cause

错误分析的原因：
- 早期代码确实使用 Prism `BindableBase` + `DelegateCommand`
- 多轮迁移（2026-07 前端架构优化）已将基类和命令迁移到 CommunityToolkit
- 但文档未同步更新，导致后续审计基于过时文档得出错误结论
- `LoginViewModel` 等 VM 使用 `new AsyncRelayCommand()`（手动实例化而非 `[RelayCommand]` 属性），被误判为 `DelegateCommand`

## [S3] 审计结果（2026-07-30 代码验证）

### 基类继承关系

| 基类 | 继承自 | 子 VM 数量 |
|------|--------|-----------|
| `NavigableViewModelBase` | `ObservableObject` | 46（含 Dialog/MasterDetail 子类） |
| `ChildViewModelBase` | `ObservableObject` | 6 |
| `HerbItemViewModelBase` | `ObservableObject` | 2 |
| 直接继承 `ObservableObject` | — | 6（Editor VMs + Controls） |

### 命令使用模式

| 模式 | 数量 | 示例 |
|------|------|------|
| `[RelayCommand]` 属性 | 33 | MainWindowViewModel, ClinicalHomeViewModel |
| `[ObservableProperty]` 属性 | 30 | 大部分 VM |
| 手动 `new AsyncRelayCommand()` | 4 | LoginViewModel, MedicalCaseCommandsViewModel, MedicalCaseWorkspaceViewModel |
| 手动 `new RelayCommand()` | 4 | 同上（混合使用） |
| `DelegateCommand` | **0** | 无 |

### 非 ViewModel 的 DelegateCommand（保留合理）

| 文件 | 用途 | 保留理由 |
|------|------|---------|
| `MenuManager.cs` | 服务层快捷键绑定 | 非 ViewModel，不适用 MVVM 源生成器 |
| `SearchBox.xaml.cs` | UI 控件 Code-behind | 非 ViewModel |
| `BaseDetailContainer.xaml.cs` | UI 控件 Code-behind | 非 ViewModel |

## [S4] 文档修正清单

| 文件 | 问题 | 修正 |
|------|------|------|
| `src/Client/Desktop/Modules/LYBT.Desktop.Auth/AGENTS.md` | "Commands use Prism DelegateCommand" | 改为 CommunityToolkit AsyncRelayCommand/RelayCommand |
| `src/Client/Desktop/Modules/LYBT.Desktop.Auth/README.md` | 6 处标注 `DelegateCommand` | 改为实际命令类型 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/README.md` | "9 个 DelegateCommand" | 改为 "9 个 CommunityToolkit 命令" |
| `docs/compose/specs/2026-07-30-navigableViewmodelbase-*.md` | 基于错误前提的迁移 spec | 标记为 invalid-superseded |

## [S5] Acceptance Criteria

- [x] `NavigableViewModelBase` 确认继承 `ObservableObject`
- [x] 全部 51 个 VM 确认使用 CommunityToolkit 命令
- [x] 修正相关文档
- [x] `dotnet build` 通过
- [x] 修正 MEMORY.md 中的错误分析记录
