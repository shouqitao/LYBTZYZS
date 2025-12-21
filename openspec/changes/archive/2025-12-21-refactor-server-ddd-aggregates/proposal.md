# Proposal: refactor-server-ddd-aggregates

## Why

### 当前问题：循环引用风险

Server端实体定义在统一的`LYBT.Entities`项目中，存在**双向导航属性**设计：

```
MedicalCase ──导航属性──> Consultation
     ^                        │
     │                        │
     └──────反向导航──────────┘
```

**问题根因**：
1. `Consultation.MedicalCase` 反向导航属性违反DDD跨聚合引用原则
2. `Prescription.MedicalCase` 同样存在反向导航
3. 如果要将实体模块化到各业务模块，会导致循环引用无法编译

**与业界优秀设计差距**：

| 维度 | 当前设计 | 业界最佳实践 |
|------|---------|-------------|
| 聚合边界 | 模糊，有双向导航 | 清晰，单向导航 |
| 跨聚合引用 | 导航属性 | 仅ID + 冗余字段 |
| 查询方式 | Include链式加载 | 专用Query Model |
| 跨聚合协调 | 直接操作 | 领域事件 |

### 调研来源

- **Ardalis Clean Architecture** (Benchmark 72.6): 聚合内单向导航，跨聚合用ID
- **Microsoft eShopOnContainers**: 跨聚合只用ID引用，无导航属性
- **ABP Framework**: "Do not reference aggregate root by navigation property"
- **Milan Jovanovic**: "Reference by ID, use Domain Events for coordination"

## What Changes

### 目标架构

```
┌─────────────────────────────────────────────────────────────────────┐
│ MedicalCase Aggregate (聚合根)                                       │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ MedicalCase (Root)                                               │ │
│ │   ├── Consultation (聚合内实体，共享生命周期)                      │ │
│ │   ├── Prescription (聚合内实体，共享生命周期)                      │ │
│ │   │     └── PrescriptionItems (值对象集合)                        │ │
│ │   ├── PatientId: Guid (跨聚合引用，仅ID) ✓                        │ │
│ │   └── UserId: Guid (跨聚合引用，仅ID) ✓                           │ │
│ └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────┐  ┌─────────────────────────┐
│ Patient Aggregate       │  │ User Aggregate          │
│ └── Patient (Root)      │  │ └── User (Root)         │
└─────────────────────────┘  └─────────────────────────┘

┌─────────────────────────┐  ┌─────────────────────────┐
│ Herb Aggregate          │  │ Formula Aggregate       │
│ └── Herb (Root)         │  │ ├── Formula (Root)      │
└─────────────────────────┘  │ └── FormulaHerbItems    │
                             └─────────────────────────┘
```

### 核心变更

1. **删除反向导航属性**
   - `Consultation.MedicalCase` -> 删除
   - `Prescription.MedicalCase` -> 删除

2. **修改EF Core配置**
   - 使用 `.HasOne<T>().WithOne()` 无反向导航配置
   - 配置backing field访问私有集合

3. **创建Query Service**
   - 替代Include链式查询
   - 使用Join或专用Query Model

4. **引入领域事件**
   - 跨聚合协调使用事件通信
   - 实现MediatR事件处理

## Impact

### 受影响项目

| 项目 | 变更类型 | 说明 |
|------|----------|------|
| LYBT.Entities | 重构 | 删除反向导航属性 |
| LYBT.Infrastructure | 重构 | 修改EF配置，添加领域事件 |
| LYBT.Module.MedicalCase | 新增 | 添加Query Service |
| LYBT.Module.Consultation | 修改 | 移除对MedicalCase的直接引用 |
| LYBT.Module.Prescriptions | 修改 | 移除对MedicalCase的直接引用 |

### 受影响规范

| 规范 | 变更类型 | 说明 |
|------|----------|------|
| server-layer-architecture | 更新 | 添加DDD聚合设计原则 |
| entity-conventions | 新建 | 实体设计规范 |

## Alternatives Considered

1. **保持现状**: 所有实体在Core层，可用但无法模块化
2. **仅删除反向导航**: 最小变更，本方案采用
3. **完全模块化实体**: 过于激进，需要大量迁移

## Risks

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 查询逻辑变更 | 中 | 分阶段迁移，保留旧查询兼容 |
| 功能回归 | 中 | 每阶段编译测试验证 |
| 性能影响 | 低 | Query Model可优化查询 |

## Success Criteria

### 量化指标

| 指标 | 当前 | 目标 |
|------|------|------|
| 反向导航属性 | 2个 | 0个 |
| 跨聚合导航属性 | 2个 | 0个 |
| Query Service覆盖 | 0% | 100% |

### 质量指标

- [ ] 编译通过 (0错误)
- [ ] 现有API功能100%正常
- [ ] 实体可独立模块化（无循环引用风险）
- [ ] 符合DDD聚合设计原则

## Related

- **Specs**: server-layer-architecture, entity-conventions
- **参考**: Ardalis Clean Architecture, Microsoft eShopOnContainers
- **模式**: CQRS, Domain Events, Repository Pattern
