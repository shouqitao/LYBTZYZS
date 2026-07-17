# 前端 WPF 架构全面优化设计规格

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/frontend-architecture-optimization.md)

> **文档版本**: v1.0
> **创建日期**: 2026-07-17
> **适用范围**: `src/Client/Desktop/` 全部项目
> **策略**: 3 轮渐进式优化

---

## [S1] 问题分析

经过 codegraph + serena 对前端代码库的深度分析，当前架构代码质量较高，但存在以下三类问题：

### 文档与代码脱节

| 文档 | 问题 |
|------|------|
| `DESKTOP_ARCHITECTURE_STANDARD.md` | 技术栈写 AutoMapper 13.0+，实际使用 Riok.Mapperly；基类命名 `UnifiedViewModelBase`/`UnifiedListViewModelBase` 已不存在，实际为 `CoreViewModelBase`/`NavigableViewModelBase`/`MasterDetailViewModelBase`；命令示例用 `DelegateCommand`，实际已迁移到 `[RelayCommand]`；最后更新 2025-10-12 |
| `Desktop/README.md` | 技术栈提到 "MVVM DataBinding + DelegateCommand + EventAggregator"，未提及 CommunityToolkit.Mvvm；项目列表缺少 Reports、Registration、Sysadmin 模块 |
| `AGENTS.md` (Desktop) | 提到 AutoMapper 与 Riok.Mapperly 并存，应明确仅 Mapperly |
| 各模块 README | 部分引用 AutoMapper Profile 模式，与实际 Mapperly 映射器不符 |

### ViewModel 模式不统一

| 问题 | 影响 |
|------|------|
| 命令混用 | `MasterDetailViewModelBase` 已用 `[RelayCommand]`，但 Shell 的 `MainWindowViewModel`、部分 Dialog VM 仍用 `DelegateCommand` |
| 错误处理碎片化 | `ErrorHandler`（服务层 INotifyDataErrorInfo）、`ValidatableModelBase`（DetailModel 验证）、ViewModel 自有 `SetError()`/`ErrorMessage` 三套并存，边界不清 |

### Shell 复杂度与模块边界

| 问题 | 影响 |
|------|------|
| Shell Extensions 链过长 | `ServiceCollectionExtensions.RegisterAllServices()` 串联 12+ 注册步骤，难以定位问题 |
| 模块间依赖例外 | ClinicalModule 依赖 PatientsModule + MedicalCaseModule（已文档化但未在架构测试中体现） |
| 薄包装 View | Clinical 模块中 HerbManagementView、FormulaManagementView、PatientManagementView、MedicalCaseManagementView 仅为薄包装 |
| 疑似死代码 | `PatientViewState`、`IPatientCommandHandler` 等无运行时消费者 |

---

## [S2] 优化策略

### Round 1 — 文档一致性（零风险，立即改善可读性）

**目标**: 所有 README/AGENTS.md/架构标准与实际代码完全对齐。

**变更清单**:

| # | 文件 | 变更 |
|---|------|------|
| 1.1 | `DESKTOP_ARCHITECTURE_STANDARD.md` | 全面重写 §1.1 技术栈（移除 AutoMapper，改为 Riok.Mapperly）；更新 §4.1 基类表格（`UnifiedViewModelBase` → `CoreViewModelBase`，`UnifiedListViewModelBase` → `MasterDetailViewModelBase`）；更新 §4.3 命令示例（`DelegateCommand` → `[RelayCommand]`）；更新 §4.5 映射示例（AutoMapper Profile → Mapperly Mapper）；更新最后修改日期 |
| 1.2 | `Desktop/README.md` | 更新技术栈描述；补充缺失模块（Reports、Registration、Sysadmin）；更新依赖关系图 |
| 1.3 | `Desktop/AGENTS.md` | 移除 AutoMapper 引用，明确 Mapperly 为唯一映射方案 |
| 1.4 | 各模块 README | 检查并修正 AutoMapper 引用（Patients、MedicalCase、Herbs、Formula、Users） |
| 1.5 | 死代码清理 | 确认并移除 `PatientViewState`、`IPatientCommandHandler` 等无消费者的代码 |

