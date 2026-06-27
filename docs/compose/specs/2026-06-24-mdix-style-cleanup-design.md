# 全面回归 MDIX 样式清理

**日期**: 2026-06-24
**状态**: 已批准，待实施
**触发**: 用户管理列表点击时闪烁（FOUC），怀疑样式干扰

---

## [S1] 问题背景

### 闪烁根因

用户管理列表（`UserMasterDetailControl`）的 DataGrid 使用 `DynamicResource MasterDetailDataGridStyle`。该样式定义在 `DataGridStyles.xaml` 中，**完全自定义了 DataGridRow 的 ControlTemplate**（左侧选中指示条）。导航到用户管理时：

1. DataGrid 先以 MDIX 默认样式渲染（`MaterialDesign2.Defaults.xaml` 自动应用）
2. `DynamicResource` 延迟解析到 `MasterDetailDataGridStyle` 后重新应用自定义 ControlTemplate
3. 模板切换导致可见的闪烁（Flash of Unstyled Content）

### 非 MDIX 样式全貌

项目积累了大量绕过 MDIX 的自定义 Style 和 ControlTemplate，分布在 12+ 文件中：

| 文件 | 含 ControlTemplate 的样式 | 纯 Setter 样式 |
|------|--------------------------|---------------|
| `Controls/ButtonStyles.xaml` | PrimaryButton, SecondaryButton, DangerButton, SuccessButton, WarningButton, InfoButton, LinkButton（7个全含模板） | — |
| `Controls/DataGridStyles.xaml` | MasterDetailDataGridRowStyle, BaseDataGridCell | BaseDataGridStyle, BaseDataGridRow, MasterDetailDataGridCellStyle, MasterDetailDataGridStyle, BaseDataGridColumnHeader |
| `Controls/InputStyles.xaml` | SearchTextBox, EditableTextBoxStyle, ValidatingTextBoxStyle | ValueDisplayStyle, FilterComboBox 等 |
| `Controls/PanelStyles.xaml` | PaginationControlButton, MasterDetailPaginationButtonStyle, DetailViewToolbarButtonStyle | 14个（含高引用 DetailLabel×61, DetailValue×56） |
| `Controls/ValidationStyles.xaml` | ValidatingComboBoxStyle, ValidatingPasswordBoxStyle | 4个 |
| `Controls/PreviewStyles.xaml` | — | 18个 |
| `Controls/HomePageStyles.xaml` | TransparentCardButton | 5个 |
| `Controls/MedicalCaseStyles.xaml` | MedicalCaseButtonBaseStyle, FormTextBoxStyle | 19个（与 Typography 有5个key冲突） |
| `Controls/BreadcrumbStyles.xaml` | BreadcrumbItemContainerStyle, BreadcrumbButtonStyle（含失效动画bug） | 2个 |
| `Controls/NavigationSuggestionsPanelStyles.xaml` | SuggestionItemContainerStyle, NavigationSuggestionsPanelStyle | 2个 |
| `Controls/NavigationHistoryPanelStyles.xaml` | （待确认） | — |
| `Shell/Styles/Controls.xaml` | StandardTextBox, FormTextBox, SearchTextBox, StandardComboBox | StandardDataGrid, PanelCard, Card 等 + 6个别名 |
| `Shell/Styles/Typography.xaml` | — | （与 MedicalCase 有5个key冲突） |
| `Shell/Styles/DialogStyles.xaml` | （待确认） | — |
| `Controls/DesignSystem.xaml` | — | BaseTextBox, TransparentButtonStyle, PaginationCurrentPage, PaginationPageNumber, ContentContainer |

**已知隐患**：
- MedicalCaseStyles.xaml 与 Typography.xaml 有 5 个 key 名冲突（SectionHeaderStyle / FieldLabelStyle / RequiredMarkStyle / PanelTitleStyle / HintTextStyle）
- BreadcrumbButtonStyle 有失效动画 bug（目标名不存在）
- PreviewStyles 多处硬编码字体未走 PrimaryFontFamily

---

## [S2] 目标

**全面回归 MDIX**：删除所有自定义 ControlTemplate、自定义 Style 和 DesignSystem 设计令牌，XAML 中统一使用 MaterialDesign 内置样式和值。

清理后 App.xaml 仅保留：
1. MDIX `BundledTheme`（Light/Brown/Amber）+ `MaterialDesign2.Defaults.xaml`
2. `Converters.xaml`（转换器，非样式，保留）
3. `Icons.xaml`（矢量图标 Path data，保留）
4. 极少量内联语义色（Success/Warning/Info，MDIX 无等效）

---

## [S3] MDIX 替换映射

### 控件样式映射

