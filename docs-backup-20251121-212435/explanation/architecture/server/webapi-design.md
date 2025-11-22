# Server端WebAPI层架构设计

> **文档版本**: v1.0
> **最后更新**: 2025-01-29
> **维护负责**: Server端开发组

---

## 📋 目录

1. [WebAPI层定位与职责](#1-webapi层定位与职责)
2. [核心架构设计](#2-核心架构设计)
3. [中间件设计体系](#3-中间件设计体系)
4. [Controller设计模式](#4-controller设计模式)
5. [认证授权架构](#5-认证授权架构)
6. [过滤器设计](#6-过滤器设计)
7. [健康检查架构](#7-健康检查架构)
8. [日志与监控架构](#8-日志与监控架构)
9. [API文档生成](#9-api文档生成)
10. [CORS策略配置](#10-cors策略配置)
11. [模块化注册模式](#11-模块化注册模式)

---

## 1. WebAPI层定位与职责

### 1.1 架构定位

WebAPI层是LYBTZYZS中医诊所管理系统的**统一API网关**，位于Server端三层架构的最上层，负责对外暴露RESTful API服务。

```
┌─────────────────────────────────────────────┐
│           WebAPI层 (API Gateway)             │
│  ┌────────────────────────────────────────┐  │
│  │  Controllers (8个模块控制器,90+端点)    │  │
│  ├────────────────────────────────────────┤  │
│  │  Middleware (全局异常/请求日志/认证)    │  │
│  ├────────────────────────────────────────┤  │
│  │  Filters (模型验证/异常过滤)            │  │
│  ├────────────────────────────────────────┤  │
│  │  HealthChecks (数据库/自定义检查)       │  │
│  └────────────────────────────────────────┘  │
└─────────────────┬───────────────────────────┘
                  │ 依赖注入
┌─────────────────▼───────────────────────────┐
│         Modules层 (8个业务模块)              │
│  Auth / Users / Patients / MedicalCase /    │
│  Consultation / Prescriptions / Herbs /     │
│  Formula                                     │
└─────────────────┬───────────────────────────┘
                  │ Repository模式
┌─────────────────▼───────────────────────────┐
│      Infrastructure层 (数据访问基础设施)     │
│  AppDbContext / BaseRepository / Migrations │
└─────────────────────────────────────────────┘
```

### 1.2 核心职责

| 职责类别 | 具体功能 |
|---------|---------|
| **API网关** | 统一API入口、请求路由、版本控制 |
| **请求处理** | 参数绑定、模型验证、响应格式化 |
| **认证授权** | JWT认证、基于角色的授权策略 |
| **异常处理** | 全局异常捕获、友好错误消息 |
| **日志监控** | 结构化日志、请求追踪、性能监控 |
| **API文档** | Swagger自动生成、交互式测试UI |
| **健康检查** | 数据库连接、业务逻辑验证 |
| **跨域支持** | CORS策略配置、Desktop客户端支持 |

### 1.3 技术选型

**核心框架**:
- **ASP.NET Core 8.0**: Web框架（Minimal API + MVC控制器）
- **Entity Framework Core 8**: 通过Infrastructure层间接使用

**认证授权**:
- **JWT Bearer Token**: 无状态认证机制
- **Claims-based Authorization**: 基于声明的授权

**API文档**:
- **Swagger/OpenAPI 3.0**: API文档自动生成
- **Swashbuckle.AspNetCore 6.x**: Swagger UI生成

**日志监控**:
- **Serilog 8.x**: 结构化日志框架
- **ASP.NET Core Health Checks**: 健康检查

**其他**:
- **FluentValidation**: DTO验证（通过模块间接集成）
- **AutoMapper**: Entity ↔ DTO映射（通过模块间接集成）

---

## 2. 核心架构设计

### 2.1 Program.cs启动流程

ASP.NET Core 8.0采用**Minimal API**启动模式，Program.cs负责应用程序的完整生命周期管理。

#### 2.1.1 启动流程架构

```
WebApplication.CreateBuilder(args)
  ↓
┌──────────────────────────────────────┐
│ Step 1: 配置Serilog结构化日志        │
│ - ReadFrom.Configuration             │
│ - WriteTo.Console / WriteTo.File     │
└──────────────────┬───────────────────┘
                   ↓
┌──────────────────────────────────────┐
│ Step 2: 服务注册 (Services)          │
│ ├─ AddDbContext<AppDbContext>        │
│ ├─ Add8Modules (Auth/Users/...)      │
│ ├─ AddControllers + AddJsonOptions   │
│ ├─ AddAuthentication (JWT)           │
│ ├─ AddAuthorization (Policies)       │
│ ├─ AddCors (CORS策略)                │
│ ├─ AddSwaggerGen (API文档)           │
│ └─ AddHealthChecks (健康检查)        │
└──────────────────┬───────────────────┘
                   ↓
┌──────────────────────────────────────┐
│ app = builder.Build()                │
└──────────────────┬───────────────────┘
                   ↓
┌──────────────────────────────────────┐
│ Step 3: 中间件管道 (Middleware)      │
│ 1. ExceptionHandlingMiddleware       │
│ 2. Swagger UI (开发环境)             │
│ 3. UseHttpsRedirection               │
│ 4. UseCors                            │
│ 5. RequestLoggingMiddleware          │
│ 6. UseAuthentication                 │
│ 7. UseAuthorization                  │
│ 8. MapControllers                    │
│ 9. MapHealthChecks                   │
└──────────────────┬───────────────────┘
                   ↓
                app.Run()
```

#### 2.1.2 代码结构（伪代码）

```csharp
var builder = WebApplication.CreateBuilder(args);

// === Serilog配置 ===
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/lybt-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// === 服务注册 ===
// 1. 数据库上下文
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. 8个业务模块注册
builder.Services.AddAuthModule();
builder.Services.AddUsersModule();
builder.Services.AddPatientsModule();
builder.Services.AddMedicalCaseModule();
builder.Services.AddConsultationModule();
builder.Services.AddPrescriptionsModule();
builder.Services.AddHerbsModule();
builder.Services.AddFormulaModule();

// 3. 控制器和JSON配置
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelStateAttribute>();
    options.Filters.Add<ApiExceptionFilterAttribute>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        JsonIgnoreCondition.WhenWritingNull;
});

// 4. JWT认证
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options => { /* JWT配置 */ });

// 5. 授权策略
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DoctorOrAdmin", policy => policy.RequireRole("Doctor", "Admin"));
});

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDesktopClient", builder => { /* CORS配置 */ });
});

// 7. Swagger文档
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => { /* Swagger配置 */ });

// 8. 健康检查
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck<CustomHealthCheck>("custom");

var app = builder.Build();

// === 中间件管道 ===
app.UseMiddleware<ExceptionHandlingMiddleware>(); // 1. 全局异常处理

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // 2. Swagger UI (开发环境)
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // 3. HTTPS重定向 (生产环境)
}

app.UseCors("AllowDesktopClient"); // 4. CORS
app.UseMiddleware<RequestLoggingMiddleware>(); // 5. 请求日志
app.UseAuthentication(); // 6. 认证
app.UseAuthorization(); // 7. 授权
app.MapControllers(); // 8. 路由映射
app.MapHealthChecks("/health", new HealthCheckOptions { /* 配置 */ }); // 9. 健康检查

app.Run();
```

### 2.2 中间件管道执行顺序

**关键原则**: 中间件的顺序直接影响请求处理流程，必须严格遵守以下顺序：

```
HTTP Request
  ↓
┌──────────────────────────────────────────────┐
│ 1. ExceptionHandlingMiddleware               │ → 捕获所有异常
│    (必须在最前面,保证捕获所有下游异常)       │
└─────────────┬────────────────────────────────┘
              ↓
┌──────────────────────────────────────────────┐
│ 2. Swagger Middleware (开发环境)             │ → API文档
└─────────────┬────────────────────────────────┘
              ↓
┌──────────────────────────────────────────────┐
│ 3. UseHttpsRedirection (生产环境)            │ → 强制HTTPS
└─────────────┬────────────────────────────────┘
              ↓
┌──────────────────────────────────────────────┐
│ 4. UseCors                                    │ → CORS策略
│    (必须在认证授权前,处理预检请求)           │
└─────────────┬────────────────────────────────┘
              ↓
┌──────────────────────────────────────────────┐
│ 5. RequestLoggingMiddleware                  │ → 请求日志
└─────────────┬────────────────────────────────┘
              ↓
┌──────────────────────────────────────────────┐
│ 6. UseAuthentication                          │ → 认证(解析JWT)
└─────────────┬────────────────────────────────┘
              ↓
┌──────────────────────────────────────────────┐
│ 7. UseAuthorization                           │ → 授权(检查权限)
└─────────────┬────────────────────────────────┘
              ↓
┌──────────────────────────────────────────────┐
│ 8. Controller Routing / Action Execution      │ → 业务逻辑
└─────────────┬────────────────────────────────┘
              ↓
          HTTP Response
```

**错误示例**（违反顺序原则）:
```csharp
// ❌ 错误：CORS在认证之后（会导致预检请求失败）
app.UseAuthentication();
app.UseCors("AllowDesktopClient"); // 错误位置

// ❌ 错误：异常处理不在最前面（无法捕获认证中间件的异常）
app.UseAuthentication();
app.UseMiddleware<ExceptionHandlingMiddleware>(); // 错误位置
```

**正确示例**:
```csharp
// ✅ 正确：严格遵守中间件顺序
app.UseMiddleware<ExceptionHandlingMiddleware>(); // 最前面
app.UseCors("AllowDesktopClient"); // 认证之前
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

---

## 3. 中间件设计体系

### 3.1 ExceptionHandlingMiddleware - 全局异常处理

#### 3.1.1 设计目标

- **统一错误返回格式**: 所有异常返回`ApiResponse`格式
- **异常类型映射**: 不同异常映射到对应HTTP状态码
- **敏感信息保护**: 生产环境隐藏堆栈跟踪
- **日志记录**: 记录所有异常详情供后续分析

#### 3.1.2 异常映射规则

| 异常类型 | HTTP状态码 | 返回消息 |
|---------|-----------|---------|
| `ArgumentNullException` | 400 Bad Request | "参数不能为空" |
| `ArgumentException` | 400 Bad Request | `exception.Message` |
| `UnauthorizedAccessException` | 401 Unauthorized | "未授权访问" |
| `SecurityException` | 401 Unauthorized | "安全验证失败" |
| `KeyNotFoundException` | 404 Not Found | "资源不存在" |
| `DbUpdateException` | 500 Internal Server Error | "数据保存失败" |
| `DbUpdateConcurrencyException` | 409 Conflict | "数据已被其他用户修改" |
| `TaskCanceledException` | 408 Request Timeout | "请求超时，请稍后重试" |
| `HttpRequestException` | 502 Bad Gateway | "网络连接失败" |
| `ValidationException` | 400 Bad Request | `exception.Message` |
| 其他未知异常 | 500 Internal Server Error | "服务器内部错误" |

#### 3.1.3 实现架构（伪代码）

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // 调用下游中间件
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求处理失败: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // 异常类型模式匹配 → HTTP状态码
        var response = exception switch
        {
            ArgumentNullException => new ApiResponse
            {
                Success = false,
                Message = "参数不能为空",
                StatusCode = (int)HttpStatusCode.BadRequest
            },
            UnauthorizedAccessException => new ApiResponse
            {
                Success = false,
                Message = "未授权访问",
                StatusCode = (int)HttpStatusCode.Unauthorized
            },
            KeyNotFoundException => new ApiResponse
            {
                Success = false,
                Message = "资源不存在",
                StatusCode = (int)HttpStatusCode.NotFound
            },
            _ => new ApiResponse
            {
                Success = false,
                Message = "服务器内部错误",
                StatusCode = (int)HttpStatusCode.InternalServerError
            }
        };

        context.Response.StatusCode = response.StatusCode;
        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
```

### 3.2 RequestLoggingMiddleware - 请求日志记录

#### 3.2.1 设计目标

- **请求追踪**: 记录所有HTTP请求的详细信息
- **性能监控**: 记录请求处理时间
- **审计日志**: 记录用户操作和敏感操作
- **问题排查**: 提供完整的请求上下文

#### 3.2.2 日志记录内容

| 日志字段 | 描述 | 示例 |
|---------|------|------|
| **RequestId** | 唯一请求标识 | `3fa85f64-5717-4562-b3fc-2c963f66afa6` |
| **Method** | HTTP方法 | `GET`, `POST`, `PUT`, `DELETE` |
| **Path** | 请求路径 | `/api/v1/patients/123` |
| **QueryString** | 查询字符串 | `?pageIndex=1&pageSize=10` |
| **StatusCode** | HTTP状态码 | `200`, `400`, `500` |
| **Duration** | 处理时间 (毫秒) | `125.5ms` |
| **UserId** | 用户ID (如已认证) | `user-123` |
| **ClientIP** | 客户端IP | `192.168.1.100` |
| **UserAgent** | 用户代理 | `LYBT-Desktop-Client/1.0` |

#### 3.2.3 实现架构（伪代码）

```csharp
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = Stopwatch.GetTimestamp();

        // 记录请求开始
        _logger.LogInformation(
            "HTTP {Method} {Path} started",
            context.Request.Method,
            context.Request.Path
        );

        try
        {
            await _next(context); // 调用下游中间件
        }
        finally
        {
            var elapsedMs = GetElapsedMilliseconds(startTime);

            // 记录请求完成
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs
            );
        }
    }

    private static double GetElapsedMilliseconds(long startTimestamp)
    {
        var endTimestamp = Stopwatch.GetTimestamp();
        var timestampDelta = endTimestamp - startTimestamp;
        return (timestampDelta * 1000.0) / Stopwatch.Frequency;
    }
}
```

---

## 4. Controller设计模式

### 4.1 RESTful API设计规范

#### 4.1.1 HTTP方法映射

| HTTP方法 | 操作 | 路由示例 | 描述 |
|---------|------|---------|------|
| **GET** | 查询列表 | `GET /api/v1/patients` | 分页查询所有患者 |
| **GET** | 查询单个 | `GET /api/v1/patients/{id}` | 按ID查询患者详情 |
| **POST** | 创建 | `POST /api/v1/patients` | 创建新患者 |
| **PUT** | 完整更新 | `PUT /api/v1/patients/{id}` | 完整更新患者信息 |
| **PATCH** | 部分更新 | `PATCH /api/v1/patients/{id}` | 部分更新患者字段 |
| **DELETE** | 删除 | `DELETE /api/v1/patients/{id}` | 删除患者 |

#### 4.1.2 URL命名规范

**✅ 正确示例**:
```
GET    /api/v1/patients              # 查询患者列表
GET    /api/v1/patients/{id}         # 查询单个患者
POST   /api/v1/patients              # 创建患者
PUT    /api/v1/patients/{id}         # 更新患者
DELETE /api/v1/patients/{id}         # 删除患者
GET    /api/v1/patients/search       # 搜索患者（特殊查询）
POST   /api/v1/patients/import       # 批量导入（特殊操作）
```

**❌ 错误示例**:
```
GET    /api/v1/GetPatients            # 不应在URL中包含动词
POST   /api/v1/DeletePatient          # DELETE操作不应使用POST
GET    /api/v1/patient/{id}           # 应使用复数形式
```

### 4.2 统一返回格式 (ApiResponse<T>)

#### 4.2.1 ApiResponse结构

```csharp
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int StatusCode { get; set; }
    public List<string>? Errors { get; set; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}
```

#### 4.2.2 返回格式示例

**成功响应（200 OK）**:
```json
{
  "Success": true,
  "Message": "查询成功",
  "StatusCode": 200,
  "Data": {
    "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "Name": "张三",
    "Age": 45
  },
  "Errors": null
}
```

**失败响应（400 Bad Request）**:
```json
{
  "Success": false,
  "Message": "参数验证失败",
  "StatusCode": 400,
  "Data": null,
  "Errors": [
    "姓名不能为空",
    "年龄必须在0-150之间"
  ]
}
```

**分页响应（200 OK）**:
```json
{
  "Success": true,
  "Message": "查询成功",
  "StatusCode": 200,
  "Data": {
    "Items": [...],
    "TotalCount": 100,
    "PageIndex": 1,
    "PageSize": 10,
    "TotalPages": 10
  },
  "Errors": null
}
```

### 4.3 Controller标准模板

#### 4.3.1 依赖注入与构造函数

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 所有端点需要认证
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientsController> _logger;

    // ✅ 正确：通过构造函数注入依赖
    public PatientsController(
        IPatientService patientService,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    // ❌ 错误：使用ServiceLocator反模式
    public async Task<IActionResult> GetById(Guid id)
    {
        var service = HttpContext.RequestServices.GetService<IPatientService>(); // 违反DI原则
    }
}
```

#### 4.3.2 Controller方法模板

```csharp
/// <summary>
/// 按ID查询患者详情
/// </summary>
/// <param name="id">患者ID</param>
/// <returns>患者详情</returns>
/// <response code="200">查询成功</response>
/// <response code="404">患者不存在</response>
/// <response code="401">未授权访问</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
[ProducesResponseType(typeof(ApiResponse), 404)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<IActionResult> GetById(Guid id)
{
    try
    {
        // Step 1: 调用Service获取数据
        var patient = await _patientService.GetByIdAsync(id);

        // Step 2: 检查数据是否存在
        if (patient == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                Message = "患者不存在"
            });
        }

        // Step 3: 返回成功响应
        return Ok(new ApiResponse<PatientDto>
        {
            Success = true,
            Data = patient,
            Message = "查询成功"
        });
    }
    catch (Exception ex)
    {
        // Step 4: 记录异常日志
        _logger.LogError(ex, "查询患者详情失败: {PatientId}", id);

        // Step 5: 抛出异常让全局异常中间件处理
        throw;
    }
}
```

#### 4.3.3 异步编程规范

**✅ 正确示例**:
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var patient = await _patientService.GetByIdAsync(id); // 异步I/O
    return Ok(patient);
}
```

**❌ 错误示例**:
```csharp
[HttpGet("{id}")]
public IActionResult GetById(Guid id)
{
    var patient = _patientService.GetByIdAsync(id).Result; // 阻塞调用，降低吞吐量
    return Ok(patient);
}
```

---

## 5. 认证授权架构

### 5.1 JWT认证配置

#### 5.1.1 JWT Token结构

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "nameid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "unique_name": "admin",
    "role": "Admin",
    "RealName": "张三",
    "nbf": 1706524800,
    "exp": 1706528400,
    "iss": "LYBT.WebAPI",
    "aud": "LYBT.Desktop"
  },
  "signature": "..."
}
```

**Claims说明**:
- `nameid`: 用户ID (Guid)
- `unique_name`: 用户名
- `role`: 用户角色 (Admin/Doctor/Receptionist)
- `RealName`: 真实姓名
- `exp`: 过期时间 (默认60分钟)

#### 5.1.2 JWT配置（appsettings.json）

```json
{
  "Jwt": {
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Desktop",
    "Key": "your-256-bit-secret-key-minimum-32-characters",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

#### 5.1.3 JWT认证配置（Program.cs）

```csharp
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
            ClockSkew = TimeSpan.Zero // 移除默认5分钟宽限期
        };
    });
```

### 5.2 授权策略配置

#### 5.2.1 预定义授权策略

| 策略名称 | 允许角色 | 描述 |
|---------|---------|------|
| **AdminOnly** | `Admin` | 仅管理员可访问 |
| **DoctorOrAdmin** | `Doctor`, `Admin` | 医生或管理员可访问 |
| **AllAuthenticated** | 所有已认证用户 | 默认策略 |

#### 5.2.2 授权策略注册

```csharp
builder.Services.AddAuthorization(options =>
{
    // 管理员策略（仅管理员可访问）
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // 医生策略（医生和管理员可访问）
    options.AddPolicy("DoctorOrAdmin", policy =>
        policy.RequireRole("Doctor", "Admin"));
});
```

#### 5.2.3 Controller中使用授权策略

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 所有端点需要认证
public class PatientsController : ControllerBase
{
    // 所有已认证用户可访问
    [HttpGet]
    public async Task<IActionResult> GetPaged() { }

    // 所有已认证用户可访问
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) { }

    // 医生或管理员可访问
    [HttpPost]
    [Authorize(Policy = "DoctorOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto) { }

    // 仅管理员可访问
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id) { }
}
```

### 5.3 Token生成与刷新机制

#### 5.3.1 Token生成流程

```
用户登录 (POST /api/v1/auth/login)
  ↓
验证用户名密码
  ↓
查询用户信息（UserDto）
  ↓
生成JWT Token
  ├─ Claims: NameIdentifier, Name, Role, RealName
  ├─ Issuer: LYBT.WebAPI
  ├─ Audience: LYBT.Desktop
  ├─ ExpiresIn: 60分钟
  └─ SigningKey: HS256
  ↓
生成Refresh Token (可选)
  ├─ 随机生成32字节字符串
  ├─ ExpiresIn: 7天
  └─ 存储到数据库（RefreshTokens表）
  ↓
返回LoginResponse
  ├─ Token: JWT Access Token
  ├─ RefreshToken: Refresh Token
  ├─ ExpiresIn: 3600 (秒)
  └─ User: UserDto
```

#### 5.3.2 Token刷新流程

```
Token即将过期 (前端检测)
  ↓
调用Refresh API (POST /api/v1/auth/refresh-token)
  ├─ 传递: RefreshToken
  ↓
验证Refresh Token
  ├─ 检查是否存在
  ├─ 检查是否过期
  ├─ 检查是否已撤销
  ↓
生成新的Access Token
  ├─ 保持原有Claims
  ├─ 更新ExpiresIn时间
  ↓
返回新的Token
  ├─ Token: 新的JWT Access Token
  ├─ ExpiresIn: 3600 (秒)
```

---

## 6. 过滤器设计

### 6.1 ValidateModelStateAttribute - 模型验证过滤器

#### 6.1.1 设计目标

- **自动模型验证**: 在Action执行前自动验证DTO
- **统一错误返回**: 验证失败返回统一格式
- **避免重复代码**: 无需在每个Action中手动检查ModelState

#### 6.1.2 实现架构（伪代码）

```csharp
public class ValidateModelStateAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            context.Result = new BadRequestObjectResult(new ApiResponse
            {
                Success = false,
                Message = "参数验证失败",
                StatusCode = 400,
                Errors = errors
            });
        }
    }
}
```

#### 6.1.3 使用示例

```csharp
// 在Program.cs中全局注册
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelStateAttribute>(); // 全局注册
});

// Controller中无需手动检查ModelState
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
{
    // ValidateModelStateAttribute会在此Action执行前自动验证dto
    // 如果验证失败，直接返回400 Bad Request

    var patient = await _patientService.CreateAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
}
```

### 6.2 ApiExceptionFilterAttribute - API异常过滤器

#### 6.2.1 设计目标

- **Controller级异常捕获**: 捕获Controller抛出的异常
- **补充全局异常处理**: 与ExceptionHandlingMiddleware互补
- **特定异常处理**: 处理业务逻辑异常（如`BusinessException`）

#### 6.2.2 实现架构（伪代码）

```csharp
public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is BusinessException businessEx)
        {
            context.Result = new BadRequestObjectResult(new ApiResponse
            {
                Success = false,
                Message = businessEx.Message,
                StatusCode = 400
            });
            context.ExceptionHandled = true; // 标记为已处理
        }
        else if (context.Exception is ValidationException validationEx)
        {
            context.Result = new BadRequestObjectResult(new ApiResponse
            {
                Success = false,
                Message = validationEx.Message,
                StatusCode = 400
            });
            context.ExceptionHandled = true;
        }
        // 其他异常由ExceptionHandlingMiddleware处理
    }
}
```

---

## 7. 健康检查架构

### 7.1 DatabaseHealthCheck - 数据库健康检查

#### 7.1.1 检查项

| 检查项 | 描述 | 失败条件 |
|-------|------|---------|
| **数据库连接** | 验证SQL Server连接可用性 | 连接超时、连接被拒绝 |
| **表访问** | 检查核心表是否可访问 | 表不存在、权限不足 |
| **记录数统计** | 统计用户表记录数 | 查询失败 |

#### 7.1.2 实现架构（伪代码）

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 尝试连接数据库
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            // 检查数据库表是否存在
            var usersCount = await _dbContext.Users.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                $"数据库连接正常，用户表记录数: {usersCount}"
            );
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "数据库连接失败",
                ex
            );
        }
    }
}
```

### 7.2 健康检查端点配置

#### 7.2.1 健康检查响应格式

**正常状态（200 OK）**:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "数据库连接正常，用户表记录数: 5",
      "duration": 125.5
    },
    {
      "name": "custom",
      "status": "Healthy",
      "description": "自定义检查通过",
      "duration": 10.2
    }
  ],
  "totalDuration": 135.7
}
```

**异常状态（503 Service Unavailable）**:
```json
{
  "status": "Unhealthy",
  "checks": [
    {
      "name": "database",
      "status": "Unhealthy",
      "description": "数据库连接失败",
      "duration": 5000.0
    }
  ],
  "totalDuration": 5000.0
}
```

#### 7.2.2 健康检查端点注册

```csharp
// Program.cs配置
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck<CustomHealthCheck>("custom");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});
```

---

## 8. 日志与监控架构

### 8.1 Serilog结构化日志配置

#### 8.1.1 日志级别定义

| 日志级别 | 使用场景 | 示例 |
|---------|---------|------|
| **Information** | 正常业务流程 | 用户登录成功、患者创建成功 |
| **Warning** | 可恢复的错误 | 参数验证失败、手机号格式错误 |
| **Error** | 需要关注的错误 | 数据库连接失败、业务逻辑异常 |
| **Critical** | 严重错误（需要立即处理） | 应用程序启动失败、配置缺失 |
| **Debug** | 开发调试信息 | 变量值、方法调用追踪 |

#### 8.1.2 Serilog配置（Program.cs）

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console() // 控制台输出
    .WriteTo.File(
        path: "logs/lybt-.txt",
        rollingInterval: RollingInterval.Day, // 按天滚动
        retainedFileCountLimit: 30 // 保留30天日志
    )
    .CreateLogger();

builder.Host.UseSerilog();
```

