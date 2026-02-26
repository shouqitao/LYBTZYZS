# A4-03: Patient/Herb AuthorizationHandler 评估

## 日期
2026-02-26

## 背景
当前 Server 端有两个资源级 AuthorizationHandler:
- `FormulaAuthorizationHandler` -- 验方资源授权 (131行)
- `MedicalCaseAuthorizationHandler` -- 医案资源授权 (110行)

Patient 和 Herb 模块**缺少**独立的 AuthorizationHandler。

## 现状分析

### 已有授权机制

| 模块 | 授权方式 | 说明 |
|------|---------|------|
| Patient | Controller `[Authorize]` + Service 层角色检查 | 无资源级授权 |
| Herb | Controller `[Authorize]` + Service 层角色检查 | 无资源级授权 |
| Formula | `FormulaAuthorizationHandler` | 资源级: Doctor 只能操作自己创建的 |
| MedicalCase | `MedicalCaseAuthorizationHandler` | 资源级: 委托 PermissionService |

### Patient 模块授权需求分析

**当前行为**:
- 所有已认证用户 (Doctor/Admin/SuperAdmin) 都可以 CRUD 患者
- 无"患者归属"概念 -- 患者是共享资源
- 删除/禁用通过 Service 层权限检查 (Admin+)

**是否需要 ResourceAuthorizationHandler?**
- **结论: 不需要**
- 理由: Patient 无 `UserId`/`CreatedBy` 归属概念，所有医生共享患者池
- Controller 级 `[Authorize]` + Service 层角色检查已足够
- 如果未来需要"患者归属"（如多诊所隔离），再添加 Handler

### Herb 模块授权需求分析

**当前行为**:
- Admin/SuperAdmin 可以 CRUD 药材
- Doctor 只读 (Service 层过滤)
- 药材是共享参考数据，无"归属"概念

**是否需要 ResourceAuthorizationHandler?**
- **结论: 不需要**
- 理由: Herb 是参考数据，权限模型简单 (Admin 写 / Doctor 读)
- Controller 级 `[Authorize(Roles = "Admin,SuperAdmin")]` 已覆盖写操作
- 无资源级细粒度需求

## 结论

| 模块 | 需要 AuthorizationHandler? | 理由 |
|------|---------------------------|------|
| Patient | 否 | 共享资源，无归属概念，Controller + Service 层已足够 |
| Herb | 否 | 参考数据，权限模型简单，角色级授权已覆盖 |

## 未来考虑

如果以下场景出现，需重新评估:
1. **多诊所隔离**: Patient 需要按诊所归属过滤 -> 需要 Handler
2. **药材自定义**: 允许 Doctor 创建私有药材 -> 需要 Handler
3. **数据级权限**: 按科室/部门限制可见患者 -> 需要 Handler

当前阶段这些场景均不存在，维持现状。
