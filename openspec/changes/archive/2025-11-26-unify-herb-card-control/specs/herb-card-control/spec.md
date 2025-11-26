# Capability: herb-card-control

药材卡片控件规范 - 统一经验方和处方的药材编辑体验。

## ADDED Requirements

### Requirement: Shared HerbCardControl Component

系统 SHALL 提供共享的 `HerbCardControl` 控件，支持药材选择、剂量输入和可选的价格显示。

#### Scenario: 基础药材选择
- **WHEN** 用户在药材名称输入框输入文本
- **THEN** 系统显示基于拼音码和名称的智能过滤建议列表
- **AND** 用户可通过键盘(Enter)或鼠标选择药材

#### Scenario: 价格显示控制
- **WHEN** `ShowPrice` 属性设置为 `true`
- **THEN** 控件显示药材单价列
- **WHEN** `ShowPrice` 属性设置为 `false`
- **THEN** 控件隐藏价格列

#### Scenario: 剂量输入完成跳转
- **WHEN** 用户在剂量输入框按下 Enter 键
- **THEN** 系统触发 `DosageCompletedCommand` 并将焦点移动到下一个药材卡片
- **AND** 如果是最后一个卡片，触发 `AddNewRowCommand` 添加新行

### Requirement: HerbItemViewModelBase Abstract Class

系统 SHALL 提供 `HerbItemViewModelBase` 抽象基类，封装药材项的共享逻辑。

#### Scenario: 拼音码过滤
- **WHEN** `HerbName` 属性变更
- **THEN** 系统基于拼音码前缀匹配、名称包含匹配等规则过滤 `FilteredHerbs` 集合
- **AND** 结果按匹配分数从高到低排序，最多显示5个

#### Scenario: 药材选择自动填充
- **WHEN** `SelectedHerb` 属性被设置为非空值
- **THEN** 系统自动填充 `HerbId`、`HerbName`、`Unit` 属性

#### Scenario: 抽象价格属性
- **WHEN** 子类实现 `UnitPrice` 属性
- **THEN** 经验方返回固定值 0
- **AND** 处方返回药材库中的实际价格

### Requirement: PrescriptionHerbItemViewModel Implementation

处方模块 SHALL 实现 `PrescriptionHerbItemViewModel`，继承 `HerbItemViewModelBase` 并提供价格计算功能。

#### Scenario: 实际价格获取
- **WHEN** 用户选择药材
- **THEN** `UnitPrice` 返回该药材在药材库中的价格

#### Scenario: 单项总价计算
- **WHEN** 剂量或单价变更
- **THEN** `ItemTotal` 属性自动计算为 `Dosage * UnitPrice`

### Requirement: Unified Layout Pattern

处方编辑和经验方编辑 SHALL 使用统一的 `ItemsControl + UniformGrid(4列)` 布局模式。

#### Scenario: 卡片布局
- **WHEN** 显示药材编辑区域
- **THEN** 药材卡片以4列网格布局显示
- **AND** 支持垂直滚动查看更多药材

#### Scenario: 处方价格汇总
- **WHEN** 处方编辑面板显示
- **THEN** 底部显示单剂价格（所有药材ItemTotal之和）
- **AND** 显示总价格（单剂价格 × 剂数）
