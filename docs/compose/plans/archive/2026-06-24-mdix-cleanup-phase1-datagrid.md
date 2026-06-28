# MDIX 样式清理 - 阶段 1：修复 DataGrid 闪烁 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 删除 DataGridStyles.xaml 中所有自定义 ControlTemplate 和 Style，让全部 DataGrid 回归 MDIX 默认样式，消除用户管理列表闪烁。

**Architecture:** DataGridStyles.xaml 定义了 MasterDetailDataGridRowStyle（含自定义 ControlTemplate，是闪烁根因）、BaseDataGridCell（含 ControlTemplate）、以及多个纯 Setter 样式。阶段 1 删除整个文件，移除 UnifiedComponents.xaml 的引用，并将 5 个 MasterDetail 控件 + UnifiedManagementTable 的 DataGrid 改为不指定自定义 Style（由 MDIX Defaults 自动应用）。DataGrid 必需的行为属性（AutoGenerateColumns=False 等）直接设在元素上。

**Tech Stack:** WPF, MaterialDesignInXAML, Prism

## Global Constraints

- **不添加注释** —— 除非用户要求
- **不添加 Emoji** —— 除非用户要求
- **commit 语言用英文** —— PowerShell 中文编码问题
- **commit 格式** —— `refactor(desktop): 描述`
- **UI 控件 MDIX 优先** —— 用 MDIX 内置样式，不自定义 ControlTemplate
- **间距用 Token** —— 但本阶段删除的样式不涉及间距迁移，后续阶段处理
- **跨模块禁止** —— Server/Desktop 模块间禁止直接引用（本阶段不涉及）
- **验证命令** —— `dotnet build LYBTZYZS.sln` 必须通过

---

## File Structure

| 文件 | 操作 | 职责 |
|------|------|------|
| `Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml` | **删除** | 含自定义 ControlTemplate 的 DataGrid 样式 |
| `Core/LYBT.Desktop.Controls/Themes/UnifiedComponents.xaml` | 修改 | 移除对 DataGridStyles.xaml 的 MergedDictionary 引用 |
| `Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml` | 修改 | DataGrid 移除自定义 Style |
| `Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml` | 修改 | DataGrid 移除自定义 Style |
| `Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml` | 修改 | DataGrid 移除自定义 Style |
| `Modules/LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml` | 修改 | DataGrid 移除自定义 Style |
| `Modules/LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml` | 修改 | DataGrid 移除自定义 Style |
| `Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml` | 修改 | DataGrid 移除 Base* 样式引用 |

---

## Task 1: 删除 DataGridStyles.xaml 并清理引用

**Covers:** [S4] 阶段1

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/UnifiedComponents.xaml`

**Interfaces:**
- Produces: DataGridStyles.xaml 不再存在，UnifiedComponents 不再合并它。后续 Task 2/3 的 DataGrid 元素不再能引用 MasterDetailDataGridStyle / BaseDataGridStyle 等任何 key。

- [ ] **Step 1: 删除 DataGridStyles.xaml 文件**

删除整个文件：
```
src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml
```

该文件定义了以下样式，全部删除：
- `BaseDataGridStyle`（纯 Setter）
- `BaseDataGridRow`（纯 Setter + Triggers）
- `MasterDetailDataGridRowStyle`（**含自定义 ControlTemplate** — 闪烁根因）
- `BaseDataGridCell`（**含自定义 ControlTemplate**）
- `MasterDetailDataGridCellStyle`（BasedOn BaseDataGridCell）
- `MasterDetailDataGridStyle`（BasedOn BaseDataGridStyle）
- `BaseDataGridColumnHeader`（纯 Setter）

- [ ] **Step 2: 从 UnifiedComponents.xaml 移除 DataGridStyles 引用**

文件：`src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/UnifiedComponents.xaml`

删除这一行（约第20行）：
```xml
        <ResourceDictionary Source="DataGridStyles.xaml" />
```

修改后的 MergedDictionaries 应为：
```xml
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Converters/Converters.xaml" />
        <ResourceDictionary Source="PreviewStyles.xaml" />
        <ResourceDictionary Source="ValidationStyles.xaml" />
        <ResourceDictionary Source="DesignSystem.xaml" />
        <ResourceDictionary Source="ButtonStyles.xaml" />
        <ResourceDictionary Source="InputStyles.xaml" />
        <ResourceDictionary Source="PanelStyles.xaml" />
    </ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 3: 检查 .csproj 是否有显式 Resource Include**