**验收标准**:
- `grep -r "AutoMapper" src/Client/Desktop/ --include="*.md"` 返回 0 结果（Shared 层除外）
- `grep -r "UnifiedViewModelBase\|UnifiedListViewModelBase" src/Client/Desktop/ --include="*.md"` 返回 0 结果
- `grep -r "DelegateCommand" src/Client/Desktop/ --include="*.md"` 仅出现在历史记录中

### Round 2 — ViewModel 体系统一（中等风险）

**目标**: 统一命令模式和错误处理机制。

**变更清单**:

| # | 范围 | 变更 |
|---|------|------|
| 2.1 | Shell ViewModels | `MainWindowViewModel`、`AccountSettingsViewModel` 中的 `DelegateCommand` → `[RelayCommand]` |
| 2.2 | Dialog ViewModels | `ConfirmationDialogViewModel`、`MessageDialogViewModel`、`InputDialogViewModel` 中的 `DelegateCommand` → `[RelayCommand]` |
| 2.3 | Infrastructure ViewModels | `UnfinishedCaseDialogViewModel` 等中的 `DelegateCommand` → `[RelayCommand]` |
| 2.4 | 错误处理收敛 | 明确分工：ViewModel 层用 `SetError()`/`ClearError()`（继承自 CoreViewModelBase）；DetailModel 用 `ValidatableModelBase`（INotifyDataErrorInfo）；服务层用 `ErrorHandler`。在架构文档中明确边界 |
| 2.5 | 架构测试更新 | 更新 `LYBT.Tests.Architecture` 中的 VM 基类白名单和命令模式检查 |

**验收标准**:
- `grep -rn "new DelegateCommand" src/Client/Desktop/ --include="*.cs"` 返回 0 结果
- `dotnet build LYBTZYZS.sln` 通过
- `dotnet test tests/LYBT.Tests.Desktop/` 全部通过
- `dotnet test tests/LYBT.Tests.Architecture/` 全部通过

### Round 3 — Shell 拆分 + 模块治理（高风险）

**目标**: 降低 Shell 复杂度，强化模块边界。

**变更清单**:

| # | 范围 | 变更 |
|---|------|------|
| 3.1 | Shell Extensions | 评估 `ServiceCollectionExtensions.RegisterAllServices()` 链，考虑按关注点拆分为独立扩展方法文件（如 `ServiceRegistration.Http.cs`、`ServiceRegistration.Infrastructure.cs`） |
| 3.2 | 模块依赖架构测试 | 在 `LYBT.Tests.Architecture` 中添加测试：模块项目不得引用其他业务模块项目（Registration 除外，需显式白名单） |
| 3.3 | 薄包装 View 评估 | 评估 Clinical 模块中的 HerbManagementView 等是否可以通过参数化导航直接复用业务模块 View，移除薄包装 |
| 3.4 | Shell README 更新 | 更新 Shell/README.md 反映 Extensions 拆分后的结构 |

**验收标准**:
- `dotnet build LYBTZYZS.sln` 通过
- `dotnet test tests/LYBT.Tests.Desktop/` 全部通过
- `dotnet test tests/LYBT.Tests.Architecture/` 全部通过（含新增模块边界测试）
- Shell Extensions 文件数合理（不超过 6 个独立文件）

---

## [S3] 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| Round 1 文档更新遗漏 | 低 | 用 grep 批量搜索验证 |
| Round 2 DelegateCommand 迁移导致编译错误 | 中 | 逐模块迁移，每改一个模块 build 验证 |
| Round 2 错误处理改动影响行为 | 中 | 保留现有行为，仅统一接口/文档 |
| Round 3 Shell 拆分破坏启动流程 | 高 | 先写架构测试，再重构；保持功能不变 |
| Round 3 薄包装移除导致导航失败 | 中 | 保留原有注册方式，仅在确认安全后移除 |

---

## [S4] 依赖关系

```
Round 1 (文档) ← 无代码依赖，可独立执行
Round 2 (VM 统一) ← 依赖 Round 1 完成（文档更新后才能确认基类命名）
Round 3 (Shell + 模块) ← 依赖 Round 2 完成（VM 统一后才能安全拆分 Shell）
```

---

## [S5] 不在范围内

- 不改变运行时行为（所有优化是代码质量/文档层面）
- 不引入新框架或库
- 不重构业务逻辑
- 不修改 Shared 层或 Server 层
- 不改变 DI 注册的实质内容（仅调整文件组织）
