# Server 层

> ASP.NET Core 8.0 后端服务，提供 RESTful API、JWT 认证、模块化业务逻辑

## 架构概览

Server 层采用三层架构: Controller -> Service -> Repository -> DbContext。
Core 提供实体定义和基础设施（数据访问、安全、配置），Modules 实现业务逻辑，
WebAPI 作为统一网关对外暴露 API 端点。

所有 API 遵循 `/api/v{version}/[controller]` 路由规则，统一 `ApiResponse<T>` 响应格式。
模块通过扩展方法在 WebAPI 的 `Program.cs` 中注册。

## 项目列表

| 项目 | 职责 | 状态 |
|------|------|------|
| LYBT.Entities | 领域实体定义、业务规则、基类 | 稳定 |
| LYBT.Infrastructure | 数据访问、JWT安全、配置管理、通用仓储 | 稳定 |
| LYBT.Server.Interfaces | 跨模块服务接口定义 | 稳定 |
| LYBT.Module.Auth | 身份认证（登录、JWT管理、权限验证） | 稳定 |
| LYBT.Module.Users | 用户管理（CRUD、角色、密码策略） | 稳定 |
| LYBT.Module.Patients | 患者档案管理（基础信息、就诊历史） | 稳定 |
| LYBT.Module.MedicalCase | 医案管理（CQRS、状态机、聚合根） | 稳定 |
| LYBT.Module.Herbs | 中药材信息管理（价格、拼音检索） | 稳定 |
| LYBT.Module.Formula | 验方模板管理（方剂库、分享） | 稳定 |
| LYBT.Module.Sync | 数据同步服务 | 开发中 |
| LYBT.WebAPI | 统一 API 网关（控制器、中间件、启动入口） | 稳定 |

## 目录结构

```
src/Server/
├── Core/
│   ├── LYBT.Entities/
│   ├── LYBT.Infrastructure/
│   ├── LYBT.Server.Interfaces/
│   └── Documentation/
├── Modules/
│   ├── LYBT.Module.Auth/
│   ├── LYBT.Module.Users/
│   ├── LYBT.Module.Patients/
│   ├── LYBT.Module.MedicalCase/
│   ├── LYBT.Module.Herbs/
│   ├── LYBT.Module.Formula/
│   └── LYBT.Module.Sync/
└── Services/
    └── LYBT.WebAPI/
```

## 依赖关系

```
WebAPI -> Modules -> Infrastructure -> Entities
                  -> Server.Interfaces
Shared.Models (DTO契约) 被 Modules 和 WebAPI 引用
```

- **上游**: 为 Desktop 客户端和未来 Web 客户端提供 API
- **下游**: 连接 SQL Server / SQLite 数据库 (双模式，详见 docs/03-architecture/dual-mode.md)
- **平级**: 依赖 Shared 层的 DTO 和接口定义

## 快速启动

```bash
dotnet run --project src/Server/Services/LYBT.WebAPI
# Swagger: https://localhost:7001/swagger
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 精简 README，详细内容迁移至 CLAUDE.md |
| 2025-12-04 | 按 README 规范重写文档 |
