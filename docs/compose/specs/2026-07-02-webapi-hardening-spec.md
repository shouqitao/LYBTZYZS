# WebAPI 完善设计规格

> 日期: 2026-07-02 | 基于全量扫描结果

## [S1] 问题

WebAPI 存在 15 个改进项，涵盖 P0-Critical 到 P3-Low，影响安全、性能、一致性和可维护性。

## [S2] P0 Critical (2项)

### CORS 缺失
`Program.cs` 未配置 `AddCors()`/`UseCors()`，Desktop 跨域请求被阻断。`appsettings.Production.json` 已有 Cors 配置但未使用。

### BuildServiceProvider 反模式
`ApiServiceCollectionExtensions.cs:59` 在 Swagger 注册中使用 `services.BuildServiceProvider()`，导致 DI 容器双重构建。

## [S3] P1 High (3项)

### OutputCache 未应用
6 个策略已注册 (HerbsCache, FormulasCache, PatientsCache, MedicalCaseCache, PrescriptionsCache, UserPermissionsCache)，仅 2 个端点使用。

### Rate Limiting 未应用
`ApiCalls` 策略 (100/min) 已注册但无端点使用。

### 响应格式不一致
- `FormulasController.GetById` 返回裸 `Forbid()` (403) 而非 ApiResponse
- `MedicalCaseProcessingController.CancelMedicalCase` 返回 `NoContent()` (204) 打破 ApiResponse 信封

## [S4] P2 Medium (8项)

### Serilog 请求日志缺失
缺少 `UseSerilogRequestLogging()`，生产环境无结构化 HTTP 日志。

### Redis 缓存未接入
`AddCachingServices()` 方法存在但未被调用。

### ProducesResponseType 缺失
9 个端点缺少 `[ProducesResponseType]` 属性。

### Validator 缺失
UpdateMedicalCase, FormulaBatchImport, HerbBatchImport, QuickVisit 4 个 Command 无 FluentValidation。

### DatabaseStartupDiagnostics 日志格式
使用 `$""` 字符串插值而非结构化日志模板。

### Redis 缓存方法死代码
`AddCachingServices()` 无调用方。

### Swagger XML 注释
SwaggerOptions 类缺失，XML 文档注释可能未映射。

### 两个 Result<T> 类型适配
PatientsController 中两种 Result 类型需要适配。

## [S5] P3 Low (2项)

### 重复健康检查
中间件 `/health` 和 Controller `/api/v1/health` 功能重叠。

### MedicalCases/Formulas 缺 OutputCache
注册了策略但未使用。

## [S6] 架构约束

- 所有修复必须通过 `dotnet build` 验证
- 不改变任何端点的 URL 或行为（纯内部改进）
- CORS 配置从 `appsettings.json` 读取
- 新 Validator 遵循 FluentValidation 模式
- 修复后运行架构测试 + 集成测试
