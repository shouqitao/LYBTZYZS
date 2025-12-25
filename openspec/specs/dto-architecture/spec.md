# dto-architecture Specification

## Purpose
TBD - created by archiving change consolidate-medicalcase-dtos. Update Purpose after archive.
## Requirements
### Requirement: DTO-ARCH-001 DTO统一定义位置

系统 **SHALL** 将共享DTO定义统一放置在Shared层(`LYBT.Shared.Models.Contracts`)。

#### Scenario: MedicalCase模块共享DTO位置
- **Given** 开发者需要为MedicalCase模块添加跨层共享的DTO
- **When** 创建DTO类
- **Then** DTO必须定义在`LYBT.Shared.Models.Contracts.MedicalCase`命名空间
- **And** 模块专用DTO可保留在`LYBT.Module.MedicalCase.Dtos`命名空间

---

### Requirement: DTO-ARCH-002 禁止重复DTO定义

系统 **SHALL** 确保每个DTO只有唯一定义，不允许在多个位置定义相同功能的DTO。

#### Scenario: 发现重复DTO
- **Given** 代码审查发现相同功能的DTO存在于多个位置
- **When** 进行代码重构
- **Then** 保留Shared层版本
- **And** 删除Server模块层版本
- **And** 更新所有引用到Shared层版本

---

### Requirement: DTO-ARCH-003 DTO命名规范

系统 **SHALL** 遵循统一的DTO命名规范。

#### Scenario: 创建数据传输对象
- **Given** 需要创建新的DTO
- **When** 命名DTO类
- **Then** 使用以下后缀规范：
  - 通用传输: `*Dto` (如`MedicalCaseDto`)
  - 创建请求: `*CreateDto` (如`PrescriptionCreateDto`)
  - 输入对象: `*InputDto` (如`ConsultationInputDto`)
  - 详情响应: `*DetailDto` (如`MedicalCaseDetailDto`)
  - 操作请求: `*Request` (如`UpdateMedicalCaseRequest`)

---

### Requirement: DTO-ARCH-004 模块专用DTO规范

系统 **SHALL** 允许模块保留仅供内部使用的专用DTO。

#### Scenario: 模块专用Response类型
- **Given** 模块需要特定的Response或简化DTO
- **When** 该DTO仅在模块内部使用
- **Then** 可保留在模块Dtos目录
- **And** 在注释中标注"模块专用"

### Requirement: Batch Operation DTO Naming Convention
批量操作相关DTO SHALL遵循以下命名规范：

| 类型 | 命名格式 | 用途 |
|------|----------|------|
| 批量输入 | `{Entity}Batch{Operation}InputDto` | 批量操作请求参数 |
| 批量结果 | `{Entity}Batch{Operation}ResultDto` | 批量操作响应结果 |
| 导入行项 | `{Entity}ImportItemDto` | 批量导入中的单行数据 |
| 导出行项 | `{Entity}ExportItemDto` | 批量导出中的单行数据 |
| 通用批量ID | `BatchIdsDto` | 通用的ID列表输入 |
| 通用批量结果 | `BatchOperationResultDto` | 通用的批量操作结果 |

#### Scenario: Batch import input naming
- **WHEN** 定义批量导入请求DTO
- **THEN** 命名格式SHALL为 `{Entity}BatchImportInputDto`（如 `PatientBatchImportInputDto`）

#### Scenario: Import item naming
- **WHEN** 定义批量导入中的单行数据DTO
- **THEN** 命名格式SHALL为 `{Entity}ImportItemDto`（如 `PatientImportItemDto`）

#### Scenario: Export item naming
- **WHEN** 定义批量导出中的单行数据DTO
- **THEN** 命名格式SHALL为 `{Entity}ExportItemDto`（如 `PatientExportItemDto`）

---

### Requirement: Batch Delete/Enable/Disable API
系统SHALL提供服务端批量删除/启用/禁用API端点，避免客户端N+1调用模式。

#### Scenario: Single API call for batch delete
- **WHEN** 用户选择多个实体执行批量删除
- **THEN** Desktop客户端SHALL发送单次API请求到 `POST /api/v1/{entity}/batch-delete`
- **AND** 请求体SHALL使用 `BatchIdsDto` 包含所有待删除ID

#### Scenario: Batch operation result
- **WHEN** 服务端完成批量操作
- **THEN** 响应SHALL使用 `BatchOperationResultDto` 返回操作结果
- **AND** 结果SHALL包含 TotalCount, SuccessCount, FailureCount, FailedIds

---

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

