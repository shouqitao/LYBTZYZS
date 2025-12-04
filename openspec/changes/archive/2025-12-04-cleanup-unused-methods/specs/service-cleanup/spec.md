# Spec: service-cleanup

## Purpose

定义Service层未被调用方法的清理规范，删除Controller端点已移除但Service方法仍存在的废弃代码。

## ADDED Requirements

### Requirement: SVC-CLEANUP-001 UserService清理

UserService SHALL 删除所有未被Controller调用的方法。

#### Scenario: 删除状态切换方法
- **WHEN** 清理UserService状态相关方法
- **THEN** SHALL 从IUserService删除DisableAsync接口定义
- **AND** SHALL 从IUserService删除EnableAsync接口定义
- **AND** SHALL 从IUserService删除ToggleStatusAsync接口定义
- **AND** SHALL 从UserService删除对应实现
- **AND** 编译 SHALL 通过，无错误

#### Scenario: 删除BatchDeleteAsync
- **WHEN** 清理UserService批量删除方法
- **THEN** SHALL 从IUserService删除BatchDeleteAsync接口定义
- **AND** SHALL 从UserService删除BatchDeleteAsync实现
- **AND** 编译 SHALL 通过，无错误

#### Scenario: 删除ResetPasswordAsync重载
- **WHEN** 清理UserService密码重置方法
- **THEN** SHALL 从IUserService删除ResetPasswordAsync(Guid id, string newPassword)接口定义
- **AND** SHALL 从UserService删除对应实现
- **AND** SHALL 保留ResetPasswordAsync(Guid id, ResetPasswordRequestDto request)版本
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: SVC-CLEANUP-002 HerbService清理

HerbService SHALL 删除未被Controller调用的BatchDeleteAsync方法。

#### Scenario: 删除BatchDeleteAsync
- **WHEN** 清理HerbService批量删除方法
- **THEN** SHALL 从IHerbService删除BatchDeleteAsync接口定义
- **AND** SHALL 从HerbService删除BatchDeleteAsync实现
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: SVC-CLEANUP-003 FormulaService清理

FormulaService SHALL 删除未被Controller调用的BatchDeleteAsync方法。

#### Scenario: 删除BatchDeleteAsync
- **WHEN** 清理FormulaService批量删除方法
- **THEN** SHALL 从IFormulaService删除BatchDeleteAsync接口定义
- **AND** SHALL 从FormulaService删除BatchDeleteAsync实现
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: SVC-CLEANUP-004 PatientService清理

PatientService SHALL 删除未被Controller调用的SearchEntityAsync方法。

#### Scenario: 删除SearchEntityAsync
- **WHEN** 清理PatientService搜索方法
- **THEN** SHALL 从IPatientServiceOptimized删除SearchEntityAsync接口定义
- **AND** SHALL 从PatientService删除SearchEntityAsync实现
- **AND** 编译 SHALL 通过，无错误

---

### Requirement: SVC-CLEANUP-005 TokenRevocationService清理

TokenRevocationService SHALL 删除未被调用的RevokeAllUserTokensAsync方法。

#### Scenario: 删除RevokeAllUserTokensAsync
- **WHEN** 清理TokenRevocationService方法
- **THEN** SHALL 从ITokenRevocationService删除RevokeAllUserTokensAsync接口定义
- **AND** SHALL 从TokenRevocationService删除RevokeAllUserTokensAsync实现
- **AND** 编译 SHALL 通过，无错误

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| service-conventions | Service层设计约定 |
| webapi-cleanup | 之前的Controller端点清理 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-04 | 1.0 | 初始版本，定义Service层未调用方法清理规范 |
