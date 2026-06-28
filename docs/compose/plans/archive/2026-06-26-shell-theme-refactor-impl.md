# Shell 主题体系重构实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 清理 Shell 主题体系的技术债务，统一 MDIX 使用，确保 Dark/Light 主题切换正常工作。

**Architecture:** 分 4 个阶段执行：幽灵画刷清理 → Token 命名统一 → MDIX 使用统一 → Dark 主题验证。每个阶段独立可测试。

**Tech Stack:** .NET 8, WPF, MaterialDesignInXamlToolkit (MDIX), Prism

## Global Constraints

- 所有颜色/画刷必须使用 `DynamicResource`（非 `StaticResource`）确保主题切换正常
- MDIX 资源键使用 `MaterialDesign.` 前缀（如 `MaterialDesignPaper`、`MaterialDesignBody`）
- 旧 Token 命名统一为新命名（`SpacingXS/SM/MD/LG/XL`、`RadiusSM/MD/LG`）
- 不修改 `App.xaml`、`ThemeService.cs`、`SysadminDarkTheme.xaml`（已正确）
- 每个 Task 完成后必须通过 `dotnet build` 验证

---

## File Structure

| 操作 | 文件 | 职责 |
|------|------|------|
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml` | 清理旧 token |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/UnifiedComponents.xaml` | 清理注释 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/ButtonStyles.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/InputStyles.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/PanelStyles.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/HomePageStyles.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/MedicalCaseStyles.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Typography.xaml` | 清理旧 token |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/*.xaml` (~15 文件) | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Shell/Styles/Controls.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Shell/Styles/Typography.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Shell/Views/MainWindow.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Shell/Controls/AccountSettingsControl.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/UnfinishedCaseDialog.xaml` | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Roles/*/Views/*.xaml` (~8 文件) | 替换幽灵画刷 |
| Modify | `src/Client/Desktop/Modules/*/Controls/*.xaml` (~5 文件) | 替换幽灵画刷 |

---

## Task 1: DesignSystem 清理旧 Token

**Covers:** [S3.2] Token 命名统一

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml`

- [ ] **Step 1: 删除旧 Token 定义**

在 `DesignSystem.xaml` 中删除以下旧命名定义（保留新命名）：

```xml
<!-- 删除这些行 -->
<Thickness x:Key="SpacingXSmall">4</Thickness>
<Thickness x:Key="SpacingSmall">8</Thickness>
<Thickness x:Key="SpacingMedium">12</Thickness>
<Thickness x:Key="SpacingLarge">16</Thickness>
<Thickness x:Key="SpacingXLarge">24</Thickness>
<Thickness x:Key="SpacingXXLarge">32</Thickness>

<CornerRadius x:Key="CornerRadiusSmall">4</CornerRadius>
<CornerRadius x:Key="CornerRadiusMedium">8</CornerRadius>
<CornerRadius x:Key="CornerRadiusLarge">12</CornerRadius>
```

- [ ] **Step 2: 替换旧 Token 引用**

在以下文件中搜索并替换：
- `SpacingXSmall` → `SpacingXS`
- `SpacingSmall` → `SpacingSM`
- `SpacingMedium` → `SpacingMD`
- `SpacingLarge` → `SpacingLG`
- `SpacingXLarge` → `SpacingXL`
- `SpacingXXLarge` → `SpacingXXL`
- `CornerRadiusSmall` → `RadiusSM`
- `CornerRadiusMedium` → `RadiusMD`
- `CornerRadiusLarge` → `RadiusLG`

涉及文件：`ButtonStyles.xaml`、`InputStyles.xaml`、`PanelStyles.xaml`、`UnifiedManagementTable.xaml`、`UnifiedManagementToolBar.xaml`、`UnifiedPaginationBar.xaml`、`SystemSettingsView.xaml`

- [ ] **Step 3: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: 提交**

```bash
git add -A
git commit -m "refactor: unify token naming to new convention (SpacingXS/SM/MD/LG/XL, RadiusSM/MD/LG)"
```

---

## Task 2: 清理幽灵画刷 - Controls/Themes

**Covers:** [S3.1] 幽灵画刷清理

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/ButtonStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/InputStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/PanelStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/HomePageStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/MedicalCaseStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Typography.xaml`

- [ ] **Step 1: 执行替换**

幽灵画刷映射表：

