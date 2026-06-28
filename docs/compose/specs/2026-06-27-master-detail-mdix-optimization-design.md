# 用户管理 Master-Detail MDIX 优化设计

**日期**: 2026-06-27
**状态**: 设计已批准，待规格审阅
**范围**: 5 个 master-detail 控件（Users/Patients/Herbs/Formula/MedicalCase）+ 共享控件层

---

## [S1] 问题

当前 5 个 master-detail 控件（`UserMasterDetailControl` / `PatientMasterDetailControl` / `HerbMasterDetailControl` / `FormulaMasterDetailControl` / `MedicalCaseMasterDetailControl`）存在三类问题：

1. **分页重复**：每个控件 ~70 行内联分页代码，5 处共 ~350 行重复（`GoToFirstPageCommand` 等绑定复制 5 遍）。共享层已存在 `UnifiedPaginationBar` 但零消费。
2. **死代码**：`UnifiedManagementTable` / `UnifiedManagementToolBar` 是未完成的统一化尝试，零消费且设计不完整。
3. **MDIX 规范未落地**：
   - AGENTS.md 规定"间距用 Token（SpacingXS~XXXL）"，但该 Token 字典**从未创建**。
   - 工具栏 10 个按钮平铺一行，次要操作（导入/模板/恢复）与主操作争夺注意力。
   - ContextMenu 用第三套图标方案（Segoe Fluent Icons 文本字形 `&#xE70F;`），与已有的 `PackIcon`（MainWindow）和 `Icons.xaml`（Geometry）混用。
   - DataGrid 每列重复 `ElementStyle`（Foreground/VerticalAlignment/Padding）。
   - Detail 区视图↔编辑切换用两个重叠控件 + Visibility 转换器，无过渡。

## [S2] 解决方案概述

采用方案 C（全面），分两阶段：
- **本次（核心 7 项）**：共享层基础 + 跨模块采用 + Master 视觉重设计 + Detail 过渡。纯 XAML/控件层，不触碰 VM 逻辑。
- **后续（第 8 项）**：`MessageBox.Show` → MDIX `DialogHost`/`SnackbarHost`（跨应用 129+ 调用点，单独规格）。

依赖既有架构：`MasterDetailLayout`（4 插槽：Header/Master/Detail/Empty）+ `MasterDetailControlBase` + `DataGridToolbar` + `DetailToolbar` + `SearchBox` + `LoadingOverlay` + `EmptyState`。本次不改动这些既有控件的公共契约，仅新增/翻新/采用。

## [S3] 共享层基础（LYBT.Desktop.Controls）

### [S3.1] 死代码清理
- 删除 `Controls/UnifiedManagementTable.xaml(.cs)`（零消费；DataGrid 无列插槽，设计不完整）
- 删除 `Controls/UnifiedManagementToolBar.xaml(.cs)`（零消费；已被 `DataGridToolbar` 取代）

### [S3.2] 新增间距 Token 字典 `Themes/Spacing.xaml`
落地 AGENTS.md 规范，提供统一 Thickness 资源：

| Token | 值 | 用途 |
|-------|-----|------|
| `SpacingXS` | 4 | 紧凑间隔（图标与文字） |
| `SpacingS` | 8 | 标准小间隔（按钮间） |
| `SpacingM` | 12 | 中间隔（分组间） |
| `SpacingL` | 16 | 标准内边距 |
| `SpacingXL` | 24 | 大间隔 |
| `SpacingXXL` | 32 | 区块间隔 |

在 `App.xaml` 的 `MergedDictionaries` 中注册（与 `Icons.xaml`、`Converters.xaml` 同级）。

### [S3.3] 翻新 UnifiedPaginationBar
当前问题 → 修复：
- 硬编码 `<x:Array>` 页大小 → 新增 `PageSizes` DP（`IList<int>`），绑定 VM 的 `PageSizes`；保留数组作默认回退
- 硬编码 `Margin="8"` → 替换为 `{StaticResource SpacingS}`
- 命令 DP 名（`FirstPageCommand` 等）保持不变——由 XAML 绑定桥接到 VM 的 `GoToFirstPageCommand`，降低 churn
- 按钮可见性保留 `NullToVis` 机制（让首/末页按钮可选隐藏）
- 用 MDIX `PackIcon` 替代文本（首页/上一页/下一页/末页用 `Kind="PageFirst/ ChevronLeft/ ChevronRight/ PageLast"`）

## [S4] 跨模块采用（5 个 master-detail 控件）

### [S4.1] 分页统一
每个控件的 ~70 行内联分页替换为：
```xml
<controls:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage}"
    TotalPages="{Binding TotalPages}"
    PageSize="{Binding PageSize}"
    TotalCount="{Binding TotalCount}"
    PageSizes="{Binding PageSizes}"
    FirstPageCommand="{Binding GoToFirstPageCommand}"
    PreviousPageCommand="{Binding GoToPreviousPageCommand}"
    NextPageCommand="{Binding GoToNextPageCommand}"
    LastPageCommand="{Binding GoToLastPageCommand}" />
```
净收益：~350 行重复消除。覆盖 Users / Patients / Herbs / Formula / MedicalCase。

### [S4.2] DataGrid 列样式抽取 `Themes/DataGridStyles.xaml`
- `DataGridTextCellStyle` — 标准正文列（Foreground + VerticalAlignment=Center + Padding）
- `DataGridSecondaryCellStyle` — 次要列（Opacity=0.7）
- `DataGridMonoCellStyle` — 等宽列（Consolas + FontSize=12，如手机号/编码）
列改为 `ElementStyle="{StaticResource DataGridTextCellStyle}"` 引用。

