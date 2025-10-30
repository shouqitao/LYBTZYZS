# Server端WebAPI层开发指南

> **文档版本**: v1.0
> **最后更新**: 2025-01-29
> **维护负责**: Server端开发组
> **适用场景**: WebAPI Controller开发、RESTful API设计、JWT认证集成

---

## 📋 目录

1. [开发流程总览](#1-开发流程总览)
2. [环境准备](#2-环境准备)
3. [Controller标准开发流程](#3-controller标准开发流程)
4. [依赖注入与构造函数](#4-依赖注入与构造函数)
5. [Action方法开发](#5-action方法开发)
6. [统一返回格式](#6-统一返回格式)
7. [ServiceResult解包模式](#7-serviceresult解包模式)
8. [认证授权实现](#8-认证授权实现)
9. [异常处理与日志](#9-异常处理与日志)
10. [Swagger文档生成](#10-swagger文档生成)
11. [健康检查端点](#11-健康检查端点)
12. [常见问题与陷阱](#12-常见问题与陷阱)
13. [检查清单](#13-检查清单)
14. [参考资料](#14-参考资料)

---

## 1. 开发流程总览

### 1.1 WebAPI开发5步流程

```
┌──────────────────────────────────────────────────────────┐
│ Step 1: 定义Service接口 (IPatientService)                │
│ - 在LYBT.Server.Interfaces中定义接口契约                │
│ - 定义方法签名、参数和返回值(ServiceResult<T>)          │
└────────────────┬─────────────────────────────────────────┘
                 ↓
┌──────────────────────────────────────────────────────────┐
│ Step 2: 实现Service逻辑 (PatientService)                 │
│ - 在LYBT.Module.Patients中实现业务逻辑                   │
│ - 调用Repository访问数据库                               │
└────────────────┬─────────────────────────────────────────┘
                 ↓
┌──────────────────────────────────────────────────────────┐
│ Step 3: 创建Controller (PatientsController)              │
│ - 在LYBT.WebAPI/Controllers中创建Controller              │
│ - 继承BaseApiController                                  │
│ - 注入IPatientService                                    │
└────────────────┬─────────────────────────────────────────┘
                 ↓
┌──────────────────────────────────────────────────────────┐
│ Step 4: 实现Action方法                                   │
│ - 定义RESTful路由 (GET/POST/PUT/DELETE)                 │
│ - 调用Service获取数据                                    │
│ - 使用BaseApiController辅助方法包装响应                 │
└────────────────┬─────────────────────────────────────────┘
                 ↓
┌──────────────────────────────────────────────────────────┐
│ Step 5: 编写Swagger文档注释                             │
│ - 添加XML注释 (///)                                      │
│ - 添加ProducesResponseType特性                          │
│ - 测试Swagger UI                                         │
└──────────────────────────────────────────────────────────┘
```

### 1.2 WebAPI层 vs Modules层职责划分

| 层级 | 职责 | 禁止 |
|------|------|------|
| **WebAPI层** | • API路由定义<br>• 参数绑定与验证<br>• ApiResponse包装<br>• JWT认证授权<br>• Swagger文档生成<br>• 日志记录 | ❌ 业务逻辑<br>❌ 数据访问<br>❌ 复杂计算<br>❌ 数据验证（FluentValidation） |
| **Modules层** | • 业务逻辑实现<br>• FluentValidation验证<br>• 数据映射 (AutoMapper)<br>• Repository调用<br>• 事务管理 | ❌ HTTP相关逻辑<br>❌ 路由定义<br>❌ ApiResponse包装 |

---

## 2. 环境准备

### 2.1 项目结构

```
LYBTZYZS/
├── src/Server/
│   ├── Services/
│   │   └── LYBT.WebAPI/                  # WebAPI项目
│   │       ├── Controllers/              # 8个模块Controller
│   │       │   ├── AuthController.cs     # 认证模块 (6端点)
│   │       │   ├── UsersController.cs    # 用户管理 (10端点)
│   │       │   ├── PatientsController.cs # 患者管理 (7端点)
│   │       │   ├── MedicalCaseController.cs  # 医案管理 (12端点)
│   │       │   ├── ConsultationController.cs # 诊疗 (8端点)
│   │       │   ├── PrescriptionsController.cs # 处方 (10端点)
│   │       │   ├── HerbsController.cs    # 药材管理 (9端点)
│   │       │   └── FormulaController.cs  # 验方管理 (7端点)
│   │       ├── Middleware/               # 自定义中间件
│   │       │   ├── ExceptionHandlingMiddleware.cs
│   │       │   └── RequestLoggingMiddleware.cs
│   │       ├── Filters/                  # 过滤器
│   │       │   ├── ValidateModelStateAttribute.cs
│   │       │   └── ApiExceptionFilterAttribute.cs
│   │       ├── HealthChecks/             # 健康检查
│   │       │   └── DatabaseHealthCheck.cs
│   │       ├── Program.cs                # 启动配置
│   │       └── appsettings.json          # 配置文件
│   │
│   └── Core/
│       └── LYBT.Infrastructure/
│           └── Web/
│               └── BaseApiController.cs  # Controller基类
```

### 2.2 NuGet依赖包

**必需包**（LYBT.WebAPI.csproj）：
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.*" />
<PackageReference Include="Microsoft.AspNetCore.OutputCaching" Version="8.0.*" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="8.0.*" />
```

### 2.3 appsettings.json配置

**最小配置示例**：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTZYZS;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Desktop",
    "Key": "your-256-bit-secret-key-minimum-32-characters-long",
    "ExpiryMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5001",
      "https://localhost:7001"
    ]
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### 2.4 Program.cs启动配置

**完整启动流程**（核心代码）：
```csharp
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// === Step 1: 配置Serilog结构化日志 ===
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/lybt-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// === Step 2: 服务注册 ===
// 2.1 数据库上下文
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2.2 注册8个业务模块 (每个模块自动注册Repository+Service+Validator+AutoMapper)
builder.Services.AddAuthModule();
builder.Services.AddUsersModule();
builder.Services.AddPatientsModule();
builder.Services.AddMedicalCaseModule();
builder.Services.AddConsultationModule();
builder.Services.AddPrescriptionsModule();
builder.Services.AddHerbsModule();
builder.Services.AddFormulaModule();

// 2.3 控制器和JSON配置
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelStateAttribute>();      // 全局模型验证过滤器
    options.Filters.Add<ApiExceptionFilterAttribute>();      // 全局异常过滤器
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;  // PascalCase
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// 2.4 JWT认证配置
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
            ClockSkew = TimeSpan.Zero  // 移除默认5分钟宽限期
        };
    });

// 2.5 授权策略配置
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DoctorOrAdmin", policy => policy.RequireRole("Doctor", "Admin"));
});

// 2.6 CORS跨域配置
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

// 2.7 Swagger文档配置
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "凌隐宝堂中医诊所管理系统 API",
        Version = "v1",
        Description = "基于ASP.NET Core 8.0的中医诊所管理REST API"
    });

    // JWT认证支持
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // XML注释文档
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 2.8 健康检查配置
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck<CustomHealthCheck>("custom");

var app = builder.Build();

// === Step 3: 中间件管道（顺序严格） ===
app.UseMiddleware<ExceptionHandlingMiddleware>();  // 1. 全局异常处理（最前面）

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();  // 2. Swagger UI（仅开发环境）
}

app.UseHttpsRedirection();  // 3. HTTPS重定向
app.UseCors("AllowDesktopClient");  // 4. CORS（必须在认证授权前）
app.UseMiddleware<RequestLoggingMiddleware>();  // 5. 请求日志
app.UseAuthentication();  // 6. 认证
app.UseAuthorization();  // 7. 授权
app.MapControllers();  // 8. 路由映射
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

app.Run();
```

---

## 3. Controller标准开发流程

### 3.1 RESTful API设计规范

#### 3.1.1 HTTP方法映射

| HTTP方法 | 操作 | 路由示例 | 返回值 |
|---------|------|---------|--------|
| **GET** | 查询列表 | `GET /api/v1/patients` | `ApiResponse<PagedResult<PatientDto>>` |
| **GET** | 查询单个 | `GET /api/v1/patients/{id}` | `ApiResponse<PatientDto>` |
| **POST** | 创建 | `POST /api/v1/patients` | `ApiResponse<PatientDto>` |
| **PUT** | 完整更新 | `PUT /api/v1/patients/{id}` | `ApiResponse<PatientDto>` |
| **DELETE** | 删除 | `DELETE /api/v1/patients/{id}` | `ApiResponse` |

#### 3.1.2 URL命名规范

**✅ 正确示例**：
```
GET    /api/v1/patients                # 查询患者列表
GET    /api/v1/patients/{id}           # 查询单个患者
POST   /api/v1/patients                # 创建患者
PUT    /api/v1/patients/{id}           # 更新患者
DELETE /api/v1/patients/{id}           # 删除患者
GET    /api/v1/patients/search         # 搜索患者（特殊查询）
POST   /api/v1/patients/import         # 批量导入（特殊操作）
GET    /api/v1/patients/import-template # 下载模板（特殊操作）
```

**❌ 错误示例**：
```
GET    /api/v1/GetPatients              # 不应在URL中包含动词
POST   /api/v1/DeletePatient            # DELETE操作不应使用POST
GET    /api/v1/patient/{id}             # 应使用复数形式
GET    /api/v1/patients_list            # 不应使用下划线
```

### 3.2 Controller基本结构

#### 3.2.1 完整Controller模板

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Dtos.Patients;
using LYBT.Server.Interfaces;
using LYBT.Infrastructure.Web;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者管理API控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]  // 默认所有端点需要认证
    public class PatientsController : BaseApiController
    {
        private readonly IPatientService _service;

        /// <summary>
        /// 构造函数 - 依赖注入
        /// </summary>
        public PatientsController(
            IPatientService service,
            IMemoryCache cache,
            ILogger<PatientsController> logger)
            : base(logger, cache)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // Action方法定义...
    }
}
```

**关键特性说明**：
- `[ApiController]`：启用API控制器特性（自动模型验证、参数绑定）
- `[ApiVersion("1")]`：API版本控制
- `[Route("api/v{version:apiVersion}/[controller]")]`：路由模板
- `[Authorize]`：默认所有端点需要认证
- `BaseApiController`：继承基类获取统一响应包装方法

---

## 4. 依赖注入与构造函数

### 4.1 构造函数注入模式

#### 4.1.1 标准构造函数

```csharp
public class PatientsController : BaseApiController
{
    private readonly IPatientService _service;

    // ✅ 正确：通过构造函数注入依赖
    public PatientsController(
        IPatientService service,
        IMemoryCache cache,
        ILogger<PatientsController> logger)
        : base(logger, cache)  // 调用父类构造函数
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }
}
```

**注入项说明**：
- `IPatientService`：业务逻辑服务接口（必需）
- `IMemoryCache`：内存缓存（BaseApiController需要，可选）
- `ILogger<T>`：结构化日志（BaseApiController需要，必需）

#### 4.1.2 禁止的反模式

```csharp
// ❌ 错误1：使用ServiceLocator反模式
public async Task<IActionResult> GetById(Guid id)
{
    var service = HttpContext.RequestServices.GetService<IPatientService>();  // 违反DI原则
    var result = await service.GetByIdAsync(id);
    return Ok(result);
}

// ❌ 错误2：使用字段注入（ASP.NET Core不支持）
public class PatientsController : BaseApiController
{
    [Inject]  // 不支持
    private IPatientService _service;
}

// ❌ 错误3：使用属性注入（不推荐）
public class PatientsController : BaseApiController
{
    public IPatientService Service { get; set; }  // 容易引发NullReferenceException
}
```

### 4.2 BaseApiController辅助方法

**可用的响应包装方法**（从BaseApiController继承）：

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `Success<T>(T data, string message)` | `ApiResponse<T>` | 成功响应（带数据） |
| `Success(string message)` | `ApiResponse` | 成功响应（无数据） |
| `Success<T>(PagedResult<T> pagedResult, string message)` | `ApiResponse<PagedResult<T>>` | 分页响应 |
| `BusinessFail<T>(string message, string? errorCode)` | `ApiResponse<T>` | 业务失败（200） |
| `ValidationFail<T>(string message, string? errorCode)` | `ApiResponse<T>` | 验证失败（400） |
| `Unauthorized<T>(string message, string? errorCode)` | `ApiResponse<T>` | 未授权（401） |
| `NotFound<T>(string message, string? errorCode)` | `ApiResponse<T>` | 资源不存在（404） |
| `HandleServiceResult<T>(ServiceResult<T> result, string successMessage)` | `ApiResponse<T>` | ServiceResult解包 |
| `HandlePagedServiceResult<T>(ServiceResult<PagedResult<T>> result, string successMessage)` | `ApiResponse<PagedResult<T>>` | 分页ServiceResult解包 |

---

## 5. Action方法开发

### 5.1 GET - 查询列表（分页）

#### 5.1.1 完整实现示例

```csharp
/// <summary>
/// 获取患者列表 - 支持分页和关键字查询
/// </summary>
/// <param name="page">页码（默认1）</param>
/// <param name="pageSize">每页数量（默认20，最大100）</param>
/// <param name="keyword">搜索关键字（可选）</param>
/// <returns>患者分页列表</returns>
/// <response code="200">查询成功</response>
/// <response code="400">参数验证失败</response>
/// <response code="401">未授权访问</response>
[HttpGet]
[ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any)]  // 缓存30分钟
[OutputCache(PolicyName = "PatientsCache")]  // 输出缓存
[ProducesResponseType(typeof(ApiResponse<PagedResult<PatientDto>>), 200)]
[ProducesResponseType(typeof(ApiResponse), 400)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> GetList(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? keyword = null)
{
    try
    {
        // Step 1: 参数验证
        if (page <= 0 || pageSize <= 0 || pageSize > 100)
        {
            return ValidationFailPaged<PatientDto>("页码和页大小参数无效（页码>0，页大小1-100）");
        }

        // Step 2: 调用Service获取分页数据
        var result = await _service.GetPagedAsync(page, pageSize, keyword);

        // Step 3: 使用BaseApiController辅助方法包装响应
        return HandlePagedServiceResult(result, "查询成功");
    }
    catch (Exception ex)
    {
        // Step 4: 异常处理（记录日志并返回友好错误）
        return HandleExceptionPaged<PatientDto>(ex, "获取患者列表", new { page, pageSize, keyword });
    }
}
```

**响应示例**（200 OK）：
```json
{
  "Success": true,
  "Message": "查询成功",
  "RequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "Data": {
    "Items": [
      {
        "Id": "...",
        "Name": "张三",
        "Age": 45
      }
    ],
    "TotalCount": 100,
    "CurrentPage": 1,
    "PageSize": 20,
    "TotalPages": 5
  }
}
```

### 5.2 GET - 查询单个资源

#### 5.2.1 完整实现示例

```csharp
/// <summary>
/// 获取患者详情
/// </summary>
/// <param name="id">患者ID</param>
/// <returns>患者详细信息</returns>
/// <response code="200">查询成功</response>
/// <response code="404">患者不存在</response>
/// <response code="401">未授权访问</response>
[HttpGet("{id}")]
[ResponseCache(Duration = 900, VaryByQueryKeys = new[] { "id" })]  // 缓存15分钟
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
[ProducesResponseType(typeof(ApiResponse), 404)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
{
    try
    {
        // Step 1: 参数验证（Guid验证）
        var validationResult = ValidateGuid<PatientDto>(id, "患者ID");
        if (validationResult != null)
        {
            return validationResult;
        }

        // Step 2: 调用Service查询
        var result = await _service.GetByIdAsync(id);

        // Step 3: 检查资源是否存在
        if (!result.IsSuccess || result.Data == null)
        {
            return NotFound<PatientDto>(
                result.ErrorMessage ?? "患者不存在",
                ApiErrorCodes.PATIENTNOTFOUND
            );
        }

        // Step 4: 返回成功响应
        return Success(result.Data, "查询成功");
    }
    catch (Exception ex)
    {
        // Step 5: 异常处理
        return HandleException<PatientDto>(ex, "获取患者详情", id);
    }
}
```

### 5.3 POST - 创建资源

#### 5.3.1 完整实现示例

```csharp
/// <summary>
/// 新增患者
/// </summary>
/// <param name="dto">患者创建DTO</param>
/// <returns>创建的患者信息</returns>
/// <response code="200">创建成功</response>
/// <response code="400">参数验证失败</response>
/// <response code="401">未授权访问</response>
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
[ProducesResponseType(typeof(ApiResponse), 400)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<ActionResult<ApiResponse<PatientDto>>> Add([FromBody] PatientCreateDto dto)
{
    try
    {
        // Step 1: 模型验证（ValidateModelStateAttribute自动验证，这里是兜底检查）
        var validationResult = ValidateModel<PatientDto>();
        if (validationResult != null)
        {
            return validationResult;
        }

        // Step 2: 调用Service创建资源
        var result = await _service.CreateAsync(dto);

        // Step 3: 检查创建结果
        if (!result.IsSuccess || result.Data == null)
        {
            return BusinessFail<PatientDto>(
                result.ErrorMessage ?? "新增患者失败",
                ApiErrorCodes.DATASAVEFAILED
            );
        }

        // Step 4: 记录操作日志
        LogOperation("新增患者成功", result.Data, result.Data.Id);

        // Step 5: 返回成功响应
        return Success(result.Data, "患者创建成功");
    }
    catch (Exception ex)
    {
        // Step 6: 异常处理
        return HandleException<PatientDto>(ex, "新增患者", dto);
    }
}
```

**请求体示例**：
```json
{
  "Name": "张三",
  "Gender": "Male",
  "Age": 45,
  "PhoneNumber": "13800138000",
  "IdCard": "110101198001011234"
}
```

### 5.4 PUT - 更新资源

#### 5.4.1 完整实现示例

```csharp
/// <summary>
/// 更新患者信息
/// </summary>
/// <param name="id">患者ID</param>
/// <param name="dto">患者更新DTO</param>
/// <returns>更新后的患者信息</returns>
/// <response code="200">更新成功</response>
/// <response code="400">参数验证失败</response>
/// <response code="404">患者不存在</response>
/// <response code="401">未授权访问</response>
[HttpPut("{id}")]
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
[ProducesResponseType(typeof(ApiResponse), 400)]
[ProducesResponseType(typeof(ApiResponse), 404)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<ActionResult<ApiResponse<PatientDto>>> Update(
    Guid id,
    [FromBody] PatientUpdateDto dto)
{
    try
    {
        // Step 1: ID验证
        var idValidation = ValidateGuid<PatientDto>(id, "患者ID");
        if (idValidation != null)
        {
            return idValidation;
        }

        // Step 2: 模型验证
        var modelValidation = ValidateModel<PatientDto>();
        if (modelValidation != null)
        {
            return modelValidation;
        }

        // Step 3: 调用Service更新
        var result = await _service.UpdateAsync(id, dto);

        // Step 4: 检查更新结果
        if (!result.IsSuccess || result.Data == null)
        {
            return BusinessFail<PatientDto>(
                result.ErrorMessage ?? "更新患者失败",
                ApiErrorCodes.DATAUPDATEFAILED
            );
        }

        // Step 5: 记录操作日志
        LogOperation("更新患者成功", result.Data, id);

        // Step 6: 返回成功响应
        return Success(result.Data, "患者更新成功");
    }
    catch (Exception ex)
    {
        // Step 7: 异常处理
        return HandleException<PatientDto>(ex, "更新患者", new { id, dto });
    }
}
```

### 5.5 DELETE - 删除资源

#### 5.5.1 完整实现示例

```csharp
/// <summary>
/// 删除患者（软删除）
/// </summary>
/// <param name="id">患者ID</param>
/// <returns>删除结果</returns>
/// <response code="200">删除成功</response>
/// <response code="404">患者不存在</response>
/// <response code="401">未授权访问</response>
[HttpDelete("{id}")]
[ProducesResponseType(typeof(ApiResponse), 200)]
[ProducesResponseType(typeof(ApiResponse), 404)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<ActionResult<ApiResponse>> Delete(Guid id)
{
    try
    {
        // Step 1: 参数验证
        var validationResult = ValidateGuid(id, "患者ID");
        if (validationResult != null)
        {
            return validationResult;
        }

        // Step 2: 调用Service删除
        var result = await _service.DeleteAsync(id);

        // Step 3: 检查删除结果
        if (!result.IsSuccess)
        {
            return NotFound("患者不存在", ApiErrorCodes.PATIENTNOTFOUND);
        }

        // Step 4: 记录操作日志
        LogOperation("删除患者成功", null, id);

        // Step 5: 返回成功响应
        return Success("删除成功");
    }
    catch (Exception ex)
    {
        // Step 6: 异常处理
        return HandleException(ex, "删除患者", id);
    }
}
```

### 5.6 POST - 文件上传（批量导入）

#### 5.6.1 完整实现示例

```csharp
/// <summary>
/// 批量导入患者数据
/// </summary>
/// <param name="file">Excel文件（.xlsx格式）</param>
/// <returns>导入结果，包含成功/失败数量和详细错误信息</returns>
/// <response code="200">导入完成（可能部分失败）</response>
/// <response code="400">文件验证失败</response>
/// <response code="401">未授权访问</response>
[HttpPost("import")]
[RequestSizeLimit(10 * 1024 * 1024)]  // 限制10MB
[ProducesResponseType(typeof(ApiResponse<ImportResultDto<PatientDto>>), 200)]
[ProducesResponseType(typeof(ApiResponse), 400)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<ActionResult<ApiResponse<ImportResultDto<PatientDto>>>> Import(IFormFile file)
{
    try
    {
        // Step 1: 文件存在性验证
        if (file == null || file.Length == 0)
        {
            return ValidationFail<ImportResultDto<PatientDto>>("文件不能为空");
        }

        // Step 2: 文件扩展名验证
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            return ValidationFail<ImportResultDto<PatientDto>>("仅支持.xlsx格式的Excel文件");
        }

        // Step 3: 文件大小验证（10MB）
        if (file.Length > 10 * 1024 * 1024)
        {
            return ValidationFail<ImportResultDto<PatientDto>>("文件大小不能超过10MB");
        }

        // Step 4: 导入数据
        using var stream = file.OpenReadStream();
        var result = await _service.ImportFromExcelAsync(stream, file.FileName);

        if (!result.IsSuccess || result.Data == null)
        {
            return BusinessFail<ImportResultDto<PatientDto>>(
                result.ErrorMessage ?? "导入失败",
                ApiErrorCodes.DATASAVEFAILED
            );
        }

        // Step 5: 记录操作日志
        LogOperation("批量导入患者",
            new { FileName = file.FileName, TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount },
            null);

        // Step 6: 返回导入结果
        return Success(result.Data, result.Data.Message);
    }
    catch (Exception ex)
    {
        return HandleException<ImportResultDto<PatientDto>>(ex, "批量导入患者", new { FileName = file?.FileName });
    }
}
```

**响应示例**（200 OK）：
```json
{
  "Success": true,
  "Message": "导入完成：成功50条，失败2条",
  "Data": {
    "TotalCount": 52,
    "SuccessCount": 50,
    "FailedCount": 2,
    "FailedItems": [
      {
        "RowNumber": 5,
        "ErrorMessage": "姓名不能为空",
        "Data": { "Name": "", "Age": 45 }
      },
      {
        "RowNumber": 12,
        "ErrorMessage": "手机号格式错误",
        "Data": { "Name": "李四", "PhoneNumber": "123" }
      }
    ]
  }
}
```

### 5.7 GET - 文件下载（模板下载）

#### 5.7.1 完整实现示例

```csharp
/// <summary>
/// 下载患者导入模板
/// </summary>
/// <returns>包含示例数据的Excel模板文件</returns>
/// <response code="200">下载成功</response>
/// <response code="500">生成模板失败</response>
[HttpGet("import-template")]
[AllowAnonymous]  // 模板下载不需要认证
[ProducesResponseType(typeof(FileContentResult), 200)]
[ProducesResponseType(typeof(ApiResponse), 500)]
public ActionResult ExportTemplate()
{
    try
    {
        // Step 1: 调用Service生成模板
        var stream = _service.GenerateImportTemplate();

        // Step 2: 生成文件名（带时间戳）
        var fileName = $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

        // Step 3: 返回文件流
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "生成患者导入模板失败");
        return StatusCode(500);
    }
}
```

---

## 6. 统一返回格式

### 6.1 ApiResponse<T>结构

#### 6.1.1 类型定义

```csharp
// 基础响应（无数据）
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public object? Errors { get; set; }
}

// 泛型响应（带数据）
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}

// 分页响应
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

### 6.2 响应格式示例

#### 6.2.1 成功响应（带数据）

```json
{
  "Success": true,
  "Message": "查询成功",
  "RequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "Data": {
    "Id": "550e8400-e29b-41d4-a716-446655440000",
    "Name": "张三",
    "Gender": "Male",
    "Age": 45,
    "PhoneNumber": "13800138000",
    "CreatedAt": "2025-01-29T10:15:30Z"
  },
  "Errors": null
}
```

#### 6.2.2 失败响应（验证错误）

```json
{
  "Success": false,
  "Message": "参数验证失败",
  "RequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "Data": null,
  "Errors": {
    "code": "VALIDATION_ERROR",
    "details": [
      "姓名不能为空",
      "年龄必须在0-150之间"
    ]
  }
}
```

#### 6.2.3 分页响应

```json
{
  "Success": true,
  "Message": "查询成功",
  "RequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "Data": {
    "Items": [
      { "Id": "...", "Name": "张三", "Age": 45 },
      { "Id": "...", "Name": "李四", "Age": 38 }
    ],
    "TotalCount": 100,
    "CurrentPage": 1,
    "PageSize": 20,
    "TotalPages": 5
  },
  "Errors": null
}
```

### 6.3 BaseApiController辅助方法使用

#### 6.3.1 成功响应方法

```csharp
// 1. 成功响应（带数据）
return Success(patient, "查询成功");
// 返回: ApiResponse<PatientDto> with HTTP 200

// 2. 成功响应（无数据）
return Success("删除成功");
// 返回: ApiResponse with HTTP 200

// 3. 分页响应
return Success(pagedResult, "查询成功");
// 返回: ApiResponse<PagedResult<PatientDto>> with HTTP 200
```

#### 6.3.2 失败响应方法

```csharp
// 1. 验证失败（HTTP 400）
return ValidationFail<PatientDto>("姓名不能为空");

// 2. 业务失败（HTTP 200，但Success=false）
return BusinessFail<PatientDto>("患者已存在", ApiErrorCodes.DUPLICATERECORD);

// 3. 未授权（HTTP 401）
return Unauthorized<PatientDto>("未授权访问");

// 4. 资源不存在（HTTP 404）
return NotFound<PatientDto>("患者不存在", ApiErrorCodes.PATIENTNOTFOUND);
```

---

## 7. ServiceResult解包模式

### 7.1 ServiceResult<T>结构

**定义位置**: `LYBT.Shared.Models/Common/ServiceResult.cs`

```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### 7.2 HandleServiceResult辅助方法

#### 7.2.1 标准解包模式

```csharp
// 在Controller中调用Service
var result = await _service.GetByIdAsync(id);

// 使用HandleServiceResult自动解包
return HandleServiceResult(result, "查询成功");

// 等价于手动解包:
if (result.IsSuccess && result.Data != null)
{
    return Success(result.Data, "查询成功");
}
else
{
    return BusinessFail<PatientDto>(result.ErrorMessage ?? "查询失败");
}
```

#### 7.2.2 分页结果解包

```csharp
// 调用Service获取分页数据
var result = await _service.GetPagedAsync(page, pageSize, keyword);

// 使用HandlePagedServiceResult解包
return HandlePagedServiceResult(result, "查询成功");

// 等价于手动解包:
if (result.IsSuccess && result.Data != null)
{
    return Success(result.Data, "查询成功");
}
else
{
    return BusinessFail<PagedResult<PatientDto>>(result.ErrorMessage ?? "查询失败");
}
```

#### 7.2.3 HandleBoolServiceResult（无返回数据）

```csharp
// 调用Service执行删除
var result = await _service.DeleteAsync(id);

// 使用HandleBoolServiceResult解包
return HandleBoolServiceResult(result, "删除成功");

// 等价于手动解包:
if (result.IsSuccess)
{
    return Success("删除成功");
}
else
{
    return BusinessFail(result.ErrorMessage ?? "删除失败");
}
```

### 7.3 完整示例：Service调用到响应

```csharp
// Service层实现（LYBT.Module.Patients/Services/PatientService.cs）
public class PatientService : IPatientService
{
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return ServiceResult<PatientDto>.Fail("患者不存在");
            }

            var dto = _mapper.Map<PatientDto>(entity);
            return ServiceResult<PatientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询患者失败: {PatientId}", id);
            return ServiceResult<PatientDto>.Fail("查询患者失败");
        }
    }
}

// Controller层解包（LYBT.WebAPI/Controllers/PatientsController.cs）
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
{
    try
    {
        var result = await _service.GetByIdAsync(id);

        // 一行代码完成ServiceResult → ApiResponse转换
        return HandleServiceResult(result, "查询成功");
    }
    catch (Exception ex)
    {
        return HandleException<PatientDto>(ex, "获取患者详情", id);
    }
}
```

---

## 8. 认证授权实现

### 8.1 JWT Token认证

#### 8.1.1 登录端点实现

```csharp
/// <summary>
/// 用户登录
/// </summary>
/// <param name="request">登录请求</param>
/// <returns>登录响应，包含JWT Token</returns>
[HttpPost("login")]
[AllowAnonymous]  // 登录端点允许匿名访问
[EnableRateLimiting("Login")]  // 启用登录限流保护，防暴力破解
[ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
[ProducesResponseType(typeof(ApiResponse), 400)]
public async Task<ActionResult<ApiResponse<LoginResponse>>> LoginAsync([FromBody] LoginRequest request)
{
    try
    {
        // Step 1: 参数验证
        var validation = ValidateModel<LoginResponse>();
        if (validation != null)
        {
            return validation;
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return ValidationFail<LoginResponse>("用户名不能为空");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationFail<LoginResponse>("密码不能为空");
        }

        // Step 2: 调用认证服务进行登录
        var result = await _authService.LoginAsync(request);

        // Step 3: 返回响应（包含JWT Token）
        return HandleServiceResult(result, "登录成功");
    }
    catch (Exception ex)
    {
        return HandleException<LoginResponse>(ex, "用户登录", request);
    }
}
```

**LoginResponse结构**：
```csharp
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;          // JWT Access Token
    public string? RefreshToken { get; set; }                  // Refresh Token（可选）
    public int ExpiresIn { get; set; } = 3600;                 // Token过期时间（秒）
    public UserDto User { get; set; } = new();                 // 用户信息
}
```

**响应示例**（200 OK）：
```json
{
  "Success": true,
  "Message": "登录成功",
  "Data": {
    "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "RefreshToken": "a7b3c5d9e1f2a4b6c8d0e2f4a6b8c0d2",
    "ExpiresIn": 3600,
    "User": {
      "Id": "550e8400-e29b-41d4-a716-446655440000",
      "UserName": "admin",
      "RealName": "张三",
      "Role": "Admin"
    }
  }
}
```

#### 8.1.2 Token验证端点

```csharp
/// <summary>
/// 验证Token（从Authorization header获取）
/// </summary>
/// <returns>验证结果包含token有效性、用户信息和过期时间</returns>
[HttpGet("validate")]
[ProducesResponseType(typeof(ApiResponse<object>), 200)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<ActionResult<ApiResponse<object>>> ValidateTokenFromHeaderAsync()
{
    try
    {
        // Step 1: 从Authorization header中提取Token
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return Unauthorized(new { valid = false, message = "Missing Authorization header" });
        }

        // Step 2: 检查Bearer格式
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { valid = false, message = "Invalid Authorization header format" });
        }

        // Step 3: 提取token
        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { valid = false, message = "Missing token in Authorization header" });
        }

        // Step 4: 调用认证服务验证Token
        var result = await _authService.ValidateTokenAsync(token);

        if (result.IsSuccess && result.Data == true)
        {
            // Token有效，返回详细信息
            var sessionInfo = await _authService.GetSessionInfoAsync(token);
            object response = new
            {
                valid = true,
                sub = sessionInfo.Data,
                message = "Token is valid"
            };
            return Success(response, "Token验证成功");
        }
        else
        {
            // Token无效
            return Unauthorized(new { valid = false, message = result.ErrorMessage ?? "Token is invalid" });
        }
    }
    catch (Exception ex)
    {
        return HandleException<object>(ex, "验证Token从Header", null);
    }
}
```

### 8.2 授权策略应用

#### 8.2.1 Controller级授权

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]  // 所有端点默认需要认证
public class PatientsController : BaseApiController
{
    // 所有Action默认需要认证
}
```

#### 8.2.2 Action级授权

```csharp
[HttpGet]
public async Task<IActionResult> GetList()  // 所有已认证用户可访问
{
}

[HttpPost]
[Authorize(Policy = "DoctorOrAdmin")]  // 仅医生或管理员可访问
public async Task<IActionResult> Create([FromBody] PatientCreateDto dto)
{
}

[HttpDelete("{id}")]
[Authorize(Policy = "AdminOnly")]  // 仅管理员可访问
public async Task<IActionResult> Delete(Guid id)
{
}
```

#### 8.2.3 匿名访问端点

```csharp
[HttpPost("login")]
[AllowAnonymous]  // 覆盖Controller级[Authorize]，允许匿名访问
public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
{
}

[HttpGet("import-template")]
[AllowAnonymous]  // 模板下载不需要认证
public ActionResult ExportTemplate()
{
}
```

### 8.3 Client端Token使用

**Client端（WPF Desktop）认证流程**：

```csharp
// Step 1: 用户登录
var loginRequest = new LoginRequest { UserName = "admin", Password = "password" };
var response = await _apiClient.PostAsync<LoginResponse>("/api/v1/auth/login", loginRequest);

if (response.Success && response.Data != null)
{
    // Step 2: 保存Token到内存（SessionManager）
    _sessionManager.SetSession(
        user: response.Data.User,
        accessToken: response.Data.Token,
        refreshToken: response.Data.RefreshToken
    );
}

// Step 3: 后续请求携带Token
// ApiClient自动从SessionManager获取Token并添加到Authorization header
var patients = await _apiClient.GetAsync<List<PatientDto>>("/api/v1/patients");
```

---

## 9. 异常处理与日志

### 9.1 Controller异常处理模式

#### 9.1.1 标准try-catch模式

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
{
    try
    {
        // 业务逻辑
        var result = await _service.GetByIdAsync(id);
        return HandleServiceResult(result, "查询成功");
    }
    catch (Exception ex)
    {
        // 使用HandleException辅助方法统一处理异常
        return HandleException<PatientDto>(ex, "获取患者详情", id);
    }
}
```

**HandleException方法功能**：
1. 记录结构化日志（包含异常堆栈、操作名称、参数）
2. 返回友好错误消息（隐藏技术细节）
3. 返回统一ApiResponse格式（HTTP 500）

#### 9.1.2 全局异常处理中间件

**ExceptionHandlingMiddleware**自动捕获所有未处理异常：

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);  // 调用下游中间件
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

### 9.2 结构化日志记录

#### 9.2.1 日志级别使用规范

| 日志级别 | 使用场景 | 示例 |
|---------|---------|------|
| **Information** | 正常业务流程 | 用户登录成功、患者创建成功、查询完成 |
| **Warning** | 可恢复的错误 | 参数验证失败、业务规则违反、资源不存在 |
| **Error** | 需要关注的错误 | 数据库连接失败、Service异常、外部API调用失败 |
| **Critical** | 严重错误（需要立即处理） | 应用程序启动失败、配置缺失、数据损坏 |

#### 9.2.2 结构化日志示例

**✅ 正确示例（结构化日志）**：
```csharp
_logger.LogInformation(
    "创建患者成功: {PatientId}, {PatientName}, {Age}",
    patient.Id,
    patient.Name,
    patient.Age
);

// 输出（JSON格式，可查询）:
{
  "Timestamp": "2025-01-29T10:15:32.123Z",
  "Level": "Information",
  "Message": "创建患者成功: 550e8400-e29b-41d4-a716-446655440000, 张三, 45",
  "PatientId": "550e8400-e29b-41d4-a716-446655440000",
  "PatientName": "张三",
  "Age": 45
}
```

**❌ 错误示例（字符串拼接）**：
```csharp
_logger.LogInformation(
    $"创建患者成功: {patient.Id}, {patient.Name}, {patient.Age}"
);

// 输出（纯文本，难以查询）:
"创建患者成功: 550e8400-e29b-41d4-a716-446655440000, 张三, 45"
```

#### 9.2.3 操作日志记录

**LogOperation辅助方法**（BaseApiController提供）：

```csharp
// 成功操作日志
LogOperation("新增患者成功", result.Data, result.Data.Id);
// 输出: Information级别日志，包含操作名称、数据快照、资源ID

// 失败操作日志
LogOperation("新增患者失败", dto, null);
// 输出: Warning级别日志，包含操作名称、输入DTO

// 异常日志（HandleException自动记录）
return HandleException<PatientDto>(ex, "获取患者详情", id);
// 输出: Error级别日志，包含异常堆栈、操作名称、参数
```

### 9.3 日志查询与分析

**日志文件位置**：
```
logs/
├── lybt-20250129.txt    # 2025-01-29日志
├── lybt-20250128.txt    # 2025-01-28日志
├── lybt-20250127.txt    # 2025-01-27日志
...
└── lybt-20250101.txt    # 自动删除30天前的日志
```

**日志查询示例**：
```bash
# 查询特定患者的操作日志
grep "PatientId\":\"550e8400" logs/lybt-20250129.txt

# 查询Error级别日志
grep "\"Level\":\"Error\"" logs/lybt-20250129.txt

# 统计API请求数量
grep "HTTP" logs/lybt-20250129.txt | wc -l
```

---

## 10. Swagger文档生成

### 10.1 XML注释规范

#### 10.1.1 完整注释示例

```csharp
/// <summary>
/// 获取患者详情
/// </summary>
/// <param name="id">患者ID（Guid格式）</param>
/// <returns>患者详细信息，包含基本信息和创建时间</returns>
/// <remarks>
/// 示例请求:
///
///     GET /api/v1/patients/550e8400-e29b-41d4-a716-446655440000
///
/// </remarks>
/// <response code="200">查询成功，返回患者详情</response>
/// <response code="404">患者不存在</response>
/// <response code="401">未授权访问，需要JWT Token</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
[ProducesResponseType(typeof(ApiResponse), 404)]
[ProducesResponseType(typeof(ApiResponse), 401)]
public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
{
    // ...
}
```

**XML注释标签说明**：
- `<summary>`：方法功能简述（必需）
- `<param>`：参数说明（必需）
- `<returns>`：返回值说明（必需）
- `<remarks>`：详细说明、示例请求（可选）
- `<response code="X">`：HTTP状态码说明（推荐）

#### 10.1.2 ProducesResponseType特性

**作用**：
1. 为Swagger UI生成准确的响应示例
2. 提供强类型响应定义
3. 改善API文档可读性

**使用规范**：
```csharp
// 查询单个资源（200 OK, 404 Not Found, 401 Unauthorized）
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
[ProducesResponseType(typeof(ApiResponse), 404)]
[ProducesResponseType(typeof(ApiResponse), 401)]

// 查询列表（200 OK, 400 Bad Request, 401 Unauthorized）
[ProducesResponseType(typeof(ApiResponse<PagedResult<PatientDto>>), 200)]
[ProducesResponseType(typeof(ApiResponse), 400)]
[ProducesResponseType(typeof(ApiResponse), 401)]

// 创建资源（200 OK, 400 Bad Request, 401 Unauthorized）
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
[ProducesResponseType(typeof(ApiResponse), 400)]
[ProducesResponseType(typeof(ApiResponse), 401)]

// 删除资源（200 OK, 404 Not Found, 401 Unauthorized）
[ProducesResponseType(typeof(ApiResponse), 200)]
[ProducesResponseType(typeof(ApiResponse), 404)]
[ProducesResponseType(typeof(ApiResponse), 401)]
```

### 10.2 Swagger UI使用

#### 10.2.1 访问Swagger UI

**开发环境访问**：
- URL: `https://localhost:7001/swagger`
- 自动加载所有Controller和端点
- 支持JWT Token测试
- 交互式API测试

**Swagger UI界面功能**：
1. **API端点列表**：按Controller分组显示所有端点
2. **模型定义**：显示DTO结构和字段说明
3. **Try it out**：交互式测试端点
4. **Authorize**：配置JWT Bearer Token
5. **Example Value**：自动生成请求/响应示例

#### 10.2.2 JWT Token测试流程

**Step 1: 登录获取Token**
```
1. 展开 POST /api/v1/auth/login 端点
2. 点击 "Try it out"
3. 输入登录请求体:
   {
     "UserName": "admin",
     "Password": "password"
   }
4. 点击 "Execute"
5. 复制响应中的 "Token" 字段
```

**Step 2: 配置Authorization**
```
1. 点击页面右上角 "Authorize" 按钮
2. 在弹出框的 "Value" 输入框中输入: Bearer {token}
   示例: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
3. 点击 "Authorize"
4. 关闭弹出框
```

**Step 3: 测试需要认证的端点**
```
1. 展开任何需要认证的端点（如 GET /api/v1/patients）
2. 点击 "Try it out"
3. 填写参数（如page=1, pageSize=20）
4. 点击 "Execute"
5. 查看响应结果（应返回200 OK而非401 Unauthorized）
```

### 10.3 隐藏特定端点

**场景**：某些端点不希望在Swagger文档中显示（如内部管理端点）

**使用方法**：
```csharp
/// <summary>
/// 超级管理员登录（隐藏端点）
/// </summary>
[HttpPost("admin/login")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]  // 从Swagger文档中隐藏
public async Task<ActionResult<ApiResponse<LoginResponse>>> SuperAdminLoginAsync(
    [FromBody] SuperAdminLoginRequest request)
{
    // ...
}
```

---

## 11. 健康检查端点

### 11.1 数据库健康检查

**实现位置**: `LYBT.WebAPI/HealthChecks/DatabaseHealthCheck.cs`

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: 检查数据库连接
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            // Step 2: 检查核心表是否存在并可访问
            var usersCount = await _dbContext.Users.CountAsync(cancellationToken);
            var patientsCount = await _dbContext.Patients.CountAsync(cancellationToken);

            // Step 3: 返回健康状态
            return HealthCheckResult.Healthy(
                $"数据库连接正常，用户表: {usersCount}条, 患者表: {patientsCount}条"
            );
        }
        catch (Exception ex)
        {
            // 返回不健康状态
            return HealthCheckResult.Unhealthy(
                "数据库连接失败",
                ex
            );
        }
    }
}
```

### 11.2 健康检查端点配置

**注册健康检查**（Program.cs）：
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")  // 数据库检查
    .AddCheck<DatabaseHealthCheck>("database-detail");  // 自定义详细检查
```

