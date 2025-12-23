# Proposal: unify-event-system

## Summary

统一Desktop层事件系统，消除EventHandler与PubSubEvent双轨兼容模式，建立一致的事件通信架构。

## Problem Statement

当前Desktop层存在严重的事件系统碎片化问题:

### 1. 双轨并行 (兼容模式)
- `LoginStateMachine`、`LogoutService`、`TokenRefreshHandler`同时发布EventHandler和PubSubEvent
- 增加维护成本，订阅者需要决定使用哪种方式

### 2. Payload类型不一致
| 事件 | Payload类型 | 问题 |
|------|-------------|------|
| LoginSuccessEvent | `UserDetailDto` | 直接使用DTO |
| LogoutEvent | `LogoutEventArgs` (class) | EventArgs风格类 |
| PatientCreatedEvent | `PatientDetailDto` | 直接使用DTO |
| TokenLifecycleStateChangedEvent | `TokenLifecycleStateChangedEventArgs` (class) | EventArgs风格类 |
| AuthEvents.* | `*Payload` (record) | Record类型 |

### 3. 事件定义分散
- `LYBT.Desktop.Foundation/Security/` - AuthEvents, TokenLifecycleStateChangedEvent
- `LYBT.Desktop.Infrastructure/Events/` - LoginSuccessEvent, LogoutEvent, PatientSelectedEvent等
- `LYBT.Desktop.Patients/Events/` - PatientCreatedEvent, PatientUpdatedEvent
- `LYBT.Desktop.MedicalCase/Events/` - WorkspaceEvents

### 4. 现状数据
- **EventHandler事件**: 38处定义
- **PubSubEvent事件类**: 11个
- **兼容模式组件**: 3个 (LoginStateMachine, LogoutService, TokenRefreshHandler)

## Proposed Solution

### 统一架构

```
                    ┌─────────────────────────────────────┐
                    │         IEventAggregator           │
                    │      (Prism EventAggregator)       │
                    └──────────────┬──────────────────────┘
                                   │
        ┌──────────────────────────┼──────────────────────────┐
        │                          │                          │
        ▼                          ▼                          ▼
┌───────────────────┐    ┌───────────────────┐    ┌───────────────────┐
│  Core Events      │    │  Module Events    │    │  Feature Events   │
│  (Foundation)     │    │  (Infrastructure) │    │  (各模块)          │
├───────────────────┤    ├───────────────────┤    ├───────────────────┤
│ AuthEvents        │    │ PatientEvents     │    │ 模块内部事件       │
│ SessionEvents     │    │ PrescriptionEvents│    │ (使用EventHandler) │
│ TokenEvents       │    │ ConsultationEvents│    │                   │
└───────────────────┘    └───────────────────┘    └───────────────────┘
```

### 设计原则

1. **跨模块通信**: 使用PubSubEvent (通过IEventAggregator)
2. **模块内部通信**: 保留EventHandler (组件间紧耦合场景)
3. **Payload规范**: 统一使用record类型
4. **命名规范**: `*Event` + `*Payload`
5. **事件聚合**: 相关事件聚合到静态类 (如AuthEvents, PatientEvents)

### 迁移策略

**Phase 1**: 定义统一事件规范和基础设施
**Phase 2**: 迁移Core层事件 (Foundation + Infrastructure)
**Phase 3**: 迁移Module层事件 (Patients, MedicalCase等)
**Phase 4**: 移除兼容模式代码

## Scope

### In Scope
- 统一事件Payload为record类型
- 聚合相关事件到命名静态类
- 移除双轨发布的兼容代码
- 迁移订阅者到统一模式

### Out of Scope
- 组件内部EventHandler (如PrescriptionCalculator.PriceCalculated)
- WPF框架事件 (CanExecuteChanged等)
- 第三方库事件

## Success Criteria

1. 消除所有兼容模式双轨发布
2. 跨模块事件100%使用PubSubEvent
3. 所有Payload使用record类型
4. 事件定义集中到聚合类
5. 编译通过，测试100%通过

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| 订阅者遗漏迁移 | 功能失效 | 编译时检查 + 全量测试 |
| 事件顺序变化 | 行为不一致 | 保持发布顺序，逐步验证 |
| 性能影响 | 响应延迟 | PubSubEvent已优化，无显著差异 |

## Dependencies

- 依赖Prism.Events包 (已存在)
- 无外部新增依赖

## Stakeholders

- Desktop层所有模块开发者
- 测试团队 (验证事件通信)