#### 8.1.3 结构化日志示例

**✅ 正确示例（结构化日志）**:
```csharp
_logger.LogInformation(
    "创建患者成功: {PatientId}, {PatientName}, {Age}",
    patient.Id,
    patient.Name,
    patient.Age
);

// 输出（JSON格式，可查询）
{
  "Timestamp": "2025-01-29T10:15:32.123Z",
  "Level": "Information",
  "Message": "创建患者成功: 3fa85f64-5717-4562-b3fc-2c963f66afa6, 张三, 45",
  "PatientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "PatientName": "张三",
  "Age": 45
}
```

**❌ 错误示例（字符串拼接）**:
```csharp
_logger.LogInformation(
    $"创建患者成功: {patient.Id}, {patient.Name}, {patient.Age}"
);

// 输出（纯文本，难以查询）
"创建患者成功: 3fa85f64-5717-4562-b3fc-2c963f66afa6, 张三, 45"
```

### 8.2 日志文件组织

```
logs/
├── lybt-20250129.txt    # 2025-01-29日志
├── lybt-20250128.txt    # 2025-01-28日志
├── lybt-20250127.txt    # 2025-01-27日志
...
└── lybt-20250101.txt    # 自动删除30天前的日志
```

### 8.3 性能监控

#### 8.3.1 请求性能监控

