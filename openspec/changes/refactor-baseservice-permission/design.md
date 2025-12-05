# Design: refactor-baseservice-permission

## Architecture Analysis

### 发现：权限验证已独立实现

```
┌─────────────────────────────────────────────────────────────────┐
│                   实际权限验证架构                               │
└─────────────────────────────────────────────────────────────────┘

MedicalCaseCommandService
    │
    ├──▶ MedicalCaseRules.CanEdit(medicalCase, userId, isAdmin)
    │         │
    │         └── 检查: 管理员 OR (本人创建 AND 状态允许)
    │
    └──▶ MedicalCasePermissionService.CanEdit(userId, role, medicalCase)
              │
              └── 更详细的权限检查，返回PermissionDto

┌─────────────────────────────────────────────────────────────────┐
│                   BaseService中的死代码                          │
└─────────────────────────────────────────────────────────────────┘

BaseService<T>
    │
    ├── ValidateEditPermission<TEntity>()  ← 从未被调用
    │       │
    │       ├── GetEntityId<TEntity>()     ← 抛NotImplementedException
    │       ├── GetCreatedUserId<TEntity>()← 抛NotImplementedException
    │       └── GetCreatedDate<TEntity>()  ← 抛NotImplementedException
    │
    └── MedicalCase服务重写了这些方法 ← 但从未使用
```

### 决策：删除而非重构

| 方案 | 工作量 | 风险 | 价值 |
|------|--------|------|------|
| A: 创建新权限服务 | 高 | 中 | 低（已有实现） |
| B: 重构为组合模式 | 中 | 中 | 低（已有实现） |
| **C: 删除死代码** | **低** | **低** | **高** |

选择方案C：直接删除死代码。

### 为什么不需要CON-001向后兼容？

1. `ValidateEditPermission(参数版本)` - 虽然是有效代码，但也从未被调用
2. 保留它只会造成维护负担和误导
3. 如果未来需要，可以重新设计更好的API

### 删除清单

```csharp
// BaseService<T>中删除:
- protected virtual Guid GetEntityId<TEntity>(TEntity entity)
- protected virtual Guid GetCreatedUserId<TEntity>(TEntity entity)
- protected virtual DateTime GetCreatedDate<TEntity>(TEntity entity)
- protected virtual ValidateEditPermission<TEntity>(...)

// MedicalCaseStateService中删除:
- protected override Guid GetEntityId<TEntity>(TEntity entity)
- protected override Guid GetCreatedUserId<TEntity>(TEntity entity)
- protected override DateTime GetCreatedDate<TEntity>(TEntity entity)

// MedicalCaseQueryService中删除:
- protected override Guid GetEntityId<TEntity>(TEntity entity)
- protected override Guid GetCreatedUserId<TEntity>(TEntity entity)
- protected override DateTime GetCreatedDate<TEntity>(TEntity entity)

// MedicalCaseCommandService中删除:
- protected override Guid GetEntityId<TEntity>(TEntity entity)
- protected override Guid GetCreatedUserId<TEntity>(TEntity entity)
- protected override DateTime GetCreatedDate<TEntity>(TEntity entity)
```

### 保留的权限验证代码

以下代码工作正常，保持不变：

1. **MedicalCaseRules** (`Services/MedicalCaseRules.cs`)
   - `CanEdit()` - 静态方法
   - `CanDelete()` - 静态方法
   - `ValidateCaseUpdate()` - 验证方法

2. **MedicalCasePermissionService** (`Services/MedicalCasePermissionService.cs`)
   - `CanEdit()` - 实例方法
   - `CanDelete()` - 实例方法
   - `GetPermissions()` - 获取完整权限DTO

3. **BaseService非泛型版本** (可选保留或删除)
   - `ValidateEditPermission(entityId, userId, ...)` - 参数版本
   - `ValidateDeletePermission(entityId, userId, ...)` - 参数版本

## Test Strategy

- 编译通过验证（无调用死代码）
- 现有MedicalCase测试通过（权限逻辑未变）
- 无需新增测试（删除的是未使用代码）
