# 架构例外清单（Architecture Exceptions）

**创建日期**: 2025-10-25
**维护者**: 项目架构团队
**目的**: 正式跟踪所有经批准的架构原则违反情况，确保例外有据可查、可追溯

---

## 📋 什么是架构例外？

架构例外是指**经过正式批准的架构原则违反**。并非所有违反架构原则的情况都是错误，有时为了实现特定目标（如DDD聚合根、性能优化），需要有意识地违反某些原则。

**核心价值**：
- ✅ **透明化**：所有例外公开记录，团队成员知晓
- ✅ **可追溯**：每个例外都有批准原因和批准日期
- ✅ **可审查**：定期审查例外是否仍然合理
- ✅ **防止滥用**：未经批准的违反将被视为架构问题

---

## 🎯 例外管理流程

### 1. 识别潜在例外

**触发场景**：
- 需要违反三层架构原则
- 需要跨聚合根直接操作
- 需要使用技术黑名单中的技术
- 需要绕过标准工作流

### 2. 提出例外申请

**申请方式**：
1. 在相关Issue中说明需要例外
2. 创建ADR（Architecture Decision Record）记录决策
3. 在ADR中明确说明：
   - 违反的原则
   - 违反的原因
   - 替代方案及为何未采纳
   - 影响范围
   - 补救措施

### 3. 批准例外

**批准流程**：
- 技术负责人评审ADR
- 确认例外的合理性和必要性
- 批准后更新本清单

### 4. 定期审查

**审查周期**：
- **P0（高风险）**：每季度审查
- **P1（中风险）**：每半年审查
- **P2（低风险）**：每年审查

---

## 📊 例外清单

### 活跃例外（Active Exceptions）

#### EXC-001: Desktop端Prescriptions/Consultation模块违反三层架构

| 属性 | 值 |
|------|------|
| **例外编号** | EXC-001 |
| **违反原则** | Desktop三层架构（View→ViewModel→Repository→ApiClient） |
| **影响模块** | `LYBT.Desktop.Prescriptions`, `LYBT.Desktop.Consultation` |
| **批准原因** | DDD聚合根模式优先级高于分层架构，避免聚合根边界被绕过 |
| **批准日期** | 2025-10-24 |
| **批准者** | 开发团队 |
| **相关ADR** | [ADR-003: Repository层简化](./decisions/ADR-003-repository-simplification.md) |
| **相关Issue** | #1606, #1608 |
| **风险级别** | P1（中风险） |
| **审查周期** | 每半年 |
| **下次审查** | 2025-04-25 |

**具体违反**：
- ViewModel直接依赖`IPrescriptionApi`（Refit接口），绕过Repository层
- Read操作：ViewModel → API（跳过Repository）
- Write操作：ViewModel → `IMedicalCaseRepository`（通过聚合根）

**补救措施**：
- [ ] **可选**：Issue #XXXX - 恢复Read-only Repository（P2优先级，2-3天工作量）
  - 创建`IPrescriptionRepository`（仅Read方法）
  - 实现`PrescriptionRepository`（薄封装API调用+可选缓存）
  - 更新所有ViewModel依赖
  - 时机：当需要添加缓存/离线支持时强制恢复
- [x] 在ADR-003中明确记录此例外
- [x] 在本清单中跟踪此例外

**监控指标**：
- ViewModel与API的直接耦合数量：~6个（PrescriptionManagementViewModel等）
- 是否有缓存/离线需求：否（当前MVP阶段）

---

#### EXC-002: 保留跨模块Component（非违反，但需跟踪）

| 属性 | 值 |
|------|------|
| **例外编号** | EXC-002 |
| **违反原则** | 无（符合Component设计三原则） |
| **影响模块** | 所有Desktop端模块 |
| **批准原因** | 符合"跨模块共享"原则，提供真实业务价值 |
| **批准日期** | 2025-10-25 |
| **批准者** | 开发团队 |
| **相关ADR** | [ADR-004: Component设计指南](./decisions/ADR-004-component-design-guidelines.md) |
| **风险级别** | P2（低风险） |
| **审查周期** | 每年 |
| **下次审查** | 2026-10-25 |

**保留的Component**：
- `NotificationService`：跨模块通知机制
- `DialogService`：弹窗管理服务
- `NavigationService`：页面导航管理

**监控指标**：
- Component数量：3个（稳定）
- 使用模块数：≥2个模块

