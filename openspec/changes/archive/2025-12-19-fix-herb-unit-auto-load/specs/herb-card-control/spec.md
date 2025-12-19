# herb-card-control Spec Delta

## MODIFIED Requirements

### Requirement: HerbItemViewModelBase Abstract Class

系统 SHALL 提供 `HerbItemViewModelBase` 抽象基类，封装药材项的共享逻辑。

#### Scenario: 拼音码过滤
- **WHEN** `HerbName` 属性变更
- **THEN** 系统基于拼音码前缀匹配、名称包含匹配等规则过滤 `FilteredHerbs` 集合
- **AND** 结果按匹配分数从高到低排序，最多显示5个

#### Scenario: 药材选择自动填充
- **WHEN** `SelectedHerb` 属性被设置为非空值
- **THEN** 系统自动填充 `HerbId`、`HerbName`、`Unit` 属性
- **AND** `Unit` 值来自药材库定义，如"克"、"条"、"枚"等

#### Scenario: 空白行单位初始化
- **WHEN** 创建新的空白药材项（用于用户输入）
- **THEN** `Unit` 属性初始化为空字符串
- **AND** 不预设默认单位值如"g"
- **AND** 当用户选择药材时，单位从药材库自动加载

#### Scenario: 抽象价格属性
- **WHEN** 子类实现 `UnitPrice` 属性
- **THEN** 经验方返回固定值 0
- **AND** 处方返回药材库中的实际价格

## ADDED Requirements

### Requirement: Herb Unit Auto-Load

系统 SHALL 确保药材单位从药材库自动加载，而非使用硬编码默认值。

#### Scenario: 经验方添加空行
- **GIVEN** 用户正在编辑经验方
- **WHEN** 点击添加药材或系统自动添加空行
- **THEN** 新行的单位字段为空
- **AND** 用户选择药材后，单位自动填充为药材库定义的值

#### Scenario: 处方添加空行
- **GIVEN** 用户正在编辑处方
- **WHEN** 点击添加药材或系统自动添加空行
- **THEN** 新行的单位字段为空
- **AND** 用户选择药材后，单位自动填充为药材库定义的值

#### Scenario: 从现有数据加载
- **GIVEN** 编辑已保存的经验方或处方
- **WHEN** 加载药材列表
- **THEN** 单位使用数据库存储的值（可能是"克"、"条"等）
- **AND** 如果存储值为空，保持为空

#### Scenario: 经验方导入
- **GIVEN** 用户导入经验方数据
- **WHEN** 药材项没有单位信息
- **THEN** 系统不强制设置默认单位
- **AND** 保存时以空值或药材库单位为准
