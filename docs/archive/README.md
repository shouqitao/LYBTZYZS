# 文档归档目录

## 📋 归档说明

本目录用于存放已完成、已废弃或已过期的文档,保持主文档目录的精简和时效性。

## 📂 目录结构

```
docs/archive/
├── requirements-completed-2025/    # 已完成实施的需求文档
├── discussions-deprecated-2025/    # 已废弃的讨论文档
└── tasks/                         # 旧版任务管理文件
```

## 🗂️ 归档策略

### 1. 已完成实施的需求文档

**归档条件**：
- GitHub Issue状态为"closed + completed"
- 功能已上线并稳定运行 >1个月
- 不再需要频繁参考

**归档位置**：`requirements-completed-{YEAR}/`

**示例**：
- ✅ `workstation-refactoring-requirements.md` - Issue #1513已完成（2025-10-21关闭）

### 2. 已废弃的讨论文档

**归档条件**：
- 讨论结果已被新方案替代
- 架构已重构,旧设计不再适用
- 明确标记为"废弃"或"已过时"

**归档位置**：`discussions-deprecated-{YEAR}/`

**示例**：
- （待识别）

### 3. 保留策略

**不归档的文档**：
- ✅ 记录架构演进过程的文档（即使包含旧方案,但有历史价值）
  - 例如：`medicalcase-fourstep-workflow-discussion.md`（记录四步→三步演进）
- ✅ 当前活跃的需求、设计、架构文档
- ✅ 未完成的Epic相关文档

## 📅 归档历史

| 日期 | 归档文档 | 原位置 | 归档原因 | 关联Issue |
|-----|---------|--------|---------|----------|
| 2025-01-24 | workstation-refactoring-requirements.md | docs/requirements/ | 已完成实施 | #1513 (closed) |

## 🔍 查询归档文档

如需查找归档文档,可使用以下命令：

```bash
# 按关键词搜索
grep -r "关键词" docs/archive/

# 查看归档列表
find docs/archive/ -name "*.md" -type f
```

## ♻️ 定期清理

建议每季度Review一次主文档目录,将符合归档条件的文档移至本目录。
