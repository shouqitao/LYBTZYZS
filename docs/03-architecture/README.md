# 架构文档

> **用户速览**：这些文档描述系统「怎么搭的」。如果你是开发者，从 00 开始看；如果你只关心某个模块，直接跳到对应的 modules/ 子目录。
>
> **权威文档在哪**：数据模型 → [04-data-model.md](04-data-model.md)；任务/进度/决策 → [13-project-master-plan.md](13-project-master-plan.md)；当前状态 → [13c-current-status.md](13c-current-status.md)。完整查询指南见 [docs/README.md](../README.md#ai-查询指南)。

## 技术栈

> 版本号以 [`Directory.Packages.props`](../../Directory.Packages.props) 为唯一真相源。

| 组件 | 技术 | 版本 |
|------|------|------|
| 运行时 | .NET SDK | 8.0.400 |
| 后端 | ASP.NET Core | 8.0 |
| ORM | EF Core | 8.0.26 |
| 数据库 | SQL Server / LocalDB | — |
| 桌面 | WPF + Prism | 8.1.97 |
| UI | MaterialDesignThemes | 5.3.2 |
| 映射 | Riok.Mapperly | 4.3.1 |
| 验证 | FluentValidation | 12.1.1 |
| 认证 | JWT Bearer | — |
| 日志 | Serilog | — |

## 文档索引

### 核心文档

| # | 文档 | 一句话说明 |
|---|------|-----------|
| 00 | [架构总览](00-architecture-summary.md) | 全景速查 |
| 01 | [系统概览](01-system-overview.md) | 解决方案结构、依赖方向 |
| 02 | [桌面端](02-desktop.md) | MVVM + Prism 架构 |
| 03 | [服务端](03-server.md) | Controller→Service→Repository |
| 04 | [数据模型](04-data-model.md) | 实体关系、字段定义 |
| 05 | [双模式](05-dual-mode.md) | 远程 + 本地 |
| 06 | [错误处理](06-error-handling.md) | 全局异常、CorrelationId |
| 07 | [配置](07-configuration.md) | Options 模式、环境分层 |
| 08 | [共享层](08-shared.md) | DTO、工具类 |
| 09 | [安全架构](09-security-architecture.md) | JWT、授权策略 |
| 10 | [打印架构](10-printing-architecture.md) | 处方模板、PDF |
| 11 | [业务流程](11-business-flows.md) | 挂号→就诊→诊断→开方→打印 |
| 12 | [权限矩阵](12-permissions-matrix.md) | 角色 × 资源 × 操作 |
| 13 | [项目总账](13-project-master-plan.md) | 唯一全局视图（任务/阶段/决策/维护规则） |
| 13a | [数据模型](13a-data-model.md) | 核心实体 + 状态枚举 |
| 13b | [API 端点](13b-api-endpoints.md) | 全部模块端点 |
| 13c | [当前状态](13c-current-status.md) | Desktop 视图 + 已知问题 |
| 14 | [结构设计蓝图](14-structure-design-blueprint.md) | 全项目结构唯一权威（SSOT） |
| 15 | [Mapperly](15-mapperly.md) | 映射规范 |
| 16 | [同步协议](16-sync-protocol.md) | v2.0 数据同步设计 |

### 子目录

| 目录 | 内容 |
|------|------|
| [decisions/](decisions/) | ADR 架构决策记录（17 条，0001~0015 + 0017~0018；0016 预留跳号） |
| [modules/](modules/) | 各模块架构规格（10 个模块） |
| [localwebapi/](localwebapi/) | 本地模式 API 架构 |

> 过程文档（审计报告、任务书、计划）不在此目录，统一归档于 [../compose/](../compose/)（plans/reports/specs）。本目录只保留反映系统当前状态的文档。

## 核心架构原则

1. **单向依赖** — Server/Shared/Client 禁止循环依赖
2. **聚合根边界** — MedicalCase 是唯一聚合根
3. **三层对齐** — View/ViewModel/Service/Repository 命名一致
4. **贫血模型** — 实体不含业务逻辑（MedicalCase 例外）
5. **接口隔离** — 所有服务通过接口注入
