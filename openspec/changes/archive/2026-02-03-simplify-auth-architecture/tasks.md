# simplify-auth-architecture Tasks

## Overview

- **变更类型**: Refactor
- **风险等级**: Medium
- **预估工作量**: 2-3小时
- **变更范围**: 移除Expiring状态及相关死代码

## Phase 1: 接口定义清理

### 1.1 移除SessionState.Expiring枚举值
- **文件**: `src/Client/Desktop/Shell/Services/Session/ISessionLifecycleManager.cs`
- **变更**:
  - 删除 `Expiring = 2` 枚举值
  - 重新编号：Expired改为2，Refreshing改为3
  - 删除 `SessionExpiringWarningEventArgs` 类定义
  - 从 `ISessionLifecycleManager` 接口移除 `SessionExpiring` 事件
- **验证**: 文件保存成功

### 1.2 移除IUserActivityTracker的SessionExpiring事件
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IUserActivityTracker.cs`
- **变更**:
  - 删除 `event EventHandler<SessionExpiringEventArgs>? SessionExpiring;` 定义
  - 删除 `SessionExpiringEventArgs` 类定义
- **验证**: 文件保存成功

### 1.3 移除ISessionManager的SessionExpiring事件
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISessionManager.cs`
- **变更**: 删除 `event EventHandler? SessionExpiring;` 定义
- **验证**: 文件保存成功

### 1.4 移除Foundation层未使用事件定义
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthEvents.cs`
- **变更**:
  - 删除 `SessionExpiringEvent` 类
  - 删除 `SessionExpiringPayload` record
- **验证**: 文件保存成功

### 1.5 移除TokenEvents中的ExpiringEvent
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/TokenEvents.cs`
- **变更**: 删除 `ExpiringEvent` 类定义
- **验证**: 文件保存成功

### 1.6 编译验证
- 运行 `dotnet build src/Client/Desktop/LYBT.Desktop.sln -c Release --no-restore`
- 预期：编译错误，因为实现类仍引用已删除的接口成员

## Phase 2: 实现类更新

### 2.1 更新SessionLifecycleManager
- **文件**: `src/Client/Desktop/Shell/Services/Session/SessionLifecycleManager.cs`
- **变更**:
  - 删除 `SessionExpiring` 事件字段
  - 删除 `OnUserActivitySessionExpiring` 方法
  - 删除 `_userActivityTracker.SessionExpiring += OnUserActivitySessionExpiring` 订阅
  - 删除 `_userActivityTracker.SessionExpiring -= OnUserActivitySessionExpiring` 取消订阅
  - 移除所有 `TransitionTo(SessionState.Expiring)` 调用
  - 更新状态检查逻辑：移除 `_currentState == SessionState.Expiring` 判断
- **验证**: 编译通过

### 2.2 更新UserActivityTracker
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/UserActivityTracker.cs`
- **变更**:
  - 删除 `event EventHandler<SessionExpiringEventArgs>? SessionExpiring;` 字段
  - 删除 `OnSessionExpiring(TimeSpan remainingTime)` 方法
  - 删除相关注释（已存在的"移除警告逻辑"注释可保留或更新）
- **验证**: 编译通过

### 2.3 更新SessionManager
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs`
- **变更**: 删除 `event EventHandler? SessionExpiring;` 字段
- **验证**: 编译通过

### 2.4 更新MainWindowViewModel
- **文件**: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- **变更**:
  - 删除 `OnSessionExpiring` 方法
  - 删除 `_userActivityTracker.SessionExpiring += OnSessionExpiring` 订阅
  - 删除 `_userActivityTracker.SessionExpiring -= OnSessionExpiring` 取消订阅
- **验证**: 编译通过

### 2.5 编译验证
- 运行 `dotnet build src/Client/Desktop/LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误

## Phase 3: 规范更新

### 3.1 更新login-state-machine规范
- **文件**: `openspec/specs/login-state-machine/spec.md`
- **变更**:
  - 修改LSM-001：从6状态改为5状态（移除Expiring）
  - 更新状态转换图
- **验证**: 规范内容一致

### 3.2 检查authentication规范
- **文件**: `openspec/specs/authentication/spec.md`（如存在）
- **变更**: 确认AUTH-003描述与静默超时行为一致
- **验证**: 规范内容一致

## Dependencies

```
Phase 1 ──────────────────┐
                          │
Phase 2 ──────────────────┼──> Phase 3
                          │
(Phase 1完成后才能        │
 开始Phase 2)             │
```

Phase 1必须先完成，因为Phase 2的实现类需要符合新的接口定义。

## Validation Checklist

- [x] Desktop解决方案编译通过
- [x] 无Expiring关键字残留（除注释外）
- [x] 超时直接触发logout（无警告）
- [ ] 正常logout流程正常（需手动测试）
- [ ] 自动登录流程正常（需手动测试）

## Notes

### 代码分析发现

1. **现有架构已相对完善**: CredentialVault、TokenManager、AuthenticationStateMachine已实现良好
2. **主要清理对象**: Expiring状态相关的事件和处理器
3. **变更范围比提案预期小**: 无需删除文件，仅需清理代码

### 关键文件位置

| 文件 | 行号参考 |
|------|----------|
| SessionState枚举 | ISessionLifecycleManager.cs:7-33 |
| SessionExpiring事件订阅 | MainWindowViewModel.cs:323, 668 |
| OnSessionExpiring处理器 | MainWindowViewModel.cs:373 |
| UserActivityTracker事件 | UserActivityTracker.cs:31, 267-282 |

---

**生成时间**: 2026-01-26
**执行完成时间**: 2026-01-27
**状态**: 已完成执行
