# LYBT.WebAPI 代码知识

## API 端点详细列表

### AuthController (/api/v1/auth)

```
POST   /api/v1/auth/login              # 用户登录 [AllowAnonymous] [RateLimit:Login]
POST   /api/v1/auth/auto-login         # AutoLoginToken自动登录 [AllowAnonymous]
POST   /api/v1/auth/logout             # 用户登出 [AllowAnonymous]
POST   /api/v1/auth/refresh            # 刷新访问令牌 [AllowAnonymous]
GET    /api/v1/auth/validate           # 验证Token有效性 [Authorize]
POST   /api/v1/auth/change-password    # 修改密码
```

### UsersController (/api/v1/users)

```
GET    /api/v1/users                   # 分页查询用户
GET    /api/v1/users/{id}              # 按 ID 查询用户
POST   /api/v1/users                   # 创建用户
PUT    /api/v1/users/{id}              # 更新用户
DELETE /api/v1/users/{id}              # 删除用户 [AdminOnly]
GET    /api/v1/users/search            # 搜索用户
```

### PatientsController (/api/v1/patients)

```
GET    /api/v1/patients                # 分页查询患者
GET    /api/v1/patients/{id}           # 按 ID 查询患者
POST   /api/v1/patients                # 创建患者
PUT    /api/v1/patients/{id}           # 更新患者
DELETE /api/v1/patients/{id}           # 删除患者 [AdminOnly]
GET    /api/v1/patients/search         # 搜索患者 (姓名/手机号/拼音)
GET    /api/v1/patients/{id}/history   # 获取患者病史
```

### MedicalCase Controllers (/api/v1/medicalcases)

已拆分为4个控制器：
- **MedicalCasesController**: CRUD 操作
- **MedicalCaseWorkflowController**: 状态流转（更新状态、关闭、挂起、取消）
- **MedicalCasePrintController**: 打印管理（记录打印完成、添加打印日志）
- **MedicalCaseAuditController**: 审计查询（权限、审计日志）

