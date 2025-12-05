# Proposal: refactor-baseservice-permission

## Summary
清理BaseService中的死代码（权限验证相关），消除NotImplementedException代码异味。

## Problem Statement

当前BaseService<T>存在设计问题：

1. **NotImplementedException反模式**：`GetEntityId`、`GetCreatedUserId`、`GetCreatedDate`三个虚方法默认抛出NotImplementedException
2. **死代码**：这些方法及相关的ValidateEditPermission<TEntity>泛型版本**从未被业务代码调用**
3. **误导性设计**：给人错觉需要通过继承实现权限验证

### 实际使用情况分析

| 组件 | 状态 | 说明 |
|------|------|------|
| `MedicalCaseRules.CanEdit()` | **实际使用** | 静态方法，业务服务调用 |
| `MedicalCasePermissionService` | **实际使用** | 独立权限服务，已实现 |
| `BaseService.ValidateEditPermission<T>()` | **未使用** | 泛型版本，无调用 |
| `BaseService.GetEntityId/GetCreatedUserId/GetCreatedDate` | **未使用** | 虽被重写但从未调用 |

### 服务重写情况

| 服务 | 重写了方法 | 实际调用这些方法 |
|------|-----------|-----------------|
| MedicalCaseStateService | Yes | **No** |
| MedicalCaseQueryService | Yes | **No** |
| MedicalCaseCommandService | Yes | **No** |
| UserService | No | No |
| HerbService | No | No |
| PatientService | No | No |

**结论**：3个服务重写了方法，但**0个服务**实际调用它们。

## Proposed Solution

**直接删除死代码**，不需要创建新服务或接口：

1. 删除BaseService<T>中的GetEntityId、GetCreatedUserId、GetCreatedDate虚方法
2. 删除ValidateEditPermission<TEntity>泛型版本
3. 删除MedicalCase服务中已无用的重写方法
4. 保留ValidateEditPermission(参数版本)供未来直接调用场景（可选）

## Scope

### In Scope
- 删除BaseService<T>中的死代码
- 删除MedicalCase服务中的无用重写

### Out of Scope
- MedicalCaseRules/MedicalCasePermissionService（保持不变，它们工作正常）
- BaseService其他功能

## Impact

- **破坏性变更**：否（删除的是未使用代码）
- **影响模块**：LYBT.Infrastructure、LYBT.Module.MedicalCase
- **风险等级**：低（只是删除死代码）

## Related

- **来源**：server-code-optimization P3阶段分析
- **现有实现**：MedicalCasePermissionService、MedicalCaseRules