通过`RequestLoggingMiddleware`记录每个请求的处理时间：

```csharp
_logger.LogInformation(
    "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms",
    context.Request.Method,
    context.Request.Path,
    context.Response.StatusCode,
    elapsedMs
);
```

#### 8.3.2 数据库查询性能监控

EF Core日志配置（appsettings.json）:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning" // 生产环境隐藏详细查询
    }
  }
}
```

---

## 9. API文档生成

### 9.1 Swagger配置

#### 9.1.1 Swagger基本配置

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "凌隐宝堂中医诊所管理系统 API",
        Version = "v1",
        Description = "基于ASP.NET Core 8.0的中医诊所管理REST API",
        Contact = new OpenApiContact
        {
            Name = "凌隐宝堂技术团队",
            Email = "support@lybtzyzs.com"
        }
    });

    // 添加JWT认证支持
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT授权令牌，格式: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 添加XML注释（如果存在）
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
```

#### 9.1.2 XML注释示例

```csharp
/// <summary>
/// 创建患者
/// </summary>
/// <param name="dto">患者创建DTO</param>
/// <returns>创建的患者信息</returns>
/// <response code="201">创建成功</response>
/// <response code="400">参数验证失败</response>
/// <response code="401">未授权访问</response>
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 201)]
[ProducesResponseType(typeof(ApiResponse), 400)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
{
    // ...
}
```

