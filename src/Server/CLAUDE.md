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
