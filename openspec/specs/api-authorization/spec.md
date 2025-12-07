# api-authorization Specification

## Purpose
TBD - created by archiving change optimize-api-permissions. Update Purpose after archive.
## Requirements
### Requirement: Authorization Policy Registration
系统 SHALL 在启动时注册标准化授权策略,包括`AdminOnly`和`DoctorOrAdmin`两种策略。

#### Scenario: AdminOnly Policy
- **WHEN** 应用启动时
- **THEN** 系统注册`AdminOnly`策略,仅允许SuperAdmin和Admin角色访问

#### Scenario: DoctorOrAdmin Policy
- **WHEN** 应用启动时
- **THEN** 系统注册`DoctorOrAdmin`策略,允许SuperAdmin、Admin和Doctor角色访问

### Requirement: Users Module Admin-Only Access
用户管理模块 SHALL 仅限管理员访问,Doctor角色无权访问。

#### Scenario: Admin accesses Users API
- **WHEN** Admin角色用户访问 `/api/users` 端点
- **THEN** 请求成功处理,返回200状态码

#### Scenario: Doctor accesses Users API
- **WHEN** Doctor角色用户访问 `/api/users` 端点
- **THEN** 请求被拒绝,返回403 Forbidden

### Requirement: Formula Resource-Level Authorization
经验方模块 SHALL 实现资源级权限控制,Admin可访问全部,Doctor仅可访问自己的和Admin创建的。

#### Scenario: Admin lists all formulas
- **WHEN** Admin角色用户请求经验方列表
- **THEN** 返回系统中所有经验方

#### Scenario: Doctor lists formulas
- **WHEN** Doctor角色用户请求经验方列表
- **THEN** 仅返回该医生创建的经验方和管理员创建的经验方

#### Scenario: Doctor updates own formula
- **WHEN** Doctor角色用户更新自己创建的经验方
- **THEN** 更新成功

#### Scenario: Doctor updates others formula
- **WHEN** Doctor角色用户尝试更新其他医生创建的经验方
- **THEN** 请求被拒绝,返回403 Forbidden

### Requirement: MedicalCase Role-Based Create Permission
医案创建权限 SHALL 限制管理员,仅Doctor角色可创建新医案。

#### Scenario: Doctor creates medical case
- **WHEN** Doctor角色用户创建新医案
- **THEN** 医案创建成功

#### Scenario: Admin creates medical case
- **WHEN** Admin角色用户尝试创建新医案
- **THEN** 请求被拒绝,返回403 Forbidden

### Requirement: MedicalCase Time-Based Edit Permission
医案编辑权限 SHALL 对Doctor角色施加时间限制,仅可编辑当天创建的自己的医案。

#### Scenario: Doctor edits own case same day
- **WHEN** Doctor角色用户在创建当天编辑自己的医案
- **THEN** 编辑成功

#### Scenario: Doctor edits own case next day
- **WHEN** Doctor角色用户在创建次日尝试编辑自己的医案
- **THEN** 请求被拒绝,返回403 Forbidden

#### Scenario: Admin edits any case anytime
- **WHEN** Admin角色用户编辑任意医案
- **THEN** 编辑成功,不受时间限制

### Requirement: MedicalCase Read Permission
医案查看权限 SHALL 按角色区分,Admin可查看全部,Doctor仅可查看自己负责的医案。

#### Scenario: Admin reads all cases
- **WHEN** Admin角色用户请求医案列表
- **THEN** 返回系统中所有医案

#### Scenario: Doctor reads cases
- **WHEN** Doctor角色用户请求医案列表
- **THEN** 仅返回该医生负责的医案

