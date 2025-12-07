# ui-style-conventions Delta

## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: UI-003 Style Reference Pattern (Modified)

View中引用样式 MUST 使用StaticResource，详情页 MUST NOT 定义重复的本地样式。

**新增规范**:
- 详情页禁止定义 `FormLabelStyle`、`EditableTextBoxStyle` 等本地样式
- 所有详情页共享样式定义在 `UnifiedComponents.xaml`

#### Scenario: Detail view style reference
- **GIVEN** 详情页需要表单样式
- **WHEN** 引用样式
- **THEN** MUST 引用 `UnifiedComponents.xaml` 中的共享样式
- **AND** NOT 在 `<UserControl.Resources>` 中重复定义
