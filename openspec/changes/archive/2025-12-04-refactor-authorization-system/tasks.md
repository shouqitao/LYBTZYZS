# Tasks: refactor-authorization-system

## Phase 1: 添加授权基础设施 (新增组件)

### Task 1.1: 创建授权操作定义
- [x] 创建 `src/Server/Services/LYBT.WebAPI/Authorization/` 目录
- [x] 创建 `MedicalCaseOperations.cs` 定义 Create/Read/Edit/Delete 操作常量
- [x] 使用 `OperationAuthorizationRequirement` 标准类型

### Task 1.2: 创建授权处理器
- [x] 创建 `MedicalCaseAuthorizationHandler.cs`
- [x] 实现 `AuthorizationHandler<OperationAuthorizationRequirement, MedicalCase>`
- [x] 注入 `IMedicalCasePermissionService` 复用现有权限逻辑
- [x] 从 `ClaimsPrincipal` 提取 userId 和 role

### Task 1.3: 注册授权服务
- [x] 在 `AuthenticationServiceCollectionExtensions.cs` 添加 Handler 注册
- [x] `services.AddSingleton<IAuthorizationHandler, MedicalCaseAuthorizationHandler>()`

### Task 1.4: 授权处理器单元测试
- [x] 创建 `tests/UnitTests/Server/WebAPI/Authorization/MedicalCaseAuthorizationHandlerTests.cs`
- [x] 测试场景: Admin可编辑所有医案 (2 tests)
- [x] 测试场景: Doctor可编辑自己的Draft/Active医案 (2 tests)
- [x] 测试场景: Doctor不能编辑他人医案 (1 test)
- [x] 测试场景: Doctor不能编辑Completed医案 (1 test)
- [x] 测试场景: 未认证用户无权限 (1 test)
- [x] Claims提取测试 (6 tests)
- **结果: 13 tests passed**

## Phase 2: 迁移 Controller 授权

### Task 2.1: 修改 MedicalCaseController
- [x] 注入 `IAuthorizationService`
- [x] 在 Update 方法使用 `AuthorizeAsync(User, resource, MedicalCaseOperations.Edit)`
- [x] 在 Delete 方法使用 `AuthorizeAsync(User, resource, MedicalCaseOperations.Delete)`
- [x] 授权失败返回 `Forbid()` 或带详情的 403 响应

### Task 2.2: 更新集成测试
- [x] 验证 Admin 可编辑任意医案
- [x] 验证 Doctor 权限边界
- [x] 验证 403 响应格式符合 RFC 7807
- [x] 修正 `UpdateConsultation_WhenStatusNotActive` 测试期望从 400 改为 403
- **结果: 30 tests passed**

## Phase 3: 清理冗余组件

### Task 3.1: 删除 MedicalCasePermissionMiddleware
- [x] 删除 `src/Server/Services/LYBT.WebAPI/Middleware/MedicalCasePermissionMiddleware.cs`
- [x] 删除 `tests/UnitTests/Server/WebAPI/Middleware/MedicalCasePermissionMiddlewareTests.cs`
- [x] 从 `UnifiedMiddlewareConfiguration.cs` 移除中间件注册

### Task 3.2: 清理 BaseService
- [x] 移除 `ExtractUserInfoAsync` 中对 `HttpContext.Items["MedicalCaseUserInfo"]` 的检查
- [x] 删除 `MedicalCaseUserInfo` 类定义
- [x] 简化为直接从 Claims 提取

### Task 3.3: 清理 MedicalCaseUserInfo 引用
- [x] 搜索并移除所有 `MedicalCaseUserInfo` 引用
- [x] 更新 BaseServiceTests.cs 移除中间件相关测试
- [x] 确保无编译错误

## Phase 4: 验证与文档

### Task 4.1: 全量测试验证
- [x] 运行 `dotnet test` 确保所有测试通过
  - WebAPI Tests: 50 passed
  - Infrastructure Tests: 183 passed
  - MedicalCase Integration Tests: 48 passed
- [x] 验证医案 CRUD 功能正常
- [x] 验证权限边界正确

### Task 4.2: 更新 Spec
- [x] authorization-infrastructure spec 已存在
- [x] 记录资源级授权模式
- [x] 为其他模块提供参考

---

## 任务依赖关系

```
Phase 1 (并行):
├─ Task 1.1 ─────────┐
├─ Task 1.2 ─────────┼──▶ Task 1.3 ──▶ Task 1.4
└────────────────────┘

Phase 2 (依赖 Phase 1):
├─ Task 2.1 ──▶ Task 2.2

Phase 3 (依赖 Phase 2):
├─ Task 3.1 ─────────┐
├─ Task 3.2 ─────────┼──▶ Task 3.3
└────────────────────┘

Phase 4 (依赖 Phase 3):
├─ Task 4.1 ──▶ Task 4.2
```

## 预估工作量

| Phase | 任务数 | 复杂度 | 状态 |
|-------|--------|--------|------|
| Phase 1 | 4 | 中等 | **完成** |
| Phase 2 | 2 | 低 | **完成** |
| Phase 3 | 3 | 低 | **完成** |
| Phase 4 | 2 | 低 | **完成** |
| **总计** | **11** | - | **全部完成** |

## 完成日期
- 完成时间: 2025-12-04
- 实现者: Claude Code