**配置健康检查端点**：
```csharp
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

### 11.3 健康检查响应示例

**正常状态（200 OK）**：
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "数据库连接正常，用户表: 5条, 患者表: 100条",
      "duration": 125.5
    }
  ],
  "totalDuration": 125.5
}
```

**异常状态（503 Service Unavailable）**：
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

---

## 12. 常见问题与陷阱

### 12.1 问题1：中间件顺序错误

#### ❌ 错误示例

```csharp
// 错误：CORS在认证之后（会导致预检请求失败）
app.UseAuthentication();
app.UseCors("AllowDesktopClient");  // 错误位置

// 错误：异常处理不在最前面（无法捕获认证中间件的异常）
app.UseAuthentication();
app.UseMiddleware<ExceptionHandlingMiddleware>();  // 错误位置
```

**问题表现**：
- Client端OPTIONS预检请求返回401 Unauthorized
- 认证失败异常未被ExceptionHandlingMiddleware捕获

#### ✅ 正确示例

```csharp
// 正确：严格遵守中间件顺序
app.UseMiddleware<ExceptionHandlingMiddleware>();  // 最前面
app.UseCors("AllowDesktopClient");  // 认证之前
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### 12.2 问题2：ServiceLocator反模式

#### ❌ 错误示例

```csharp
public async Task<IActionResult> GetById(Guid id)
{
    // 错误：使用ServiceLocator获取依赖
    var service = HttpContext.RequestServices.GetService<IPatientService>();
    var result = await service.GetByIdAsync(id);
    return Ok(result);
}
```

**问题表现**：
- 违反依赖注入原则
- 单元测试困难
- 依赖关系不清晰

#### ✅ 正确示例

```csharp
private readonly IPatientService _service;