``
GET    /api/v1/medicalcases                         # 分页查询医案
GET    /api/v1/medicalcases/{id}                    # 医案详情
POST   /api/v1/medicalcases                         # 创建医案
PUT    /api/v1/medicalcases/{id}                    # 更新医案
DELETE /api/v1/medicalcases/{id}                    # 删除医案 (软删除)
PUT    /api/v1/medicalcases/{id}/consultation       # 更新诊断记录
PUT    /api/v1/medicalcases/{id}/prescription-flag  # 设置处方标志
POST   /api/v1/medicalcases/{id}/prescription       # 创建处方
PUT    /api/v1/medicalcases/{id}/prescription       # 更新处方
DELETE /api/v1/medicalcases/{id}/prescription       # 删除处方
PUT    /api/v1/medicalcases/{id}/status             # 更新状态
POST   /api/v1/medicalcases/{id}/complete           # 完成医案
POST   /api/v1/medicalcases/{id}/close              # 关闭医案
POST   /api/v1/medicalcases/{id}/suspend            # 挂起医案
POST   /api/v1/medicalcases/{id}/save               # 统一保存
```

### HerbsController (/api/v1/herbs)

```
GET    /api/v1/herbs                   # 分页查询药材
GET    /api/v1/herbs/{id}              # 按 ID 查询药材
POST   /api/v1/herbs                   # 创建药材
PUT    /api/v1/herbs/{id}              # 更新药材
DELETE /api/v1/herbs/{id}              # 删除药材
GET    /api/v1/herbs/search            # 搜索药材
POST   /api/v1/herbs/import            # 批量导入药材
GET    /api/v1/herbs/export            # 导出药材
```

### FormulasController (/api/v1/formulas)

```
GET    /api/v1/formulas                # 分页查询验方
GET    /api/v1/formulas/{id}           # 按 ID 查询验方
POST   /api/v1/formulas                # 创建验方
PUT    /api/v1/formulas/{id}           # 更新验方
DELETE /api/v1/formulas/{id}           # 删除验方
GET    /api/v1/formulas/search         # 搜索验方
POST   /api/v1/formulas/{id}/clone     # 克隆验方
```

### SyncController (/api/v1/sync)

数据同步相关端点。

### DiagnosticsController (/api/v1/diagnostics)

系统诊断信息端点。

### HealthController (/health)

```
GET    /health                         # 健康检查 (数据库 + 自定义检查)
```

## 中间件管道

启动顺序 (Program.cs):

```
1. UseExceptionHandler / UseDeveloperExceptionPage  -- 全局异常捕获
2. UseStatusCodePagesWithProblemDetails             -- RFC 7807 错误响应
3. UseCorrelationId                                  -- 请求追踪 ID
4. UseHttpsRedirection + UseHsts (仅生产环境)        -- 强制 HTTPS
5. UseSecurityHeaders                                -- 安全响应头
6. UseResponseCompression                            -- 响应压缩
7. Swagger (仅非生产环境)                             -- API 文档
8. UseRouting                                        -- 路由
9. UseRateLimiter                                    -- 速率限制
10. UseAuthentication                                -- JWT 认证
11. UseClaimsNormalization                           -- Claims 标准化
12. UseAuthorization                                 -- 授权策略
13. UseResponseCaching + UseOutputCache              -- 缓存
14. MapHealthChecks ("/health")                      -- 健康检查端点
15. MapControllers                                   -- 路由映射
```

服务注册顺序:

```csharp
// 1. Serilog 日志
// 2. DbContext (SQL Server)
// 3. 业务模块 (AddAuthModule, AddUsersModule, ...)
// 4. Controllers + 全局过滤器 (ValidateModelState, ApiExceptionFilter)
// 5. JWT 认证 + 授权策略 (AdminOnly, DoctorOrAdmin)
// 6. Swagger (OpenAPI + JWT SecurityDefinition)
// 7. HealthChecks (database + custom)
```

## API 统一响应格式

成功:
```json
{
  "Success": true,
  "Message": "操作成功",
  "StatusCode": 200,
  "Data": { ... }
}
```

分页:
```json
{
  "Success": true,
  "Data": {
    "Items": [...],
    "TotalCount": 100,
    "PageIndex": 1,
    "PageSize": 10,
    "TotalPages": 10
  }
}
```

错误:
```json
{
  "Success": false,
  "Message": "参数验证失败",
  "StatusCode": 400,
  "Errors": ["姓名不能为空"]
}
```

## 异常映射规则

全局异常处理 (LYBT.Shared.ExceptionHandling) 中的异常到 HTTP 状态码映射:

| 异常类型 | HTTP 状态码 | 消息 |
|----------|-------------|------|
| ArgumentNullException | 400 | 参数不能为空 |
| ArgumentException | 400 | exception.Message |
| UnauthorizedAccessException | 401 | 未授权访问 |
| KeyNotFoundException | 404 | 资源不存在 |
| BusinessException | 400 | exception.Message |
| 其他 Exception | 500 | 服务器内部错误 |

## 授权策略

| 策略名称 | 角色要求 | 典型使用 |
|----------|----------|----------|
| AdminOnly | Admin | 删除用户/患者 |
| DoctorOrAdmin | Doctor, Admin | 大部分业务端点 |
| (默认 [Authorize]) | 任意已认证用户 | 查询类端点 |

## 架构决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 薄 Controller 模式 | Controller 仅做 HTTP 协议转换，业务逻辑委托给模块 Service | 初始设计 |
| 模块化扩展方法注册 | 每个模块 AddXxxModule() 自注册，WebAPI 无需了解内部实现 | 初始设计 |
| 全局异常中间件 | 统一错误格式，防止内部信息泄露 | 初始设计 |
| Serilog 结构化日志 | 按日滚动文件，满足小型诊所运维审计 | 初始设计 |
| PascalCase JSON 序列化 | 与 WPF 客户端 DTO 命名一致，避免额外映射 | 初始设计 |
| ClockSkew = 30 seconds | JWT 时钟偏差容忍 30 秒（默认 5 分钟过大） | 初始设计 |
| 速率限制 (RateLimiting) | 防止登录端点暴力攻击 | 后续增强 |
| BackgroundServices 缓存失效 | 事件驱动的缓存失效机制 | 2026-02 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| Swagger 仅开发环境启用 | 生产环境不应暴露 API 文档 | IsDevelopment() 条件判断 |
| JSON 序列化使用 PascalCase | 默认是 camelCase，但 WPF DTO 是 PascalCase | PropertyNamingPolicy = null |
| FindAsync 与全局查询过滤器 | EF Core 8 的 FindAsync 在实体不在 ChangeTracker 中时会应用 IsDeleted 过滤器 | 用 IgnoreQueryFilters() 查询软删除记录 |
| 配置文件中不应包含真实密钥 | appsettings.json 会被提交到版本控制 | 使用 .env 文件或 User Secrets |

## 代码文件结构

### Controllers/ (12 文件)

| 文件 | 类 | 继承 | 用途 |
|------|-----|------|------|
| AuthController.cs | `AuthController` | BaseApiController | 认证控制器: 登录/自动登录/登出/Token刷新/Token验证 |
| UsersController.cs | `UsersController` | BaseApiController | 用户管理: CRUD/密码管理/状态切换/批量操作 |
| PatientsController.cs | `PatientsController` | BaseApiController | 患者管理: CRUD/导入导出/引用检查/批量操作 |
| MedicalCasesController.cs | `MedicalCasesController` | BaseApiController | 医案CRUD: 创建/更新/删除/查询/搜索 |
| MedicalCaseWorkflowController.cs | `MedicalCaseWorkflowController` | BaseApiController | 医案工作流: 状态更新/关闭/挂起/取消 |
| MedicalCasePrintController.cs | `MedicalCasePrintController` | BaseApiController | 医案打印: 打印记录/日志 |
| MedicalCaseAuditController.cs | `MedicalCaseAuditController` | BaseApiController | 医案审计: 权限查询/审计日志 |
| HerbsController.cs | `HerbsController` | BaseApiController | 药材管理: CRUD/导入导出/引用检查/批量操作 |
| FormulasController.cs | `FormulasController` | BaseApiController | 验方管理: CRUD/导入导出/药材验证/批量操作 |
| SyncController.cs | `SyncController` | BaseApiController | 数据同步: 元数据/比对/上传/下载/删除 |
| HealthController.cs | `HealthController` | BaseApiController | 健康检查: 基础探活/Ping/详细健康检查(含数据库) |
| DiagnosticsController.cs | `DiagnosticsController` | BaseApiController | 系统诊断: 日志级别查看/调试模式/运行时日志级别调整 |

#### AuthController 端点

```
POST   /api/v1/auth/login              [AllowAnonymous] [RateLimit:Login] 用户登录
POST   /api/v1/auth/auto-login         [AllowAnonymous] [RateLimit:Login] AutoLoginToken自动登录
POST   /api/v1/auth/logout             [AllowAnonymous] 用户登出 (允许过期Token)
POST   /api/v1/auth/refresh            [AllowAnonymous] 刷新访问令牌
GET    /api/v1/auth/validate           [Authorize] 验证Token有效性
GET    /api/v1/auth                    [Authorize] 返回405
```

#### UsersController 端点

```
GET    /api/v1/users                   [AdminOnly] 分页查询用户
GET    /api/v1/users/current           [Authorize] 获取当前登录用户信息
GET    /api/v1/users/{id}              [AdminOnly] 获取单个用户
POST   /api/v1/users                   [AdminOnly] 创建用户
PUT    /api/v1/users/{id}              [AdminOnly] 更新用户
DELETE /api/v1/users/{id}              [AdminOnly] 删除用户
POST   /api/v1/users/{id}/reset-password [AdminOnly] 重置用户密码
PUT    /api/v1/users/{id}/profile      [Authorize] 修改个人资料
PUT    /api/v1/users/{id}/change-password [Authorize] 修改密码
POST   /api/v1/users/{id}/toggle-status [AdminOnly] 切换用户状态
POST   /api/v1/users/{id}/restore     [AdminOnly] 恢复已删除用户
POST   /api/v1/users/batch-delete     [AdminOnly] 批量删除
POST   /api/v1/users/batch-enable     [AdminOnly] 批量启用
POST   /api/v1/users/batch-disable    [AdminOnly] 批量禁用
```

#### PatientsController 端点

```
GET    /api/v1/patients                [PatientAccess] [OutputCache] 分页查询
GET    /api/v1/patients/{id}           [PatientAccess] 获取详情
POST   /api/v1/patients                [PatientAccess] 创建患者 (201)
PUT    /api/v1/patients/{id}           [PatientAccess] 更新 (含所有权检查)
DELETE /api/v1/patients/{id}           [PatientAccess] 软删除 (含所有权检查)
POST   /api/v1/patients/import         [PatientAccess] 批量导入 (.xlsx, 10MB限制)
GET    /api/v1/patients/import-template [PatientAccess] 下载导入模板
GET    /api/v1/patients/export         [PatientAccess] 导出Excel
POST   /api/v1/patients/{id}/toggle-status [PatientAccess] 切换状态
POST   /api/v1/patients/{id}/restore   [PatientAccess] 恢复已删除
POST   /api/v1/patients/batch-delete   [PatientAccess] 批量删除
GET    /api/v1/patients/{id}/check-reference [PatientAccess] 检查引用关系
POST   /api/v1/patients/batch-check-reference [PatientAccess] 批量引用检查
```

#### MedicalCase Controllers 端点

```
# Write Layer (写操作)
POST   /api/v1/medicalcases            [Roles:Doctor] 创建医案
PUT    /api/v1/medicalcases/{id}       [DoctorOrAdmin] 聚合保存(Consultation+Prescription)
PUT    /api/v1/medicalcases/{id}/prescription-flag [DoctorOrAdmin] 设置处方标志
PUT    /api/v1/medicalcases/{id}/print-completed   [DoctorOrAdmin] 记录打印完成
POST   /api/v1/medicalcases/{id}/print-logs        [DoctorOrAdmin] 添加打印日志
PUT    /api/v1/medicalcases/{id}/status [DoctorOrAdmin] 更新状态
PUT    /api/v1/medicalcases/{id}/close  [DoctorOrAdmin] 关闭医案(跳过流程验证)
PUT    /api/v1/medicalcases/{id}/suspend [DoctorOrAdmin] 挂起医案
PUT    /api/v1/medicalcases/{id}/cancel [DoctorOrAdmin] 取消医案(软删除)
DELETE /api/v1/medicalcases/{id}       [DoctorOrAdmin] 删除医案(软删除)
POST   /api/v1/medicalcases/batch-delete [DoctorOrAdmin] 批量删除