### [S4.3] PackIcon 替代 Segoe Fluent Icons 文本
所有 `<TextBlock FontFamily="Segoe Fluent Icons" Text="&#xE70F;"/>` 替换为 `<materialDesign:PackIcon Kind="Edit"/>`。
覆盖：5 个控件的 ContextMenu 图标 + 工具栏图标。命名空间 `xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"`（MainWindow 已用此约定）。

## [S5] Master 区视觉重设计

### [S5.1] 工具栏分组（方案 A：溢出菜单）
当前 `DataGridToolbar.AdditionalContent` 平铺 6 按钮。重设计为：
- **主操作组**（保留按钮）：编辑（outlined，PackIcon Edit）
- **次要操作组**（收进"⋯ 更多"溢出 Menu）：重置密码 / 切换状态 / 恢复 / 导入 / 模板 / 导出
- 分组间用 `{StaticResource SpacingM}` 间隔，按钮间用 `{StaticResource SpacingS}`
- 溢出用 MDIX `Menu` + `MenuItem`（每项带 PackIcon），而非自定义弹窗

### [S5.2] 筛选区 Token 化
当前 `StackPanel` 硬编码 `Margin="0,0,8,0"`。改为：
- 角色/状态 ComboBox + 显示已禁用 CheckBox + 清除筛选 按钮
- 容器用 `StackPanel` 配合子元素 `{StaticResource SpacingS}` Margin（或引入简单 `WrapPanel`）
- 移除硬编码 `Margin`，统一 Token

## [S6] Detail 区过渡优化

### [S6.1] Transitioner 替代 Visibility 切换
当前 Detail 区用两个重叠控件 + `BoolToVis`/`InverseBoolToVis` 切换：
```xml
<userControls:UserViewControl Visibility="{Binding IsEditMode, Converter={x:Static Cvt.InverseBoolToVis}}" />
<userControls:UserEditControl Visibility="{Binding IsEditMode, Converter={x:Static Cvt.BoolToVis}}" />
```
改为 MDIX `Transitioner`（`materialDesign:Transitioner`）：
```xml
<materialDesign:Transitioner SelectedIndex="{Binding IsEditMode, Converter={x:Static Cvt.BoolToInt}}">
    <materialDesign:TransitionerSlide>
        <userControls:UserViewControl ... />
    </materialDesign:TransitionerSlide>
    <materialDesign:TransitionerSlide>
        <userControls:UserEditControl ... />
    </materialDesign:TransitionerSlide>
</materialDesign:Transitioner>
```
- 需新增 `BoolToInt` 转换器（`Converters.xaml`）：`false→0, true→1`
- 视图↔编辑切换有平滑滑动过渡，提升体验
- 覆盖 5 个控件的 Detail 区

### [S6.2] DetailToolbar 一致性
`DetailToolbar` 已抽象，本次仅确认其与 Token 系统一致（间距用 Token），不改公共契约。

## [S7] 推行顺序

按依赖关系分阶段，每阶段独立可验证：

1. **阶段 1（基础）**：S3.1 删死代码 → S3.2 Spacing.xaml → S3.3 翻新 PaginationBar → S4.2 DataGridStyles.xaml → S6.1 BoolToInt 转换器
2. **阶段 2（采用）**：S4.1 五控件采用 PaginationBar + S4.3 PackIcon + S5.1 工具栏分组 + S5.2 筛选区 + S6.1 Transitioner
3. **每阶段后**：`dotnet build` + 启动 Desktop 验证视觉

以 `UserMasterDetailControl` 为首个完整改造样本，验证模式无误后批量套用到其余 4 个控件。

## [S8] 测试策略

- **编译验证**：每个控件改造后 `dotnet build LYBTZYZS.sln` 必须通过
- **视觉验证**：启动 Desktop，进入 Admin 角色台 → 用户管理，验证：
  - 分页控件正常（首页/上一页/下一页/末页/页大小切换）
  - 工具栏溢出菜单展开正常
  - 筛选区布局无错位
  - 视图↔编辑切换有过渡动画
  - ContextMenu 图标显示正确（PackIcon）
- **既有测试**：`dotnet test tests/LYBT.Tests.Desktop/` 保持绿色（本次纯 XAML/控件层，VM 测试不受影响）
- **无新单元测试**：本次是 XAML/控件 DP 改动，不引入新 VM 逻辑

## [S9] 不在范围内

- **第 8 项（反馈层现代化）**：`MessageBox.Show` → `DialogHost`/`SnackbarHost`，跨应用 129+ 调用点，单独规格后续处理
- **VM 层逻辑改动**：本次不改 `MasterDetailViewModelBase` / `PaginationService` / `ErrorHandler` 等逻辑
- **MasterDetailLayout 插槽扩展**：现有 4 插槽足够，不新增 ToolbarSlot/FilterSlot
- **新功能**：不增加任何新业务功能，仅优化既有表面的视觉与结构

## [S10] 风险与缓解

| 风险 | 缓解 |
|------|------|
| Transitioner 过渡在某些主题下闪烁 | 可回退为 Visibility 切换；先在 Users 验证 |
| 溢出菜单发现性降低（用户找不到导入/模板） | Menu 项带 PackIcon + 文字；首次可加 Tooltip |
| 5 控件批量改造引入回归 | 分阶段，Users 先行验证模式，再套用其余 |
| PaginationBar PageSizes 绑定类型不匹配 | DP 用 `IList<int>`，VM 暴露 `IReadOnlyList<int>`，兼容 |
