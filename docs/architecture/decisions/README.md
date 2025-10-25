# 架构决策记录（Architecture Decision Records）

**创建日期**: 2025-10-25
**维护者**: 项目架构团队
**目的**: 记录所有重要的架构决策，确保决策可追溯、可理解、可评审

---

## 📋 什么是ADR？

Architecture Decision Record（ADR）是一种轻量级的文档格式，用于记录软件项目中的重要架构决策。

**核心价值**：
- ✅ **可追溯性**：记录决策的背景、原因和后果
- ✅ **团队对齐**：确保所有成员理解架构选择
- ✅ **知识传承**：新成员可以快速了解架构演进历史
- ✅ **决策评审**：重大决策经过正式评审流程

---

## 🎯 使用场景

**必须创建ADR的场景**：
1. **架构模式变更**：如引入DDD聚合根、CQRS、Event Sourcing等
2. **技术栈调整**：如更换数据库、引入新框架、移除旧依赖
3. **架构例外批准**：如违反三层架构、跨聚合根直接操作
4. **重大重构决策**：如删除Repository层、合并模块、拆分服务
5. **性能优化方案**：如引入缓存、异步处理、批量操作

**可选创建ADR的场景**：
- 设计模式选择（如Strategy vs Template Method）
- 命名规范调整
- 文件组织方式变更

---

## 📝 ADR模板

参见 [template.md](./template.md)

---

## 📚 ADR索引

### 活跃ADR（Active）

| ADR编号 | 标题 | 状态 | 日期 | 影响范围 |
|---------|------|------|------|----------|
| [ADR-001](./ADR-001-three-tier-alignment.md) | 三层对齐架构标准 | ✅ Accepted | 2025-10-15 | 全系统 |
| [ADR-002](./ADR-002-ddd-aggregate-root.md) | MedicalCase DDD聚合根模式 | ✅ Accepted | 2025-10-20 | Server端（医案/诊疗/处方） |
| [ADR-003](./ADR-003-repository-simplification.md) | Prescriptions/Consultation Repository层简化 | ✅ Accepted | 2025-10-24 | Desktop端（处方/诊疗模块） |
| [ADR-004](./ADR-004-component-design-guidelines.md) | Component设计指南 | 📝 Proposed | 2025-10-25 | Desktop端（所有模块） |

### 已弃用ADR（Deprecated）

| ADR编号 | 标题 | 原状态 | 弃用日期 | 弃用原因 |
|---------|------|--------|----------|----------|
| 暂无 | - | - | - | - |

---

## 🔄 ADR状态说明

| 状态 | 图标 | 说明 |
|------|------|------|
| **Proposed** | 📝 | 提案阶段，等待评审 |
| **Accepted** | ✅ | 已批准，正在实施或已实施 |
| **Deprecated** | ⚠️ | 已弃用，被新决策替代 |
| **Superseded** | 🔄 | 被后续ADR取代 |

---

## 📐 ADR编写流程

### Step 1: 识别需要ADR的决策

**判断标准**：
- 是否影响系统架构？
- 是否需要多人协作实施？
- 是否有多个可选方案？
- 是否违反现有架构原则？

### Step 2: 使用模板创建ADR文档

```bash
# 复制模板
cp docs/architecture/decisions/template.md docs/architecture/decisions/ADR-XXX-short-title.md

# 编辑ADR内容
# 编号规则：ADR-001, ADR-002, ADR-003...
# 标题规则：简短、描述性、kebab-case
```

### Step 3: 填写ADR各部分内容

1. **标题**：简洁描述决策内容（如"三层对齐架构标准"）
2. **元数据**：
   - 日期：决策提出日期
   - 状态：Proposed / Accepted / Deprecated
   - 决策者：团队成员/架构师
   - 标签：#架构 #性能 #重构
3. **背景**：为什么需要做这个决策？遇到了什么问题？
4. **决策**：具体的决策内容，简洁明确
5. **后果**：决策的优缺点、风险、影响范围
6. **替代方案**（可选）：考虑过但未采纳的方案
7. **参考资料**（可选）：相关文档、Issue、PR

### Step 4: 提交评审（重大决策）

**需要评审的ADR**：
- 影响全系统的架构决策
- 违反现有架构原则的例外批准
- 引入新技术栈或移除旧依赖

**评审流程**：
1. 创建GitHub Issue关联ADR
2. 标记为`architecture`、`needs-review`
3. 等待架构师/团队评审
4. 评审通过后状态改为`Accepted`

### Step 5: 更新ADR索引

在本README中添加ADR条目到索引表。

---

## 🔗 相关资源

- **ADR工具和最佳实践**: https://adr.github.io/
- **架构文档提案**: `../shared/architecture-documentation-system-proposal.md`
- **架构例外清单**: [../exceptions.md](../exceptions.md)
- **架构原则文档**: `../principles.md`（计划中）

---

## 📞 联系方式

如有疑问或建议，请：
- 创建GitHub Issue（标签：`architecture`, `documentation`）
- 参考：`docs/architecture/shared/architecture-documentation-system-proposal.md`

---

**最后更新**: 2025-10-25
