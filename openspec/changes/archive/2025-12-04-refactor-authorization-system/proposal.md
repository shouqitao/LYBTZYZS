# Proposal: refactor-authorization-system

## Status
Approved

## Why

当前授权体系存在以下问题：

1. **冗余中间件**: `MedicalCasePermissionMiddleware` 仅提取用户信息放入 `HttpContext.Items`，但 `BaseService` 已有备用方案直接从 Claims 读取
2. **授权逻辑分散**:
   - 认证由 `[Authorize]` 属性处理
   - 角色检查由 `MedicalCasePermissionService` 在 Service 层处理
   - 中间件做了重复工作
3. **未使用 ASP.NET Core 授权框架**: 项目定义了策略（AdminOnly, DoctorOrAdmin），但未使用 `IAuthorizationHandler` 实现资源级授权
4. **不符合 ASP.NET Core 最佳实践**: 资源级授权应使用 Policy + IAuthorizationHandler 模式

## What Changes

### 1. 移除冗余组件
- 删除 `MedicalCasePermissionMiddleware`
- 删除 `MedicalCasePermissionMiddlewareTests`
- 移除中间件注册

### 2. 引入标准资源授权模式
- 创建 `MedicalCaseAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, MedicalCase>`
- 复用现有 `MedicalCasePermissionService` 的权限逻辑
- 添加授权策略: `MedicalCaseEdit`, `MedicalCaseDelete`

### 3. 优化 Controller 授权
- Controller 使用 `IAuthorizationService.AuthorizeAsync()` 进行资源级授权
- 移除对 `HttpContext.Items["MedicalCaseUserInfo"]` 的依赖

### 4. 清理 BaseService
- 移除 `MedicalCaseUserInfo` 相关代码
- 简化 `ExtractUserInfoAsync` 方法

## Scope

### In Scope
- MedicalCase 模块授权重构
- 建立可复用的授权模式供其他模块使用

### Out of Scope
- 其他模块的授权改造（后续可按需采用相同模式）
- 认证机制改动（JWT 认证保持不变）

## Dependencies
- 无外部依赖
- 基于现有 `MedicalCasePermissionService` 实现

## Risks
- **低风险**: 授权逻辑已在 Service 层实现，重构仅改变调用位置
- **测试覆盖**: 现有权限测试可迁移至新 Handler 测试

## Success Criteria
1. `MedicalCasePermissionMiddleware` 完全移除
2. 医案编辑/删除权限通过 `[Authorize(Policy = "MedicalCaseEdit")]` 或 `IAuthorizationService` 验证
3. 所有现有测试通过
4. 新增 `MedicalCaseAuthorizationHandler` 单元测试
