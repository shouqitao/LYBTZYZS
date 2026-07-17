# 前端 WPF 架构全面优化实施计划

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/frontend-architecture-optimization.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 通过 3 轮渐进式优化，使前端 WPF 架构的文档、代码模式、职责边界达到一致且高质量的状态。

**Architecture:** 文档更新（Round 1）→ ViewModel 模式统一（Round 2）→ Shell 拆分 + 模块治理（Round 3）。每轮独立可验证，前一轮是后一轮的前提。

**Tech Stack:** WPF, Prism.DryIoc 8.x, CommunityToolkit.Mvvm, Riok.Mapperly, .NET 8.0

## Global Constraints

- 英文标识符，中文业务文档/注释
- 无 Emoji（除非用户要求）
- Riok.Mapperly 为唯一映射方案（AutoMapper 禁止）
- CommunityToolkit.Mvvm `[RelayCommand]` 为唯一命令模式（DelegateCommand 禁止新增）
- 每个 task 结束前 `dotnet build LYBTZYZS.sln` 必须通过
- 模块间禁止直接引用（Registration 除外，已文档化）

---

## Round 1 — 文档一致性

### Task 1.1: 更新 DESKTOP_ARCHITECTURE_STANDARD.md

**Covers:** [S1], [S2]

**Files:**
- Modify: `src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md`

**变更内容:**

1. **§1.1 技术栈** — 将 `AutoMapper 13.0+` 改为 `Riok.Mapperly (编译期映射)`；移除 AutoMapper 相关描述
2. **§4.1 基类表格** — 更新为实际基类：
   - `CoreViewModelBase` — 核心基类（ObservableObject + IViewModelServices 聚合）
   - `NavigableViewModelBase` — 可导航 VM（INavigationAware + IRegionMemberLifetime）
   - `MasterDetailViewModelBase<TListItem, TDetail>` — Master-Detail CRUD
   - `DialogViewModelBase` — 对话框 VM（IDialogAware）
   - `ChildViewModelBase` — 复合 VM 子组件
3. **§4.3 命令示例** — `DelegateCommand` 全部改为 `[RelayCommand]` + `partial` 方法
4. **§4.5 映射示例** — AutoMapper Profile 改为 Mapperly `[Mapper]` + `[MapProperty]` 示例
5. **§10 代码示例** — 更新完整模块示例中的所有 AutoMapper/DelegateCommand 引用
6. **最后修改日期** — 更新为 2026-07-17

- [ ] **Step 1: 读取当前文件确认内容**
- [ ] **Step 2: 逐节更新上述 6 个区域**
- [ ] **Step 3: 验证**
  ```bash
  grep -n "AutoMapper\|UnifiedViewModelBase\|UnifiedListViewModelBase" src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md
  ```
  Expected: 0 结果
- [ ] **Step 4: Commit**
  ```bash
  git add src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md
  git commit -m "docs(desktop): update architecture standard — Mapperly, CTK MVVM, current base classes"
  ```

---

### Task 1.2: 更新 Desktop README.md

**Covers:** [S1], [S2]

**Files:**
- Modify: `src/Client/Desktop/README.md`

**变更内容:**

1. **技术栈** — 添加 CommunityToolkit.Mvvm、Riok.Mapperly；移除 DelegateCommand 引用
2. **项目列表** — 补充缺失模块：
   - LYBT.Desktop.Reports（统计报表）
   - LYBT.Desktop.Registration（挂号管理）
   - LYBT.Desktop.Sysadmin（系统运维工作台）
3. **目录结构** — 同步更新 Roles/ 子目录（补 Receptionist、Sysadmin）
4. **MVVM 规范** — 更新命令模式为 `[RelayCommand]`
5. **角色体系** — 更新为 4 个角色：Admin、Doctor、Receptionist、SuperAdmin

- [ ] **Step 1: 读取当前文件**
- [ ] **Step 2: 逐节更新**
- [ ] **Step 3: 验证**
  ```bash
  grep -n "AutoMapper" src/Client/Desktop/README.md
  ```
  Expected: 0 结果
- [ ] **Step 4: Commit**
  ```bash
  git add src/Client/Desktop/README.md
  git commit -m "docs(desktop): update README — add missing modules, Mapperly, CTK MVVM"
  ```

---

### Task 1.3: 更新 Desktop AGENTS.md

**Covers:** [S1]

**Files:**
- Modify: `src/Client/Desktop/AGENTS.md`

**变更内容:**

1. **External dependencies** — 移除 `AutoMapper`，保留 `Riok.Mapperly`
2. **Common Patterns** — 确认基类名称与实际一致（`NavigableViewModelBase`、`MasterDetailViewModelBase`）
3. **For AI Agents** — 确认 "AutoMapper is forbidden" 描述准确