某些项目会在 .csproj 中用 `<Page Include="...DataGridStyles.xaml">` 显式声明。搜索确认：

Run: `rg "DataGridStyles" src/Client/Desktop/Core/LYBT.Desktop.Controls/ --type csproj -l`
Expected: 无结果（XAML 通常由通配规则自动包含）

如果有 .csproj 显式引用，删除对应 `<Page>` 或 `<Resource>` 条目。

- [ ] **Step 4: 验证编译（预期会有引用错误）**

Run: `dotnet build LYBTZYZS.sln`
Expected: **编译失败** —— 6 个文件引用了已删除的样式 key。错误信息会列出具体文件和行号。这是预期的，Task 2/3 会修复。

记录报错的文件列表，供 Task 2/3 参考。

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DataGridStyles.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/UnifiedComponents.xaml
git commit -m "refactor(desktop): remove DataGridStyles.xaml custom templates (phase 1/6)"
```

---

## Task 2: 更新 5 个 MasterDetail 控件的 DataGrid

**Covers:** [S4] 阶段1

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml`

**Interfaces:**
- Consumes: Task 1 已删除 DataGridStyles.xaml，这些文件中的 `Style="{DynamicResource MasterDetailDataGridStyle}"` 引用现在失效。
- Produces: 所有 DataGrid 元素不再引用自定义样式，改用 MDIX 默认 + 内联行为属性。

**替换原则：**

每个 MasterDetail 控件的 DataGrid 元素当前形如：
```xml
<DataGrid
    ...
    Style="{DynamicResource MasterDetailDataGridStyle}">
```

替换为（移除 Style，添加 MDIX 默认下必需的行为属性）：
```xml
<DataGrid
    ...
    AutoGenerateColumns="False"
    CanUserAddRows="False"
    CanUserDeleteRows="False"
    IsReadOnly="True"
    SelectionMode="Single"
    SelectionUnit="FullRow"
    GridLinesVisibility="None"
    HeadersVisibility="Column"
    BorderThickness="0"
    Background="Transparent"
    RowHeaderWidth="0">
```

**注意：** 保留每个 DataGrid 元素上已有的其他属性（ItemsSource, SelectedItem, behaviors:DataGridSelectionBehavior 等）。只删除 `Style="{DynamicResource MasterDetailDataGridStyle}"` 这一行，添加上述行为属性中缺失的。

- [ ] **Step 1: 修改 UserMasterDetailControl.xaml**

文件：`src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`
约第157-165行。

将：
```xml
                <DataGrid
                    Grid.Row="2"
                    Margin="0,0,0,8"
                    behaviors:DataGridSelectionBehavior.SelectedItems="{Binding SelectedItems, Mode=OneWay}"
                    behaviors:DataGridSelectionBehavior.ShowCheckBoxColumn="True"
                    ItemsSource="{Binding Items}"
                    RowHeaderWidth="0"
                    SelectedItem="{Binding SelectedItem, Mode=TwoWay}"
                    Style="{DynamicResource MasterDetailDataGridStyle}">
```

改为：
```xml
                <DataGrid
                    Grid.Row="2"
                    Margin="0,0,0,8"
                    AutoGenerateColumns="False"
                    CanUserAddRows="False"
                    CanUserDeleteRows="False"
                    IsReadOnly="True"
                    SelectionMode="Single"
                    SelectionUnit="FullRow"
                    GridLinesVisibility="None"
                    HeadersVisibility="Column"
                    BorderThickness="0"
                    Background="Transparent"
                    behaviors:DataGridSelectionBehavior.SelectedItems="{Binding SelectedItems, Mode=OneWay}"
                    behaviors:DataGridSelectionBehavior.ShowCheckBoxColumn="True"
                    ItemsSource="{Binding Items}"
                    RowHeaderWidth="0"
                    SelectedItem="{Binding SelectedItem, Mode=TwoWay}">
```

- [ ] **Step 2: 修改 PatientMasterDetailControl.xaml**

文件：`src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml`
约第113行。

