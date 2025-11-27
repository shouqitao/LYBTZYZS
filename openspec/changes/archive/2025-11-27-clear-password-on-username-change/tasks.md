# Tasks: clear-password-on-username-change

## Task Overview

| Task | 描述 | 工作量 | 状态 |
|------|------|--------|------|
| 1.1 | 添加 `_savedUsername` 字段 | 5min | Done |
| 1.2 | 修改 `Username` setter 添加清空逻辑 | 10min | Done |
| 1.3 | 修改 `LoadSavedCredentialsAsync` 记录用户名 | 5min | Done |
| 2.1 | 单元测试 | 20min | Done |

**总工作量**: ~40min

---

## Phase 1: 实现改动

### Task 1.1: 添加 `_savedUsername` 字段
**Priority**: P0
**Effort**: 5min
**Dependencies**: None
**Status**: Done

在 `LoginViewModel.cs` 中添加：
- [x] 添加 `private string? _savedUsername;` 字段
- [x] 添加注释说明用途

**验收标准**:
- 字段声明在其他私有字段附近

### Task 1.2: 修改 Username setter
**Priority**: P0
**Effort**: 10min
**Dependencies**: Task 1.1
**Status**: Done

- [x] 在 `SetProperty` 前检测用户名是否变更
- [x] 变更时清空 `Password`
- [x] 添加日志记录

**验收标准**:
- 用户名变更时密码被清空
- 初始加载不触发清空

### Task 1.3: 修改 LoadSavedCredentialsAsync
**Priority**: P0
**Effort**: 5min
**Dependencies**: Task 1.1
**Status**: Done

- [x] 成功加载凭据后设置 `_savedUsername`
- [x] 仅加载用户名（无密码）时也设置 `_savedUsername`

**验收标准**:
- `_savedUsername` 在加载完成后正确赋值

---

## Phase 2: 测试

### Task 2.1: 单元测试
**Priority**: P1
**Effort**: 20min
**Dependencies**: Phase 1
**Status**: Done

测试用例：
- [x] `UsernameChange_WhenSavedCredentials_ShouldClearPassword`
- [x] `UsernameChange_WhenNoSavedCredentials_ShouldNotAffectPassword`
- [x] `InitialLoad_ShouldNotClearPassword`
- [x] `UsernameChange_ToEmpty_ShouldNotClearPassword`
- [x] `UsernameChange_BackToSaved_ShouldNotRestorePassword`
- [x] `UsernameChange_WhenOnlyUsernameSaved_ShouldNotTriggerClear`

**验收标准**:
- 所有测试通过 (6/6)

---

## Summary

| Phase | Tasks | Total Effort | Status |
|-------|-------|--------------|--------|
| Phase 1 | 3 | 20min | Done |
| Phase 2 | 1 | 20min | Done |
| **Total** | **4** | **40min** | **4/4 Done** |

## Dependency Graph

```
Task 1.1 (_savedUsername 字段) ✓
    ├── Task 1.2 (Username setter) ✓
    └── Task 1.3 (LoadSavedCredentialsAsync) ✓

Task 1.2 + Task 1.3 完成 → Task 2.1 (单元测试) ✓
```