public PatientsController(IPatientService service, ILogger<PatientsController> logger)
    : base(logger, null)
{
    _service = service;  // 构造函数注入
}

public async Task<IActionResult> GetById(Guid id)
{
    var result = await _service.GetByIdAsync(id);
    return Ok(result);
}
```

### 12.3 问题3：未使用BaseApiController辅助方法

#### ❌ 错误示例

```csharp
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _service.GetByIdAsync(id);

    // 错误：手动构造ApiResponse（重复代码）
    if (result.IsSuccess && result.Data != null)
    {
        return Ok(new ApiResponse<PatientDto>
        {
            Success = true,
            Data = result.Data,
            Message = "查询成功"
        });
    }
    else
    {
        return BadRequest(new ApiResponse<PatientDto>
        {
            Success = false,
            Message = result.ErrorMessage ?? "查询失败"
        });
    }
}
```

#### ✅ 正确示例

```csharp
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _service.GetByIdAsync(id);

    // 正确：使用HandleServiceResult自动解包
    return HandleServiceResult(result, "查询成功");
}
```

### 12.4 问题4：异步方法使用阻塞调用

#### ❌ 错误示例

```csharp
[HttpGet("{id}")]
public IActionResult GetById(Guid id)
{
    // 错误：使用.Result阻塞调用（降低吞吐量）
    var patient = _service.GetByIdAsync(id).Result;
    return Ok(patient);
}
```

**问题表现**：
- 线程阻塞，降低并发性能
- 可能导致死锁

#### ✅ 正确示例

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    // 正确：使用async/await异步调用
    var result = await _service.GetByIdAsync(id);
    return HandleServiceResult(result, "查询成功");
}
```

