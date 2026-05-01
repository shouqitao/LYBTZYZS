# LYBT.WebAPI 架构与设计分析报告

**报告版本**: v1.0  
**生成日期**: 2026-04-01  
**分析范围**: `src/Server/Services/LYBT.WebAPI/` 及其依赖链  
**代码基线**: 当前 working tree

---

## 目录

1. [执行摘要](#1-执行摘要)
2. [项目结构概览](#2-项目结构概览)
3. [依赖关系图](#3-依赖关系图)
4. [中间件管道评估](#4-中间件管道评估)
5. [安全态势评估](#5-安全态势评估)
6. [API 设计一致性](#6-api-设计一致性)
7. [错误处理策略](#7-错误处理策略)
8. [性能与缓存](#8-性能与缓存)
9. [配置管理](#9-配置管理)
10. [跨模块通信](#10-跨模块通信)
11. [架构问题与反模式](#11-架构问题与反模式)
12. [改进建议](#12-改进建议)
13. [附录](#13-附录)

---

## 1. 执行摘要

LYBT.WebAPI 是凌隐宝堂中医诊所管理系统的服务端入口，基于 ASP.NET Core 8.0 构建。整体架构成熟度处于 **中高水平**：

**优势**:
- 清晰的三层分离 (Controller → Service → Repository)，由架构测试强制执行
- 完善的安全基础设施 (JWT + 速率限制 + 安全头 + RBAC)
- 模块化的服务注册 (7 个 Extension 文件，职责清晰)
- 统一的响应格式 (`ApiResponse<T>`)
- 日志和可观测性完备 (CorrelationId + Serilog + 结构化日志)

**关注点**:
- ProblemDetails 基础设施存在但未被异常处理器使用，形成死代码
- 控制器间响应构造方式不一致 (Helper 方法 vs 直接构造)
- QuickVisit 工作流缺少事务边界
- 分页验证逻辑在多个控制器中重复
- 部分请求 DTO 内联定义在控制器文件中

**总体评分**: ★★★★☆ (4/5) — 架构基础扎实，改进空间集中在一致性和少量结构性问题。

---

## 2. 项目结构概览

### 2.1 文件组织

```
LYBT.WebAPI/
├── Program.cs                          # 入口 — Serilog 两阶段初始化 + 模块化 builder 配置
├── LYBT.WebAPI.csproj                  # .NET 8, net8.0 TFM
├── appsettings.json / .Development.json
├── Extensions/                         # 7 个服务注册扩展 (模块化 Composition Root)
│   ├── ServiceCollectionExtensions.cs           # 顶层协调器 — 调用其他 Extension
│   ├── ApiServiceCollectionExtensions.cs        # Controllers, JSON, Swagger, Versioning, CORS
│   ├── AuthenticationServiceCollectionExtensions.cs  # JWT, Policies, Rate Limiting
│   ├── DatabaseServiceCollectionExtensions.cs   # EF Core, DbContext, Health Checks
│   ├── UnifiedMiddlewareConfiguration.cs        # 中间件管道编排
│   ├── UnifiedApplicationInitialization.cs      # DB Migration, Seeding, Admin 创建
│   └── EnvironmentAwareHosting.cs               # Kestrel, HTTPS, 端口配置
├── Middleware/                          # 3 个自定义中间件
│   ├── CorrelationIdMiddleware.cs               # X-Correlation-Id 传播
│   ├── SecurityHeadersMiddleware.cs             # 安全响应头注入
│   └── ClaimsNormalizationMiddleware.cs         # JWT Claims 标准化
├── Filters/
│   └── ApiLoggingFilter.cs                      # 全局 Action Filter — 请求/响应日志
├── Configuration/
│   └── ProblemDetailsConfiguration.cs           # ProblemDetails 扩展配置
├── HealthCheck/
│   ├── SqlServerHealthCheck.cs                  # SQL Server 连接健康检查
│   └── DatabaseStartupDiagnostics.cs            # 启动时数据库诊断
├── BackgroundServices/
│   └── SecurityAuditCleanupService.cs           # 安全审计日志清理 (24h 周期)
├── Serialization/
│   └── LybtJsonContext.cs                       # System.Text.Json Source Generator
└── Controllers/                        # 13 个控制器
    ├── AuthController.cs                        # 认证 (Login/Register/Refresh/Auto)
    ├── UsersController.cs                       # 用户管理
    ├── PatientsController.cs                    # 患者管理
    ├── MedicalCasesController.cs                # 医案 CRUD + 查询 (Facade 模式)
    ├── MedicalCaseProcessingController.cs       # 医案流程操作
    ├── MedicalCasePrintController.cs            # 医案打印
    ├── MedicalCaseAuditController.cs            # 医案审计
    ├── HerbsController.cs                       # 药材管理
    ├── FormulasController.cs                    # 验方管理
    ├── RegistrationsController.cs               # 挂号管理 (Sprint 2)
    ├── SyncController.cs                        # 数据同步
    ├── HealthController.cs                      # 运行状态
    └── DiagnosticsController.cs                 # 诊断端点
```

### 2.2 控制器清单

| 控制器 | 路由前缀 | 方法数 | 授权策略 | 主要职责 |
|--------|----------|--------|----------|----------|
| AuthController | `/api/v1/auth` | 8 | 混合 (AllowAnonymous + RequireAuthenticated) | 登录/注册/Token 刷新 |
| UsersController | `/api/v1/users` | 8 | AdminOnly | 用户 CRUD + 密码重置 |
| PatientsController | `/api/v1/patients` | 10 | PatientAccess | 患者 CRUD + Excel 导入导出 |
| MedicalCasesController | `/api/v1/medicalcases` | 9 | DoctorOrAdmin | 医案 CRUD + 查询 |
| MedicalCaseProcessingController | `/api/v1/medicalcases` | 5 | DoctorOrAdmin | 状态流转 (确诊/开方/完成) |
| MedicalCasePrintController | `/api/v1/medicalcases` | 3 | DoctorOrAdmin | 打印数据获取 |
| MedicalCaseAuditController | `/api/v1/medicalcases` | 3 | AdminOnly | 审计日志查询 |
| HerbsController | `/api/v1/herbs` | 8 | DoctorOrAdmin | 药材 CRUD |
| FormulasController | `/api/v1/Formulas` | 7 | DoctorOrAdmin | 验方 CRUD |
| RegistrationsController | `/api/v1/registrations` | 7 | DoctorOrAdmin | 挂号 + 快速就诊 |
| SyncController | `/api/v1/sync` | 6 | RequireAuthenticated | 双向数据同步 |
| HealthController | `/api/v1/health` | 2 | AllowAnonymous | 健康检查 |
| DiagnosticsController | `/api/v1/diagnostics` | 2 | SuperAdminOnly | 系统诊断 |

**合计**: 78 个端点 (13 个控制器)

### 2.3 关键架构特征

- **所有控制器集中于 WebAPI 项目** — 各业务模块项目仅包含 Service/Interface，不含 Controller
- **4 个控制器共享 `/api/v1/medicalcases` 路由前缀** — 这是前期 MedicalCaseController 拆分的结果，通过不同 HTTP 动词和路径后缀区分
- **API 版本化** — 使用 `Asp.Versioning.Mvc`，当前仅 v1
- **PascalCase JSON** — 匹配 WPF 客户端 DTO 命名约定

---

## 3. 依赖关系图

### 3.1 项目引用链

```
LYBT.WebAPI (Composition Root)
├── LYBT.Module.Auth           → Service, Repository, Validator
├── LYBT.Module.User           → Service, Repository, Validator
├── LYBT.Module.Patient        → Service, Repository, Validator, Mapper
├── LYBT.Module.MedicalCase    → Service, Facade, Repository, Validator, Mapper
├── LYBT.Module.Herb           → Service, Repository, Validator
├── LYBT.Module.Formula        → Service, Repository, Validator
├── LYBT.Module.Sync           → Service, Repository
├── LYBT.Module.Registration   → Service, Repository, Validator (Sprint 2)
├── LYBT.Infrastructure        → DbContext, BaseRepository, CrossModuleService, Web/BaseApiController
├── LYBT.Shared.Models         → DTOs, ApiResponse, Enums (被所有层引用)
├── LYBT.Shared.ExceptionHandling → BusinessExceptionHandler, SystemExceptionHandler
└── LYBT.Entities              → 领域实体, 值对象
```

### 3.2 NuGet 依赖 (关键)

| 包 | 版本 | 用途 |
|----|------|------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.x | ORM (SQL Server provider) |
| Asp.Versioning.Mvc.ApiExplorer | 8.1.0 | API 版本管理 |
| FluentValidation.AspNetCore | 11.x | 请求验证 |
| Serilog.AspNetCore | 8.x | 结构化日志 |
| Swashbuckle.AspNetCore | 6.x | Swagger/OpenAPI |
| DotNetEnv | 3.1.1 | .env 文件加载 |
| Microsoft.Extensions.Caching.StackExchangeRedis | 8.0.x | Redis 缓存 (可选) |

### 3.3 依赖规则 (由架构测试强制执行)

```
✅ Controller → Service (via Interface)
✅ Service → Repository (via Interface)
✅ Repository → DbContext
✅ Any layer → Shared.Models (DTOs, Enums)
❌ Controller ✗→ Repository (禁止跨层)
❌ Module A ✗→ Module B (禁止直接模块间引用)
✅ Module A → CrossModuleService (via ISP Interface) → Module B
```

**评价**: 依赖规则清晰且有架构测试保障，这是高于平均水平的工程实践。

---

## 4. 中间件管道评估

### 4.1 管道编排 (UnifiedMiddlewareConfiguration.cs)

```
请求入站 →
  ① ExceptionHandler (IExceptionHandler chain)
  ② StatusCodePages
  ③ CorrelationIdMiddleware          ← 自定义: X-Correlation-Id
  ④ UseHttpsRedirection
  ⑤ SecurityHeadersMiddleware        ← 自定义: 安全响应头
  ⑥ ResponseCompression
  ⑦ UseRouting
  ⑧ UseRateLimiter
  ⑨ UseAuthentication
  ⑩ ClaimsNormalizationMiddleware    ← 自定义: JWT Claims 标准化
  ⑪ UseAuthorization
  ⑫ UseResponseCaching / UseOutputCache
  ⑬ MapHealthChecks
  ⑭ MapControllers
→ 响应出站
```

### 4.2 排序正确性分析

| 检查项 | 状态 | 说明 |
|--------|------|------|
| ExceptionHandler 在最外层 | ✅ 正确 | 确保所有异常被捕获 |
| CorrelationId 在 ExceptionHandler 之后 | ✅ 正确 | 异常响应也携带 CorrelationId |
| SecurityHeaders 在路由之前 | ✅ 正确 | 所有响应都添加安全头 |
| Authentication 在 Authorization 之前 | ✅ 正确 | 标准顺序 |
| ClaimsNormalization 在两者之间 | ✅ 正确 | 认证后、授权前标准化 Claims |
| ResponseCompression 在路由之前 | ✅ 正确 | 压缩所有响应 |
| RateLimiter 在路由之后 | ✅ 正确 | 需要路由信息来匹配策略 |
| OutputCache 在授权之后 | ✅ 正确 | 缓存尊重授权结果 |

**评价**: 管道排序完全正确，没有发现顺序问题。三个自定义中间件各司其职，无冗余。

### 4.3 各中间件详评

**CorrelationIdMiddleware**: 
- 优先读取请求头 `X-Correlation-Id`，否则生成 `Guid.NewGuid()`
- 同时设置 `traceparent` (W3C Trace Context)
- 注入 `Serilog.Context.LogContext` 实现日志关联
- ✅ 实现完善

**SecurityHeadersMiddleware**:
- 注入 7 个安全响应头: `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, `Content-Security-Policy`, `Strict-Transport-Security`, `Referrer-Policy`, `Permissions-Policy`
- CSP 策略: `default-src 'self'`
- ✅ 覆盖全面，符合 OWASP 建议

**ClaimsNormalizationMiddleware**:
- 统一 JWT Claims 命名 (多标准兼容)
- 处理 `ClaimTypes.NameIdentifier` / `sub` / `userId` → 统一为 `sub`
- 处理 `ClaimTypes.Role` / `role` → 统一为 `role`
- ✅ 解决了 JWT Claims 命名不一致的常见痛点

---

## 5. 安全态势评估

### 5.1 认证机制

```
认证流程:
  Login → JWT AccessToken (60min) + RefreshToken (7d)
  AutoLogin → AutoLoginToken (30d) → 换取正常 Token 对
  Token 刷新 → RefreshToken → 新 AccessToken + 新 RefreshToken (旋转)
```

| 安全项 | 配置 | 评价 |
|--------|------|------|
| 签名算法 | HMAC-SHA256 | ✅ 对称签名，适合单体应用 |
| Token 有效期 | 60 分钟 | ✅ 合理 |
| RefreshToken 有效期 | 7 天 | ✅ 合理 |
| RefreshToken 旋转 | ✅ 每次刷新生成新 Token | ✅ 防止 Token 重用 |
| ClockSkew | appsettings.json = 300s | ⚠️ 见下方 |
| HTTPS 强制 | ✅ HttpsRedirection + HSTS | ✅ |
| 密钥来源 | 环境变量 `JWT_SECRET` | ✅ 不硬编码 |

### 5.2 授权策略

```csharp
// 5 个命名策略 + 1 个 Fallback
AdminOnly         = SuperAdmin, Admin
DoctorOrAdmin     = SuperAdmin, Admin, Doctor
PatientAccess     = SuperAdmin, Admin, Doctor, Receptionist
SuperAdminOnly    = SuperAdmin
RequireAuthenticated = 任意已认证用户
FallbackPolicy    = 默认需要认证 (未标记 [AllowAnonymous] 的端点自动生效)
```

**评价**: 
- ✅ FallbackPolicy 是重要的安全默认值 — 防止忘记标注 `[Authorize]` 导致端点裸露
- ✅ 策略命名清晰，角色层次合理
- ✅ 策略覆盖了所有业务场景

### 5.3 速率限制

| 策略 | 限制 | 窗口 | 分区键 |
|------|------|------|--------|
| Login | 5 次 | 60 秒 | Remote IP |
| ApiCalls | 100 次 | 60 秒 | Remote IP |

**评价**: 
- ✅ Login 端点有独立的更严格限制
- ⚠️ 仅按 IP 分区 — 在 NAT/代理环境下可能误伤，但对诊所场景可接受

### 5.4 已识别安全问题

| # | 问题 | 严重度 | 说明 |
|---|------|--------|------|
| S-1 | ClockSkew 配置不一致 | 低 | `appsettings.json` 配置 300s，但 `CLAUDE.md` 文档记录为 Zero。实际运行值取决于代码中读取哪个配置。300s 的 ClockSkew 在高安全场景偏大，建议统一为 30-60s。 |
| S-2 | AutoLoginToken 有效期 30 天 | 低 | 长期 Token 增加被盗用风险。可考虑绑定设备指纹或缩短有效期。当前诊所内网场景下风险可控。 |

---

## 6. API 设计一致性

### 6.1 响应格式

所有端点统一返回 `ApiResponse<T>` 格式：

```json
{
  "success": true,
  "data": { ... },
  "message": "操作成功",
  "errorCode": null,
  "errors": null,
  "timestamp": "2026-04-01T10:00:00+08:00"
}
```

分页响应使用 `PagedApiResponse<T>`:

```json
{
  "success": true,
  "data": [ ... ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8,
  "message": null
}
```

**评价**: ✅ 响应格式统一，前端可预期。PascalCase 命名与 WPF 客户端一致。

### 6.2 响应构造方式不一致

虽然最终格式统一，但构造方式存在两种模式：

**模式 A — BaseApiController Helper 方法** (推荐模式):
```csharp
return Success(dto);                    // 200
return HandleResult(result);            // 自动映射 ErrorCode → HTTP Status
return SuccessPaged(items, total, page, size);
```

**模式 B — 直接构造** (遗留模式):
```csharp
return Ok(ApiResponse<T>.CreateSuccess(dto));
return Ok(new ApiResponse<T> { Success = true, Data = dto });
```

| 控制器 | 主要模式 | 混用情况 |
|--------|----------|----------|
| AuthController | A | 少量 B |
| PatientsController | A | 基本纯净 |
| MedicalCasesController | A | 基本纯净 (Facade 模式后重构) |
| RegistrationsController | A + B | 混用较多 (Sprint 2 新代码) |
| HerbsController | A | 少量 B |
| FormulasController | B 为主 | 较多直接构造 |

**评价**: ⚠️ 不影响功能，但增加维护认知负担。建议统一为模式 A。

### 6.3 路由命名

| 控制器 | 路由定义方式 | 实际路径 |
|--------|-------------|----------|
| 大多数控制器 | `[Route("api/v{version:apiVersion}/[controller]")]` | `/api/v1/patients` |
| FormulasController | `[Route("api/v{version:apiVersion}/Formulas")]` | `/api/v1/Formulas` |

**评价**: ⚠️ FormulasController 硬编码路由名为大写 `Formulas`，其他控制器使用 `[controller]` 占位符 (自动小写)。功能无影响，但不一致。

### 6.4 内联 DTO 问题

部分控制器在文件内定义请求 DTO:

```csharp
// MedicalCaseProcessingController.cs 内部
public record UpdateStatusRequest(string Status, string? Reason);
public record CancelMedicalCaseRequest(string Reason);
```

**评价**: ⚠️ 违反了 "DTO 统一放在 `LYBT.Shared.Models`" 的约定。这些内联 DTO 无法被其他层 (如测试、客户端) 重用。

### 6.5 分页参数验证

多个控制器重复相同的分页验证逻辑:

```csharp
if (page <= 0 || pageSize <= 0 || pageSize > 100)
    return ValidationFail("分页参数无效");
```

出现在: PatientsController, MedicalCasesController, HerbsController, FormulasController, RegistrationsController

**评价**: ⚠️ 典型的 DRY 违反。应提取到 BaseApiController 或创建分页参数验证器。

---

## 7. 错误处理策略

### 7.1 异常处理链

ASP.NET Core 8 的 `IExceptionHandler` 链式处理:

```
异常发生 →
  ① BusinessExceptionHandler
     ├── 匹配 AppException 及其子类
     ├── Warning 级别日志
     ├── 返回 ApiResponse JSON (ErrorCode + Message)
     └── HTTP 状态码由异常类型决定:
         ├── BusinessException → 400
         ├── ValidationException → 400
         ├── NotFoundException → 404
         ├── ConflictException → 409
         ├── UnauthorizedException → 401
         └── ApiException → 自定义 StatusCode
  ② SystemExceptionHandler (fallback)
     ├── 捕获所有未处理异常
     ├── Error 级别日志
     ├── Development 环境: 返回 StackTrace
     ├── Production 环境: 返回通用错误信息
     └── 异常类型映射:
         ├── OperationCanceledException → 499
         ├── UnauthorizedAccessException → 401
         ├── ArgumentException → 400
         ├── InvalidOperationException → 422
         ├── TimeoutException → 504
         └── 其他 → 500
```

**评价**: 
- ✅ 双层处理链设计优秀 — 业务异常和系统异常分别处理
- ✅ AppException 层次结构清晰，覆盖常见业务场景
- ✅ ExceptionFactory 提供领域特定的工厂方法，异常创建语义清晰

### 7.2 ProblemDetails vs ApiResponse 二元性

**现状**:
- 异常处理器 (`BusinessExceptionHandler`, `SystemExceptionHandler`) 返回 `ApiResponse` 格式
- `ProblemDetailsConfiguration.cs` 存在并配置了 ProblemDetails 扩展字段 (correlationId, timestamp, traceId)
- `Shared.ExceptionHandling` 中存在多个 ProblemDetails 相关类 (ProblemDetailsFactory, ProblemDetailsExtensions, ExceptionSeverityMapper)
- 控制器和客户端均按 `ApiResponse` 格式解析

**结论**: ProblemDetails 基础设施是 **死代码** — 已配置但从未被异常处理器使用。异常处理器完全绕过 ProblemDetails，直接返回 ApiResponse JSON。

| 组件 | 状态 | 建议 |
|------|------|------|
| BusinessExceptionHandler | ✅ 活跃 | 保留 |
| SystemExceptionHandler | ✅ 活跃 | 保留 |
| ProblemDetailsConfiguration.cs | ⚠️ 死代码 | 移除或整合 |
| ProblemDetailsFactory | ⚠️ 死代码 | 移除 |
| ProblemDetailsExtensions | ⚠️ 死代码 | 移除 |
| ExceptionSeverityMapper | ⚠️ 死代码 | 移除 |
| AddServerExceptionHandling extension | ⚠️ 可疑 | 验证是否被调用 |

### 7.3 控制器级错误处理

控制器内 **不再使用 try-catch** (此前优化已移除)，全部委托给全局异常处理器。

**评价**: ✅ 正确做法。控制器只负责参数验证和调度，异常统一在管道中处理。

---

## 8. 性能与缓存

### 8.1 缓存策略

```csharp
// OutputCache 策略 (配置于 ApiServiceCollectionExtensions.cs)
HerbList      → 300s (5 min), VaryByQueryKeys: ["page", "pageSize", "search", "category"]
FormulaList   → 300s (5 min), VaryByQueryKeys: ["page", "pageSize", "search", "category"]
PatientList   → 120s (2 min), VaryByQueryKeys: ["page", "pageSize", "search"]
StaticData    → 3600s (1 hour), 无 VaryBy

// MemoryCache (服务层内部)
各模块 Service 使用 IMemoryCache 缓存热数据

// Redis (可选)
配置存在但有 fallback — Redis 不可用时降级为 MemoryCache
```

**评价**: 
- ✅ OutputCache 策略合理，VaryByQueryKeys 正确包含分页和搜索参数
- ✅ 缓存时间根据数据变更频率分级 (静态数据 1h, 药材 5m, 患者 2m)
- ⚠️ 缓存失效策略: 写操作后是否主动 evict OutputCache? 需验证

### 8.2 响应压缩

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

**评价**: ✅ Brotli + Gzip 双压缩，HTTPS 下启用压缩。

### 8.3 数据库性能

- EF Core 默认 `AsNoTracking()` (通过 BaseRepository 强制)
- 查询分页在数据库层执行 (`Skip().Take()`)
- 全局查询过滤器 `IsDeleted` (软删除)
- ⚠️ 未见 `AsSplitQuery()` 在导航集合查询中的使用 — 可能有 N+1 风险

### 8.4 请求体限制

```csharp
options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
```

**评价**: ✅ 已配置全局请求体限制，防止大请求攻击。对 Excel 导入场景 10MB 足够。

---

## 9. 配置管理

### 9.1 配置架构

```
配置源优先级 (低 → 高):
  appsettings.json
  → appsettings.{Environment}.json
  → .env 文件 (via DotNetEnv)
  → 环境变量
  → 命令行参数
```

### 9.2 强类型配置 (Options Pattern)

| Options 类 | 用途 |
|------------|------|
| JwtSettings | JWT 签名、有效期、发行者 |
| DatabaseSettings | 连接字符串、超时、池大小 |
| CacheSettings | Redis 连接、缓存策略时间 |
| PasswordPolicySettings | 密码强度要求 (dev vs production) |
| SerilogSettings | 日志级别、Sink 配置 |
| RateLimitSettings | 速率限制窗口和数量 |

**评价**: ✅ 全面使用 Options Pattern，无魔法字符串。

### 9.3 生产配置验证

```csharp
// UnifiedApplicationInitialization.cs
// 启动时验证关键配置项
Critical: JWT_SECRET 非空、连接字符串有效
Important: HTTPS 证书、日志路径可写
```

**评价**: ✅ 启动时 Fail-Fast，防止配置错误导致运行时故障。

---

## 10. 跨模块通信

### 10.1 设计模式

```
模块间通信通过 ISP (Interface Segregation Principle) 实现:

CrossModuleService (单一实现类)
  ├── implements IPatientCrossModuleService    → 患者查询供其他模块使用
  ├── implements IHerbCrossModuleService       → 药材查询供处方模块使用
  ├── implements IUserCrossModuleService       → 用户查询供审计模块使用
  └── implements ICrossModuleAuthService       → 认证信息供其他模块使用

DI 注册 (forwarding pattern):
  services.AddScoped<CrossModuleService>();
  services.AddScoped<IPatientCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
  services.AddScoped<IHerbCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
  // ... 同一实例，不同接口
```

**评价**: 
- ✅ ISP 接口隔离 — 每个模块只能看到它需要的跨模块操作
- ✅ 单实例 forwarding — 避免重复创建
- ⚠️ 所有跨模块通信集中在一个实现类中 — 如果模块增长，可能需要拆分

### 10.2 RegistrationsController 的跨模块调用

```csharp
// QuickVisit 端点 — 创建挂号 + 创建医案
var registration = await _registrationService.CreateAsync(dto);
var medicalCase = await _medicalCaseCommandService.CreateFromRegistrationAsync(registration.Id);
```

**评价**: ⚠️ 两步操作无事务包裹。如果第二步失败，会产生孤立的挂号记录。详见 §11.2。

---

## 11. 架构问题与反模式

### 11.1 [P2] 响应构造方式不一致

**问题**: 两种 ApiResponse 构造方式共存 (BaseApiController Helper vs 直接构造)。

**影响**: 
- 新开发者不确定该用哪种方式
- 直接构造可能遗漏标准字段 (如 timestamp)
- 代码审查时需要记住两套 API

**建议**: 统一为 BaseApiController Helper 方法，对遗留的直接构造逐步迁移。

### 11.2 [P2] QuickVisit 缺少事务边界

**问题**: `RegistrationsController.QuickVisit` 执行两步操作 (创建挂号 → 创建医案)，但未包裹在事务中。

**风险**: 
- 第二步失败 → 数据库中存在无对应医案的挂号记录
- 由于 EF Core `SaveChangesAsync` 在每个 Service 内部调用，两步操作分属不同事务

**建议**: 
```csharp
using var transaction = await _dbContext.Database.BeginTransactionAsync();
try
{
    var registration = await _registrationService.CreateAsync(dto);
    var medicalCase = await _medicalCaseCommandService.CreateFromRegistrationAsync(registration.Id);
    await transaction.CommitAsync();
    return Success(new QuickVisitResponse(registration.Id, medicalCase.Id));
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 11.3 [P3] ProblemDetails 死代码

**问题**: `Shared.ExceptionHandling` 中存在 4+ 个 ProblemDetails 相关类从未被异常处理器使用。

**影响**: 
- 增加代码库认知负担
- 误导开发者认为系统使用 ProblemDetails 格式
- 维护成本 (依赖更新时也需要考虑这些类)

**建议**: 
- 方案 A: 移除所有 ProblemDetails 相关死代码 (推荐)
- 方案 B: 迁移异常处理器到 ProblemDetails 格式 (RFC 7807 合规，但需同步修改客户端)

### 11.4 [P3] 分页验证重复

**问题**: `page <= 0 || pageSize <= 0 || pageSize > 100` 在 5+ 个控制器中重复出现。

**建议**: 
```csharp
// BaseApiController 中添加
protected IActionResult? ValidatePagination(int page, int pageSize, int maxPageSize = 100)
{
    if (page <= 0 || pageSize <= 0 || pageSize > maxPageSize)
        return ValidationFail($"分页参数无效: page={page}, pageSize={pageSize}");
    return null; // 验证通过
}
```
或使用 FluentValidation 的 PaginationRequestValidator。

### 11.5 [P3] 内联 DTO

**问题**: 部分请求 DTO 定义在 Controller 文件内部，而非 `LYBT.Shared.Models`。

**影响**: 
- 测试项目需要引用 WebAPI 项目才能使用这些 DTO
- 客户端无法重用
- 违反 "Shared.Models 是 DTO 唯一定义位置" 的约定

**建议**: 迁移至 `LYBT.Shared.Models` 对应模块目录。

### 11.6 [P4] FormulasController 路由大写

**问题**: `[Route("api/v{version:apiVersion}/Formulas")]` 硬编码大写 `F`。

**影响**: URL 大小写不一致 (`/api/v1/patients` vs `/api/v1/Formulas`)。HTTP URL 技术上大小写不敏感，但不一致影响开发体验。

**建议**: 改为 `[Route("api/v{version:apiVersion}/[controller]")]`。

### 11.7 [P4] 4 个控制器共享路由前缀

**问题**: MedicalCases, MedicalCaseProcessing, MedicalCasePrint, MedicalCaseAudit 都使用 `/api/v1/medicalcases` 路由前缀。

**影响**: 
- Swagger 文档中这些端点混在一起
- 新开发者难以定位某个端点在哪个控制器中
- 路由冲突的维护风险

**评价**: 这是前期大控制器拆分的合理结果。通过 HTTP 方法和路径后缀区分 (如 `/print`, `/audit`)，实际不会冲突。可通过 Swagger Tag 分组改善文档体验。

---

## 12. 改进建议

### 按优先级排序

| 优先级 | 编号 | 建议 | 影响 | 工作量 |
|--------|------|------|------|--------|
| **P1** | R-1 | 为 QuickVisit 添加显式事务 | 数据一致性 | 小 (1h) |
| **P2** | R-2 | 统一响应构造方式为 BaseApiController Helper | 代码一致性 | 中 (4h) |
| **P2** | R-3 | 提取分页验证到 BaseApiController | DRY | 小 (1h) |
| **P2** | R-4 | 统一 ClockSkew 配置 (建议 30-60s) | 安全 + 文档一致性 | 小 (0.5h) |
| **P3** | R-5 | 清理 ProblemDetails 死代码 | 代码卫生 | 中 (2h) |
| **P3** | R-6 | 迁移内联 DTO 到 Shared.Models | 架构规范 | 小 (1h) |
| **P3** | R-7 | 修复 FormulasController 路由大写 | 一致性 | 极小 (5min) |
| **P3** | R-8 | 为 MedicalCase 相关控制器添加 Swagger Tag 分组 | 文档体验 | 小 (0.5h) |
| **P4** | R-9 | 验证 OutputCache 写后失效机制 | 缓存正确性 | 小 (1h) |
| **P4** | R-10 | 检查导航集合查询是否需要 AsSplitQuery | 性能 | 中 (2h) |

### 建议实施路径

**Sprint 内即刻修复** (< 2h 总计):
- R-1: QuickVisit 事务
- R-3: 分页验证提取
- R-4: ClockSkew 统一
- R-7: 路由大写修复

**下一个技术债务 Sprint** (约 1 天):
- R-2: 统一响应构造
- R-5: ProblemDetails 清理
- R-6: 内联 DTO 迁移
- R-8: Swagger Tag

**Backlog** (择机执行):
- R-9: 缓存失效验证
- R-10: 查询性能审计

---

## 13. 附录

### A. 架构守护测试

项目 `LYBT.Tests.Architecture` (76 tests) 通过 NetArchTest 强制执行:
- 控制器不能直接引用 Repository
- Service 层不能引用 Controller 层
- 模块间不能直接引用
- 所有 Entity 必须在 LYBT.Entities 项目中

### B. 日志架构

```
Serilog 两阶段初始化:
  Bootstrap: Console Sink (启动期间)
  Full: Console + File + SQL Server Sink (配置加载后)

日志丰富器:
  - CorrelationId (自定义中间件注入)
  - MachineName, ProcessId, ThreadId
  - HttpContext (RequestPath, StatusCode, Elapsed)
  
敏感数据保护:
  - ApiLoggingFilter: 密码、Token 字段自动脱敏
  - JSON 序列化: SensitiveDataJsonConverter 处理标记属性
```

### C. 健康检查端点

```
/health           → 聚合检查 (SQL Server + 自定义检查)
/health/ready     → 就绪检查
/health/live      → 存活检查

SqlServerHealthCheck: 执行 SELECT 1 验证连接
DatabaseStartupDiagnostics: 启动时验证 Migration 状态
```

### D. BaseApiController 方法清单

| 方法 | 用途 | 返回类型 |
|------|------|----------|
| `GetOperator()` | 从 JWT 提取操作者信息 | `(Guid Id, string Name, string Role)` |
| `Success()` | 200 无数据响应 | `ApiResponse` |
| `Success<T>(data)` | 200 带数据响应 | `ApiResponse<T>` |
| `SuccessPaged<T>(items, total, page, size)` | 200 分页响应 | `PagedApiResponse<T>` |
| `Error(message, code)` | 错误响应 | `ApiResponse` |
| `NotFound(message)` | 404 响应 | `ApiResponse` |
| `BusinessFail(message)` | 422 业务失败 | `ApiResponse` |
| `ValidationFail(message)` | 400 验证失败 | `ApiResponse` |
| `Forbid(message)` | 403 禁止访问 | `ApiResponse` |
| `HandleResult<T>(result)` | 映射 Result → HTTP 响应 | 动态 |
| `HandlePagedResult<T>(result)` | 映射分页 Result → 响应 | 动态 |
| `HandleBoolResult(result)` | 映射布尔 Result → 响应 | 动态 |
| `HandleAuthResult<T>(result)` | 映射认证 Result → 响应 | 动态 |
| `ValidateGuid(id)` | 验证 GUID 格式 | `bool` |
| `ValidateModel(model)` | 手动模型验证 | `IActionResult?` |
| `ValidateOwnership(entityOwnerId)` | 验证资源所有权 | `bool` |
| `GetEntityWithOwnershipCheckAsync<T>()` | 获取 + 所有权检查 | `(T?, IActionResult?)` |
| `IsAdminOrOwner(entityOwnerId)` | 管理员或所有者检查 | `bool` |
| `LogOperation(action, detail)` | 脱敏操作日志 | `void` |

---

*报告完成。以上分析基于截至 2026-04-01 的代码库状态。*