**备注**：此项不是真正的例外，而是为了明确记录"哪些Component是合理的"，避免误删。

---

#### EXC-003: 值对象和特殊实体的软删除例外

| 属性 | 值 |
|------|------|
| **例外编号** | EXC-003 |
| **违反原则** | AR-003软删除一致性原则 |
| **影响模块** | `LYBT.Entities` |
| **批准原因** | 值对象、日志表、会话表、安全敏感表不适用软删除模式 |
| **批准日期** | 2025-10-27 |
| **批准者** | 开发团队 |
| **相关Issue** | #1611 Phase 4 |
| **相关测试** | `AggregateRootArchTests.AR003_All_Entities_Should_Support_Soft_Delete` |
| **风险级别** | P2（低风险） |
| **审查周期** | 每年 |
| **下次审查** | 2026-10-27 |

**例外实体清单**：
1. **AdminSecretModel**（安全敏感）
   - 用途：存储密码哈希
   - 原因：安全敏感数据，不应使用软删除，直接删除更符合安全最佳实践

2. **PrescriptionItem**（值对象）
   - 用途：处方中的药材明细
   - 原因：通过父实体Prescription管理生命周期，无需独立软删除

3. **FormulaHerbItem**（值对象）
   - 用途：方剂中的药材明细
   - 原因：通过父实体Formula管理生命周期，无需独立软删除

4. **SystemLog**（日志表）
   - 用途：系统日志记录
   - 原因：只增不删（Append-only），用于审计追踪，不允许软删除

5. **AuthSession**（会话表）
   - 用途：用户会话管理
   - 原因：过期自动清理机制，无需软删除标记

**架构测试保护**：
- 白名单维护在`AggregateRootArchTests.AR003_All_Entities_Should_Support_Soft_Delete()`
- 新增实体默认要求软删除，除非明确加入白名单并说明原因

**监控指标**：
- 例外实体数量：5个（稳定）
- 新增例外需求：无

---

#### EXC-004: EventBus项目和独立Repository保留（技术债）

| 属性 | 值 |
|------|------|
| **例外编号** | EXC-004 |
| **违反原则** | Dead Code清理原则 |
| **影响模块** | `LYBT.EventBus`, `ConsultationRepository`, `PrescriptionRepository` |
| **批准原因** | Epic #1725执行"温和改进"，"激进重构"暂时搁置，避免MVP阶段过度工程 |
| **批准日期** | 2025-10-30 |
| **批准者** | 开发团队 |
| **相关ADR** | [ADR-007: Repository和Service层简化](./decisions/ADR-007-repository-service-simplification.md) |
| **相关Issue** | #1724（Backlog） |
| **风险级别** | P2（低风险） |
| **审查周期** | 每年 |
| **下次审查** | 2026-10-30 |

**技术债内容**：
1. **LYBT.EventBus项目保留**（~500行Dead Code）
   - 问题：项目存在但仅保留IEventBus接口，无实际功能
   - 现状：Epic #1725已移除所有IEventBus注入，不再有新引用
   - 影响：占用代码库空间，但不影响功能

2. **独立Repository保留**（ConsultationRepository, PrescriptionRepository）
   - 问题：未完全对齐聚合根边界（MedicalCase聚合根）
   - 现状：Epic #1725添加BaseRepository辅助方法，减少代码重复
   - 影响：存在轻微的架构不一致，但功能正常

3. **Service层依赖关系未重构**
   - 问题：ConsultationService和PrescriptionService未改为依赖IMedicalCaseRepository
   - 现状：Epic #1725提取LoadRelatedDataAsync方法，减少重复逻辑
   - 影响：代码重复风险降低，但未达到"激进重构"目标

**Epic #1725实际完成**：
- ✅ Phase 1: 移除7个Service的IEventBus注入（~14行）
- ✅ Phase 2: 添加BaseRepository.GetPagedResultAsync辅助方法（~101行简化）
- ✅ Phase 3: 提取PrescriptionService.LoadRelatedDataAsync方法（~30行简化）
- ✅ Phase 4: 创建ADR-007和验证报告
- **完成度**：10%（145行 vs Issue #1724推荐的1370行）

**"激进重构"剩余工作**（Issue #1724推荐但未执行）：
- [ ] 删除LYBT.EventBus项目（~500行）
- [ ] 删除ConsultationRepository和PrescriptionRepository（~270行）
- [ ] 重构Service层依赖关系（~600行）
- [ ] 创建ADR-006（EventBus删除）