### 12.5 问题5：未记录操作日志

#### ❌ 错误示例

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] PatientCreateDto dto)
{
    var result = await _service.CreateAsync(dto);

    // 错误：创建成功但未记录日志
    return HandleServiceResult(result, "创建成功");
}
```

#### ✅ 正确示例

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] PatientCreateDto dto)
{
    var result = await _service.CreateAsync(dto);

    if (result.IsSuccess && result.Data != null)
    {
        // 正确：记录操作日志（审计追踪）
        LogOperation("新增患者成功", result.Data, result.Data.Id);
    }

    return HandleServiceResult(result, "创建成功");
}
```

---

## 13. 检查清单

### 13.1 Controller创建检查清单

- [ ] **1. Controller类定义**
  - [ ] 继承`BaseApiController`
  - [ ] 添加`[ApiController]`特性
  - [ ] 添加`[ApiVersion("1")]`特性
  - [ ] 添加`[Route("api/v{version:apiVersion}/[controller]")]`特性
  - [ ] 添加`[Authorize]`特性（默认需要认证）

- [ ] **2. 构造函数注入**
  - [ ] 注入IXxxService接口
  - [ ] 注入IMemoryCache（可选）
  - [ ] 注入ILogger<TController>
  - [ ] 调用父类构造函数: `base(logger, cache)`
  - [ ] 添加空值检查: `?? throw new ArgumentNullException(...)`

