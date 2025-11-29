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

