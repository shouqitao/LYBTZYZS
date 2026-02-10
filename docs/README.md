# 凌隐宝堂中医诊所管理系统 -- 文档中心

## 文档目录

| # | 目录 | 内容 | 文件数 |
|---|------|------|--------|
| 01 | [产品文档](01-product/) | 产品愿景、功能概览、用户角色、业务词汇表 | 4 |
| 02 | [需求文档](02-requirements/) | PRD (9 个模块，92 条功能需求) | 10 |
| 03 | [架构文档](03-architecture/) | 系统架构、数据模型、安全、ADR | 8 |
| 04 | [API 参考](04-api-reference/) | 全部 API 端点文档 (99 个端点) | 8 |
| 05 | [开发指南](05-development/) | 快速开始、编码规范、设计模式、测试 | 5 |
| 06 | [运维文档](06-operations/) | 部署、配置、监控、日志 | 1 |

**总计: ~36 个文档文件**

## 快速导航

### 新人上手

1. [快速开始](05-development/README.md) -- 5 分钟从零到运行
2. [环境搭建](05-development/setup.md) -- 详细配置步骤
3. [编码规范](05-development/code-standards.md) -- 必读规范

### 理解系统

1. [产品愿景](01-product/vision.md) -- 系统做什么
2. [功能概览](01-product/features.md) -- 9 大功能模块
3. [系统架构](03-architecture/system-architecture.md) -- 整体架构

### API 开发

1. [API 总览](04-api-reference/README.md) -- 通用格式、端点索引
2. [认证 API](04-api-reference/auth.md) -- Login / Token 刷新
3. [医案 API](04-api-reference/medical-cases.md) -- 核心业务

### 架构决策

1. [ADR 索引](03-architecture/adr/) -- 历史架构决策记录
2. [数据模型](03-architecture/data-model.md) -- 实体关系
3. [安全架构](03-architecture/security.md) -- 认证授权体系

## 文档约定

- 正文使用中文，技术标识符保留英文
- 每个文档底部包含变更记录表
- 需求文档使用 `FR-XXX` 编号体系 (Functional Requirement)
- 架构决策使用 `ADR-XXX` 编号体系

## 相关资源

| 资源 | 位置 |
|------|------|
| 设计/计划文档 | `docs/plans/` |
| Claude AI 配置 | `CLAUDE.md` + `.claude/rules/` |
| 解决方案文件 | `LYBT.All.sln` |

---

*文档版本: v1.0 | 最后更新: 2026-02-10*
