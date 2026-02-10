# Research Findings: 文档体系重构

## 现状分析

### docs/ 目录审计结果
- **总文件数**: 608 个 markdown 文件
- **总行数**: 444,670 行
- **顶级目录**: 17 个 (含 18 个完全空的子目录)
- **核心问题**: 分类重叠、空目录、过程文档臃肿、缺少统一 PRD

### 主要来源分布
| 目录 | 文件数 | 行数 | 处理方式 |
|------|--------|------|----------|
| process/ | 216 | 114,905 | 全部删除 |
| reference/ | 142 | 124,459 | 提取 API/指南后删除 |
| state/ | 131 | 129,689 | 提取 ADR 后删除 |
| support/ | 29 | 18,218 | 运行时模板归 src/，其余删除 |
| explanation/ | 19 | 21,624 | 有价值内容合并后删除 |
| 其他 | 71 | 35,775 | 逐个评估 |

### OpenSpec 规范分析
- **48 个 spec 目录**，覆盖:
  - 功能规范 14 个 (authentication, medicalcase-lifecycle 等)
  - 架构规范 5 个 (project/server/client/shared/desktop-architecture)
  - 模式规范 15 个 (repository-patterns, service-conventions 等)
  - UI 规范 2 个
  - 清理规范 8 个
  - 其他 4 个
- **处理方式**: 业务规则 → 02-requirements/，架构规则 → 03-architecture/

## Brainstorm 决策记录

| 问题 | 用户选择 | 日期 |
|------|----------|------|
| 总体结构 | 6 目录扁平结构，数字前缀排序 | 2026-02-10 |
| OpenSpec 处理 | 合并到新体系，最终废弃 | 2026-02-10 |
| 根目录文件 | README 精简，CHANGELOG 保留，CLAUDE.md 不动 | 2026-02-10 |
| 旧文档清理 | 提取 → 合并 → 删除，只保留新体系 6 目录 | 2026-02-10 |
| 资源文件 | 运行时模板归 src/，文档图片归 docs/assets/ | 2026-02-10 |
| 语言规范 | 中文正文 + 英文技术标识 | 2026-02-10 |
| 双模式文档 | 每个需求功能必须有远程/本地对比，未决项标记"待讨论" | 2026-02-10 |
| 模式切换 | 手动触发 (已确定) | 2026-02-10 |
| 本地模式功能受限 | 待讨论深化 | 2026-02-10 |
| 数据同步策略 | 待讨论深化 | 2026-02-10 |

## 项目业务信息摘要

### 核心业务流程
1. 患者登记 → 创建 Patient
2. 开始诊疗 → 创建 MedicalCase (聚合根)
3. 中医诊断 → 填写 Consultation (望闻问切)
4. 开具处方 → 创建 Prescription + PrescriptionItems
5. 完成医案 → 保存完整 MedicalCase

### 8 个业务模块
Auth / Users / Patients / Herbs / Formulas / MedicalCase / Sync / Printing

### 双模式架构
| 维度 | 远程模式 | 本地模式 |
|------|----------|----------|
| 数据链路 | WPF → HTTP API → Controller → Service → SQL Server | WPF → DataSource → SQLite |
| 认证 | JWT Token (服务端验证) | LocalAuthService (本地验证) |
| 切换方式 | 手动触发 | 手动触发 |
| 同步 | 不需要 | 双向同步 (Sync 模块) |

### 待讨论项
- 本地模式功能受限范围
- 数据同步冲突解决策略

---
*Updated: 2026-02-10*