| 旧引用 | 替换为 |
|--------|--------|
| `{DynamicResource SecondaryRegionBrush}` | `{DynamicResource MaterialDesignBody}` |
| `{DynamicResource ThirdlyTextBrush}` | `{DynamicResource MaterialDesignBodyLight}` |
| `{DynamicResource SecondaryBorderBrush}` | `{DynamicResource MaterialDesignOutline}` |
| `{DynamicResource RegionBrush}` | `{DynamicResource MaterialDesignPaper}` |
| `{DynamicResource DisabledBackgroundBrush}` | `{DynamicResource MaterialDesignDivider}` |
| `{DynamicResource PrimaryRegionBrush}` | `{DynamicResource MaterialDesignBrus Primary}` |
| `{DynamicResource DataGridAlternatingRowBrush}` | `{DynamicResource MaterialDesignBody}` |

在上述 7 个文件中搜索并替换。

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "refactor: replace ghost brushes with MDIX resources in Controls/Themes"
```

---

## Task 3: 清理幽灵画刷 - Controls/Controls

**Covers:** [S3.1] 幽灵画刷清理

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BaseDetailContainer.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbBar.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbControl.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/CardReaderStatusControl.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/DetailToolbar.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/EmptyState.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/HerbList/HerbListControl.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/InfoCard.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/MasterDetailLayout.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/PatientInfoCardControl.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SearchBox.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml`

- [ ] **Step 1: 执行替换**

使用 Task 2 的映射表，在上述 13 个文件中搜索并替换幽灵画刷。

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "refactor: replace ghost brushes with MDIX resources in Controls/Controls"
```

---

## Task 4: 清理幽灵画刷 - Shell 层

**Covers:** [S3.1] 幽灵画刷清理

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml`
- Modify: `src/Client/Desktop/Shell/Styles/Controls.xaml`
- Modify: `src/Client/Desktop/Shell/Styles/Typography.xaml`
- Modify: `src/Client/Desktop/Shell/Controls/AccountSettingsControl.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/UnfinishedCaseDialog.xaml`

- [ ] **Step 1: 执行替换**

使用 Task 2 的映射表，在上述 5 个文件中搜索并替换幽灵画刷。

- [ ] **Step 2: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "refactor: replace ghost brushes with MDIX resources in Shell layer"
```

---

## Task 5: 清理幽灵画刷 - 角色视图

**Covers:** [S3.1] 幽灵画刷清理

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/Views/ReceptionistHomeView.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Reports/Views/ReportsHomeView.xaml`

- [ ] **Step 1: 执行替换**

使用 Task 2 的映射表，在上述 7 个文件中搜索并替换幽灵画刷。

- [ ] **Step 2: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "refactor: replace ghost brushes with MDIX resources in role views"
```

---

## Task 6: 清理幽灵画刷 - Module 视图

**Covers:** [S3.1] 幽灵画刷清理

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseViewControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/WorkflowStepIndicator.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientEditControl.xaml`

- [ ] **Step 1: 执行替换**

使用 Task 2 的映射表，在上述 4 个文件中搜索并替换幽灵画刷。

- [ ] **Step 2: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "refactor: replace ghost brushes with MDIX resources in module views"
```

---

## Task 7: 清理注释残留

**Covers:** [S3.5] 注释残留清理

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/ButtonStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/InputStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/PanelStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/UnifiedComponents.xaml`

- [ ] **Step 1: 搜索 HandyControl 引用**

```bash
rg -n "HandyControl|Handy" src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/
```

- [ ] **Step 2: 清理注释**

移除或更新所有 HandyControl 相关注释，替换为 MDIX 描述。

- [ ] **Step 3: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: 提交**

```bash
git add -A
git commit -m "refactor: clean up HandyControl comment remnants"
```

---

## Task 8: 完整构建验证

**Covers:** 所有任务的集成验证

- [ ] **Step 1: 完整解决方案构建**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL (0 错误)

- [ ] **Step 2: 搜索残留幽灵画刷**

```bash
rg -n "SecondaryRegionBrush|ThirdlyTextBrush|SecondaryBorderBrush|DisabledBackgroundBrush|PrimaryRegionBrush|DataGridAlternatingRowBrush" src/ --include="*.xaml"
```
Expected: 0 结果

- [ ] **Step 3: 搜索残留旧 Token**

```bash
rg -n "SpacingXSmall|SpacingSmall|SpacingMedium|SpacingLarge|SpacingXLarge|SpacingXXLarge|CornerRadiusSmall|CornerRadiusMedium|CornerRadiusLarge" src/ --include="*.xaml"
```
Expected: 0 结果（DesignSystem.xaml 中的定义除外）

- [ ] **Step 4: 提交最终版本**

```bash
git add -A
git commit -m "refactor: complete shell theme体系重构 - 统一 MDIX 使用"
```

---

## 变更日志

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-26 | v1.0 | 初始实施计划 |