# Read Layer (读操作)
GET    /api/v1/medicalcases/{id}       [DoctorOrAdmin] 医案详情(含Consultation+Prescription)
GET    /api/v1/medicalcases/{id}/permissions [DoctorOrAdmin] 查询当前用户权限
GET    /api/v1/medicalcases/{id}/audit-logs  [DoctorOrAdmin] 审计日志(分页)
GET    /api/v1/medicalcases            [DoctorOrAdmin] 分页查询(支持角色过滤)
GET    /api/v1/medicalcases/query      [DoctorOrAdmin] 统一查询端点(All/ByPatient/Pending/Unfinished/Recent)
GET    /api/v1/medicalcases/search     [DoctorOrAdmin] 跨医案搜索
GET    /api/v1/medicalcases/pending    [DoctorOrAdmin] [Obsolete] 待看诊队列(迁移到/query)
POST   /api/v1/medicalcases/batch-details [DoctorOrAdmin] 批量获取详情
GET    /api/v1/medicalcases/{id}/consultations [DoctorOrAdmin] 辨证记录列表
GET    /api/v1/medicalcases/{id}/prescriptions [DoctorOrAdmin] 处方列表
```

#### HerbsController 端点

```
GET    /api/v1/herbs                   [DoctorOrAdmin] [OutputCache] 分页查询
GET    /api/v1/herbs/{id}              [DoctorOrAdmin] 药材详情
POST   /api/v1/herbs                   [DoctorOrAdmin] 创建药材
PUT    /api/v1/herbs/{id}              [DoctorOrAdmin] 更新 (含所有权检查)
DELETE /api/v1/herbs/{id}              [DoctorOrAdmin] 软删除 (含引用检查)
POST   /api/v1/herbs/import            [DoctorOrAdmin] Excel文件导入 (.xlsx, 10MB)
GET    /api/v1/herbs/export            [DoctorOrAdmin] 导出Excel
GET    /api/v1/herbs/import-template   [DoctorOrAdmin] 下载导入模板
POST   /api/v1/herbs/batch-import      [DoctorOrAdmin] 批量导入(JSON)
GET    /api/v1/herbs/export-all        [DoctorOrAdmin] 导出全部(JSON)
GET    /api/v1/herbs/{id}/check-reference [DoctorOrAdmin] 检查引用关系
POST   /api/v1/herbs/batch-check-reference [DoctorOrAdmin] 批量引用检查
POST   /api/v1/herbs/{id}/toggle-status [DoctorOrAdmin] 切换状态
POST   /api/v1/herbs/{id}/restore     [DoctorOrAdmin] 恢复已删除
POST   /api/v1/herbs/batch-enable     [DoctorOrAdmin] 批量启用
POST   /api/v1/herbs/batch-disable    [DoctorOrAdmin] 批量禁用
POST   /api/v1/herbs/batch-delete     [DoctorOrAdmin] 批量删除
```

#### FormulasController 端点

```
GET    /api/v1/formulas                [DoctorOrAdmin] [OutputCache] 分页查询(角色过滤)
GET    /api/v1/formulas/{id}           [DoctorOrAdmin] 验方详情
POST   /api/v1/formulas                [DoctorOrAdmin] 创建验方(设置所有权)
PUT    /api/v1/formulas/{id}           [DoctorOrAdmin] 更新 (含所有权检查)
DELETE /api/v1/formulas/{id}           [DoctorOrAdmin] 软删除 (含所有权检查)
POST   /api/v1/formulas/batch-import   [DoctorOrAdmin] 批量导入(JSON DTO)
GET    /api/v1/formulas/export         [DoctorOrAdmin] 导出Excel
GET    /api/v1/formulas/import-template [DoctorOrAdmin] 下载导入模板
GET    /api/v1/formulas/pending-validation [DoctorOrAdmin] 待校验验方列表
POST   /api/v1/formulas/{id}/herbs/{herbItemId}/validate [DoctorOrAdmin] 验证验方药材
POST   /api/v1/formulas/{id}/toggle-status [DoctorOrAdmin] 切换状态
POST   /api/v1/formulas/{id}/restore  [DoctorOrAdmin] 恢复已删除
POST   /api/v1/formulas/batch-delete  [DoctorOrAdmin] 批量删除
POST   /api/v1/formulas/batch-enable  [DoctorOrAdmin] 批量启用
POST   /api/v1/formulas/batch-disable [DoctorOrAdmin] 批量禁用
```

#### SyncController 端点

```
GET    /api/v1/sync/entity-types       [DoctorOrAdmin] 获取支持的实体类型
GET    /api/v1/sync/metadata           [DoctorOrAdmin] 获取实体元数据
POST   /api/v1/sync/compare            [DoctorOrAdmin] 比对本地与服务器差异
POST   /api/v1/sync/upload             [DoctorOrAdmin] 上传本地数据
POST   /api/v1/sync/download           [DoctorOrAdmin] 下载服务器数据
POST   /api/v1/sync/delete             [DoctorOrAdmin] 同步删除(带引用检查)
```

#### DiagnosticsController 端点

```
GET    /api/v1/diagnostics/logging/status       [Roles:SuperAdmin] 获取日志级别状态
POST   /api/v1/diagnostics/logging/debug/enable [Roles:SuperAdmin] 启用调试模式(临时降级)
POST   /api/v1/diagnostics/logging/debug/disable [Roles:SuperAdmin] 禁用调试模式
POST   /api/v1/diagnostics/logging/level        [Roles:SuperAdmin] 设置日志级别
```

#### HealthController 端点

```
GET    /api/v1/health                  [AllowAnonymous] 基础健康检查
GET    /api/v1/health/ping             [AllowAnonymous] Ping探活
GET    /api/v1/health/details          [Authorize] 详细健康检查(含数据库)
```

### Authorization/ [DELETED]

资源级授权 Handler 基础设施已删除 (2026-03-01)。原 5 个文件 (MedicalCaseOperations/FormulaOperations/MedicalCaseAuthorizationHandler/FormulaAuthorizationHandler/ClaimsPrincipalExtensions) 已移除。

实际权限控制由 Service 层 `EnsureCanEdit`/`EnsureCanDelete` 方法实现。

#### 当前授权体系

策略级授权 (Policy-based) -- 在 AuthenticationServiceCollectionExtensions 中注册:
- `AdminOnly`: SuperAdmin + Admin
- `DoctorOrAdmin`: SuperAdmin + Admin + Doctor
- `PatientAccess`: SuperAdmin + Admin + Doctor + Receptionist
- `RequireAuthenticated`: 任意已认证用户
- FallbackPolicy: 默认要求认证

### Middleware/ (3 文件)

| 文件 | 类 | 管道顺序 | 用途 |
|------|-----|----------|------|
| CorrelationIdMiddleware.cs | `CorrelationIdMiddleware` | 阶段1.2 | 请求追踪: 从traceparent/X-Correlation-ID提取或生成CorrelationId, 注入LogContext |
| SecurityHeadersMiddleware.cs | `SecurityHeadersMiddleware` | 阶段1.4 | 安全头: X-Content-Type-Options/X-Frame-Options/CSP/HSTS等 |
| ClaimsNormalizationMiddleware.cs | `ClaimsNormalizationMiddleware` | 阶段4.2 | Claims标准化: 确保NameIdentifier/sub/Role等Claims格式统一 |

### Filters/ (1 文件)

| 文件 | 类 | 用途 |
|------|-----|------|
| ApiLoggingFilter.cs | `ApiLoggingFilter` : IAsyncActionFilter | 全局Action过滤器: 记录每个API Action的开始/结束/耗时/参数(脱敏), 通过ServiceCollectionExtensions注册 |

### Configuration/ (1 文件)

| 文件 | 类 | 用途 |
|------|-----|------|
| ProblemDetailsConfiguration.cs | `ProblemDetailsConfiguration` (static) | RFC 7807配置: 注入CorrelationId/时间戳/traceId到ProblemDetails, StatusCodePages中间件配置 |

### HealthCheck/ (2 文件)

| 文件 | 类 | 用途 |
|------|-----|------|
| SqlServerHealthCheck.cs | `SqlServerHealthCheck` : IHealthCheck | SQL Server健康检查: 连接测试(5秒超时) + 错误码诊断建议 |
| DatabaseStartupDiagnostics.cs | `DatabaseStartupDiagnostics` : IHostedService | 启动时数据库诊断: 连接验证/连接池配置显示/故障排查建议 |

### BackgroundServices/ (1 文件)

| 文件 | 类 | 用途 |
|------|-----|------|
| SecurityAuditCleanupService.cs | `SecurityAuditCleanupService` : BackgroundService | 每日凌晨3点清理过期审计日志, 保留天数从SecurityOptions.AuditRetentionDays读取 |

### Extensions/ (7 文件)

| 文件 | 类 | 用途 |
|------|-----|------|
| ServiceCollectionExtensions.cs | `ServiceCollectionExtensions` (static) | 服务注册主入口: RegisterAllApplicationServices协调所有模块注册 |
| ApiServiceCollectionExtensions.cs | `ApiServiceCollectionExtensions` (static) | API服务: API版本管理/Swagger/ProblemDetails/ExceptionHandler/速率限制 |
| AuthenticationServiceCollectionExtensions.cs | `AuthenticationServiceCollectionExtensions` (static) | 认证授权: JWT Bearer配置/授权策略注册/AuthorizationHandler注册 |
| DatabaseServiceCollectionExtensions.cs | `DatabaseServiceCollectionExtensions` (static) | 基础设施: DbContext/MemoryCache/OutputCache/HealthChecks/BackgroundService/跨模块服务 |
| UnifiedMiddlewareConfiguration.cs | `UnifiedMiddlewareConfiguration` (static) | 中间件管道: ConfigureAllMiddleware统一装配6个阶段的中间件 |
| UnifiedApplicationInitialization.cs | `UnifiedApplicationInitialization` (static) | 应用初始化: 数据库初始化/配置验证/启动日志/优雅关闭 |
| EnvironmentAwareHosting.cs | `EnvironmentAwareHosting` (static) | 环境感知: Development控制台模式/Production Windows Service模式/启动信息显示 |

### 根目录 (1 文件)

| 文件 | 类 | 用途 |
|------|-----|------|
| Program.cs | `Program` | 应用入口: Serilog两阶段初始化/.env加载/密码配置验证/服务注册/中间件配置/生产环境配置验证 |

### 内嵌请求DTO

以下DTO定义在Controller文件内部 (未独立到Contracts项目):

| 所在文件 | 类名 | 用途 |
|----------|------|------|
| MedicalCaseWorkflowController.cs | `UpdateStatusRequest` | 更新医案状态请求 (Status字段) |
| MedicalCaseWorkflowController.cs | `CancelMedicalCaseRequest` | 取消医案请求 (Reason字段) |
| DiagnosticsController.cs | `EnableDebugModeRequest` | 启用调试模式请求 (Level/DurationMinutes) |
| DiagnosticsController.cs | `SetLoggingLevelRequest` | 设置日志级别请求 (Level) |

## 死代码分析

| 类型 | 状态 | 说明 |
|------|------|------|
| `AuthorizationExtensions` | [已清理] 2026-03-01 | 文件及 Extensions/ServiceCollection/ 目录已删除 |
| `Authorization/` (5 文件) | [已清理] 2026-03-01 | MedicalCaseOperations/FormulaOperations/Handler/ClaimsPrincipalExtensions 全部删除，实际授权由 Service 层实现 |

## 环境配置

```bash
# 开发环境启动
dotnet run --project src/Server/Services/LYBT.WebAPI

# Swagger: https://localhost:7001/swagger
# Health:  https://localhost:7001/health

# 数据库迁移
dotnet ef database update \
  --project src/Server/Core/LYBT.Infrastructure \
  --startup-project src/Server/Services/LYBT.WebAPI

# 发布
dotnet publish -c Release -o ./publish
```
