# .spec-workflow 迁移说明

**迁移日期**：2025-11-09  
**相关Issue**：[#1933 文档系统整合](https://github.com/shouqitao/LYBTZYZS/issues/1933)  
**迁移原因**：消除多套文档体系，统一到docs/目录的Diátaxis框架

---

## 迁移映射表

### steering/ 核心文档迁移

| 原文档 | 新位置 | 迁移方式 | 状态 |
|-------|--------|----------|------|
| `steering/product.md` | `docs/explanation/product-vision.md` | 完整迁移 | ✅ 已完成 |
| `steering/structure.md` | `docs/explanation/project-structure.md` | 完整迁移 | ✅ 已完成 |
| `steering/constitution.md` | `docs/explanation/architecture/principles.md` | 内容整合 | ✅ 已完成 |
| `steering/tech.md` | `docs/explanation/architecture/principles.md` + ADR文档 | 内容整合 | ✅ 已完成 |

### specs/ 和 approvals/ 废弃

| 原目录 | 处理方式 | 原因 |
|-------|---------|------|
| `specs/` | 归档保留 | Spec工作流已废弃，改用GitHub Issues + 标准文档流程 |
| `approvals/` | 归档保留 | 审批流程已废弃，改用GitHub PR Review机制 |

---

## 文档查阅指引

### 如果你要查找...

#### **产品愿景和战略目标**
- ❌ 旧路径：`.spec-workflow/steering/product.md`
- ✅ 新路径：`docs/explanation/product-vision.md`
- 📖 访问：[产品愿景与战略目标](../../explanation/product-vision.md)

#### **项目结构与组织**
- ❌ 旧路径：`.spec-workflow/steering/structure.md`
- ✅ 新路径：`docs/explanation/project-structure.md`
- 📖 访问：[项目结构与组织指南](../../explanation/project-structure.md)

#### **项目宪法（技术约束）**
- ❌ 旧路径：`.spec-workflow/steering/constitution.md`
- ✅ 新路径：`docs/explanation/architecture/principles.md`（Level 0强制原则部分）
- 📖 访问：[架构原则](../../explanation/architecture/principles.md)

#### **技术决策和架构原则**
- ❌ 旧路径：`.spec-workflow/steering/tech.md`
- ✅ 新路径：
  - 技术栈选择：`docs/explanation/architecture/principles.md`
  - 具体技术决策：`docs/explanation/architecture/decisions/ADR-*.md`
- 📖 访问：
  - [架构原则](../../explanation/architecture/principles.md)
  - [ADR总览](../../explanation/architecture/decisions/README.md)

---

## 为什么废弃 .spec-workflow？

### 原问题
1. **多套文档体系并存**：.spec-workflow, docs/, .claude/ 三套体系，内容重复
2. **Spec工作流未实际使用**：specs/和approvals/目录创建后从未实际使用
3. **与GitHub Issues重复**：GitHub Issues + PR已能满足需求规格和审批需求
4. **steering/文档定位不清**：与docs/explanation/高度重复

### 新方案
1. **统一文档体系**：所有文档归入docs/目录，遵循Diátaxis框架
2. **GitHub Issues驱动**：需求和任务管理通过GitHub Issues
3. **PR Review审批**：代码审查通过GitHub PR Review机制
4. **Claude Skills自动化**：使用lybtzyzs-workflow-orchestrator自动化开发流程

### 迁移后的优势
- ✅ 单一文档入口：`docs/index.md`
- ✅ 清晰的文档分类：Tutorial/How-to/Reference/Explanation
- ✅ 文档与工具对齐：Claude Skills直接读取docs/
- ✅ 版本控制清晰：文档与代码同步演进

---

## 回退方案

如果发现迁移后有遗漏或问题，可以通过以下方式回退：

```bash
# 1. 恢复原.spec-workflow目录
cp -r docs/archive/spec-workflow-legacy-2025-11-09 .spec-workflow

# 2. 或仅恢复steering/文档
cp -r docs/archive/spec-workflow-legacy-2025-11-09/steering .spec-workflow/steering
```

**注意**：回退前请先创建GitHub Issue说明原因，并在Issue中讨论解决方案。

---

## 相关文档

- **文档整合报告**：`docs/reports/documentation-consolidation-phase1-analysis-2025-11-09.md`
- **新文档索引**：`docs/index.md`
- **架构文档总览**：`docs/explanation/architecture/README.md`

---

**最后更新**：2025-11-09  
**维护者**：项目团队