找到 `Style="{DynamicResource MasterDetailDataGridStyle}"`，删除该属性行。
在 DataGrid 元素上确保有以下属性（如果缺失则添加）：
```
AutoGenerateColumns="False"
CanUserAddRows="False"
CanUserDeleteRows="False"
IsReadOnly="True"
SelectionMode="Single"
SelectionUnit="FullRow"
GridLinesVisibility="None"
HeadersVisibility="Column"
BorderThickness="0"
Background="Transparent"
```

保留该文件中 DataGrid 已有的其他属性不变。

- [ ] **Step 3: 修改 MedicalCaseMasterDetailControl.xaml**

文件：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml`
约第64行。

同 Step 2 的操作：删除 `Style="{DynamicResource MasterDetailDataGridStyle}"`，添加行为属性。

- [ ] **Step 4: 修改 HerbMasterDetailControl.xaml**

文件：`src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml`
约第80行。

同 Step 2 的操作。

- [ ] **Step 5: 修改 FormulaMasterDetailControl.xaml**

文件：`src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml`
约第69行。

同 Step 2 的操作。

- [ ] **Step 6: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: 仍有 1 个错误 —— UnifiedManagementTable.xaml 引用 Base* 样式（Task 3 修复）。5 个 MasterDetail 控件的错误应已消除。

- [ ] **Step 7: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml
git commit -m "refactor(desktop): migrate MasterDetail DataGrids to MDIX defaults (phase 1/6)"
```

---

## Task 3: 更新 UnifiedManagementTable + 最终验证

**Covers:** [S4] 阶段1

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml`

**Interfaces:**
- Consumes: Task 1 已删除 BaseDataGridStyle / BaseDataGridCell / BaseDataGridRow / BaseDataGridColumnHeader。

- [ ] **Step 1: 修改 UnifiedManagementTable.xaml**

文件：`src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml`
第19-28行。

将：
```xml
        <DataGrid x:Name="DataGrid"
                  ItemsSource="{Binding ItemsSource, RelativeSource={RelativeSource AncestorType=UserControl}}"
                  SelectedItem="{Binding SelectedItem, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}"
                  SelectionMode="Extended"
                  SelectionUnit="FullRow"
                  Style="{StaticResource BaseDataGridStyle}"
                  CellStyle="{StaticResource BaseDataGridCell}"
                  RowStyle="{StaticResource BaseDataGridRow}"
                  ColumnHeaderStyle="{StaticResource BaseDataGridColumnHeader}"
                  AutoGenerateColumns="False" />
```

改为：
```xml
        <DataGrid x:Name="DataGrid"
                  ItemsSource="{Binding ItemsSource, RelativeSource={RelativeSource AncestorType=UserControl}}"
                  SelectedItem="{Binding SelectedItem, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}"
                  SelectionMode="Extended"
                  SelectionUnit="FullRow"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True"
                  GridLinesVisibility="None"
                  HeadersVisibility="Column"
                  BorderThickness="0"
                  Background="Transparent" />
```

- [ ] **Step 2: 验证编译通过**

Run: `dotnet build LYBTZYZS.sln`
Expected: **BUILD SUCCEEDED**，0 errors。

如果仍有错误，检查是否有遗漏的引用点（运行 `rg "MasterDetailDataGrid|BaseDataGrid" src/Client/Desktop/ --type xaml`），逐一修复。

- [ ] **Step 3: 确认无残留引用**

Run: `rg "DataGridStyles|MasterDetailDataGridStyle|MasterDetailDataGridRowStyle|MasterDetailDataGridCellStyle|BaseDataGridStyle|BaseDataGridRow|BaseDataGridCell|BaseDataGridColumnHeader" src/Client/Desktop/ --type xaml`
Expected: **无结果**（所有引用已清除）

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/UnifiedManagementTable.xaml
git commit -m "refactor(desktop): migrate UnifiedManagementTable to MDIX defaults (phase 1/6)"
```

- [ ] **Step 5: 标记阶段 1 完成**

阶段 1（修复 DataGrid 闪烁）全部完成。后续阶段（2-6）在用户确认后另行规划。

更新任务状态，通知用户阶段 1 已完成，可以手动测试用户管理页确认闪烁消除。