| 自定义样式 | MDIX 等效 | 说明 |
|-----------|-----------|------|
| PrimaryButton | `MaterialDesignRaisedButton` | 主色填充按钮 |
| SecondaryButton / OutlineButton | `MaterialDesignOutlinedButton` | 描边按钮 |
| DangerButton | `MaterialDesignRaisedButton` + 内联 Background=`{DynamicResource MaterialDesign.Brush.Error}` | 红色危险按钮 |
| SuccessButton / WarningButton / InfoButton | `MaterialDesignRaisedButton` + 内联 Background 硬编码色值 | MDIX 无语义色 |
| LinkButton | `MaterialDesignFlatButton` | 扁平文字按钮 |
| StandardTextBox / FormTextBox | `MaterialDesignOutlinedTextBox` | 描边输入框 |
| SearchTextBox | `MaterialDesignOutlinedTextBox` + 内联图标 | 搜索框图标内联 |
| StandardComboBox | `MaterialDesignOutlinedComboBox` | 描边下拉框 |
| MasterDetailDataGridStyle | MDIX 默认（移除 Style 引用，Defaults 自动应用） | DataGrid |
| BaseDataGridCell 模板 | 删除（MDIX 默认） | 去除自定义模板 |
| MasterDetailDataGridRowStyle | 删除（MDIX 默认） | 去除自定义模板 |

### 颜色令牌映射

