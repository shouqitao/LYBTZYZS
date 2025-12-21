# entity-conventions Deltas

## ADDED Requirements

### Requirement: REQ-ENT-001: 聚合根标识

所有聚合根实体 MUST 实现`IAggregateRoot`标记接口。

#### Scenario: 聚合根定义

```gherkin
Given 一个领域实体是聚合根
When 定义该实体类时
Then 必须实现IAggregateRoot接口
And 必须继承自BaseEntity
And 可以包含指向聚合内子实体的导航属性
```

### Requirement: REQ-ENT-002: 聚合内实体

聚合内实体 MUST 由聚合根管理其生命周期，不能独立存在。

#### Scenario: 聚合内实体定义

```gherkin
Given 一个实体属于某个聚合（如Consultation属于MedicalCase聚合）
When 定义该实体类时
Then 必须继承自BaseEntity
And 不实现IAggregateRoot接口
And 不能有指向聚合根的导航属性
And 构造函数应为internal（仅聚合根可创建）
```

### Requirement: REQ-ENT-003: 值对象

纯数据对象 SHALL 定义为值对象，具有不可变性。

#### Scenario: 值对象定义

```gherkin
Given 一个领域概念是纯数据（如Address、Money）
When 定义该类型时
Then 应定义为record类型或实现不可变类
And 所有属性应为只读（get-only或init-only）
And 重写Equals和GetHashCode基于值比较
```

### Requirement: REQ-ENT-004: 跨聚合引用约定

跨聚合引用 MUST 遵循ID引用原则。

#### Scenario: 跨聚合外键

```gherkin
Given 实体A需要引用另一个聚合的根实体B
When 定义A的属性时
Then 必须定义BId属性（Guid类型）
And 不能定义B类型的导航属性
And 可以定义冗余的展示字段（如BName）用于读优化
And 冗余字段通过事件或命令处理时同步
```

### Requirement: REQ-ENT-005: 集合封装

实体的集合属性 MUST 封装，防止外部直接修改。

#### Scenario: 集合属性定义

```gherkin
Given 实体包含子实体集合
When 定义集合属性时
Then 使用私有backing field存储（如_items）
And 公开IReadOnlyCollection<T>类型的只读属性
And 提供Add/Remove方法封装集合操作
And EF Core配置使用PropertyAccessMode.Field
```

### Requirement: REQ-ENT-006: Query Model分离

复杂查询 SHALL 使用专用Query Model，与领域实体分离。

#### Scenario: Query Model定义

```gherkin
Given 需要查询展示跨多个聚合的数据
When 定义查询模型时
Then 创建专用的XxxQueryModel类
And Query Model放在Queries/Models目录
And Query Model是简单POCO，无业务逻辑
And Query Model可展平多个聚合的数据
```

#### Scenario: Query Service实现

```gherkin
Given 需要执行复杂查询
When 实现Query Service时
Then 使用LINQ投影直接构建Query Model
And 使用子查询替代Include获取关联数据
And Query Service注入DbContext直接查询
And 返回Query Model而非领域实体
```
