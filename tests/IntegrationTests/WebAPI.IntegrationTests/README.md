# WebAPI.IntegrationTests

> WebAPI 端端到端集成测试，通过 WebApplicationFactory 验证控制器端点、中间件管道、日志集成

## 项目定位

- **层级**: IntegrationTests
- **被测模块**: LYBT.WebAPI
- **状态**: Active

## 测试文件

| 文件 | 被测类/端点 |
|------|------------|
| AuthControllerTests | /api/auth 认证端点 |
| BatchOperationsTests | 批量操作端点 |
| DiagnosticsControllerTests | /api/diagnostics 诊断端点 |
| FormulaControllerTests | /api/formula 验方端点 |
| HealthControllerTests | /api/health 健康检查端点 |
| HerbControllerTests | /api/herb 药材端点 |
| MedicalCasePermissionsTests | 医案权限控制 |
| PatientControllerTests | /api/patient 患者端点 |
| PendingQueueTests | 待处理队列端点 |
| SyncControllerTests | /api/sync 同步端点 |
| UserControllerTests | /api/user 用户端点 |
| LoggingIntegrationTests | 日志中间件集成 |
| MiddlewareIntegrationTests | 中间件管道集成 |

## 运行方式

```bash
dotnet test tests/IntegrationTests/WebAPI.IntegrationTests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.WebAPI, LYBT.Infrastructure, LYBT.Entities, LYBT.Shared.Models
- Microsoft.AspNetCore.Mvc.Testing
- 目标框架: net8.0

## 更新记录

- 2026-03-01: 创建 README