**触发"激进重构"条件**（ADR-005渐进式演进原则）：
- 业务规则数量 >20条（当前：14条）
- Service方法长度 >200行（当前：<150行）
- 聚合根关系复杂度显著增加
- MVP完成后的重大版本迭代

**补救措施**：
- [x] 在Issue #1724添加Backlog标记
- [x] 在ADR-007记录技术债
- [x] 在本清单跟踪此例外
- [ ] **未来行动**：当达到触发条件时，创建新Epic执行"激进重构"

**监控指标**：
- EventBus项目引用数：0（Epic #1725后）
- 独立Repository数量：2个（ConsultationRepository, PrescriptionRepository）
- Dead Code总量：~500行（LYBT.EventBus项目）
- 重构ROI：当前低，MVP后期提升

**符合原则**：
- ✅ ADR-005渐进式演进原则：温和改进 → 验证效果 → 再决定激进重构
- ✅ Constitution MVP原则：避免过度工程，够用即好
- ✅ 风险可控：Dead Code不影响当前功能，可在重大版本迭代时清理

---

### 已解决例外（Resolved Exceptions）

*当前无已解决例外*

**解决标准**：
- 补救措施已全部完成
- 架构原则违反已消除
- 移至"已解决"部分，保留记录

---

### 已拒绝例外（Rejected Exceptions）

*当前无已拒绝例外*

**拒绝原因示例**：
- 违反核心原则（如技术黑名单）
- 存在更好的替代方案
- 风险过高，收益不足

---

## 🔍 例外分类

### 按风险级别分类

| 风险级别 | 审查周期 | 数量 | 编号 |
|---------|---------|------|------|
| **P0（高风险）** | 每季度 | 0 | - |
| **P1（中风险）** | 每半年 | 1 | EXC-001 |
| **P2（低风险）** | 每年 | 3 | EXC-002, EXC-003, EXC-004 |

### 按违反原则分类

| 原则类别 | 数量 | 编号 |
|---------|------|------|
| **分层架构违反** | 1 | EXC-001 |
| **软删除一致性违反** | 1 | EXC-003 |
| **Dead Code清理违反** | 1 | EXC-004 |
| **DDD边界违反** | 0 | - |
| **技术黑名单违反** | 0 | - |
| **Component设计违反** | 0 | - |

---

## 📐 例外模板

### 新增例外记录格式

```markdown
#### EXC-XXX: [简短描述]

| 属性 | 值 |
|------|------|
| **例外编号** | EXC-XXX |
| **违反原则** | [具体原则] |
| **影响模块** | [模块列表] |
| **批准原因** | [为什么需要例外] |
| **批准日期** | YYYY-MM-DD |
| **批准者** | [姓名/团队] |
| **相关ADR** | [ADR链接] |
| **相关Issue** | #XXXX |
| **风险级别** | P0/P1/P2 |
| **审查周期** | 每季度/每半年/每年 |
| **下次审查** | YYYY-MM-DD |

**具体违反**：
[详细描述违反情况]

**补救措施**：
- [ ] [措施1]
- [ ] [措施2]

**监控指标**：
- [指标1]: [当前值]
- [指标2]: [当前值]
```

---

## 🔗 相关资源

- **ADR索引**: [docs/architecture/decisions/README.md](./decisions/README.md)
- **架构原则文档**: `docs/explanation/architecture/principles.md`（计划中）
- **架构文档提案**: [architecture-documentation-system-proposal.md](./shared/architecture-documentation-system-proposal.md)
- **业务规则文档**: [docs/business-rules.md](../explanation/business-rules.md)

---

## 📅 审查日志（Review Log）

| 日期 | 审查者 | 例外编号 | 审查结果 | 备注 |
|------|--------|---------|---------|------|
| 2025-10-25 | Claude Code | EXC-001, EXC-002 | 批准 | 初始创建 |
| 2025-10-27 | Claude Code | EXC-003 | 批准 | Issue #1611 Phase 4架构测试发现 |
| 2025-10-30 | Claude Code | EXC-004 | 批准 | Issue #1724技术债，Epic #1725"温和改进"vs"激进重构" |

---

**最后更新**: 2025-10-30
**维护者**: 项目架构团队
