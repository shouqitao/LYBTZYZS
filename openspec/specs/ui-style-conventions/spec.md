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