- [ ] **3. XML注释**
  - [ ] Controller类添加`<summary>`注释
  - [ ] 所有Action方法添加完整XML注释
  - [ ] 添加`<param>`、`<returns>`、`<response>`标签

- [ ] **4. ProducesResponseType特性**
  - [ ] 所有Action添加200成功响应类型
  - [ ] 添加400/401/404等错误响应类型
  - [ ] 泛型类型使用`typeof(ApiResponse<T>)`

### 13.2 Action方法开发检查清单

- [ ] **1. 路由与HTTP方法**
  - [ ] 使用正确的HTTP方法特性（[HttpGet]/[HttpPost]/[HttpPut]/[HttpDelete]）
  - [ ] 路由模板符合RESTful规范
  - [ ] 使用复数形式资源名称

- [ ] **2. 参数绑定**
  - [ ] GET方法使用`[FromQuery]`
  - [ ] POST/PUT方法使用`[FromBody]`
  - [ ] 路由参数使用`{id}`占位符

- [ ] **3. 参数验证**
  - [ ] Guid参数使用`ValidateGuid`验证
  - [ ] DTO参数使用`ValidateModel`验证
  - [ ] 分页参数验证范围（page>0, pageSize:1-100）

- [ ] **4. Service调用**
  - [ ] 使用`await`异步调用Service
  - [ ] 使用`HandleServiceResult`解包ServiceResult
  - [ ] 分页数据使用`HandlePagedServiceResult`

