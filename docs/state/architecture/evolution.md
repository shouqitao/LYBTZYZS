# 架构演进时间线

**创建日期**: 2025-10-25
**维护者**: 项目架构团队
**目的**: 记录凌隐宝堂中医诊所项目架构的演进历史，帮助团队理解架构决策的背景和影响

---

## 📋 演进概述

凌隐宝堂中医诊所项目从最初的快速原型逐步演进为符合DDD和三层架构的规范化系统。本文档记录了关键的架构演进节点。

---

## 🕐 时间线

### 2025-10-15: 三层对齐架构标准（ADR-001）

**背景**：项目初期架构文档混乱，Server/Client/Shared文档结构不一致

**决策**：
- 建立三层对齐架构文档体系（`docs/explanation/architecture/server/`、`docs/explanation/architecture/client/`、`docs/explanation/architecture/shared/`）
- 定义Server端三层架构标准（Presentation → Application → Domain）
- 定义Client端MVVM架构标准（View → ViewModel → Model）

**影响**：
- ✅ 文档结构清晰，新成员快速上手
- ✅ 架构原则统一，减少理解成本

**相关资源**：
- ADR-001（假设）
- `docs/explanation/architecture/server/README.md`
- `docs/explanation/architecture/client/README.md`

---

### 2025-10-20: MedicalCase DDD聚合根模式（ADR-002）

**背景**：Consultation/Prescription可以独立操作，导致业务规则分散，数据一致性难以保证

**决策**：
- MedicalCase作为聚合根，Consultation/Prescription/Diagnosis作为子实体
- 所有子实体的创建/更新/删除必须通过MedicalCase聚合根方法
- Server端Repository提供聚合根子实体操作方法（`CreatePrescriptionAsync`等）

**影响**：
- ✅ 业务规则集中在聚合根，保证一致性
- ✅ 明确事务边界和持久化边界
- ⚠️ Desktop端需要调整，通过聚合根Repository操作

**相关资源**：
- ADR-002（假设）
- `docs/explanation/business-rules.md` - 业务规则#3
- `docs/explanation/architecture/patterns/aggregate-root-pattern.md`

---

### 2025-10-24: Prescriptions/Consultation Repository层简化（ADR-003）

**背景**：MedicalCase聚合根模式后，Desktop端仍保留`IPrescriptionRepository`和`IConsultationRepository`，导致可以绕过聚合根直接操作

**决策**：
- **删除Desktop端Repository**：`IPrescriptionRepository`、`IConsultationRepository`
- **Read操作**：ViewModel直接依赖`IPrescriptionApi`（Refit接口）
- **Write操作**：ViewModel通过`IMedicalCaseRepository`聚合根方法操作

**影响**：
- ✅ 简化代码，减少20%Repository层代码
- ✅ 强制聚合根模式，防止跨聚合直接操作
- ❌ 违反Desktop三层架构（View→ViewModel→Repository→ApiClient）
- ⚠️ **架构例外**：批准Desktop三层架构违反（EXC-001）

**相关资源**：
- [ADR-003](./decisions/ADR-003-repository-simplification.md)
- [架构例外清单](./exceptions.md) - EXC-001
- Issue #1606, #1608

---

### 2025-10-25: Component设计指南（ADR-004）

**背景**：Issue #1608发现`PrescriptionCommandHandler`和`PrescriptionDataManager`存在过度设计问题

**决策**：
- 制定Component设计三原则：
  1. 跨模块共享优先
  2. 避免薄封装
  3. 职责清晰优先
- 删除薄封装Component（`PrescriptionCommandHandler`、`PrescriptionDataManager`）
- 明确允许的Component（`NotificationService`、`DialogService`、`NavigationService`）

**影响**：
- ✅ 简化架构，减少不必要的抽象层
- ✅ 降低学习成本，新成员容易理解何时创建Component
- ✅ 符合MVP原则（够用即好）

**相关资源**：
- [ADR-004](./decisions/ADR-004-component-design-guidelines.md)
- `docs/explanation/architecture/patterns/component-pattern.md`
- Issue #1608

---

### 2025-10-25: 架构文档治理体系建立（Issue #1610）

**背景**：架构决策分散在Issue和PR中，缺乏统一的记录和追溯机制

**决策**：
- **Phase 1**：建立ADR系统（Architecture Decision Records）
  - 创建ADR目录结构和模板
  - 回顾性记录ADR-003和ADR-004
  - 创建架构例外清单（`exceptions.md`）

