# OpenSpec Proposal: optimize-api-permissions

**Status:** proposed
**Created:** 2025-12-07
**Author:** Claude Code

## Summary

优化API端点权限控制,实现基于角色的细粒度访问控制。当前所有Controller仅使用`[Authorize]`基础认证,缺乏角色区分和资源级权限控制。

## Problem Statement

**现状问题:**
1. 所有API端点仅验证用户已登录,不区分角色权限
2. 敏感操作(如用户管理)未限制为管理员专用
3. 资源级权限(如医生只能查看自己的医案)未在API层强制执行
4. 缺乏统一的权限授权框架

**业务需求:**
- 用户管理: 仅管理员可访问
- 患者/药材: 管理员和医生可访问(预留前台扩展)
- 经验方: 管理员全部可见,医生仅见自己的和管理员创建的
- 医案: 管理员可查看全部/修改但不能新建,医生只能操作自己的且受时间限制

## Proposed Solution

### 1. 权限模型设计

**角色定义(已有):**
- `SuperAdmin (100)` - 超级管理员
- `Admin (10)` - 管理员
- `Doctor (1)` - 医生
- `FrontDesk (预留)` - 前台(未来扩展)

**权限策略:**

| 模块 | Admin | Doctor | 备注 |
|------|-------|--------|------|
| Users | CRUD | - | 管理员专属 |
| Patients | CRUD | CRUD | 通用访问 |
| Herbs | CRUD | CRUD | 通用访问 |
| Formulas | CRUD(全部) | CRUD(自己的+Admin的) | 资源级过滤 |
| MedicalCase | RU(全部) | CRUD(自己的,时间限制) | 资源级+时间限制 |

### 2. 技术实现方案

**A. 角色级授权 - 使用Policy-Based Authorization:**
```csharp
// 在Program.cs注册策略
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "SuperAdmin"));
    options.AddPolicy("DoctorOrAdmin", policy => policy.RequireRole("Admin", "SuperAdmin", "Doctor"));
});

// 在Controller使用
[Authorize(Policy = "AdminOnly")]
public class UsersController : BaseApiController
```

**B. 资源级授权 - 复用现有IAuthorizationService模式:**
```csharp
// MedicalCase已有此模式,扩展到Formulas
var authResult = await _authorizationService.AuthorizeAsync(User, formula, FormulaOperations.Read);
```

**C. 查询过滤 - Service层实现:**
```csharp
// FormulaService.GetPagedAsync增加角色过滤
if (!isAdmin)
{
    query = query.Where(f => f.UserId == currentUserId || f.CreatedBy == adminId);
}
```

### 3. 实现范围

**Phase 1: 角色级授权**
- UsersController: 添加`[Authorize(Policy = "AdminOnly")]`
- PatientsController/HerbsController: 添加`[Authorize(Policy = "DoctorOrAdmin")]`

**Phase 2: 资源级授权 - Formulas**
- 创建`FormulaAuthorizationHandler`
- Service层添加角色过滤逻辑

**Phase 3: 资源级授权 - MedicalCase**
- 增强现有授权逻辑
- Admin禁止Create操作
- Doctor时间限制(当天)强化

## Impact Assessment

**影响范围:**
- Server/WebAPI: 5个Controller需修改
- Server/Modules: FormulaService, MedicalCaseService需增强
- Infrastructure: 添加Authorization Handlers
- Shared/Models: 可能需要扩展DTO添加权限标识

**风险评估:**
- 低风险: 使用ASP.NET Core标准授权框架
- 向后兼容: 现有Admin用户不受影响
- 测试覆盖: 需要授权相关单元测试

**不做的事:**
- 不修改Client端代码(Client应已根据角色显示/隐藏功能)
- 不修改数据库Schema
- 不引入新的第三方库

## Alternatives Considered

1. **自定义中间件** - 不采用,ASP.NET Core内置授权更标准
2. **数据库存储权限** - 不采用,当前角色固定,无需动态权限配置
3. **Attribute级细粒度控制** - 部分采用,结合Policy和Resource授权

## Success Criteria

- [ ] 非Admin用户无法访问Users API
- [ ] Doctor只能查看自己的经验方和Admin创建的经验方
- [ ] Admin不能创建新医案
- [ ] Doctor只能在当天编辑自己的医案
- [ ] 所有权限测试通过
- [ ] 现有功能回归测试通过
