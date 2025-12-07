# Technical Design: optimize-api-permissions

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    API Layer (WebAPI)                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │ [Authorize] │  │   Policy    │  │ IAuthorizationService│ │
│  │  (基础认证)  │  │ (角色策略)  │  │   (资源级授权)        │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
│         │                │                    │             │
│         ▼                ▼                    ▼             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │                  Controller Actions                   │  │
│  │   Users: AdminOnly                                    │  │
│  │   Patients/Herbs: DoctorOrAdmin                      │  │
│  │   Formulas: DoctorOrAdmin + Resource Filter          │  │
│  │   MedicalCase: DoctorOrAdmin + Resource Auth         │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Service Layer                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         Query Filtering by Role                      │   │
│  │   FormulaService: Filter by UserId/CreatedBy        │   │
│  │   MedicalCaseQueryService: Filter by DoctorId       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Detailed Design

### 1. Authorization Policies Registration

**文件:** `src/Server/WebAPI/Program.cs`

```csharp
// 添加授权策略
builder.Services.AddAuthorization(options =>
{
    // 管理员专用策略
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("SuperAdmin", "Admin"));
    
    // 医生或管理员策略
    options.AddPolicy("DoctorOrAdmin", policy => 
        policy.RequireRole("SuperAdmin", "Admin", "Doctor"));
    
    // 预留: 前台扩展策略
    // options.AddPolicy("FrontDeskOrAbove", policy => 
    //     policy.RequireRole("SuperAdmin", "Admin", "Doctor", "FrontDesk"));
});
```

### 2. Controller Authorization Attributes

**UsersController:**
```csharp
[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseApiController
```

**PatientsController & HerbsController:**
```csharp
[Authorize(Policy = "DoctorOrAdmin")]
[ApiController]
[Route("api/[controller]")]
public class PatientsController : BaseApiController
```

**FormulasController:**
```csharp
[Authorize(Policy = "DoctorOrAdmin")]
[ApiController]
[Route("api/[controller]")]
public class FormulasController : BaseApiController
{
    // GetPaged需要Service层过滤
    // Create/Update/Delete需要资源授权检查
}
```

**MedicalCaseController:**
```csharp
[Authorize(Policy = "DoctorOrAdmin")]
[ApiController]
[Route("api/[controller]")]
public class MedicalCaseController : BaseApiController
{
    // Create: 仅Doctor可用
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult> Create(...)
    
    // 其他操作: 资源级授权(已有逻辑增强)
}
```

### 3. Formula Resource Authorization

**新建Handler:** `src/Server/Infrastructure/Authorization/FormulaAuthorizationHandler.cs`

```csharp
public static class FormulaOperations
{
    public static OperationAuthorizationRequirement Read = new() { Name = nameof(Read) };
    public static OperationAuthorizationRequirement Update = new() { Name = nameof(Update) };
    public static OperationAuthorizationRequirement Delete = new() { Name = nameof(Delete) };
}

public class FormulaAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Formula>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Formula resource)
    {
        var userId = context.User.GetUserId();
        var isAdmin = context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin");
        
        // Admin可以操作所有Formula
        if (isAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        // Doctor只能操作自己的Formula或查看Admin创建的
        if (requirement.Name == FormulaOperations.Read.Name)
        {
            // 可以读自己的或Admin创建的
            if (resource.UserId == userId || IsCreatedByAdmin(resource.CreatedBy))
            {
                context.Succeed(requirement);
            }
        }
        else // Update/Delete
        {
            // 只能修改/删除自己的
            if (resource.UserId == userId)
            {
                context.Succeed(requirement);
            }
        }
        
        return Task.CompletedTask;
    }
}
```

### 4. Service Layer Query Filtering

**FormulaService.GetPagedAsync增强:**

```csharp
public async Task<PagedResult<FormulaDto>> GetPagedAsync(
    FormulaQueryParams queryParams, 
    Guid? currentUserId,
    bool isAdmin)
{
    var query = _context.Formulas.AsQueryable();
    
    // 角色过滤: Doctor只能看自己的和Admin创建的
    if (!isAdmin && currentUserId.HasValue)
    {
        var adminUserIds = await GetAdminUserIdsAsync();
        query = query.Where(f => 
            f.UserId == currentUserId.Value || 
            adminUserIds.Contains(f.CreatedBy ?? Guid.Empty));
    }
    
    // 其余查询逻辑...
}
```

### 5. MedicalCase Authorization Enhancement

**现有逻辑增强:**

```csharp
// MedicalCaseAuthorizationHandler增强
protected override Task HandleRequirementAsync(...)
{
    var isAdmin = context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin");
    var userId = context.User.GetUserId();
    
    if (requirement.Name == MedicalCaseOperations.Create.Name)
    {
        // Admin不能创建新医案
        if (isAdmin)
        {
            context.Fail();
            return Task.CompletedTask;
        }
        // Doctor可以创建
        if (context.User.IsInRole("Doctor"))
        {
            context.Succeed(requirement);
        }
    }
    else if (requirement.Name == MedicalCaseOperations.Edit.Name)
    {
        // Admin可以编辑所有
        if (isAdmin)
        {
            context.Succeed(requirement);
        }
        // Doctor只能在当天编辑自己的
        else if (resource.DoctorId == userId && resource.CreatedAt.Date == DateTime.Today)
        {
            context.Succeed(requirement);
        }
    }
    else if (requirement.Name == MedicalCaseOperations.Read.Name)
    {
        // Admin可以查看所有
        if (isAdmin)
        {
            context.Succeed(requirement);
        }
        // Doctor只能查看自己的
        else if (resource.DoctorId == userId)
        {
            context.Succeed(requirement);
        }
    }
    
    return Task.CompletedTask;
}
```

## File Changes Summary

| 文件 | 变更类型 | 说明 |
|------|---------|------|
| `WebAPI/Program.cs` | 修改 | 添加Authorization Policies |
| `WebAPI/Controllers/UsersController.cs` | 修改 | 添加AdminOnly策略 |
| `WebAPI/Controllers/PatientsController.cs` | 修改 | 添加DoctorOrAdmin策略 |
| `WebAPI/Controllers/HerbsController.cs` | 修改 | 添加DoctorOrAdmin策略 |
| `WebAPI/Controllers/FormulasController.cs` | 修改 | 添加策略+资源授权调用 |
| `WebAPI/Controllers/MedicalCaseController.cs` | 修改 | Create添加Doctor限制 |
| `Infrastructure/Authorization/FormulaAuthorizationHandler.cs` | 新建 | Formula资源授权 |
| `Infrastructure/Authorization/FormulaOperations.cs` | 新建 | Formula操作定义 |
| `Modules/Formula/Services/FormulaService.cs` | 修改 | 添加角色过滤 |
| `Infrastructure/Authorization/MedicalCaseAuthorizationHandler.cs` | 修改 | 增强Admin/Doctor逻辑 |

## Testing Strategy

1. **单元测试:** Authorization Handler逻辑测试
2. **集成测试:** API端点权限验证
3. **手工测试:** 
   - Admin账号: 验证Users访问正常,MedicalCase不能创建
   - Doctor账号: 验证Users访问被拒,Formula只能看自己的
