# 全模块 UI 升级实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute to implement task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** 将 DESIGN.md v2 规范应用到所有业务模块（Herb/Formula/Patient/MedicalCase），统一视觉风格。

**Architecture:** 纯 XAML 改动，不涉及 ViewModel/C# 逻辑。按模块分 Task，每个模块独立可验证。参照已升级的 User 模块模式。

**Tech Stack:** WPF / MDIX / Prism

## Global Constraints

- 颜色用 `DynamicResource ValidationErrorBrush`，禁止 `#C75050`
- 间距用 Token 或数值+注释，禁止无注释硬编码
- 等宽字体 `Cascadia Code, Consolas, Microsoft YaHei UI`，禁止裸 `Consolas`
- ComboBox 用 `MinWidth="120"`，不用 `Width="100"`
- ViewControl 合并多卡片为单卡片+分区隔线（参照 UserViewControl 模式）
- EditControl 去掉 `Width="20"` 空列，用 Margin 做列间距
- commit message: `refactor(ui): apply DESIGN.md v2 to <module>`

## 已完成

- ✅ User 模块（MasterDetailControl + ViewControl + EditControl）
- ✅ 共享基础（Surfaces.xaml / Spacing / InfoCard / DetailToolbar / DataGridStyles / App.xaml）
- ✅ MasterDetailLayout 层次（L0 暖灰底 + L1 白面板）

---

### Task 1: Formula 模块升级

**Covers:** DESIGN.md §8.5（表单）、§8.4（DataGrid）

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaEditControl.xaml`

- [ ] **Step 1: FormulaMasterDetailControl — DataGrid 列宽弹性化**

读取文件，将 DataGrid 列的 `Width="N"` 固定值改为 `Width="*" MinWidth="N"` 或 `Width="Auto"`。字段名/简称用 `*`，短内容（状态、分类）用 `Auto`。

- [ ] **Step 2: FormulaEditControl — 修复硬编码颜色**

第23行 `<Setter Property="Foreground" Value="#C75050"/>` → `<Setter Property="Foreground" Value="{DynamicResource ValidationErrorBrush}"/>`

- [ ] **Step 3: FormulaEditControl — 去掉空列**

`<ColumnDefinition Width="20"/>` 删除，右列 `Grid.Column="2"` → `Grid.Column="1"`，左列加 `Margin="0,0,24,0"`。

- [ ] **Step 4: 编译验证**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Formula/LYBT.Desktop.Formula.csproj --nologo`
Expected: 0 errors

---

### Task 2: Herb 模块升级

**Covers:** DESIGN.md §8.5（表单）、§8.4（DataGrid）

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbViewControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbEditControl.xaml`

- [ ] **Step 1: HerbMasterDetailControl — 列宽弹性化**

同 Task 1 Step 1 模式。

- [ ] **Step 2: HerbViewControl — 合并多卡片为单卡片**

当前：多个 InfoCard（基本信息/规格信息/价格信息/功效用法/系统信息）。
改为：单 InfoCard + 概要头 + 分区隔线（参照 UserViewControl 模式）。每个区用 `TextBlock Opacity="0.6"` 做区标题 + `Border Height="1" Opacity="0.12"` 做分隔。

- [ ] **Step 3: HerbViewControl — 修复裸 Consolas**

所有 `FontFamily="Consolas"` → `FontFamily="Cascadia Code, Consolas, Microsoft YaHei UI"`。

- [ ] **Step 4: HerbViewControl — Label 列宽改 Auto**

`<ColumnDefinition Width="80"/>` → `<ColumnDefinition Width="Auto"/>`，Label 加 `Margin="0,8,16,8"`。

- [ ] **Step 5: HerbEditControl — 修复硬编码 + 去空列**

同 Task 1 Steps 2-3 模式。

- [ ] **Step 6: 编译验证**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Herbs/LYBT.Desktop.Herbs.csproj --nologo`
Expected: 0 errors

---

### Task 3: Patient 模块升级

**Covers:** DESIGN.md §8.5（表单）、§8.4（DataGrid）

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientViewControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientEditControl.xaml`

- [ ] **Step 1: PatientMasterDetailControl — 列宽弹性化 + 筛选栏**

同 Task 1 模式。如有 ComboBox `Width="100"` → `MinWidth="120"`。

- [ ] **Step 2: PatientViewControl — 合并多卡片 + 修复 Consolas**

当前：多 InfoCard（基本信息/联系信息/身份信息/系统信息）。
改为：单 InfoCard + 分区隔线。所有 `FontFamily="Consolas"` → `Cascadia Code, Consolas, Microsoft YaHei UI`。Label 列宽 `Width="80"` → `Auto`。

- [ ] **Step 3: PatientEditControl — 修复硬编码 + 去空列**

同 Task 1 Steps 2-3 模式。

- [ ] **Step 4: 编译验证**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Patients/LYBT.Desktop.Patients.csproj --nologo`
Expected: 0 errors

---

### Task 4: MedicalCase 模块升级

**Covers:** DESIGN.md §8.5（表单）、§8.4（DataGrid）

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseViewControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`

- [ ] **Step 1: MedicalCaseMasterDetailControl — 列宽弹性化**

同 Task 1 模式。

- [ ] **Step 2: MedicalCaseViewControl — 合并多卡片**

同 Task 2 Step 2 模式。

- [ ] **Step 3: MedicalCaseEditControl — 修复硬编码 + 去空列**

同 Task 1 Steps 2-3 模式。

- [ ] **Step 4: 编译验证**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj --nologo`
Expected: 0 errors

---

### Task 5: 全量编译验证

**Covers:** DESIGN.md 全局一致性

**Files:** 无修改

- [ ] **Step 1: 全解决方案编译**

Run: `dotnet build LYBTZYZS.sln --nologo`
Expected: 0 errors

- [ ] **Step 2: 硬编码扫描**

Run: `rg "#C75050|#FDE7E9" --include "*.xaml" src/Client/Desktop/`
Expected: 0 matches

Run: `rg 'FontFamily="Consolas"' --include "*.xaml" src/Client/Desktop/`
Expected: 0 matches（已全部替换为含中文回退的栈）

- [ ] **Step 3: 交付视觉验证**

提示用户逐一检查各模块 master-detail 视图：
- Formula：验方管理
- Herbs：药材管理
- Patients：患者管理
- MedicalCase：医案管理

---

## Self-Review

**Spec coverage:**
- §8.1 InfoCard → 已在前次完成 ✓
- §8.2 MasterDetailLayout → 已在前次完成 ✓
- §8.3 DetailToolbar → 已在前次完成 ✓
- §8.4 DataGrid → Task 1-4 Step 1 ✓
- §8.5 表单 → Task 1-4 Step 3 ✓
- §8.6 筛选栏 → Task 1-4 Step 1 ✓
- §8.7 PopupBox → 仅 User 模块有溢出菜单，其他模块无需 ✓

**Placeholder scan:** 无 TBD/TODO ✓

**Type consistency:** 所有 Task 使用相同的替换模式（颜色/列宽/空列）✓
