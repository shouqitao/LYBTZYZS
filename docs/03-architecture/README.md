# 架构文档

## 概述

本目录包含凌隐宝堂中医诊所管理系统的完整架构文档。系统采用 Server/Shared/Client 三层架构，支持远程 (SQL Server) 和本地 (SQL Server，当前实现) 双模式运行。

## 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| 运行时 | .NET SDK | 8.0.406 |
| 后端框架 | ASP.NET Core Web API | 8.0 |
| ORM | Entity Framework Core | 8.0.20 |
| 远程数据库 | SQL Server | 2019+ |
| 本地数据库 | SQL Server (当前实现) | - |
| 桌面框架 | WPF | .NET 8 |
| MVVM 框架 | Prism | 9.0 |
| DI 容器 | DryIoc (via Prism) | - |
| UI 控件库 | HandyControl | 3.5.1 |
| 对象映射 | Riok.Mapperly | 4.1.1 |
| 输入验证 | FluentValidation | 12.0 |
| 认证 | JWT Bearer | - |
| 密码哈希 | BCrypt.Net-Next | 4.0.3 |
| 日志 | Serilog | - |
| 测试框架 | xUnit + NSubstitute + FluentAssertions | - |
| 包管理 | Central Package Management | - |

## 文档索引

| 文档 | 内容 |
|------|------|
| [system-overview.md](system-overview.md) | 系统整体架构图、解决方案结构、依赖方向 |
| [server.md](server.md) | 服务端三层架构: Controller -> Service -> Repository |
| [desktop.md](desktop.md) | 桌面端 MVVM + Prism 架构 |
| [shared.md](shared.md) | 共享层: DTO、工具类、组件 |
| [dual-mode.md](dual-mode.md) | 双模式架构: 远程 + 本地 |
| [data-model.md](data-model.md) | 数据模型: 实体关系、字段定义 |
| [configuration.md](configuration.md) | 配置架构: Options 模式、验证管道、环境分层 |
| [decisions/](decisions/) | 架构决策记录 (ADR) |

## 核心架构原则

1. **单向依赖** -- Server/Shared/Client 之间禁止循环依赖
2. **聚合根边界** -- MedicalCase 是唯一聚合根，Consultation 和 Prescription 通过它访问
3. **三层对齐** -- View/ViewModel/Service/Repository 命名保持一致
4. **贫血模型** -- 实体不包含业务逻辑，逻辑集中在 Service 层
5. **接口隔离** -- 所有服务通过接口注入，便于测试和替换

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 openspec 规范整合 |
