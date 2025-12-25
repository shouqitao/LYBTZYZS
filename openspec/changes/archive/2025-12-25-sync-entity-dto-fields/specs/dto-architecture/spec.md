# dto-architecture (delta)

## ADDED Requirements

### Requirement: DTO-ARCH-005 字段类型同步

系统 **SHALL** 确保DTO字段类型与Entity完全一致，特别是可空类型。

#### Scenario: 可空decimal字段同步
- **Given** Entity定义了可空字段 `decimal? CostPrice`
- **When** 创建对应的DTO
- **Then** DTO必须使用相同的可空类型 `decimal? CostPrice`
- **And** 前端DependencyProperty必须使用 `typeof(decimal?)`
- **And** XAML绑定必须添加 `TargetNullValue=''`

#### Scenario: 必填字符串字段同步
- **Given** Entity定义了必填字段 `[Required] string Name`
- **When** 创建对应的DTO
- **Then** DTO必须保持相同的 `[Required]` 注解
- **And** FluentValidator必须包含 `NotEmpty()` 规则
- **And** XAML标签必须添加 `*` 标识

---

### Requirement: DTO-ARCH-006 ListDto字段选择标准

系统 **SHALL** 按照统一标准选择ListDto包含的字段。

#### Scenario: ListDto必需字段
- **Given** 需要创建新的ListDto
- **When** 选择包含的字段
- **Then** 必须包含主键Id
- **And** 必须包含主要名称字段
- **And** 必须包含状态字段
- **And** 可包含列表筛选/排序所需的关键业务字段

#### Scenario: ListDto排除字段
- **Given** 创建ListDto
- **When** 决定排除哪些字段
- **Then** 应排除大文本字段（Remark, Description, Effect, Usage）
- **And** 应排除非必要的审计字段
- **And** 应排除关联实体的完整详情

---

### Requirement: DTO-ARCH-007 DetailDto字段完整性

系统 **SHALL** 确保DetailDto包含Entity的全部业务字段。

#### Scenario: DetailDto完整字段
- **Given** 需要展示实体详情
- **When** 创建DetailDto
- **Then** 必须包含Entity的所有业务字段
- **And** 必须包含状态字段
- **And** 必须包含审计字段（CreatedAt, UpdatedAt等）

---

### Requirement: DTO-ARCH-008 标签文本一致性

系统 **SHALL** 使用Entity的DisplayName作为UI标签的单一来源。

#### Scenario: 必填字段标签
- **Given** Entity字段定义了 `[Required]` 和 `[DisplayName("药材名称")]`
- **When** 在XAML中显示该字段标签
- **Then** 标签文本必须为 "药材名称 *"（DisplayName + 星号）

#### Scenario: 可选字段标签
- **Given** Entity字段定义了 `[DisplayName("成本价")]` 但无 `[Required]`
- **When** 在XAML中显示该字段标签
- **Then** 标签文本必须为 "成本价"（不带星号）

---

### Requirement: DTO-ARCH-009 验证规则一致性

系统 **SHALL** 确保各层验证规则保持一致。

#### Scenario: 必填验证同步
- **Given** Entity字段有 `[Required]` 注解
- **When** 实现验证逻辑
- **Then** FluentValidator必须包含 `NotEmpty()` 规则
- **And** ViewModel验证必须检查非空

#### Scenario: 可空字段验证同步
- **Given** Entity字段为可空类型（无 `[Required]`）
- **When** 实现验证逻辑
- **Then** FluentValidator不应要求必填
- **And** ViewModel验证应使用 `if (value.HasValue && ...)` 模式
- **And** 不应在任何层要求该字段必须有值
