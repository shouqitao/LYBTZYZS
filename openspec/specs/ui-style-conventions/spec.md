# ui-style-conventions Specification

## Purpose
TBD - created by archiving change cleanup-ui-layer. Update Purpose after archive.
## Requirements
### Requirement: UI-001 Global Style Library

所有UI样式 MUST 定义在全局样式库中。

**规范**:
- 全局样式入口为 `Presentation/Themes/GlobalStyles.xaml`
- 颜色定义在 `Colors.xaml`
- 字体样式在 `Typography.xaml`
- 控件样式在 `Controls/*.xaml`

#### Scenario: Color definition
- **GIVEN** 需要使用颜色
- **WHEN** 定义颜色资源
- **THEN** MUST 定义在 `Colors.xaml`
- **AND** 使用语义化命名 (如 `PrimaryColor`, `ErrorColor`)
- **AND** NOT 在模块内重复定义相同颜色

#### Scenario: Control style definition
- **GIVEN** 需要定义按钮/文本框等控件样式
- **WHEN** 创建Style资源
- **THEN** MUST 放在对应的 `Controls/*.xaml` 文件
- **AND** 使用 `BasedOn` 继承默认样式
- **AND** 提供语义化的 `x:Key` 名称

#### Scenario: Module-specific style extension
- **GIVEN** 模块需要特定样式
- **WHEN** 无法使用全局样式满足
- **THEN** MAY 在模块内定义扩展样式
- **AND** MUST 基于全局样式扩展
- **AND** 样式名称以模块名为前缀 (如 `MedicalCase_CardStyle`)

### Requirement: UI-002 Style Naming Convention

样式命名 MUST 遵循统一规范。

**规范**:
- 格式: `{控件类型}_{用途}Style`
- 控件类型: Button, TextBox, DataGrid等
- 用途: Primary, Secondary, Danger, Card等

#### Scenario: Button style naming
- **GIVEN** 需要定义按钮样式
- **WHEN** 命名样式
- **THEN** 使用格式 `Button_{用途}Style`
- **EXAMPLE** `Button_PrimaryStyle`, `Button_DangerStyle`

#### Scenario: TextBox style naming
- **GIVEN** 需要定义文本框样式
- **WHEN** 命名样式
- **THEN** 使用格式 `TextBox_{用途}Style`
- **EXAMPLE** `TextBox_SearchStyle`, `TextBox_ReadOnlyStyle`

### Requirement: UI-003 Style Reference Pattern

View中引用样式 MUST 使用StaticResource。

**规范**:
- 使用 `{StaticResource StyleKey}` 引用
- 避免内联样式定义
- 避免DynamicResource除非需要运行时切换

#### Scenario: Style reference in View
- **GIVEN** View需要应用样式
- **WHEN** 引用样式资源
- **THEN** 使用 `Style="{StaticResource Button_PrimaryStyle}"`
- **AND** NOT 使用内联 `<Button.Style>` 定义
- **AND** NOT 重复定义已存在的样式

#### Scenario: Conditional style
- **GIVEN** 需要根据条件切换样式
- **WHEN** 实现样式切换
- **THEN** 使用DataTrigger或StyleSelector
- **AND** NOT 在ViewModel中设置Style属性

### Requirement: UI-004 Resource Dictionary Organization

资源字典 MUST 按功能分类组织。

**规范**:
```
Themes/
├─ GlobalStyles.xaml       # 合并入口
├─ Colors.xaml             # 颜色
├─ Typography.xaml         # 字体
├─ Controls/
│   ├─ ButtonStyles.xaml
│   ├─ TextBoxStyles.xaml
│   ├─ DataGridStyles.xaml
│   └─ CardStyles.xaml
└─ Templates/
    ├─ DialogTemplates.xaml
    └─ ListTemplates.xaml
```

#### Scenario: Adding new control style
- **GIVEN** 需要添加新控件类型的样式
- **WHEN** 创建样式文件
- **THEN** 放在 `Controls/` 目录
- **AND** 文件名为 `{控件类型}Styles.xaml`
- **AND** 在 `GlobalStyles.xaml` 中合并