- **Phase 2**：建立架构原则和设计模式文档
  - 创建架构原则三级分类体系（`principles.md`）
  - 创建设计模式文档（Repository/Component/Aggregate Root/MVVM）

- **Phase 3**：建立合规性检查和演进追踪
  - 创建合规性检查清单（`compliance-checklist.md`）
  - 创建架构演进时间线（`evolution.md`）

**影响**：
- ✅ 架构决策可追溯，新成员快速了解架构演进
- ✅ 架构例外正式管理，风险可控
- ✅ 架构原则明确，减少争议
- ✅ 设计模式文档化，提高代码一致性

**相关资源**：
- [ADR索引](./decisions/README.md)
- [架构例外清单](./exceptions.md)
- [架构原则](./principles.md)
- [设计模式](./patterns/)
- Issue #1610

---

## 📊 架构演进统计

### 架构决策记录（ADR）

| 时间 | ADR编号 | 标题 | 状态 | 影响范围 |
|------|---------|------|------|----------|
| 2025-10-15 | ADR-001 | 三层对齐架构标准 | ✅ Accepted | 全系统 |
| 2025-10-20 | ADR-002 | MedicalCase DDD聚合根模式 | ✅ Accepted | Server端 |
| 2025-10-24 | ADR-003 | Prescriptions/Consultation Repository层简化 | ✅ Accepted | Desktop端 |
| 2025-10-25 | ADR-004 | Component设计指南 | 📝 Proposed | Desktop端 |

### 架构例外记录

| 时间 | 例外编号 | 违反原则 | 风险级别 | 状态 |
|------|---------|---------|---------|------|
| 2025-10-24 | EXC-001 | Desktop三层架构 | P1 | ✅ Active |
| 2025-10-25 | EXC-002 | 保留跨模块Component | P2 | ✅ Active |

### 设计模式采用

| 模式 | 引入时间 | 适用范围 | 复杂度 |
|------|---------|---------|--------|
| Repository模式 | 项目初期 | Server端 + Desktop端（部分） | ⭐⭐ |
| MVVM模式 | 项目初期 | Desktop端 | ⭐⭐ |
| Aggregate Root模式 | 2025-10-20 | Server端 | ⭐⭐⭐ |
| Component模式 | 2025-10-25 | Desktop端（限制使用） | ⭐ |

---

## 🔮 未来演进规划

### 短期规划（1-2个月）

**目标**：巩固当前架构，完成MVP功能

**计划**：
- [ ] 完成Epic #1343（57个子任务）
- [ ] 审查并批准ADR-004（Component设计指南）
- [ ] 定期审查架构例外（EXC-001每半年，EXC-002每年）
- [ ] 补充缺失的单元测试和集成测试

### 中期规划（3-6个月）

**目标**：性能优化和用户体验提升

**可能的架构调整**：
- [ ] **可选**：恢复Desktop端Read-only Repository（如需添加缓存）
  - 创建`IPrescriptionRepository`（仅Read方法）
  - 实现`PrescriptionRepository`（薄封装API调用+可选缓存）
  - 移除EXC-001架构例外

- [ ] **可选**：引入离线支持（需要评审）
  - 评估Local Database方案（SQLite）
  - 创建ADR记录决策
  - 实现数据同步机制

- [ ] **可选**：性能优化（需要评审）
  - 分析N+1查询问题
  - 实现批量加载优化
  - 添加性能监控

### 长期规划（6个月以上）

**目标**：系统扩展和新功能支持

**可能的架构调整**：
- [ ] **评审**：跨平台支持（Avalonia）
  - 评估Avalonia可行性
  - 创建ADR记录决策
  - 实现跨平台UI组件

- [ ] **评审**：API版本管理
  - 引入API版本控制
  - 实现向后兼容机制

- [ ] **评审**：微服务拆分（⚠️ 需要重新评估MVP约束）
  - 当前禁止微服务架构（Constitution第2条）
  - 如确有必要，需要更新Constitution和ADR

---

## 🔗 相关资源

- **ADR索引**: [decisions/README.md](./decisions/README.md)
- **架构例外清单**: [exceptions.md](./exceptions.md)
- **架构原则**: [principles.md](./principles.md)
- **设计模式**: [patterns/](./patterns/)
- **业务规则**: [../business-rules.md](../explanation/business-rules.md)
- **Constitution**: `.spec-workflow/steering/constitution.md`

---

## 📅 更新日志

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-25 | v1.0 | 初始创建（记录2025-10-15至2025-10-25的演进） | Claude Code |

---

**最后更新**: 2025-10-25
**维护者**: 项目架构团队