| DesignSystem 令牌 | MDIX 替代 | 说明 |
|------------------|-----------|------|
| `PrimaryBrush` | `MaterialDesign.Brush.Primary` | Brown主题已配 |
| `DarkPrimaryBrush` | `MaterialDesign.Brush.Primary.Dark` | |
| `LightPrimaryBrush` | `MaterialDesign.Brush.Primary.Light` | |
| `BackgroundBrush` | `MaterialDesign.Brush.Background` | |
| `SurfaceBrush` | `MaterialDesign.Brush.Paper` | |
| `BorderBrush` | `MaterialDesign.Brush.Outline` | |
| `PrimaryTextBrush` | `MaterialDesign.Brush.Foreground` | |
| `SecondaryTextBrush` | `MaterialDesign.Brush.Foreground` + Opacity=0.56 | |
| `DisabledTextBrush` | `MaterialDesign.Brush.BodyLight` | |
| `DangerBrush` | `MaterialDesign.Brush.Error` | |
| `SuccessBrush` (#2E8B57) | **内联硬编码** `#2E8B57` | MDIX 无语义色 |
| `WarningBrush` (#DAA520) | **内联硬编码** `#DAA520` | MDIX 无语义色 |
| `InfoBrush` (#5B8FA8) | **内联硬编码** `#5B8FA8` | MDIX 无语义色 |
| Light/Dark 变体 | **内联硬编码** | 少量使用处 |

### 间距 / 圆角 / 阴影令牌

| 令牌 | 替代 | 说明 |
|------|------|------|
| `SpacingXS`(4) ~ `SpacingXXXL`(48) | **硬编码** `Margin="4"` / `Padding="16"` 等 | MDIX 无间距系统 |
| `RadiusSM`(4) / `RadiusMD`(8) / `RadiusLG`(12) | **硬编码** `CornerRadius="8"` | |
| `CardShadow` / `FloatingShadow` / `SubtleShadow` | MDIX `Card` 控件自带阴影，或 `materialDesign:ShadowAssist.ShadowDepth="2"` | |
| `PrimaryFontFamily` | 删除（MDIX 自带字体） | |

---

## [S4] 6 阶段执行计划

每阶段独立 `dotnet build` 验证 + 独立 git 提交。可随时暂停，已完成阶段保持稳定。

### 阶段 1：修复用户管理闪烁（最高优先）

**目标**：消除 MasterDetail 页 DataGrid 闪烁

**操作**：
1. `DataGridStyles.xaml`：删除 `MasterDetailDataGridRowStyle`（含自定义 ControlTemplate）、`BaseDataGridCell`（含自定义 ControlTemplate）、`MasterDetailDataGridCellStyle`
2. `BaseDataGridStyle` / `BaseDataGridRow` / `MasterDetailDataGridStyle` / `BaseDataGridColumnHeader`：保留纯 Setter 或合并到 MDIX 默认（视引用情况决定）
3. 搜索所有引用 `MasterDetailDataGridStyle` / `MasterDetailDataGridRowStyle` / `MasterDetailDataGridCellStyle` 的 XAML，移除 `Style="{DynamicResource ...}"` 让 DataGrid 使用 MDIX 默认
4. 选中行指示条等视觉效果如需保留，改用 MDIX `DataGridRow` 的 Style trigger（纯 Setter，无 ControlTemplate）

**验证**：`dotnet build` + 手动确认用户管理页无闪烁

**涉及文件**：DataGridStyles.xaml + 所有 MasterDetail UserControl（Users/Patients/Herbs/Formula 等）

### 阶段 2：按钮回归 MDIX

**目标**：删除 ButtonStyles.xaml 全部自定义按钮模板

**操作**：
1. `ButtonStyles.xaml`：删除全部 7 个样式定义（文件最终清空或删除）
2. 全项目搜索引用 `PrimaryButton` / `SecondaryButton` / `DangerButton` / `SuccessButton` / `WarningButton` / `InfoButton` / `LinkButton` 的 XAML
3. 按映射表替换为 MaterialDesign 按钮样式
4. Shell/Controls.xaml 中的按钮别名（PrimaryButtonStyle 等）一并删除

**验证**：`dotnet build`

### 阶段 3：输入控件回归 MDIX

**目标**：删除 TextBox / ComboBox / PasswordBox 自定义模板

**操作**：
1. `InputStyles.xaml`：删除 SearchTextBox / EditableTextBoxStyle / ValidatingTextBoxStyle 模板
2. `Shell/Controls.xaml`：删除 StandardTextBox / FormTextBox / FormTextArea / SearchTextBox / StandardComboBox 模板 + StandardDataGrid / StandardDataGridColumnHeader
3. `ValidationStyles.xaml`：删除 ValidatingComboBoxStyle / ValidatingPasswordBoxStyle 模板
4. `DesignSystem.xaml`：删除 BaseTextBox
5. 搜索全项目引用点，按映射表替换为 MaterialDesign 输入样式

**验证**：`dotnet build`

### 阶段 4：面板 / 详情样式

**目标**：清理 PanelStyles.xaml（17个样式，含高引用 DetailLabel×61 / DetailValue×56）

**操作**：
1. `PanelStyles.xaml`：删除 PaginationControlButton / MasterDetailPaginationButtonStyle / DetailViewToolbarButtonStyle（ControlTemplate）
2. 纯 Setter 的 TextBlock / Border 样式：改为内联或 Typography 等效
3. `DesignSystem.xaml`：删除 ContentContainer / TransparentButtonStyle / PaginationCurrentPage / PaginationPageNumber
4. `Shell/Controls.xaml`：删除 PanelCard / PanelHeader / ActionBar / InfoBar / Card / Divider 及别名
5. 高引用样式（DetailLabel / DetailValue）的引用点批量替换为内联 Setter

**验证**：`dotnet build`

### 阶段 5：令牌迁移

**目标**：删除 DesignSystem.xaml 全部令牌，改用 MDIX 值或硬编码

**操作**：
1. `DesignSystem.xaml`：删除所有 Color / Brush / Thickness / CornerRadius / DropShadowEffect / FontFamily 资源
2. 全项目搜索引用点：
   - 颜色令牌 → MDIX Brush 或硬编码（按映射表）
   - 间距令牌 → 硬编码数值
   - 圆角令牌 → 硬编码数值
   - 阴影 → MDIX ShadowAssist
3. 删除 DesignSystem.xaml 文件

**验证**：`dotnet build`

### 阶段 6：其余样式文件 + App.xaml 收尾

**目标**：清理剩余文件，精简 App.xaml

**操作**：
1. `PreviewStyles.xaml`：18个纯 Setter 样式 → 内联或删除
2. `HomePageStyles.xaml`：TransparentCardButton 模板 + 5个 Setter → MDIX Card + 内联
3. `MedicalCaseStyles.xaml`：21个样式（含与 Typography 冲突的5个）→ 清理或内联
4. `BreadcrumbStyles.xaml`：修复/删除失效动画，改用 MDIX
5. `NavigationSuggestions/HistoryPanelStyles.xaml`：清理自定义 ListBox 模板
6. `Shell/Styles/Typography.xaml` + `DialogStyles.xaml`：清理或合并到 MDIX
7. `App.xaml`：移除所有已清空的 ResourceDictionary 引用，最终仅保留 MDIX + Converters + Icons

**验证**：`dotnet build` + 全局视觉检查

---

## [S5] 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| MDIX 无语义色（Success/Warning/Info） | StatusBadge 等状态显示失色 | 内联硬编码色值，集中定义极少量语义色 |
| 高引用样式替换遗漏 | 编译失败（找不到资源） | 每阶段 `dotnet build` 验证，搜索确保无遗漏 |
| MDIX 默认 DataGrid 外观与自定义差异大 | 视觉变化 | 阶段1完成后手动确认可接受，必要时用纯Setter微调 |
| 间距硬编码后不一致 | 视觉对齐问题 | 统一使用标准值（4/8/12/16/24/32/48） |
| 一次性改动量大 | 回滚困难 | 分6阶段独立提交，每阶段可独立回滚 |

---

## [S6] 验证策略

- **每阶段**：`dotnet build LYBTZYZS.sln` 必须通过
- **阶段1**：额外手动确认用户管理页无闪烁
- **阶段6**：全局视觉检查（用户管理/患者/药材/验方/医案各页）
- **成功标准**：
  1. App.xaml 仅引用 MDIX + Converters + Icons
  2. 无自定义 ControlTemplate 残留
  3. 无 DesignSystem 令牌残留
  4. 用户管理页无闪烁
  5. `dotnet build` 通过
