# WPF 前端架构重构 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 系统性重构 WPF 前端架构，解决模块耦合、ViewModel 臃肿、导航复杂、UI 不一致等问题。

**Architecture:** 按领域拆分模块、简化 ViewModel 基类层级、统一导航模式、清理 DI 注册、标准化 UI 组件。

**Tech Stack:** C# / .NET 8 / WPF / Prism / CommunityToolkit.Mvvm

---

## 前置条件

本会话已完成的重构（不需要重做）：
- ✅ OpenSpec 清理 + 死代码清理
- ✅ DesignSystem.xaml 统一
- ✅ MainWindowViewModel 拆分（Token 移到 TokenLifecycleService）
- ✅ EnhancedNavigationService 拆分（Breadcrumb + History managers）
- ✅ CredentialVault 拆分（DpapiProtector + CredentialStorage）
- ✅ 启动流程简化（AppStartupOrchestrator）
- ✅ SplashScreen 移除
- ✅ 用户模块统一（ApplicationUser）
- ✅ 登录界面完成
- ✅ 模块延迟加载 + 启动并行化

---

## Task 1: 审计当前架构 — 识别剩余问题

**Files:** Read-only analysis

- [ ] **Step 1: 列出所有 >300 行的 ViewModel/Service 文件**

```bash
Get-ChildItem -Recurse -Include *.cs -Path src/Client/Desktop | Where-Object { $_.FullName -notmatch '\\obj\\' } | ForEach-Object { $lines = (Get-Content $_.FullName | Measure-Object -Line).Lines; if ($lines -gt 300) { [PSCustomObject]@{File=$_.FullName; Lines=$lines} } } | Sort-Object Lines -Descending
```

- [ ] **Step 2: 识别模块间非法引用**

```bash
rg "using LYBT.Desktop.(Patients|Herbs|Formula|Users|MedicalCase|Registration)" src/Client/Desktop/Modules -t cs | Where-Object { $_ -notmatch '\\obj\\' }
```

- [ ] **Step 3: 检查 DI 注册一致性**

审查 `DataSourceRegistrationExtensions.cs` + `ServiceCollectionExtensions.cs` + 各 Module.cs 的注册是否有重复/遗漏。

- [ ] **Step 4: 审计 UI 一致性**

检查所有业务模块的 MasterDetailControl 是否使用统一的设计 token（DesignSystem.xaml）。

- [ ] **Commit findings as architecture-audit.md**

---

## Task 2: ViewModel 基类层级简化

**Files:**
- `Core/LYBT.Desktop.Models/ViewModels/Base/CoreViewModelBase.cs`
- `Core/LYBT.Desktop.Models/ViewModels/Base/NavigableViewModelBase.cs`
- `Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
- `Core/LYBT.Desktop.Infrastructure/ViewModels/DialogViewModelBase.cs`

**目标：** 审查 4 层基类继承链是否可以简化。当前：
```
CoreViewModelBase → NavigableViewModelBase → MasterDetailViewModelBase
CoreViewModelBase → DialogViewModelBase
```

- [ ] **Step 1: 评估每个基类的职责**
- [ ] **Step 2: 如果有重叠职责，合并或提取**
- [ ] **Step 3: 确保子类不需要 override 过多方法**
- [ ] **Step 4: 构建验证 + Commit**

---

## Task 3: 导航系统简化

**Files:**
- `Shell/Services/NavigationCoordinator.cs`
- `Core/LYBT.Desktop.Infrastructure/Navigation/EnhancedNavigationService.cs`
- `Core/LYBT.Desktop.Contracts/Services/INavigationCoordinator.cs`

**目标：** NavigationCoordinator 和 EnhancedNavigationService 职责重叠。评估是否可以合并。

- [ ] **Step 1: 对比两者公开 API**
- [ ] **Step 2: 识别重叠的方法（NavigateTo, NavigateBack, history）**
- [ ] **Step 3: 如果重叠 >50%，合并为一个服务**
- [ ] **Step 4: 更新所有引用**
- [ ] **Step 5: 构建验证 + Commit**

---

## Task 4: DI 注册统一

**Files:**
- `Shell/Extensions/ServiceCollectionExtensions.cs`
- `Shell/Extensions/DataSourceRegistrationExtensions.cs`
- `Shell/Extensions/UnifiedApiClientExtensions.cs`
- `Core/LYBT.Desktop.Infrastructure/DependencyInjection/ViewModelServicesExtensions.cs`
- 各 Module.cs

**目标：** 所有 DI 注册集中到 2 个文件：`ServiceCollectionExtensions`（应用服务）+ `DataSourceRegistrationExtensions`（数据访问）。

- [ ] **Step 1: 列出所有 Register*/RegisterSingleton* 调用**
- [ ] **Step 2: 识别散落在 Module.cs 中的注册**
- [ ] **Step 3: 将业务模块的 DI 注册集中**
- [ ] **Step 4: 构建验证 + Commit**

---

## Task 5: UI 组件标准化

**Files:**
- 各模块的 *EditControl.xaml, *MasterDetailControl.xaml
- `Core/LYBT.Desktop.Infrastructure/Controls/UnifiedManagementTable.xaml`

**目标：** 所有 CRUD 界面使用统一的控件和样式。替换硬编码颜色为 DesignSystem token。

- [ ] **Step 1: 扫描所有 .xaml 中的硬编码颜色（#XXXXXX）**
- [ ] **Step 2: 替换为 DesignSystem token 或保留硬编码（如果是有意的）**
- [ ] **Step 3: 验证所有 MasterDetailControl 使用 UnifiedManagementTable**
- [ ] **Step 4: 构建验证 + Commit**

---

## Task 6: 清理残留废弃代码

**Files:** 全局扫描

- [ ] **Step 1: 搜索 TODO/FIXME/HACK 注释**
```bash
rg "TODO|FIXME|HACK" src/Client/Desktop -t cs -t xml
```
- [ ] **Step 2: 搜索空方法/空 region**
- [ ] **Step 3: 搜索 unused private methods（通过 CodeGraph）**
- [ ] **Step 4: 清理或记录**
- [ ] **Step 5: 构建验证 + Commit**

---

## Task 7: 最终验证

- [ ] **Step 1: 全量构建** `dotnet build LYBTZYZS.sln`
- [ ] **Step 2: Architecture 测试** `dotnet test tests/LYBT.Tests.Architecture/`
- [ ] **Step 3: Desktop 测试** `dotnet test tests/LYBT.Tests.Desktop/`
- [ ] **Step 4: Commit + Push**
