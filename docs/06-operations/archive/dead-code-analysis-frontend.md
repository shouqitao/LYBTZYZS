# LYBTZYZS 前端死代码分析清单

> 分析时间: 2026-04-22 12:55  
> 分析范围: `src/Client/Desktop/` (Shell + Roles + Modules + Core)  
> 项目: .NET 8 WPF/Prism 中医诊所管理系统

---

## 一、已废弃的 View（无入口/已弃用）

### 🔴 DEAD: LoginWindow（完全死代码）

| 项目 | 详情 |
|------|------|
| 路径 | `Modules/LYBT.Desktop.Auth/Views/LoginWindow.xaml` + `.xaml.cs` |
| 状态 | 代码注释标记「已弃用，现在使用单窗口模式」 |
| 引用 | **0 外部引用** — 无任何代码引用或实例化 |
| 原因 | 从多窗口模式迁移至单窗口模式后遗留 |
| 建议 | **删除** — 已被 `LoginView` 替代 |

### 🔴 DEAD: ViewNames.MedicalCaseList（悬空常量）

| 项目 | 详情 |
|------|------|
| 常量 | `ViewNames.MedicalCaseList = "MedicalCaseListView"` |
| XAML | **不存在** — MedicalCaseListView.xaml 从未创建或已被删除 |
| 备注 | Issue #1799 注释: 「删除MedicalCaseListView（功能与ManagementView重复）」 |
| 代码引用 | 仅 `EnhancedNavigationService.cs` 中的硬编码字符串（显示用） |
| 建议 | 从 `ViewNames.cs` 删除此常量 |

### 🔴 DEAD: ViewNames.ControlExamples（悬空常量）

| 项目 | 详情 |
|------|------|
| 常量 | `ViewNames.ControlExamples = "ControlExamplesView"` |
| XAML | **不存在** — ControlExamplesView.xaml 从未创建 |
| 代码引用 | `MenuManager.cs:262` — `ExecuteShowControlExamples()` 导航到此 View |
| 后果 | 点击"控件示例"菜单会导致导航失败/空白页 |
| 建议 | 1) 创建 ControlExamplesView 或 2) 删除菜单入口和此常量 |

---

## 二、备份文件（应在源码中删除）

共 **12 个** `.backup` / `.bak` 文件，不应存在于版本控制中：

| 文件 | 位置 |
|------|------|
| `NavigationConverters.cs.bak` | `Core/Infrastructure/Converters/` |
| `NullToVisibilityConverter.cs.backup` | `Core/Infrastructure/Converters/` |
| `StatusToColorConverter.cs.backup` | `Core/Infrastructure/Converters/` |
| `StringToVisibilityConverter.cs.backup` | `Core/Infrastructure/Converters/` |
| `IEnhancedNavigationService.cs.backup` | `Core/Infrastructure/Navigation/` |
| `NavigationAnalyticsService.cs.backup` | `Core/Infrastructure/Navigation/` |
| `NavigationModels.cs.backup` | `Core/Infrastructure/Navigation/` |
| `BaseDetailContainer.xaml.cs.backup` | `Core/Infrastructure/Views/` |
| `MedicalCaseEditControl.xaml.bak` | `Modules/MedicalCase/Controls/` |
| `WorkspaceState.cs.backup` | `Modules/MedicalCase/Models/` |
| `MedicalCaseCommandsViewModel.cs.backup` | `Modules/MedicalCase/ViewModels/Workspace/` |
| `ApiHealthMonitor.cs.backup` | `Shell/Services/HealthCheck/` |

建议在 `.gitignore` 中添加 `*.backup` 和 `*.bak` 规则，然后删除这些文件。

---

## 三、重复组件（需确认）

### BreadcrumbControl（两个版本并存）

| 版本 | 路径 | 用途 | 实际使用 |
|------|------|------|----------|
| `Infrastructure.Controls.BreadcrumbControl` | `Core/Infrastructure/Controls/` | 静态属性绑定（Items, OnNavigate） | ✅ `MainWindow.xaml:131` |
| `Navigation.Controls.BreadcrumbControl` | `Core/Infrastructure/Navigation/Controls/` | ViewModel 驱动（BreadcrumbControlViewModel） | ✅ `BreadcrumbStyles.xaml:12` |

**结论**: 两者**均在使用**，非重复代码。前者是 MainWindow 中的轻量级绑定控件，后者是主题中 ViewModel 驱动的增强版。长期应统一，但目前均有引用。

---

## 四、健康清单（无问题）

### ✅ 导航注册覆盖

| 注册方式 | View 数量 | 状态 |
|----------|-----------|------|
| `RegisterForNavigation<>()` | 25 个 | 正常 |
| `ViewNames` 常量引用 | 19 个 | 2 个悬空（见上） |
| 角色主页导航 | 3 个 | 正常 |

### ✅ Dialogs（全部被引用）
- `FormulaImportDialog` — 12 refs
- `HistoryCopyDialog` — 8 refs
- `UnsavedChangesDialog` — 7 refs
- `SyncConflictDialog` — 7 refs
- `ConfirmationDialog` — 11 refs
- `InputDialog` — 10 refs
- `MessageDialog` — 18 refs
- `UnfinishedCaseDialog` — 9 refs
- `RegistrationCreateDialog` — 6 refs

### ✅ Controls（全部被引用，最少 5 refs）
- 30 个自定义控件均有外部引用
- `MasterDetailLayout` 最多（60 refs）

### ✅ ViewModel-View 配对
- 42 个 ViewModel 中，31 个有对应 XAML
- 11 个"无 XAML"ViewModel 均为**子 ViewModel**（嵌入父控件中使用），非死代码

### ✅ Services
- 79 个 Service 类，均有 DI 注册
- `#if DEBUG` 仅 1 处（`CardReaderFactory.cs:121`）

---

## 五、处理优先级

| 优先级 | 项目 | 操作 | 预计影响 |
|--------|------|------|----------|
| P0 | 删除 `LoginWindow` | 删除 2 个文件 | 零（已弃用） |
| P0 | 删除备份文件 (12个) | `git rm` + `.gitignore` | 零 |
| P1 | 清理 `ViewNames.MedicalCaseList` | 删除常量 | 零（无实际 View） |
| P1 | 处理 `ViewNames.ControlExamples` | 删除常量 + 菜单入口 或 创建 View | 低 |
| P2 | 统一 `BreadcrumbControl` | 长期规划 | 需评估 |

---

## 六、总览

```
前端代码总量: 114 个 XAML 文件, 42 个 ViewModel, 79 个 Service
死代码:       2 个废弃 View 引用, 12 个备份文件
风险等级:     低 — 无功能性死代码，主要是残留文件
```

**核心结论**: 前端代码质量良好，无功能性死代码。唯一需要清理的是：
1. 已弃用的 `LoginWindow`（2 个文件）
2. 2 个 ViewNames 悬空常量（指向不存在的 View）
3. 12 个版本控制中不应存在的备份文件
