# LYBT.WebAPI

> Server端 Web API 入口 | ASP.NET Core 8.0 | 统一 API 网关

## 项目定位

- **层级**: Server Services
- **职责**: 统一 API 网关，集成 6 个业务模块 (Auth/Users/Patients/MedicalCase/Herbs/Formula)，通过 RESTful API 提供中医诊所管理功能
- **状态**: Active

## 目录结构

```
LYBT.WebAPI/
├── Authorization/            # 授权策略与处理器
├── BackgroundServices/       # 后台服务 (缓存失效等)
├── Configuration/            # 配置模型
├── Controllers/              # API 控制器 (9 个)
├── Extensions/               # 服务注册 + 中间件管道配置
├── Filters/                  # 模型验证 + API 异常过滤器
├── HealthCheck/              # 数据库 + 自定义健康检查
├── Middleware/                # 异常处理 + 请求日志中间件
├── Services/                 # 应用层服务
├── Program.cs                # 启动入口
└── appsettings.*.json        # 环境配置 (默认/开发/测试)
```

## 核心组件

| 组件 | 说明 |
|------|------|
| ServiceCollectionExtensions | 统一入口：基础设施、认证、业务模块、API文档、控制器注册 |
| ExceptionHandlingMiddleware | 全局异常处理，统一 ApiResponse 错误返回 |
| RequestLoggingMiddleware | 结构化请求日志 (Serilog) |
| ValidateModelStateAttribute | FluentValidation 自动验证 DTO |
| HealthChecks | 数据库连接 + 业务逻辑健康检查 |

## 模块注册

```csharp
services.AddAuthModule(configuration);
services.AddUsersModule(configuration);
services.AddPatientsModule(configuration);
services.AddHerbsModule(configuration);
services.AddFormulaModule();
services.AddMedicalCaseModule();
services.AddSyncModule(configuration);
```

## API 端点概览

| 控制器 | 路由前缀 | 说明 |
|--------|----------|------|
| AuthController | /api/v1/auth | 登录/登出/Token刷新 |
| UsersController | /api/v1/users | 用户 CRUD/角色管理 |
| PatientsController | /api/v1/patients | 患者档案/搜索 |
| MedicalCasesController | /api/v1/medicalcases | 医案流程/状态迁移 |
| HerbsController | /api/v1/herbs | 药材搜索/导入导出 |
| FormulasController | /api/v1/formulas | 验方模板/克隆 |
| SyncController | /api/v1/sync | 数据同步 |
| DiagnosticsController | /api/v1/diagnostics | 诊断信息 |
| HealthController | /health | 健康检查 |

## 设计依据

- 薄 Controller 模式: Controller 仅负责 HTTP 协议转换，业务逻辑委托给各模块 Service 层
- 模块化服务注册: 每个模块通过静态扩展方法 (AddXxxModule) 自注册，WebAPI 层无需了解模块内部实现
- 统一响应格式 ApiResponse/PagedResult 包装所有 API 返回，前端无需处理不同格式
- 全局异常中间件拦截未处理异常，避免泄露内部错误信息到客户端
- Serilog 结构化日志 + 按日滚动文件，满足小型诊所的运维审计需求
- 速率限制 (RateLimiting) 防止登录端点暴力攻击

## 依赖关系

### 依赖
- LYBT.Infrastructure (AppDbContext, BaseRepository)
- LYBT.Entities (领域实体)
- LYBT.Shared.Models (DTO, ApiResponse)
- 所有 Module.* 业务模块

### 被依赖
- LYBT.Desktop.Shell (WPF 客户端通过 HTTP 调用)
- 测试项目 (Unit/Integration/Architecture)

## 快速启动

```bash
# 启动
dotnet run --project src/Server/Services/LYBT.WebAPI

# Swagger 文档
# https://localhost:7001/swagger

# 数据库迁移
dotnet ef database update \
  --project src/Server/Core/LYBT.Infrastructure \
  --startup-project src/Server/Services/LYBT.WebAPI
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 精简 README，添加设计依据章节 |
| 2025-12-04 | 按 README 规范重写文档 |
| 2025-10-29 | 初始版本 |