- [ ] **Step 1: 读取并更新**
- [ ] **Step 2: 验证**
  ```bash
  grep -n "AutoMapper" src/Client/Desktop/AGENTS.md
  ```
  Expected: 0 结果（"AutoMapper is forbidden" 的描述保留是合理的）
- [ ] **Step 3: Commit**
  ```bash
  git add src/Client/Desktop/AGENTS.md
  git commit -m "docs(desktop): remove AutoMapper from AGENTS.md dependencies"
  ```

---

### Task 1.4: 检查并修正各模块 README 中的 AutoMapper 引用

**Covers:** [S1]

**Files:**
- 检查: `src/Client/Desktop/Modules/*/README.md`
- 检查: `src/Client/Desktop/Roles/*/README.md`
- 可能修改: Patients、MedicalCase、Herbs、Formula、Users 的 README

**变更内容:**

搜索所有模块 README 中的 AutoMapper 引用，替换为 Mapperly 对应描述。

- [ ] **Step 1: 搜索所有 AutoMapper 引用**
  ```bash
  grep -rn "AutoMapper\|IMapper\|Profile\|CreateMap" src/Client/Desktop/Modules/ --include="*.md"
  grep -rn "AutoMapper\|IMapper\|Profile\|CreateMap" src/Client/Desktop/Roles/ --include="*.md"
  ```
- [ ] **Step 2: 逐文件修正**
- [ ] **Step 3: 验证**
  ```bash
  grep -rn "AutoMapper" src/Client/Desktop/Modules/ --include="*.md"
  grep -rn "AutoMapper" src/Client/Desktop/Roles/ --include="*.md"
  ```
  Expected: 0 结果
- [ ] **Step 4: Commit**
  ```bash
  git add src/Client/Desktop/Modules/ src/Client/Desktop/Roles/
  git commit -m "docs(modules): replace AutoMapper references with Mapperly in module READMEs"
  ```

---

### Task 1.5: 死代码清理

**Covers:** [S1]

**Files:**
- 可能删除: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Models/ViewState/PatientViewState.cs`
- 可能删除: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Interfaces/IPatientCommandHandler.cs`

**变更内容:**

1. 确认 `PatientViewState` 无运行时消费者（grep 搜索引用）
2. 确认 `IPatientCommandHandler` 无运行时消费者
3. 如果确认无引用，删除文件并更新 README 中的相关描述

- [ ] **Step 1: 搜索引用**
  ```bash
  grep -rn "PatientViewState" src/Client/Desktop/ --include="*.cs" --include="*.xaml"
  grep -rn "IPatientCommandHandler" src/Client/Desktop/ --include="*.cs"
  ```
- [ ] **Step 2: 如果仅定义处有引用，删除文件**
- [ ] **Step 3: 编译验证**
  ```bash
  dotnet build LYBTZYZS.sln
  ```
- [ ] **Step 4: 测试验证**
  ```bash
  dotnet test tests/LYBT.Tests.Desktop/
  ```
- [ ] **Step 5: Commit**
  ```bash
  git add -A
  git commit -m "refactor(desktop): remove dead code PatientViewState, IPatientCommandHandler"
  ```

---

## Round 2 — ViewModel 体系统一

### Task 2.1: Shell ViewModels — DelegateCommand → [RelayCommand]

**Covers:** [S2]

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs`

**变更内容:**

将 `DelegateCommand` 字段改为 `[RelayCommand]` partial 方法。例如：
```csharp
// Before
public DelegateCommand LogoutCommand { get; }
LogoutCommand = new DelegateCommand(async () => await LogoutAsync());

// After
[RelayCommand]
private async Task LogoutAsync() { ... }
```

- [ ] **Step 1: 读取 MainWindowViewModel.cs，列出所有 DelegateCommand**
- [ ] **Step 2: 逐个替换为 [RelayCommand]**
- [ ] **Step 3: 读取 AccountSettingsViewModel.cs，同样替换**
- [ ] **Step 4: 编译验证**
  ```bash
  dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
  ```
- [ ] **Step 5: Commit**
  ```bash
  git add src/Client/Desktop/Shell/ViewModels/
  git commit -m "refactor(shell): migrate DelegateCommand to [RelayCommand] in Shell ViewModels"
  ```

---

### Task 2.2: Dialog ViewModels — DelegateCommand → [RelayCommand]

**Covers:** [S2]

**Files:**
- Modify: `src/Client/Desktop/Shell/Dialogs/ViewModels/ConfirmationDialogViewModel.cs`
- Modify: `src/Client/Desktop/Shell/Dialogs/ViewModels/MessageDialogViewModel.cs`
- Modify: `src/Client/Desktop/Shell/Dialogs/ViewModels/InputDialogViewModel.cs`

**变更内容:**

将所有 `DelegateCommand` 替换为 `[RelayCommand]`。注意 Dialog VM 继承自 `DialogViewModelBase`，其 `Confirm`/`Cancel` 命令可能已在基类中定义为 `[RelayCommand]`，需检查避免重复。

- [ ] **Step 1: 读取 3 个 Dialog VM 文件**
- [ ] **Step 2: 检查基类 DialogViewModelBase 是否已有 Confirm/Cancel 命令**
- [ ] **Step 3: 替换（避免与基类冲突）**
- [ ] **Step 4: 编译验证**
  ```bash
  dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
  ```
- [ ] **Step 5: Commit**
  ```bash
  git add src/Client/Desktop/Shell/Dialogs/ViewModels/
  git commit -m "refactor(shell): migrate Dialog VMs from DelegateCommand to [RelayCommand]"
  ```

---

### Task 2.3: Infrastructure ViewModels — DelegateCommand → [RelayCommand]

**Covers:** [S2]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/UnfinishedCaseDialogViewModel.cs`
- 其他 Infrastructure 层 VM（需 grep 确认）

