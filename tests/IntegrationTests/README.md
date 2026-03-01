# Legacy 集成测试

> 端到端业务流程验证 | 跨模块交互测试 | HTTP API 集成测试

## 项目列表

| 项目 | 测试范围 | 测试文件数 |
|------|----------|-----------|
| Client/Desktop/LYBT.Desktop.IntegrationTests | 桌面端 E2E (业务流程、模块 CRUD、导航、本地模式、HTTP 基础设施) | 16 |
| Server/Modules/LYBT.Module.Formula.IntegrationTests | 验方模块服务层集成 (FormulaService + 数据库) | 1 |
| WebAPI.IntegrationTests | Web API 控制器集成 (Auth, Batch, Diagnostics, CRUD, Sync, Logging, Middleware) | 18 |

**总计**: 3 个测试项目, 35 个测试文件

## 测试分类

**桌面端 E2E** -- EndToEnd/ 覆盖各模块完整业务流程; Foundation/ 覆盖 HTTP 重试、Token 刷新、认证; LocalMode/ 覆盖本地模式数据源切换和登录流程。

**Web API 集成** -- Controllers/ 覆盖全部 API 端点 (含权限控制、医生过滤、待处理医案等场景); Logging/ 覆盖数据库日志; Middleware/ 覆盖 CorrelationId 和 ProblemDetails。

## 与核心测试项目的关系

`LYBT.Tests.Server.Integration` 和 `LYBT.Tests.Desktop.Integration` 是新一代整合集成测试入口。本目录的项目提供更细粒度的模块级集成验证。

## 运行方式

全部运行: `dotnet test tests/IntegrationTests/ --recursive`

单项目: `dotnet test tests/IntegrationTests/WebAPI.IntegrationTests/`

## 更新记录

- 2026-03-01: 创建 README 文档
