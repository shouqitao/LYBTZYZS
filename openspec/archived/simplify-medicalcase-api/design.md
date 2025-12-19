# Design: simplify-medicalcase-api

## Context

MedicalCase是系统核心模块，但API设计偏离了RESTful最佳实践。其他模块(Patients, Users, Herbs, Formula)已采用统一的简洁API设计，MedicalCase需要对齐。

## Goals

1. 简化API从28端点到13端点
2. 统一命名风格与其他模块一致
3. 删除Ghost APIs消除Client-Server契约不一致
4. 修复当前保存功能的400 Bug

## Non-Goals

1. 不改变业务逻辑
2. 不改变数据模型
3. 不考虑向后兼容(用户已确认)

## Decisions

### Decision 1: 查询端点合并策略

**选择**: 使用include和filter参数合并查询变体

```csharp
// 新API
GET /api/v1/medicalcases?include=details,consultations,prescriptions
GET /api/v1/medicalcases/{id}?include=all
GET /api/v1/medicalcases/patient/{patientId}?filter=unfinished|recent|all
```

**理由**: 减少端点数量同时保持灵活性，符合REST最佳实践

### Decision 2: 聚合更新方式

**选择**: PUT `/{id}` 替代 PUT `/{id}/aggregate`

```csharp
// 旧API
PUT /api/v1/medicalcases/{id}/aggregate
Body: { id, consultation, prescriptions, ... }

// 新API
PUT /api/v1/medicalcases/{id}
Body: { id, consultation, prescriptions, ... }
```

**理由**: `/aggregate`命名增加理解难度，PUT `/{id}`是RESTful标准

### Decision 3: 状态变更统一

**选择**: PATCH `/{id}/status` 统一Cancel/Close/UpdateStatus

```csharp
// 新API
PATCH /api/v1/medicalcases/{id}/status
Body: { status: "Cancelled|Completed|Draft", reason?: "..." }
```

**理由**: 状态变更是同一操作的不同参数，不需要3个端点

### Decision 4: 处方作为子资源

**选择**: 删除独立的Prescription CRUD端点，通过PUT `/{id}`更新

**理由**:
- 处方是医案的聚合子实体
- 独立端点导致数据一致性问题
- 减少API复杂度

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|----------|
| 改动范围大 | 分Phase执行，每步验证 |
| 测试覆盖不足 | 先更新测试再改代码 |
| Client调用点多 | 全局搜索确保无遗漏 |

## Migration Plan

1. Server端重构（保留旧端点但标记Obsolete）
2. Client端更新调用
3. 验证功能正常
4. 删除旧端点

由于用户确认不需要兼容，可简化为:
1. Server端直接重构
2. Client端同步更新
3. 验证

## Open Questions

无（用户已确认不需要兼容性）
