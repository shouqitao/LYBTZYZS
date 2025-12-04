# Design: refactor-authorization-system

## 架构概述

### 当前架构 (Before)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           请求处理流程                                    │
└─────────────────────────────────────────────────────────────────────────┘

HTTP Request
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ MedicalCasePermissionMiddleware                                          │
│ ├─ 检查是否为 /api/v1/medicalcases + PUT/PATCH/DELETE                    │
│ ├─ 从 JWT Claims 提取用户信息                                            │
│ └─ 放入 HttpContext.Items["MedicalCaseUserInfo"]                         │
└─────────────────────────────────────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ [Authorize] 属性                                                         │
│ └─ 验证用户已认证                                                        │
└─────────────────────────────────────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ MedicalCaseController                                                    │
│ ├─ GetOperator() 获取用户信息                                            │
│ └─ 调用 MedicalCasePermissionService.CanEdit()                          │
└─────────────────────────────────────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ MedicalCasePermissionService                                             │
│ └─ 实际权限逻辑 (Admin可编辑所有, Doctor仅编辑自己未完成的医案)            │
└─────────────────────────────────────────────────────────────────────────┘

问题:
1. Middleware 与 Service 重复提取用户信息
2. BaseService 已有备用方案直接从 Claims 读取
3. 未使用 ASP.NET Core 原生授权框架
```

### 目标架构 (After)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           请求处理流程                                    │
└─────────────────────────────────────────────────────────────────────────┘

HTTP Request
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ [Authorize] 属性                                                         │
│ └─ 验证用户已认证                                                        │
└─────────────────────────────────────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ MedicalCaseController                                                    │
│ ├─ 获取资源 (MedicalCase)                                                │
│ └─ await _authorizationService.AuthorizeAsync(User, resource, "Edit")   │
└─────────────────────────────────────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ MedicalCaseAuthorizationHandler                                          │
│ ├─ 实现 AuthorizationHandler<OperationAuthorizationRequirement, MC>     │
│ └─ 委托给 IMedicalCasePermissionService                                  │
└─────────────────────────────────────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ MedicalCasePermissionService (复用现有)                                   │
│ └─ CanEdit / CanDelete / GetPermissions                                  │
└─────────────────────────────────────────────────────────────────────────┘

优势:
1. 移除冗余中间件
2. 使用 ASP.NET Core 标准授权模式
3. 权限检查可声明式使用或命令式调用
4. Handler 可单元测试
```

## 组件设计

### 1. MedicalCaseOperations (常量类)

```csharp
namespace LYBT.WebAPI.Authorization
{
    /// <summary>
    /// 医案授权操作定义
    /// </summary>
    public static class MedicalCaseOperations
    {
        public static readonly OperationAuthorizationRequirement Create =
            new() { Name = nameof(Create) };
        public static readonly OperationAuthorizationRequirement Read =
            new() { Name = nameof(Read) };
        public static readonly OperationAuthorizationRequirement Edit =
            new() { Name = nameof(Edit) };
        public static readonly OperationAuthorizationRequirement Delete =
            new() { Name = nameof(Delete) };
    }
}
```

### 2. MedicalCaseAuthorizationHandler

```csharp
namespace LYBT.WebAPI.Authorization
{
    /// <summary>
    /// 医案资源授权处理器
    /// 实现 ASP.NET Core 资源级授权
    /// </summary>
    public class MedicalCaseAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, MedicalCase>
    {
        private readonly IMedicalCasePermissionService _permissionService;

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            MedicalCase resource)
        {
            // 从 ClaimsPrincipal 提取用户信息
            var (userId, role) = ExtractUserInfo(context.User);

            bool authorized = requirement.Name switch
            {
                nameof(MedicalCaseOperations.Create) =>
                    _permissionService.CanCreate(userId, role),
                nameof(MedicalCaseOperations.Edit) =>
                    _permissionService.CanEdit(userId, role, resource),
                nameof(MedicalCaseOperations.Delete) =>
                    _permissionService.CanDelete(userId, role, resource),
                nameof(MedicalCaseOperations.Read) => true, // 已认证即可读
                _ => false
            };

            if (authorized)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
```

### 3. Controller 使用方式

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateMedicalCase(Guid id, UpdateRequest request)
{
    var medicalCase = await _queryService.GetByIdAsync(id);
    if (medicalCase == null) return NotFound();

    // 资源级授权检查
    var authResult = await _authorizationService.AuthorizeAsync(
        User, medicalCase, MedicalCaseOperations.Edit);

    if (!authResult.Succeeded)
        return Forbid();

    // 执行更新...
}
```

### 4. DI 注册

```csharp
// AuthorizationExtensions.cs
services.AddSingleton<IAuthorizationHandler, MedicalCaseAuthorizationHandler>();
```

## 迁移策略

### Phase 1: 添加新组件 (不破坏现有)
1. 创建 `MedicalCaseOperations`
2. 创建 `MedicalCaseAuthorizationHandler`
3. 注册 DI
4. 添加单元测试

### Phase 2: 迁移 Controller
1. 修改 `MedicalCaseController` 使用 `IAuthorizationService`
2. 移除对 `HttpContext.Items["MedicalCaseUserInfo"]` 的依赖
3. 更新集成测试

### Phase 3: 清理
1. 删除 `MedicalCasePermissionMiddleware`
2. 删除 `MedicalCasePermissionMiddlewareTests`
3. 清理 `BaseService` 中的 `MedicalCaseUserInfo` 引用
4. 更新中间件注册配置

## 技术决策

### 选择 IAuthorizationService vs Policy-based

| 方式 | 优点 | 缺点 |
|------|------|------|
| `[Authorize(Policy="X")]` | 声明式，简洁 | 无法访问资源实例 |
| `IAuthorizationService.AuthorizeAsync()` | 可访问资源实例 | 命令式，需手动调用 |

**决策**: 使用 `IAuthorizationService.AuthorizeAsync()` 因为医案权限需要检查资源的 `DoctorId` 和 `CaseStatus`。

### 保留 MedicalCasePermissionService vs 合并到 Handler

**决策**: 保留 `MedicalCasePermissionService`
- Handler 委托给 Service，职责单一
- Service 可被其他组件复用（如返回权限详情给前端）
- 现有测试可继续使用

## 影响分析

### 受影响文件
1. `MedicalCaseController.cs` - 修改授权方式
2. `MedicalCasePermissionMiddleware.cs` - 删除
3. `BaseService.cs` - 清理 MedicalCaseUserInfo
4. `UnifiedMiddlewareConfiguration.cs` - 移除中间件注册
5. `AuthenticationServiceCollectionExtensions.cs` - 添加 Handler 注册

### 不受影响
- `MedicalCasePermissionService` - 保持不变
- 其他 Controller - 无变化
- 认证机制 - 无变化
