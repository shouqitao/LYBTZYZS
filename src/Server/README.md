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

## 开发笔记

# Server 层开发指南

## 技术栈

- ASP.NET Core 8.0 + EF Core 8.0 + SQL Server
- AutoMapper 13.0.1 | Serilog 8.0 | FluentValidation 12.0.0
- JWT 认证 (BCrypt 密码哈希) | Polly 弹性策略
- Swagger (Swashbuckle 6.9.0)

## 模块注册

在 `LYBT.WebAPI/Program.cs` 通过扩展方法注册:

```csharp
services.AddAuthModule(configuration);
services.AddUsersModule(configuration);
services.AddPatientsModule(configuration);
services.AddMedicalCaseModule();
services.AddHerbsModule(configuration);
services.AddFormulaModule();
```

## API 规范

- 路由: `[ApiVersion("1")]` + `[Route("api/v{version:apiVersion}/[controller]")]`
- 响应格式: `ApiResponse<T>` (Success, Message, Data, Timestamp, RequestId)
- 分页: `PagedResult<T>` (Items, TotalCount, PageIndex, PageSize, TotalPages)
- 认证: JWT Bearer Token，支持 Admin/Doctor 双角色

### 核心端点

| 控制器 | 路由前缀 | 功能 |
|--------|----------|------|
| AuthController | /api/v1/auth | 登录、登出、Token刷新 |
| UsersController | /api/v1/users | 用户 CRUD、角色管理 |
| PatientsController | /api/v1/patients | 患者档案、搜索 |
| MedicalCase* (4个控制器) | /api/v1/medicalcases | 医案流程、状态迁移、打印、审计 |
| HerbsController | /api/v1/herbs | 药材搜索、价格维护 |
| FormulasController | /api/v1/formulas | 验方模板、方剂库 |
| HealthController | /health | 健康检查 |

## 环境配置

```bash
# 数据库迁移
dotnet ef database update \
  --project src/Server/Core/LYBT.Infrastructure \
  --startup-project src/Server/Services/LYBT.WebAPI

# 运行
dotnet run --project src/Server/Services/LYBT.WebAPI
# Swagger: https://localhost:7001/swagger

# 测试 (Testing Trophy: 真实 SQL Server + Respawn, 零 mock)
dotnet test tests/LYBT.Tests.Server/
```

## 默认管理员

- 用户名: sysadmin
- 角色: Admin

## 开发注意事项

- EF Core 8 的 `FindAsync` 在实体不在 ChangeTracker 中时会应用全局查询过滤器 (IsDeleted)
- MedicalCase 是唯一聚合根，Consultation 和 Prescription 是内部实体
- 跨模块查询通过 `IPatientCrossModuleService` 实现，避免直接模块依赖