**变更内容:**

搜索 Infrastructure 层所有 DelegateCommand 使用并替换。

- [ ] **Step 1: 搜索**
  ```bash
  grep -rn "DelegateCommand" src/Client/Desktop/Core/ --include="*.cs"
  ```
- [ ] **Step 2: 逐文件替换**
- [ ] **Step 3: 编译验证**
  ```bash
  dotnet build LYBTZYZS.sln
  ```
- [ ] **Step 4: Commit**
  ```bash
  git add src/Client/Desktop/Core/
  git commit -m "refactor(infrastructure): migrate remaining DelegateCommand to [RelayCommand]"
  ```

---

### Task 2.4: Modules ViewModels — DelegateCommand → [RelayCommand]

**Covers:** [S2]

**Files:**
- 搜索并修改: `src/Client/Desktop/Modules/**/ViewModels/*.cs`

**变更内容:**

搜索所有业务模块中剩余的 DelegateCommand 使用并替换。

- [ ] **Step 1: 搜索**
  ```bash
  grep -rn "new DelegateCommand\|DelegateCommand<" src/Client/Desktop/Modules/ --include="*.cs"
  grep -rn "new DelegateCommand\|DelegateCommand<" src/Client/Desktop/Roles/ --include="*.cs"
  ```
- [ ] **Step 2: 逐文件替换**
- [ ] **Step 3: 编译验证**
  ```bash
  dotnet build LYBTZYZS.sln
  ```
- [ ] **Step 4: 测试验证**
  ```bash
  dotnet test tests/LYBT.Tests.Desktop/
  ```
- [ ] **Step 5: Commit**
  ```bash
  git add src/Client/Desktop/Modules/ src/Client/Desktop/Roles/
  git commit -m "refactor(modules): migrate all remaining DelegateCommand to [RelayCommand]"
  ```

---

### Task 2.5: 更新架构测试 — 基类白名单 + 命令模式检查

**Covers:** [S2]

**Files:**
- Modify: `tests/LYBT.Tests.Architecture/ArchTests.cs`

**变更内容:**

1. 更新 VM 基类白名单（移除 `UnifiedViewModelBase`、`UnifiedListViewModelBase`、`ModernViewModelBase`、`ModernManagementViewModel`，保留 `CoreViewModelBase`、`NavigableViewModelBase`、`MasterDetailViewModelBase`、`DialogViewModelBase`、`ChildViewModelBase`）
2. 添加新测试：验证没有 `new DelegateCommand` 的使用

- [ ] **Step 1: 读取 ArchTests.cs**
- [ ] **Step 2: 更新白名单**
- [ ] **Step 3: 添加 DelegateCommand 检查测试**
- [ ] **Step 4: 运行架构测试**
  ```bash
  dotnet test tests/LYBT.Tests.Architecture/
  ```
- [ ] **Step 5: Commit**
  ```bash
  git add tests/LYBT.Tests.Architecture/
  git commit -m "test(arch): update VM base class whitelist, add DelegateCommand ban test"
  ```

---

## Round 3 — Shell 拆分 + 模块治理

### Task 3.1: Shell Extensions 评估与拆分

**Covers:** [S3]

**Files:**
- Read: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- 可能拆分为:
  - `ServiceCollectionExtensions.cs`（主入口，链式调用）
  - `HttpServiceRegistrationExtensions.cs`（已有，确认独立性）
  - `DataSourceRegistrationExtensions.cs`（已有，确认独立性）
  - `ViewModelServiceRegistrationExtensions.cs`（新建，提取 VM 相关注册）

**变更内容:**

