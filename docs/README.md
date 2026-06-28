# 凌隐宝堂中医诊所管理系统 -- 文档中心

## 文档目录

| # | 目录 | 内容 | 文件数 |
|---|------|------|--------|
| 01 | [产品文档](01-product/) | 产品愿景、用户画像、术语表 | 4 |
| 02 | [需求文档](02-requirements/) | PRD (9 模块，136 US) + NFR | 12 |
| 03 | [架构文档](03-architecture/) | 系统架构、数据模型、双模式、12 ADR、权限矩阵、业务流程 | 31 |
| 04 | [API 参考](04-api-reference/) | 全部 API 端点文档 | 14 |
| 05 | [开发指南](05-development/) | 快速开始、编码规范、测试标准、测试覆盖地图 | 26 |
| 06 | [运维文档](06-operations/) | 部署、配置、监控、备份、配置与密钥 | 13 |
| — | [compose/](compose/README.md) | 设计规格 / 实施计划 / 审查报告（工作流产物） | 90+ |
| — | [plans/](plans/README.md) | ⚠️ 已弃用，重定向至 compose/ | — |
| — | [training/](training/) | 培训材料 | 1 |

**总计: ~110 个核心文档 + 90+ compose 工作流产物**

## 快速导航

### 新人上手

1. [快速开始](05-development/01-setup.md) — 5 分钟从零到运行
2. [编码规范](05-development/03-code-standards.md) — 必读规范
3. [架构总览](03-architecture/00-architecture-summary.md) — 系统全景

### 理解系统

1. [产品愿景](01-product/01-vision.md) — 系统做什么
2. [功能概览](01-product/README.md) — 核心功能模块
3. [系统架构](03-architecture/01-system-overview.md) — 整体架构
4. [权限矩阵](03-architecture/12-permissions-matrix.md) — 谁能做什么
5. [业务流程](03-architecture/11-business-flows.md) — 关键流程

### 开发者

1. [测试标准](05-development/12-testing-standards.md) — 测试规范
2. [测试覆盖地图](05-development/13-test-coverage-map.md) — 已有/建议/空白
3. [配置与密钥](06-operations/10-variables-secrets.md) — 环境配置

### API 开发

1. [API 总览](04-api-reference/README.md) — 通用格式、端点索引
2. [认证 API](04-api-reference/01-auth.md) — Login / Token 刷新
3. [医案 API](04-api-reference/06-medical-cases.md) — 核心业务

### 架构决策

1. [ADR 索引](03-architecture/decisions/) — 架构决策记录 (12 条)
2. [数据模型](03-architecture/04-data-model.md) — 实体关系
3. [双模式架构](03-architecture/05-dual-mode.md) — 本地/远程双模式

## 文档约定

- 正文使用中文，技术标识符保留英文
- 需求文档使用 `US-{DOMAIN}-{NNN}` 编号体系（如 `US-MC-017`、`US-SHELL-010`）
- 架构决策使用 `ADR-NNNN` 编号体系（四位，如 `ADR-0001`）
- 配置项使用表格格式，标注风险等级

## 相关资源

| 资源 | 位置 |
|------|------|
| Compose 设计/计划 | `docs/compose/{specs,plans}/` |
| AI Agent 配置 | `AGENTS.md` (项目根) |
| 解决方案文件 | `LYBTZYZS.sln` |

---

*文档版本: v2.0 | 最后更新: 2026-06-28*
