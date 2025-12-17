# Prescription Capability - Delta Changes

## MODIFIED Requirements

### Requirement: HerbCardControl显示

处方药材编辑控件(HerbCardControl)SHALL提供药材输入、剂量编辑和煎法选择功能。

**显示字段**:
- 药材名称输入框（支持拼音码过滤）
- 剂量输入框
- 煎法下拉选择器
- 删除按钮

**隐藏字段**:
- 单位（Unit）- 数据模型保留，UI不显示，打印时显示

#### Scenario: 药材选择自动填充单位
- **WHEN** 用户从下拉列表选择药材
- **THEN** 系统自动从药材库同步HerbId、HerbName、Unit和UnitPrice
- **AND** Unit字段在UI上不可见

#### Scenario: 煎法选择
- **WHEN** 用户点击煎法下拉
- **THEN** 显示可选煎法列表：默认、先煎、后下、烊化、冲服、包煎、另煎
- **AND** 选择后煎法值保存到DecocteMethod字段

#### Scenario: 默认煎法显示
- **WHEN** 药材煎法为默认
- **THEN** 煎法下拉显示为空或"默认"

#### Scenario: 完整药材名称回车跳转
- **WHEN** 用户输入完整正确的药材名称（如"当归"）
- **AND** 建议框未打开或无选中项
- **AND** 用户按下回车键
- **THEN** 焦点SHALL跳转到剂量输入框

#### Scenario: 无效药材名称校验
- **WHEN** 用户输入的药材名称在药材库中不存在（如只输入"当"）
- **AND** 用户按下回车键
- **THEN** 系统SHALL提示"药材不存在"
- **AND** 焦点保持在药材名称输入框

#### Scenario: 回车键焦点跳转顺序
- **WHEN** 用户在剂量输入框按下回车键
- **THEN** 焦点SHALL跳转到下一行药材名称输入框
- **AND** 焦点SHALL跳过煎法下拉选择器

## ADDED Requirements

### Requirement: 处方药材煎法数据模型

处方药材项(PrescriptionItem)SHALL支持煎法(DecocteMethod)字段存储。

**DecocteMethod枚举值**:
| 值 | 名称 | 显示文本 |
|----|------|----------|
| 0 | Default | 默认 |
| 1 | PreDecoct | 先煎 |
| 2 | PostAdd | 后下 |
| 3 | MeltIn | 烊化 |
| 4 | TakeWithWater | 冲服 |
| 5 | WrapDecoct | 包煎 |
| 6 | SeparateDecoct | 另煎 |

#### Scenario: 煎法数据持久化
- **WHEN** 用户保存处方
- **THEN** 每个药材项的DecocteMethod值保存到数据库
- **AND** 默认值为Default(0)

#### Scenario: 煎法数据加载
- **WHEN** 用户打开已保存的处方
- **THEN** 每个药材项的DecocteMethod正确加载并显示

### Requirement: 处方打印煎法显示

处方打印时SHALL正确显示药材的单位和煎法标注。

**显示规则**:
- 始终显示单位（如"10g"）
- 仅非默认煎法显示括号标注（如"(先煎)"）

#### Scenario: 默认煎法打印格式
- **WHEN** 药材煎法为默认
- **THEN** 打印格式为"药材名 剂量单位"
- **EXAMPLE** "当归10g"

#### Scenario: 特殊煎法打印格式
- **WHEN** 药材煎法为非默认值
- **THEN** 打印格式为"药材名 剂量单位(煎法)"
- **EXAMPLE** "附子10g(先煎)"