- [ ] **5. 异常处理**
  - [ ] 包裹在try-catch块中
  - [ ] 使用`HandleException`记录日志并返回友好错误

- [ ] **6. 操作日志**
  - [ ] 成功操作调用`LogOperation`
  - [ ] 记录操作名称、数据快照、资源ID

### 13.3 认证授权检查清单

- [ ] **1. Controller级授权**
  - [ ] 添加`[Authorize]`特性（默认需要认证）
  - [ ] 公开端点使用`[AllowAnonymous]`覆盖

- [ ] **2. Action级授权**
  - [ ] 敏感操作添加`[Authorize(Policy = "...")]`
  - [ ] 管理员操作使用`AdminOnly`策略
  - [ ] 医生操作使用`DoctorOrAdmin`策略

- [ ] **3. JWT Token**
  - [ ] 登录端点返回`LoginResponse`（包含Token）
  - [ ] Token验证端点正确解析Authorization header
  - [ ] Client端正确存储和使用Token

### 13.4 Swagger文档检查清单

- [ ] **1. XML注释**
  - [ ] 所有Controller添加`<summary>`
  - [ ] 所有Action添加完整XML注释
  - [ ] `<param>`、`<returns>`、`<response>`完整

- [ ] **2. ProducesResponseType**
  - [ ] 所有Action添加响应类型特性
  - [ ] 泛型类型正确（`ApiResponse<T>`）
  - [ ] 状态码与实际返回一致

