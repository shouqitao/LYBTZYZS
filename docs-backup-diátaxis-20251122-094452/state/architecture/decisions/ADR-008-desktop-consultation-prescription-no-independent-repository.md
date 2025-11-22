# ADR-008: Desktop端Consultation/Prescription不独立实现Repository

## 状态

**已接受** - 2025-11-01

## 背景

在Issue #1768清理[Obsolete]代码过程中，发现Desktop端存在两个空接口桩：
- `LYBT.Desktop.Prescriptions.Interfaces.IPrescriptionRepository`
- `LYBT.Desktop.Consultation.Interfaces.IConsultationRepository`

这两个接口被标记为[Obsolete]，注释说明是"临时接口桩，仅作为编译过渡使用"。然而，经过深度分析发现：

### 关键发现

1. **无实际代码引用**：所有引用都是注释或文档说明，没有任何实际的依赖注入或方法调用
2. **聚合根模式已实施**：MedicalCase作为聚合根，所有Consultation/Prescription的Write操作已通过`IMedicalCaseRepository`完成（Issue #1606）
3. **Server端与Desktop端职责不同**：
   - Server Repository：数据库访问层（EF Core），存在Read-only Repository用于查询优化
   - Desktop Repository：HTTP客户端层（Refit），封装API调用

### 评估方案

经过三方案深度评估（Option A预留接口、Option B实现Repository、Option C删除接口），从以下维度分析：

| 评估维度 | Option A (预留) | Option B (实现) | Option C (删除) |
|---------|----------------|----------------|----------------|
| YAGNI原则 | ⚠️ 部分违反 | ❌ 严重违反 | ✅ 完全符合 |
| MVP原则 | ⚠️ 可接受 | ❌ 违反 | ✅ 完全符合 |
| 维护成本 | ⚠️ 中等 | ❌ 高 | ✅ 最低 |
| DDD架构 | ⚠️ 违反聚合边界 | ⚠️ 违反聚合边界 | ✅ 符合 |
| 当前需求 | ❌ 超前 | ❌ 超前 | ✅ 精确匹配 |

### 业务现状约束

当前业务数据（截至2025-11）：
- ✅ 数据规模：< 1万条
- ✅ 查询需求：基本CRUD（无复杂统计、关键词搜索）
- ✅ 性能表现：无瓶颈（响应时间 < 200ms）
- ✅ 团队规模：1人（维护成本敏感）

无一满足独立Repository的触发条件：
1. 查询复杂度 > 5个方法
2. 单查询响应时间 > 200ms
3. 业务需求：独立统计/报表
4. 数据规模 > 10,000条

## 决策

**删除Desktop端空接口**：
- 删除 `LYBT.Desktop.Prescriptions.Interfaces.IPrescriptionRepository`
- 删除 `LYBT.Desktop.Consultation.Interfaces.IConsultationRepository`
- 删除 `LYBT.Desktop.Consultation.Interfaces.IConsultationApiClient`

**架构原则**：
1. **子实体通过聚合根访问**（DDD标准）：Consultation和Prescription作为MedicalCase的子实体，所有操作（Read + Write）通过`IMedicalCaseRepository`完成
2. **Desktop层无CQRS优化必要**：HTTP调用层面无查询优化价值，瓶颈在网络而非查询逻辑
3. **渐进式演进**：需要时再添加（那时会有明确需求和真实数据）

## 后果

### 正面影响

1. ✅ **架构清晰**：子实体不独立访问，符合DDD聚合根模式
2. ✅ **维护成本降低**：无额外代码、测试、文档需要维护
3. ✅ **避免开发者困惑**：消除"为什么有接口但无实现"的疑问
4. ✅ **符合MVP原则**：不预留未来可能不需要的接口
5. ✅ **符合YAGNI原则**：需要时再添加，基于真实需求而非预测

### 负面影响

1. ⚠️ **未来扩展成本**：如果需要独立Repository，需要重新创建接口和实现（但这是正常演进）
2. ⚠️ **心理舒适度**：部分开发者可能担心"万一需要呢"（但这违反YAGNI）

### 风险缓解

**如果未来需要独立Repository**（满足触发条件时），可通过以下步骤渐进式演进：

```markdown
Phase 1: 创建Read-only Repository接口
- 创建IConsultationRepository（Read-only）
- 创建IPrescriptionRepository（Read-only）

Phase 2: 实现Repository类
- 创建ConsultationRepository（继承RepositoryBase）
- 创建PrescriptionRepository（继承RepositoryBase）

Phase 3: 创建Refit ApiClient接口
- 创建IConsultationApiClient（Refit标注）
- 创建IPrescriptionApiClient（Refit标注）

Phase 4: 更新ViewModel注入
- 调整需要复杂查询的ViewModel
- 注入新Repository替代MedicalCaseRepository（仅限查询部分）
```

**预计工作量**：2-3天（明确需求时实施，ROI更高）

## 相关决策

- [ADR-005: 聚合根长期架构演进](ADR-005-aggregate-root-long-term-architecture.md) - 定义了6个触发条件和演进路径
- [ADR-006: MedicalCase/Consultation/Prescription重构](ADR-006-medicalcase-consultation-prescription-refactoring.md) - 确立聚合根模式
- [ADR-007: Repository/Service简化](ADR-007-repository-service-simplification.md) - Repository模式标准化

## 参考资料

- Issue #1606: MedicalCase聚合根重构（Read-only Repository模式）
- Issue #1607: Consultation模块Desktop端重构（已暂缓）
- Issue #1608: Prescription模块Desktop端重构（已暂缓）
- Issue #1768: 全局[Obsolete]代码清理
- Issue #1769: 全局接口设计与实现合理性审查
- `docs/explanation/architecture/client/README.md` - Desktop端Phase 2架构（ViewModel → Repository）

## 注意事项

1. **Server端Read-only Repository保留**：Server端的IConsultationRepository和IPrescriptionRepository仍然保留，因为有数据库层面的查询优化价值
2. **聚合根模式不变**："没有特殊情况病案会一直作为聚合根"（用户确认），所有Write操作永久通过MedicalCaseRepository
3. **未来评估标准**：当满足任一触发条件时，重新评估是否需要独立Repository（基于真实需求而非预测）
