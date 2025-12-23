# Tasks: unify-event-system

## Phase 1: 事件基础设施

### 1.1 创建TokenEvents聚合类
- [x] 创建`TokenEvents.cs`在Foundation/Security/
- [x] 定义TokenRefreshSucceededEvent (复用现有Payload)
- [x] 定义TokenRefreshFailedEvent (复用现有Payload)
- [x] 定义TokenLifecycleChangedEvent + Payload
- [x] 编写单元测试

**验证**: TokenEvents编译通过，Payload可序列化

### 1.2 创建PatientEvents聚合类
- [x] 创建`PatientEvents.cs`在Infrastructure/Events/
- [x] 定义PatientCreatedEvent + PatientCreatedPayload
- [x] 定义PatientUpdatedEvent + PatientUpdatedPayload
- [x] 定义PatientSelectedEvent + PatientSelectedPayload
- [x] 编写单元测试

**验证**: PatientEvents编译通过

### 1.3 创建CaseEvents聚合类
- [x] 创建`CaseEvents.cs`在Infrastructure/Events/
- [x] 定义ConsultationCompletedEvent + Payload
- [x] 定义PrescriptionCompletedEvent + Payload
- [x] 编写单元测试

**验证**: CaseEvents编译通过

### 1.4 扩展AuthEvents
- [x] 添加PasswordChangedEvent + PasswordChangedPayload
- [x] 验证现有AuthEvents Payload完整性

**验证**: AuthEvents扩展完成

## Phase 2: Core层迁移

### 2.1 TokenRefreshHandler迁移
- [x] 移除`TokenRefreshFailed` EventHandler事件
- [x] 移除`TokenRefreshSucceeded` EventHandler事件
- [x] 确保只发布PubSubEvent
- [x] 更新ITokenRefreshHandler接口
- [x] 更新相关测试

**验证**: TokenRefreshHandler无EventHandler事件，测试通过

### 2.2 LoginStateMachine迁移
- [x] 移除`StateChanged` EventHandler事件
- [x] 确保只发布AuthEvents.LoginStateChangedEvent
- [x] 更新ILoginStateMachine接口
- [x] 更新相关测试

**验证**: LoginStateMachine无EventHandler事件，测试通过

### 2.3 LogoutService迁移
- [x] 移除`ServerLogoutFailed` EventHandler事件
- [x] 移除`PendingLogoutsCleared` EventHandler事件
- [x] 确保只发布PubSubEvent
- [x] 更新ILogoutService接口
- [x] 更新相关测试

**验证**: LogoutService无EventHandler事件，测试通过

### 2.4 TokenLifecycleService迁移
- [x] 改用TokenEvents.LifecycleChangedEvent
- [x] 更新Payload类型
- [x] 更新订阅者

**验证**: TokenLifecycleService使用统一事件

## Phase 3: Infrastructure层迁移

### 3.1 迁移旧事件类到聚合类
- [x] LoginSuccessEvent -> AuthEvents.LoginSucceededEvent (重定向)
- [x] LogoutEvent -> AuthEvents.LogoutCompletedEvent (重定向)
- [x] PatientCreatedEvent -> PatientEvents.CreatedEvent
- [x] PatientUpdatedEvent -> PatientEvents.UpdatedEvent
- [x] PatientSelectedEvent -> PatientEvents.SelectedEvent
- [x] ConsultationCompletedEvent -> CaseEvents.ConsultationCompletedEvent
- [x] PrescriptionCompletedEvent -> CaseEvents.PrescriptionCompletedEvent
- [x] PasswordChangedEvent -> AuthEvents.PasswordChangedEvent

**验证**: 旧事件类已删除

### 3.2 迁移订阅者
- [x] 扫描所有GetEvent<旧事件>()调用
- [x] 更新为新聚合类事件
- [x] 验证Payload类型兼容

**验证**: 无旧事件类引用

## Phase 4: 清理和验证

### 4.1 删除旧事件类
- [x] 删除独立的旧事件类文件
- [x] 清理未使用的EventArgs类
- [x] 清理未使用的枚举

**验证**: 无废弃代码

### 4.2 接口清理
- [x] 移除接口中的EventHandler定义
- [x] 更新接口文档

**验证**: 接口整洁

### 4.3 全量测试
- [x] 运行所有单元测试
- [x] 运行所有集成测试
- [ ] 手动验证登录/登出流程
- [ ] 手动验证患者CRUD事件

**验证**: 编译通过，自动化测试通过，需手动验证功能

### 4.4 文档更新
- [ ] 更新CHANGELOG
- [ ] 更新事件系统架构文档

**验证**: 文档与实现一致

## Dependencies

```
Phase 1.1 (TokenEvents) ──┐
Phase 1.2 (PatientEvents)─┼──▶ Phase 2 (Core层迁移)
Phase 1.3 (CaseEvents) ───┤
Phase 1.4 (AuthEvents扩展)┘

Phase 2.1 (TokenRefreshHandler) ──┐
Phase 2.2 (LoginStateMachine) ────┼──▶ Phase 3 (Infrastructure层迁移)
Phase 2.3 (LogoutService) ────────┤
Phase 2.4 (TokenLifecycleService)─┘

Phase 3 ──▶ Phase 4 (清理和验证)
```

## Parallelizable Work

以下任务可并行执行:
- Phase 1.1, 1.2, 1.3, 1.4 (无依赖)
- Phase 2.1, 2.2, 2.3, 2.4 (无依赖)

## Rollback Plan

1. 保留旧事件类作为别名 (Phase 3.1)
2. 如有问题，恢复EventHandler事件发布
3. 订阅者可同时监听新旧事件

## Estimated Effort

| Phase | 任务数 | 预计复杂度 |
|-------|--------|-----------|
| Phase 1 | 4 | 低 |
| Phase 2 | 4 | 中 |
| Phase 3 | 2 | 中 |
| Phase 4 | 4 | 低 |
| **Total** | **14** | - |

## Completion Summary

**完成日期**: 2025-12-21

**主要变更**:
1. 创建4个事件聚合类: TokenEvents, PatientEvents, CaseEvents, AuthEvents(扩展)
2. 迁移Core层组件: TokenRefreshHandler, LoginStateMachine, LogoutService, TokenLifecycleService
3. 迁移Infrastructure层所有事件订阅者到新聚合类
4. 删除8个旧事件类文件
5. 更新测试文件以使用新事件类型

**删除的文件**:
- LoginSuccessEvent.cs
- LogoutEvent.cs
- PasswordChangedEvent.cs
- PatientSelectedEvent.cs
- ConsultationCompletedEvent.cs
- PrescriptionCompletedEvent.cs
- PatientCreatedEvent.cs
- PatientUpdatedEvent.cs

**编译状态**: 成功 (0错误, 0警告)