- [ ] **3. Swagger UI测试**
  - [ ] 可以访问`/swagger`端点
  - [ ] 所有端点正确显示
  - [ ] JWT Token测试流程正常
  - [ ] Try it out功能正常

### 13.5 日志与监控检查清单

- [ ] **1. 结构化日志**
  - [ ] 使用占位符语法（`{Key}`）
  - [ ] 避免字符串拼接（`$"..."`）
  - [ ] 包含关键业务字段（ID、名称、时间）

- [ ] **2. 日志级别**
  - [ ] Information: 正常业务流程
  - [ ] Warning: 可恢复错误
  - [ ] Error: 需要关注的异常
  - [ ] Critical: 严重错误

- [ ] **3. 操作审计**
  - [ ] 创建/更新/删除操作记录日志
  - [ ] 日志包含用户信息（如果可用）
  - [ ] 敏感操作记录详细参数

---

## 14. 参考资料

### 14.1 架构文档

- [Server端WebAPI层架构设计](../../explanation/architecture/server/webapi-design.md) - 完整架构说明（1374行）
- [Server端三层架构总览](../../explanation/architecture/server/README.md) - Server端架构概览
- [Interfaces层设计](../../explanation/architecture/server/interfaces-layer-design.md) - 服务接口定义规范

### 14.2 相关开发指南

