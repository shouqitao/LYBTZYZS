# Spec: service-permission-separation

## Overview
清理BaseService中未使用的权限验证代码，消除NotImplementedException代码异味。

## REMOVED Requirements

### REMOVED: 隐式权限验证契约
**原设计**: BaseService<T>提供GetEntityId等虚方法，子类重写以支持权限验证
**移除原因**: 这些方法从未被调用，是死代码

---

## MODIFIED Requirements

### MODIFIED: service-conventions/SC-001
**原规范**: Service可继承BaseService<T>获得权限验证能力
**修改为**: BaseService<T>仅提供ExecuteAsync、ValidateAsync等通用方法；权限验证通过MedicalCaseRules或专用权限服务实现

---

## ADDED Requirements

### Requirement: PERM-001 权限验证独立实现
权限验证逻辑 **MUST** 通过专用服务或规则类实现，**SHALL NOT** 通过BaseService继承实现。

#### Scenario: MedicalCase权限验证使用MedicalCaseRules
- **Given** 需要验证医案编辑权限
- **When** 调用权限验证
- **Then** 使用MedicalCaseRules.CanEdit()方法
- **And** 不依赖BaseService的任何权限方法

### Requirement: PERM-002 BaseService无NotImplementedException
BaseService及其泛型版本 **SHALL NOT** 包含抛出NotImplementedException的方法。

#### Scenario: BaseService所有虚方法有合理默认实现
- **Given** 审查BaseService<T>类
- **When** 检查所有protected virtual方法
- **Then** 每个方法要么有合理默认实现
- **Or** 被标记为abstract强制子类实现
- **And** 不存在throw NotImplementedException语句

### Requirement: PERM-003 现有权限服务保持稳定
MedicalCasePermissionService和MedicalCaseRules **MUST** 保持现有API不变。

#### Scenario: 删除BaseService代码不影响权限功能
- **Given** BaseService中的权限方法已删除
- **When** 执行医案编辑权限检查
- **Then** MedicalCaseRules.CanEdit()正常工作
- **And** MedicalCasePermissionService.CanEdit()正常工作
- **And** 所有现有测试通过

---

## Constraints

### CON-001: 无向后兼容要求
删除的代码从未被使用，无需保持向后兼容。

### CON-002: 保留有效代码
BaseService中的以下方法保留：
- ExecuteAsync<TResult>()
- ValidateAsync<TDto>()
- Validate<TDto>()
- ExtractUserInfoAsync()
- 非泛型版本的ValidateEditPermission/ValidateDeletePermission（待评估）