评估 `RegisterAllServices()` 的 12+ 步骤是否已合理拆分到独立文件。如果 `ServiceCollectionExtensions.cs` 仍然过大，将 ViewModel 相关注册提取到独立文件。

- [ ] **Step 1: 读取 ServiceCollectionExtensions.cs，统计行数和步骤数**
- [ ] **Step 2: 评估是否需要拆分（>200 行或 >8 个注册步骤建议拆分）**
- [ ] **Step 3: 如需拆分，提取 ViewModel 相关注册到独立文件**
- [ ] **Step 4: 编译验证**
  ```bash
  dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
  ```
- [ ] **Step 5: 测试验证**
  ```bash
  dotnet test tests/LYBT.Tests.Desktop/
  ```
- [ ] **Step 6: Commit**
  ```bash
  git add src/Client/Desktop/Shell/Extensions/
  git commit -m "refactor(shell): split ServiceCollectionExtensions by concern"
  ```

---

### Task 3.2: 模块边界架构测试

**Covers:** [S3]

**Files:**
- Modify: `tests/LYBT.Tests.Architecture/ArchTests.cs`

**变更内容:**

添加架构测试：业务模块项目不得引用其他业务模块项目（Registration 除外）。

```csharp
[Fact]
public void Business_Modules_Should_Not_Reference_Other_Business_Modules()
{
    // 白名单: Registration 可依赖 Patients/Users/MedicalCase.Models
    var moduleProjects = new[]
    {
        "LYBT.Desktop.Patients",
        "LYBT.Desktop.Herbs",
        "LYBT.Desktop.Formula",
        "LYBT.Desktop.MedicalCase",
        "LYBT.Desktop.Users",
        "LYBT.Desktop.Auth",
        "LYBT.Desktop.Reports"
    };
    // 检查每个模块的引用不包含其他业务模块
}
```

- [ ] **Step 1: 读取 ArchTests.cs，了解现有测试模式**
- [ ] **Step 2: 添加模块边界测试**
- [ ] **Step 3: 运行测试，确认当前代码已合规（或标记已知例外）**
  ```bash
  dotnet test tests/LYBT.Tests.Architecture/
  ```
- [ ] **Step 4: Commit**
  ```bash
  git add tests/LYBT.Tests.Architecture/
  git commit -m "test(arch): add module boundary isolation test with Registration exception"
  ```

---

### Task 3.3: 薄包装 View 评估

**Covers:** [S3]

**Files:**
- Read: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/HerbManagementView.xaml`
- Read: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/FormulaManagementView.xaml`
- Read: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientManagementView.xaml`
- Read: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseManagementView.xaml`

**变更内容:**

评估这 4 个薄包装 View 的价值。如果它们仅是简单包装（<20 行 XAML），记录为已知设计决策（角色台复用需要），不做代码变更。

- [ ] **Step 1: 读取 4 个薄包装 View 的 XAML**
- [ ] **Step 2: 评估每个的复杂度和价值**
- [ ] **Step 3: 在 Clinical README 中记录评估结论**
- [ ] **Step 4: Commit（如有变更）**
  ```bash
  git add src/Client/Desktop/Roles/LYBT.Desktop.Clinical/
  git commit -m "docs(clinical): document thin-wrapper View design decisions"
  ```

---

### Task 3.4: Shell README 更新

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Shell/README.md`

**变更内容:**

更新 Shell README 反映 Extensions 拆分后的结构（如有变更），以及最新的启动流程。

- [ ] **Step 1: 读取当前 Shell README**
- [ ] **Step 2: 更新 Extensions 目录结构描述**
- [ ] **Step 3: Commit**
  ```bash
  git add src/Client/Desktop/Shell/README.md
  git commit -m "docs(shell): update README to reflect current Extensions structure"
  ```

---

### Task 3.5: Round 3 全量验证

**Covers:** [S1], [S2], [S3]

**变更内容:**

Round 3 完成后的全量验证。

- [ ] **Step 1: 编译验证**
  ```bash
  dotnet build LYBTZYZS.sln
  ```
- [ ] **Step 2: Desktop 测试**
  ```bash
  dotnet test tests/LYBT.Tests.Desktop/
  ```
- [ ] **Step 3: 架构测试**
  ```bash
  dotnet test tests/LYBT.Tests.Architecture/
  ```
- [ ] **Step 4: 最终 grep 验证**
  ```bash
  grep -rn "AutoMapper" src/Client/Desktop/ --include="*.md" --include="*.cs" | grep -v "forbidden\|禁止\|不使用"
  grep -rn "new DelegateCommand" src/Client/Desktop/ --include="*.cs"
  grep -rn "UnifiedViewModelBase\|UnifiedListViewModelBase" src/Client/Desktop/ --include="*.cs" --include="*.md"
  ```
  Expected: 全部 0 结果
