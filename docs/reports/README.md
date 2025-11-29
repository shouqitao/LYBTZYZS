# Reports (报告) - 技术报告归档

> **归档目录**: 存放技术分析报告、重构报告、问题反思报告
> **适合人群**: 架构师、技术负责人、开发者
> **使用方式**: 查阅历史技术决策、学习问题解决经验

## 报告分类

### 问题修复反思报告

| 报告名称 | 日期 | 涉及模块 | 关键问题 |
|---------|------|---------|---------|
| [医案工作区问题修复反思](medicalcase-workspace-bug-reflection-2025-11-29.md) | 2025-11-29 | MedicalCase | RowVersion并发、PropertyChanged副作用 |

### 重构分析报告

| 报告名称 | 日期 | 涉及模块 | 主题 |
|---------|------|---------|------|
| [密码统一重构报告](password-unification-refactoring-report.md) | - | Auth/Users | 密码管理统一化 |

### 测试分析报告

| 报告名称 | 日期 | 涉及模块 | 主题 |
|---------|------|---------|------|
| [JWT测试失败分析](jwt-test-failure-analysis.md) | - | Auth | JWT Token测试问题 |
| [硬编码密码修复测试报告](hardcoded-password-fix-test-report.md) | - | Auth | 密码安全修复验证 |
| [测试失败报告](test-failure-report.md) | - | 多模块 | 测试失败汇总分析 |

### 文档重构报告

| 报告名称 | 日期 | 主题 |
|---------|------|------|
| [文档重构完成报告](docs-restructure-completion-report.md) | - | Diataxis框架迁移 |
| [文档重构分析](docs-restructure-analysis.md) | - | 文档结构分析 |
| [文档重构总结](docs-restructure-summary.md) | - | 重构成果总结 |
| [文档流程记忆总结](docs-process-memory-summary.md) | - | 文档流程知识 |

## 报告模板

新报告应遵循以下结构:

```markdown
# [报告标题]

**日期**: YYYY-MM-DD
**涉及模块**: [模块名称]
**相关Issue**: #[Issue编号]

---

## 一、问题描述/背景

## 二、分析过程

## 三、解决方案

## 四、经验教训

## 五、改进建议

---

**报告生成时间**: YYYY-MM-DD HH:MM CST
**报告作者**: [作者]
```

## 相关资源

- [故障排查指南](../how-to-guides/troubleshooting/)
- [技术规范](../reference/technical-specs/)
- [设计决策](../explanation/design-decisions/)

---

**目录类型**: Reports Index
**更新时间**: 2025-11-29
**维护团队**: 架构组