- [Interfaces层使用指南](./interfaces-usage.md) - Service接口开发规范
- [DTO开发指南](../shared/dto-development.md) - DTO设计与验证
- [认证集成指南](./auth-integration.md) - JWT认证详细指南 *(待创建)*
- [WebAPI部署指南](./webapi-deployment.md) - IIS/Docker部署 *(待创建)*

### 14.3 API参考

- [API端点快速参考](../../reference/quick-reference/api-endpoints.md) - 90+端点索引 *(待创建)*
- [健康检查参考](../../reference/server/health-checks.md) - 健康检查配置 *(待创建)*
- [日志记录参考](../../reference/server/logging.md) - Serilog配置指南 *(待创建)*

### 14.4 源代码位置

- **Controllers**: `src/Server/Services/LYBT.WebAPI/Controllers/`
- **BaseApiController**: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`
- **Middleware**: `src/Server/Services/LYBT.WebAPI/Middleware/`
- **HealthChecks**: `src/Server/Services/LYBT.WebAPI/HealthChecks/`
- **Program.cs**: `src/Server/Services/LYBT.WebAPI/Program.cs`

### 14.5 外部资源

- [ASP.NET Core官方文档](https://learn.microsoft.com/zh-cn/aspnet/core/)
- [RESTful API设计指南](https://restfulapi.net/)
- [JWT认证最佳实践](https://datatracker.ietf.org/doc/html/rfc8725)
- [Serilog结构化日志](https://serilog.net/)
- [Swagger/OpenAPI规范](https://swagger.io/specification/)

---

**文档更新历史**：
- v1.0 (2025-01-29): 初始版本，完整的WebAPI开发指南（13章节，包含完整代码示例）
