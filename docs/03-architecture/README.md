# 架构文档

## 概述

本目录包含凌隐宝堂中医诊所管理系统的完整架构文档。系统采用 Server/Shared/Client 三层架构，支持远程 (SQL Server) 和本地 (嵌入式 LocalWebAPI + SQL Server) 双模式运行。

## 技术栈

> 版本号以 [`Directory.Packages.props`](../../Directory.Packages.props) 为唯一真相源。

| 组件 | 技术 | 版本 |
|------|------|------|
| 运行时 | .NET SDK | 8.0.400 (rollForward: latestMinor) |
| 后端框架 | ASP.NET Core Web API | 8.0 |
| ORM | Entity Framework Core | 8.0.26 |
| 远程数据库 | SQL Server | 2019+ |
| 本地数据库 | SQL Server LocalDB | - |
| 桌面框架 | WPF | .NET 8 |
| MVVM 框架 | Prism | 8.1.97 |
| DI 容器 | DryIoc (via Prism) | - |
| UI 控件库 | MaterialDesignThemes.XAML | 5.3.2 |
| 对象映射 | Riok.Mapperly | 4.3.1 |
| 输入验证 | FluentValidation | 12.1.1 |
| 认证 | JWT Bearer | - |
| 密码哈希 | BCrypt.Net-Next | 4.1.0 |
| 日志 | Serilog | - |
| 测试框架 | xUnit + NSubstitute + FluentAssertions | - |
| 包管理 | Central Package Management | - |

## 文档索引

| 文档 | 内容 |
|------|------|
| [00-architecture-summary.md](00-architecture-summary.md) | 架构总览速查（产品概览 / 技术栈 / 信任边界 / 已知风险） |
| [01-system-overview.md](01-system-overview.md) | 系统整体架构图、解决方案结构、依赖方向 |
| [02-desktop.md](02-desktop.md) | 桌面端 MVVM + Prism 架构 |
| [03-server.md](03-server.md) | 服务端三层架构: Controller -> Service -> Repository |
| [04-data-model.md](04-data-model.md) | 数据模型: 实体关系、字段定义 |
| [05-dual-mode.md](05-dual-mode.md) | 双模式架构: 远程 + 本地 |
| [06-error-handling.md](06-error-handling.md) | 错误处理架构（权威）: 全局异常处理、错误传播、CorrelationId 追踪 |
| [07-configuration.md](07-configuration.md) | 配置架构: Options 模式、验证管道、环境分层 |
| [08-shared.md](08-shared.md) | 共享层: DTO、工具类、组件 |
| [09-security-architecture.md](09-security-architecture.md) | 安全架构: JWT 认证、授权策略、Token 生命周期、安全响应头 |
| [10-printing-architecture.md](10-printing-architecture.md) | 打印架构: 处方模板、打印预览、PDF 导出 |
| [11-business-flows.md](11-business-flows.md) | 关键业务流程: 挂号→就诊→诊断→开方→打印 |
| [12-permissions-matrix.md](12-permissions-matrix.md) | 权限矩阵: 角色 × 资源 × 操作、行级安全、Policy 清单 |
| [13-error-handling-flow.md](13-error-handling-flow.md) | 错误处理流程速查（⚠️ 已过期，以 06-error-handling.md 为准） |
| [implementation-tasks.md](implementation-tasks.md) | v1.0 实现任务清单（缺口与待修复项） |
| [localwebapi/](localwebapi/) | LocalWebAPI 本地模式架构文档 |
| [decisions/](decisions/) | 架构决策记录 (ADR-0001 ~ ADR-0012，共 12 条) |

## 核心架构原则

1. **单向依赖** -- Server/Shared/Client 之间禁止循环依赖
2. **聚合根边界** -- MedicalCase 是唯一聚合根，Consultation 和 Prescription 通过它访问
3. **三层对齐** -- View/ViewModel/Service/Repository 命名保持一致
4. **贫血模型 (MedicalCase 除外)** -- 实体不包含业务逻辑（MedicalCase 例外：含 SoftDelete 等领域方法），逻辑集中在 Service 层
5. **接口隔离** -- 所有服务通过接口注入，便于测试和替换

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 openspec 规范整合 |
| 2026-06-12 | v1.1 | Mapperly 4.1.1→4.3.1; LocalDB 描述修正; 贫血模型原则补充 MedicalCase 例外 |
| 2026-06-28 | v1.2 | 技术栈版本对齐 Directory.Packages.props（Prism 8.1.97 / BCrypt 4.1.0 / FluentValidation 12.1.1 / EF Core 8.0.26）; 文档索引补 00/09/10/11/12/13/implementation-tasks; ADR 数量更正为 12 |
