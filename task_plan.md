# Task Plan: Sprint 7 - 权限矩阵修复与测试优化收尾

> **更新日期**: 2026-03-16
> **状态**: 🔄 Sprint 7 已规划，待开始
> **计划文件**: `~/.claude/plans/compressed-greeting-clover.md`

---

## 当前 Sprint (Sprint 7)

### 目标
整合 03-10 ~ 03-12 期间未完成的工作，完成高优先级任务并清空历史计划。

### Phase 1: 权限矩阵高优先级缺陷修复 (Day 1-3)

| Task | 问题 | 优先级 | 状态 |
|------|------|--------|------|
| 1.1 | D-4 Receptionist 医案可见性 | HIGH | 📝 待开始 |
| 1.2 | I-2 "当天可编辑"锁定逻辑 | HIGH | 📝 待开始 |
| 1.3 | G-9 Registration 回退流程 | HIGH | 📝 待开始 |
| 1.4 | G-11 医生禁用后挂号处理 | MEDIUM | 📝 待开始 |

### Phase 2: 测试性能优化收尾 (Day 4)

| Task | 描述 | 状态 |
|------|------|------|
| 2.1 | 批量迁移 MustHave 测试到 Transactional 模型 | 📝 待开始 |
| 2.2 | 执行性能基准测试 | 📝 待开始 |

### Phase 3: 验证与文档 (Day 5)

| Task | 描述 | 状态 |
|------|------|------|
| 3.1 | 全量测试运行 | 📝 待开始 |
| 3.2 | 归档当前计划文件 | 📝 待开始 |

---

## 已完成并归档

### Desktop 层重构优化 (2026-03-14 ~ 2026-03-15)

- **Phase 1-4 全部完成** - 详见归档: `docs/plans/archive/desktop-refactoring-2026-03-15/`
- **成果**: 新增 104 个测试，硬编码颜色减少 20%，循环依赖消除

---

## 历史计划归档

2026-03-10 ~ 03-12 期间的历史计划已整合到本 Sprint 7。详细历史状态见 `findings.md`。

| 计划文件 | 日期 | 归档状态 | 关键成果 |
|----------|------|----------|----------|
| Server Test PRD-Driven | 03-10 | ✅ 已整合 | 6 Domain Fixtures |
| Permission Matrix Defect | 03-10 | 📝 本 Sprint 实施 | 4个高优先级问题 |
| Test-Driven Implementation | 03-11 | 📝 本 Sprint 实施 | TDD 验证 |
| Test Architecture | 03-12 | ✅ 已整合 | Server.Unit 项目 |
| Integration Test Performance | 03-12 | 📝 本 Sprint 收尾 | 批量迁移 |

---

## References

- 归档目录: `docs/plans/archive/desktop-refactoring-2026-03-15/`
- 历史计划: `docs/plans/`