### 9.2 Swagger UI访问

**开发环境访问**:
- URL: `https://localhost:7001/swagger`
- 自动加载所有Controller和端点
- 支持JWT Token测试

---

## 10. CORS策略配置

### 10.1 CORS配置（Program.cs）

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDesktopClient", builder =>
    {
        builder.WithOrigins("http://localhost:5001", "https://localhost:7001")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// 中间件管道中启用CORS（必须在认证授权之前）
app.UseCors("AllowDesktopClient");
```

### 10.2 环境分离配置

**开发环境（appsettings.Development.json）**:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5001",
      "https://localhost:7001"
    ]
  }
}
```

**生产环境（appsettings.Production.json）**:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://lybtzyzs.com",
      "https://www.lybtzyzs.com"
    ]
  }
}
```

---

## 11. 模块化注册模式

### 11.1 8个业务模块注册

每个业务模块提供统一的扩展方法进行服务注册：

```csharp
// Program.cs中统一注册8个模块
builder.Services.AddAuthModule();           // 认证模块
builder.Services.AddUsersModule();          // 用户管理模块
builder.Services.AddPatientsModule();       // 患者管理模块
builder.Services.AddMedicalCaseModule();    // 医案管理模块
builder.Services.AddConsultationModule();   // 诊疗模块
builder.Services.AddPrescriptionsModule();  // 处方管理模块
builder.Services.AddHerbsModule();          // 药材管理模块
builder.Services.AddFormulaModule();        // 验方管理模块
```

### 11.2 模块注册实现示例

```csharp
// LYBT.Module.Herbs/HerbsModule.cs
public static class HerbsModule
{
    public static IServiceCollection AddHerbsModule(this IServiceCollection services)
    {
        // 1. 注册Repository
        services.AddScoped<IHerbRepository, HerbRepository>();

        // 2. 注册Service
        services.AddScoped<IHerbService, HerbService>();

        // 3. 注册Validator
        services.AddValidatorsFromAssemblyContaining<HerbCreateDtoValidator>();

        // 4. 注册AutoMapper Profile
        services.AddAutoMapper(typeof(HerbMappingProfile));

        return services;
    }
}
```

---

## 📚 相关文档

### 架构文档
- [Server端三层架构总览](../README.md) - 完整Server端架构说明
- [Interfaces层设计](./interfaces-layer-design.md) - 服务接口定义规范
- [Infrastructure层设计](../../shared/infrastructure-design.md) - 数据访问基础设施

### 开发指南
- [WebAPI开发指南](../../../how-to-guides/server/webapi-development.md) - Controller开发规范 *(待创建)*
- [认证集成指南](../../../how-to-guides/server/auth-integration.md) - JWT认证集成 *(待创建)*
- [WebAPI部署指南](../../../how-to-guides/server/webapi-deployment.md) - IIS/Docker部署 *(待创建)*

### API参考
- [API端点快速参考](../../../reference/quick-reference/api-endpoints.md) - 90+端点索引 *(待创建)*
- [健康检查参考](../../../reference/server/health-checks.md) - 健康检查配置 *(待创建)*
- [日志记录参考](../../../reference/server/logging.md) - Serilog配置指南 *(待创建)*

---

**文档更新历史**:
- v1.0 (2025-01-29): 初始版本，完整的WebAPI层架构设计文档
