# authorization-infrastructure Specification

## Purpose
定义 LYBTZYZS 项目的资源级授权基础设施，基于 ASP.NET Core Authorization 框架实现细粒度权限控制。

## ADDED Requirements

### Requirement: AUTHZ-001 资源级授权处理器
系统 **SHALL** 使用 `IAuthorizationHandler` 实现资源级授权，支持对特定实体实例进行权限检查。

#### Scenario: 医案编辑权限检查
- **Given** 用户已认证且角色为 Doctor
- **And** 用户请求编辑医案 A
- **When** Controller 调用 `IAuthorizationService.AuthorizeAsync(User, medicalCase, MedicalCaseOperations.Edit)`
- **Then** `MedicalCaseAuthorizationHandler` 被调用
- **And** Handler 检查用户是否为医案创建者
- **And** Handler 检查医案状态是否为 Draft 或 Active
- **And** 返回授权结果

#### Scenario: 管理员绕过权限限制
- **Given** 用户已认证且角色为 Admin 或 SuperAdmin
- **When** 用户请求编辑任意医案
- **Then** Handler 直接返回授权成功
- **And** 不检查医案创建者和状态

#### Scenario: 未授权返回 403
- **Given** 用户无权编辑指定医案
- **When** 授权检查失败
- **Then** Controller 返回 HTTP 403 Forbidden
- **And** 响应体符合 RFC 7807 Problem Details 格式
- **And** 包含 correlationId 用于追踪

---

### Requirement: AUTHZ-002 授权操作定义
系统 **SHALL** 使用 `OperationAuthorizationRequirement` 定义标准化的授权操作。

#### Scenario: 定义医案操作常量
- **Given** 需要对医案进行 CRUD 权限检查
- **When** 定义 `MedicalCaseOperations` 静态类
- **Then** 包含 Create、Read、Edit、Delete 四个操作定义
- **And** 每个操作使用 `OperationAuthorizationRequirement` 类型

---

### Requirement: AUTHZ-003 Handler 与 Service 分离
系统 **SHALL** 将授权处理器（Handler）与权限业务逻辑（Service）分离，Handler 委托给 Service 执行实际权限判断。

#### Scenario: Handler 委托给 PermissionService
- **Given** `MedicalCaseAuthorizationHandler` 收到授权请求
- **When** Handler 需要判断用户是否有权编辑医案
- **Then** Handler 调用 `IMedicalCasePermissionService.CanEdit(userId, role, medicalCase)`
- **And** Handler 根据 Service 返回值决定是否 `context.Succeed(requirement)`

#### Scenario: Service 可独立使用
- **Given** 前端需要获取用户对医案的权限详情
- **When** Controller 调用 `IMedicalCasePermissionService.GetPermissions()`
- **Then** Service 返回 `MedicalCasePermissionDto` 包含 CanEdit、CanDelete、DenialReason
- **And** 前端可据此控制 UI 按钮状态

---

### Requirement: AUTHZ-004 移除冗余中间件
系统 **SHALL NOT** 使用自定义中间件进行授权预处理，所有授权逻辑统一通过 ASP.NET Core Authorization 框架处理。

#### Scenario: 删除 MedicalCasePermissionMiddleware
- **Given** 系统已迁移至 IAuthorizationHandler 模式
- **When** 完成迁移验证
- **Then** 删除 `MedicalCasePermissionMiddleware` 类
- **And** 删除中间件注册配置
- **And** 删除相关单元测试

#### Scenario: 清理 HttpContext.Items 依赖
- **Given** BaseService 之前从 `HttpContext.Items["MedicalCaseUserInfo"]` 获取用户信息
- **When** 中间件移除后
- **Then** BaseService 直接从 `ClaimsPrincipal` 提取用户信息
- **And** 移除 `MedicalCaseUserInfo` 类定义

---

### Requirement: AUTHZ-005 用户信息提取标准化
系统 **SHALL** 从 JWT Claims 中以标准方式提取用户身份信息。

#### Scenario: 从 Claims 提取用户ID
- **Given** 用户携带有效 JWT Token 访问 API
- **When** 授权 Handler 需要用户ID
- **Then** 从 `ClaimTypes.NameIdentifier` 或 `sub` claim 提取
- **And** 解析为 `Guid` 类型

#### Scenario: 从 Claims 提取用户角色
- **Given** 用户携带有效 JWT Token 访问 API
- **When** 授权 Handler 需要用户角色
- **Then** 从 `ClaimTypes.Role` 或 `role` claim 提取
- **And** 解析为 `UserRole` 枚举
- **And** 处理遗留命名（SysAdmin → SuperAdmin）

---

### Requirement: AUTHZ-006 可扩展授权模式
系统 **SHALL** 提供可扩展的授权模式，其他模块可按相同模式实现资源级授权。

#### Scenario: 新模块采用相同授权模式
- **Given** 需要为 Prescription 模块添加资源级授权
- **When** 参考 MedicalCase 授权实现
- **Then** 创建 `PrescriptionOperations` 操作定义
- **And** 创建 `PrescriptionAuthorizationHandler`
- **And** 复用现有 DI 注册模式

---

### Requirement: AUTHZ-007 授权日志记录
系统 **SHALL** 记录授权决策日志，便于安全审计和问题排查。

#### Scenario: 记录授权成功
- **Given** 用户通过资源级授权检查
- **When** Handler 调用 `context.Succeed(requirement)`
- **Then** 记录 Debug 级别日志
- **And** 包含用户ID、资源ID、操作类型

#### Scenario: 记录授权失败
- **Given** 用户未通过资源级授权检查
- **When** Handler 不调用 `context.Succeed()`
- **Then** 记录 Warning 级别日志
- **And** 包含用户ID、资源ID、操作类型、拒绝原因