#### Scenario: Adding new template
- **GIVEN** 需要添加数据模板
- **WHEN** 创建模板
- **THEN** 放在 `Templates/` 目录
- **AND** 按用途分组到对应文件

### Requirement: UI-010 Detail View Layout Convention

所有实体详情页 MUST 采用统一的三行布局结构。

**规范**:
- 顶部工具栏: 固定高度56px，包含返回按钮、标题、编辑按钮
- 内容区域: 自适应高度，无滚动设计（FHD优先）
- 底部操作栏: 固定高度56px，包含状态信息和操作按钮

#### Scenario: Detail view structure
- **GIVEN** 创建实体详情页
- **WHEN** 定义页面布局
- **THEN** MUST 使用三行Grid布局
- **AND** 顶部工具栏高度为56px
- **AND** 底部操作栏高度为56px
- **AND** 内容区域使用 `Height="*"` 自适应

#### Scenario: Edit button placement
- **GIVEN** 详情页需要编辑功能
- **WHEN** 放置编辑按钮
- **THEN** MUST 放在顶部工具栏右侧
- **AND** 使用 `DetailViewEditButtonStyle` 样式
- **AND** NOT 放在底部操作栏或内容区域

#### Scenario: FHD no-scroll design
- **GIVEN** 详情页在1920x1080分辨率下显示
- **WHEN** 内容区域填充数据
- **THEN** SHOULD 无需滚动即可显示所有内容
- **AND** 使用2-4列灵活布局压缩垂直空间
- **AND** MAY 保留ScrollViewer作为后备

### Requirement: UI-011 Detail View Shared Styles

详情页 MUST 使用统一的共享样式。

**规范**:
- 工具栏样式: `DetailViewToolbarStyle`
- 标题样式: `DetailViewTitleStyle`
- 编辑按钮样式: `DetailViewEditButtonStyle`
- 内容区域样式: `DetailViewContentStyle`
- 底部栏样式: `DetailViewFooterStyle`
- 表单标签样式: `FormLabelStyle`
- 表单值样式: `FormValueStyle`

#### Scenario: Toolbar styling
- **GIVEN** 详情页顶部工具栏
- **WHEN** 应用样式
- **THEN** MUST 使用 `Style="{StaticResource DetailViewToolbarStyle}"`
- **AND** 标题使用 `DetailViewTitleStyle`
- **AND** 编辑按钮使用 `DetailViewEditButtonStyle`

#### Scenario: Form field styling
- **GIVEN** 详情页表单字段
- **WHEN** 显示标签和值
- **THEN** 标签使用 `Style="{StaticResource FormLabelStyle}"`
- **AND** 值使用 `Style="{StaticResource FormValueStyle}"`
- **AND** NOT 在视图内定义重复的本地样式

### Requirement: UI-012 Form Layout Flexibility

表单布局 MUST 根据内容灵活使用多列布局。

**规范**:
- 2列布局: 字段少，内容简单
- 3列布局: 字段中等
- 4列布局: 字段多，内容紧凑
- 使用比例宽度 `*` 而非固定像素

#### Scenario: Patient detail layout
- **GIVEN** 患者详情页
- **WHEN** 排列表单字段
- **THEN** 使用4列布局
- **AND** 每行放置4个简短字段（如：姓名、性别、年龄、手机）

#### Scenario: Herb detail layout
- **GIVEN** 药材详情页
- **WHEN** 排列表单字段
- **THEN** 使用3列布局
- **AND** 功效和备注字段占用全宽

#### Scenario: Responsive column width
- **GIVEN** 表单使用多列布局
- **WHEN** 定义列宽
- **THEN** 使用 `Width="*"` 比例宽度
- **AND** NOT 使用固定像素宽度

### Requirement: UI-013 Detail View Style Prohibition

详情页 MUST NOT 定义重复的本地样式。

**规范**:
- 详情页禁止定义 `FormLabelStyle`、`EditableTextBoxStyle` 等本地样式
- 所有详情页共享样式定义在 `UnifiedComponents.xaml`

#### Scenario: Detail view style reference
- **GIVEN** 详情页需要表单样式
- **WHEN** 引用样式
- **THEN** MUST 引用 `UnifiedComponents.xaml` 中的共享样式
- **AND** NOT 在 `<UserControl.Resources>` 中重复定义

