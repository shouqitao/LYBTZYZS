# server-layer-architecture Deltas

## ADDED Requirements

### Requirement: REQ-SLA-DDD-001: DDD聚合设计原则

系统 MUST 遵循DDD聚合设计原则，确保领域模型边界清晰。

#### Scenario: 聚合内导航属性

```gherkin
Given 一个聚合根实体（如MedicalCase）
When 定义与聚合内实体的关系时
Then 只允许从聚合根到子实体的单向导航属性
And 子实体不应有指向聚合根的反向导航属性
```

#### Scenario: 跨聚合引用

```gherkin
Given 两个不同的聚合（如MedicalCase和Patient）
When MedicalCase需要引用Patient时
Then 只能使用PatientId（Guid类型）进行引用
And 不能定义Patient导航属性
And 可以添加冗余字段（如PatientName）用于读优化
```

#### Scenario: 跨聚合查询

```gherkin
Given 需要查询跨多个聚合的数据
When 执行查询操作时
Then 应使用Query Service和专用Query Model
And 不应使用Include链式加载跨聚合数据
And Query Model可以展平多个聚合的数据
```

### Requirement: REQ-SLA-DDD-002: 领域事件通信

跨聚合的状态协调 MUST 通过领域事件实现，不能直接操作其他聚合。

#### Scenario: 医案完成更新患者就诊记录

```gherkin
Given 医案被标记为完成状态
When 需要更新患者的最后就诊时间
Then MedicalCase聚合发布MedicalCaseCompletedEvent
And Patient聚合的事件处理器接收事件
And 事件处理器更新Patient.LastVisitTime
And 两个操作可以在不同事务中执行
```

## MODIFIED Requirements

### Requirement: REQ-SLA-003: 实体关系配置

实体关系 MUST 通过EF Core Fluent API配置，遵循聚合边界原则。

**Original**:
> 实体关系通过EF Core Fluent API配置，支持导航属性用于便捷查询。

**Updated**:
> 实体关系 MUST 通过EF Core Fluent API配置。聚合内实体可使用单向导航属性（从根到子），跨聚合关系只能使用外键ID引用，禁止跨聚合导航属性。

#### Scenario: EF Core配置跨聚合关系

```gherkin
Given 需要在EF Core中配置跨聚合外键关系
When 编写EntityTypeConfiguration时
Then 使用HasOne<T>()泛型方法（不指定导航属性）
And 使用WithMany()或WithOne()（不指定反向导航）
And 使用HasForeignKey()指定外键属性
```
