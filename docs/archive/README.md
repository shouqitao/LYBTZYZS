# 文档归档目录

本目录存放已废弃或迁移的历史文档，用于保留项目演进轨迹。

## 归档原则

- **归档时机**：文档体系重构、工具废弃、流程变更时
- **命名规范**：`{原目录名}-legacy-{归档日期YYYY-MM-DD}`
- **保留期限**：至少保留6个月，供历史追溯和回退参考
- **访问限制**：仅供查阅，禁止作为当前开发依据

## 归档清单

### spec-workflow-legacy-2025-11-09

**归档日期**：2025-11-09  
**原路径**：`.spec-workflow/`  
**归档原因**：文档系统整合（Issue #1933）

**迁移说明**：
- ✅ **steering/product.md** → 迁移至 `docs/explanation/product-vision.md`
- ✅ **steering/structure.md** → 迁移至 `docs/explanation/project-structure.md`
- ✅ **steering/constitution.md** → 内容已整合至 `docs/explanation/architecture/principles.md`
- ✅ **steering/tech.md** → 内容已整合至 `docs/explanation/architecture/principles.md` 和各ADR文档

**废弃内容**：
- `specs/` 目录：Spec工作流文档（已不再使用）
- `approvals/` 目录：审批流程文档（已不再使用）

**查阅方式**：
```bash
# 查看归档内容
ls docs/archive/spec-workflow-legacy-2025-11-09/

# 查看steering/核心文档
cat docs/archive/spec-workflow-legacy-2025-11-09/steering/product.md
```

**相关Issue**：[#1933 文档系统整合](https://github.com/shouqitao/LYBTZYZS/issues/1933)

---

## 历史归档索引

| 归档目录 | 归档日期 | 原因 | 相关Issue |
|---------|---------|------|----------|
| spec-workflow-legacy-2025-11-09 | 2025-11-09 | 文档系统整合，steering/文档迁移至docs/explanation/ | #1933 |

---

**维护说明**：
- 每次归档需更新本README的归档清单
- 归档目录命名必须包含日期后缀
- 重要迁移需在归档目录内创建MIGRATION.md说明迁移映射
