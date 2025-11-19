# LYBT.WebAPI - Web API核心服务

## 📦 项目定位

- **层级**:Server端
- **类型**:Web服务层(ASP.NET Core Web API)
- **职责**:作为系统的统一API网关，集成8个业务模块（Auth、Users、Patients、MedicalCase、Consultation、Prescriptions、Herbs、Formula），通过RESTful API对外提供完整的中医诊所管理功能。基于ASP.NET Core 8.0构建，采用分层架构设计，支持JWT认证、Swagger文档、全局异常处理、结构化日志、健康检查等企业级特性。专为小型中医诊所(<20人)优化，提供高性能、可靠、安全的后端服务。

## 📂 代码结构

```
LYBT.WebAPI/
├── Controllers/                         # API控制器层(8个模块控制器)
│   ├── AuthController.cs                # 认证控制器(登录/登出/Token刷新) - 5个端点
│   ├── UsersController.cs               # 用户管理控制器(CRUD/角色管理) - 12个端点
│   ├── PatientsController.cs            # 患者管理控制器(档案管理/搜索) - 10个端点
│   ├── MedicalCasesController.cs        # 医案管理控制器(医案流程/状态管理) - 15个端点
│   ├── ConsultationsController.cs       # 诊疗控制器(诊断记录/四诊管理) - 8个端点
│   ├── PrescriptionsController.cs       # 处方管理控制器(处方CRUD/配药管理) - 18个端点
│   ├── HerbsController.cs               # 药材管理控制器(药材信息/导入导出) - 13个端点
│   └── FormulasController.cs            # 验方管理控制器(验方模板/克隆) - 19个端点
├── Middleware/                          # 自定义中间件
│   ├── ExceptionHandlingMiddleware.cs   # 全局异常处理中间件
│   └── RequestLoggingMiddleware.cs      # 请求日志记录中间件
├── Extensions/                          # 扩展方法
│   ├── ServiceCollectionExtensions.cs   # 服务注册扩展(模块注册/认证/Swagger/CORS)
│   └── WebApplicationExtensions.cs      # 应用配置扩展(中间件管道/健康检查)
├── Filters/                             # 过滤器
│   ├── ValidateModelStateAttribute.cs   # 模型验证过滤器
│   └── ApiExceptionFilterAttribute.cs   # API异常过滤器
├── HealthChecks/                        # 健康检查
│   ├── DatabaseHealthCheck.cs           # 数据库健康检查
│   └── CustomHealthCheck.cs             # 自定义健康检查
├── Program.cs                           # 应用程序启动入口(服务注册/中间件配置/启动流程)
├── appsettings.json                     # 应用配置文件(数据库/JWT/日志/CORS/Swagger)
├── appsettings.Development.json         # 开发环境配置(开发数据库/详细日志)
└── appsettings.Production.json          # 生产环境配置(生产数据库/优化日志)
```

**说明**:
- **Controllers**: 8个模块控制器，共计90+个API端点，覆盖完整的中医诊所管理功能
- **Middleware**: 全局异常处理中间件（统一错误返回）、请求日志记录中间件（追踪所有请求）
- **Extensions**: 服务注册扩展（模块化注册8个业务模块）、应用配置扩展（中间件管道配置）
- **Filters**: 模型验证过滤器（自动验证DTO）、API异常过滤器（捕获Controller异常）
- **HealthChecks**: 数据库健康检查（SQL Server连接）、自定义健康检查（业务逻辑验证）
- **Program.cs**: ASP.NET Core 8.0 Minimal API启动入口，负责服务注册和中间件配置
- **appsettings**: 三层配置文件（基础配置/开发环境/生产环境）

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(8个核心实体)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository、数据库迁移)
3. **LYBT.Shared.Models** - 共享DTO模型(请求/响应DTO、ApiResponse统一返回格式)
4. **LYBT.Shared.Interfaces** - Server端接口定义(8个模块Service接口)
5. **LYBT.Module.Auth** - 认证模块(登录/Token管理)
6. **LYBT.Module.Users** - 用户管理模块(用户CRUD/角色管理)
7. **LYBT.Module.Patients** - 患者管理模块(患者档案/搜索)
8. **LYBT.Module.MedicalCase** - 医案管理模块(医案流程/状态管理)
9. **LYBT.Module.Consultation** - 诊疗模块(诊断记录/四诊管理)
10. **LYBT.Module.Prescriptions** - 处方管理模块(处方CRUD/配药)
11. **LYBT.Module.Herbs** - 药材管理模块(药材信息/导入导出)
12. **LYBT.Module.Formula** - 验方管理模块(验方模板/克隆)

### 被依赖项目
1. **LYBT.Desktop.Shell** - WPF客户端通过HTTP客户端调用API
2. **LYBT.Desktop.Workstation.Core** - 工作台通过RestSharp调用API
3. **测试项目**:
   - LYBT.WebAPI.Tests（单元测试）
   - LYBT.WebAPI.IntegrationTests（集成测试）
   - LYBT.Server.ArchTests（架构测试）

### NuGet包
- **Microsoft.AspNetCore.OpenApi** (8.0.x) - OpenAPI支持
- **Swashbuckle.AspNetCore** (6.x) - Swagger UI生成
- **Microsoft.AspNetCore.Authentication.JwtBearer** (8.0.x) - JWT认证
- **Microsoft.EntityFrameworkCore.Design** (8.0.x) - EF Core设计时工具
- **Serilog.AspNetCore** (8.x) - 结构化日志框架
- **Serilog.Sinks.File** (6.x) - 文件日志输出
- **Microsoft.Extensions.Diagnostics.HealthChecks** (8.0.x) - 健康检查
- **Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore** (8.0.x) - EF Core健康检查

## 🛠 技术栈

- **.NET 8**: 基础框架
- **ASP.NET Core 8.0**: Web框架（Minimal API + MVC控制器）
- **Entity Framework Core 8**: 通过Infrastructure层间接使用，用于数据持久化
- **JWT Bearer Token**: 用户认证和授权
- **Swagger/OpenAPI 3.0**: API文档自动生成和测试UI
- **Serilog**: 结构化日志记录（文件输出/控制台输出）
- **ASP.NET Core Health Checks**: 健康检查（数据库/自定义检查）
- **CORS**: 跨域资源共享配置（支持Desktop客户端）
- **FluentValidation**: 通过各模块间接集成，DTO验证
- **AutoMapper**: 通过各模块间接集成，Entity ↔ DTO映射

##  核心功能详解

### 1. Program.cs - 应用程序启动流程

**完整启动流程**（CreateBuilder → 服务注册 → 中间件管道 → Run）:

```csharp
using LYBT.Infrastructure;
using LYBT.Module.Auth;
using LYBT.Module.Users;
using LYBT.Module.Patients;
using LYBT.Module.MedicalCase;
using LYBT.Module.Consultation;
using LYBT.Module.Prescriptions;
using LYBT.Module.Herbs;
using LYBT.Module.Formula;
using LYBT.WebAPI.Extensions;
using LYBT.WebAPI.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ========== 配置Serilog结构化日志 ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/lybt-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30
    )
    .CreateLogger();

builder.Host.UseSerilog();

// ========== 服务注册 ==========

// 1. 注册基础设施层（数据库上下文）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. 注册8个业务模块（通过扩展方法）
builder.Services.AddAuthModule();           // 认证模块
builder.Services.AddUsersModule();          // 用户管理模块
builder.Services.AddPatientsModule();       // 患者管理模块
builder.Services.AddMedicalCaseModule();    // 医案管理模块
builder.Services.AddConsultationModule();   // 诊疗模块
builder.Services.AddPrescriptionsModule();  // 处方管理模块
builder.Services.AddHerbsModule();          // 药材管理模块
builder.Services.AddFormulaModule();        // 验方管理模块

// 3. 注册控制器和API服务
builder.Services.AddControllers(options =>
{
    // 添加全局过滤器
    options.Filters.Add<ValidateModelStateAttribute>();
    options.Filters.Add<ApiExceptionFilterAttribute>();
})
.AddJsonOptions(options =>
{
    // 配置JSON序列化选项
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持PascalCase
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// 4. 配置JWT认证
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
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

// 5. 配置授权策略
builder.Services.AddAuthorization(options =>
{
    // 管理员策略（仅管理员可访问）
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // 医生策略（医生和管理员可访问）
    options.AddPolicy("DoctorOrAdmin", policy =>
        policy.RequireRole("Doctor", "Admin"));
});

// 6. 配置CORS（支持Desktop客户端跨域）
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

// 7. 配置Swagger文档
builder.Services.AddEndpointsApiExplorer();
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

// 8. 配置健康检查
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck<CustomHealthCheck>("custom");

var app = builder.Build();

// ========== 中间件管道 ==========

// 1. 全局异常处理中间件（必须在最前面）
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. 开发环境配置
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LYBT API v1");
        options.RoutePrefix = "swagger"; // Swagger UI路径: /swagger
    });
}

// 3. HTTPS重定向（生产环境强制HTTPS）
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 4. CORS中间件（必须在认证授权前）
app.UseCors("AllowDesktopClient");

// 5. 请求日志中间件（可选）
app.UseMiddleware<RequestLoggingMiddleware>();

// 6. 认证和授权中间件
app.UseAuthentication();
app.UseAuthorization();

// 7. 路由和控制器映射
app.MapControllers();

// 8. 健康检查端点
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

// ========== 启动应用 ==========

try
{
    Log.Information("启动凌隐宝堂WebAPI服务...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "WebAPI服务启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
```

### 2. 全局异常处理中间件

**ExceptionHandlingMiddleware.cs** - 捕获所有未处理异常并返回统一格式:

```csharp
using LYBT.Shared.Models;
using System.Net;
using System.Text.Json;

namespace LYBT.WebAPI.Middleware
{
    /// <summary>
    /// 全局异常处理中间件
    /// 捕获所有未处理异常并返回统一的ApiResponse格式
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求处理失败: {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// 处理异常并返回统一格式
        /// </summary>
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
                ArgumentException => new ApiResponse
                {
                    Success = false,
                    Message = exception.Message,
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

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // 保持PascalCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
```

### 3. JWT认证配置

**appsettings.json** - JWT配置:

```json
{
  "Jwt": {
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Desktop",
    "Key": "your-256-bit-secret-key-minimum-32-characters",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=LYBTZYZS;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

**JWT Token生成示例** (在AuthService中):

```csharp
public string GenerateJwtToken(UserDto user)
{
    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
    );
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("RealName", user.RealName ?? string.Empty)
    };

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddMinutes(
            int.Parse(_configuration["Jwt:ExpiryMinutes"]!)
        ),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### 4. Controller示例 - PatientsController

**完整Controller实现** - 患者管理API:

```csharp
using LYBT.Shared.Interfaces;
using LYBT.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者管理控制器
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize] // 所有端点需要认证
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(
            IPatientService patientService,
            ILogger<PatientsController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        /// <param name="pageIndex">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="searchTerm">搜索关键词（姓名/手机号）</param>
        /// <returns>分页患者列表</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientDto>>), 200)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                var result = await _patientService.GetPagedAsync(
                    pageIndex, pageSize, searchTerm
                );

                return Ok(new ApiResponse<PagedResult<PatientDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "查询成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询患者失败");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "查询失败"
                });
            }
        }

        /// <summary>
        /// 按ID查询患者详情
        /// </summary>
        /// <param name="id">患者ID</param>
        /// <returns>患者详情</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var patient = await _patientService.GetByIdAsync(id);
                if (patient == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "患者不存在"
                    });
                }

                return Ok(new ApiResponse<PatientDto>
                {
                    Success = true,
                    Data = patient,
                    Message = "查询成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询患者详情失败: {PatientId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "查询失败"
                });
            }
        }

        /// <summary>
        /// 创建患者
        /// </summary>
        /// <param name="dto">患者创建DTO</param>
        /// <returns>创建的患者</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
        {
            try
            {
                var patient = await _patientService.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = patient.Id },
                    new ApiResponse<PatientDto>
                    {
                        Success = true,
                        Data = patient,
                        Message = "创建成功"
                    }
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "创建患者参数错误: {Message}", ex.Message);
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者失败");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "创建失败"
                });
            }
        }

        /// <summary>
        /// 更新患者
        /// </summary>
        /// <param name="id">患者ID</param>
        /// <param name="dto">患者更新DTO</param>
        /// <returns>更新后的患者</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdatePatientDto dto)
        {
            try
            {
                var patient = await _patientService.UpdateAsync(id, dto);
                if (patient == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "患者不存在"
                    });
                }

                return Ok(new ApiResponse<PatientDto>
                {
                    Success = true,
                    Data = patient,
                    Message = "更新成功"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "更新患者参数错误: {Message}", ex.Message);
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者失败: {PatientId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "更新失败"
                });
            }
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        /// <param name="id">患者ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")] // 仅管理员可删除
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var success = await _patientService.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "患者不存在"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "删除成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败: {PatientId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "删除失败"
                });
            }
        }

        /// <summary>
        /// 搜索患者（按姓名/手机号/拼音）
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的患者列表</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PatientDto>>), 200)]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            try
            {
                var patients = await _patientService.SearchAsync(keyword);

                return Ok(new ApiResponse<List<PatientDto>>
                {
                    Success = true,
                    Data = patients,
                    Message = "搜索成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者失败: {Keyword}", keyword);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "搜索失败"
                });
            }
        }
    }
}
```

### 5. 健康检查配置

**DatabaseHealthCheck.cs** - 数据库健康检查:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LYBT.WebAPI.HealthChecks
{
    /// <summary>
    /// 数据库健康检查
    /// </summary>
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
}
```

**健康检查访问**:
```bash
# 访问健康检查端点
curl https://localhost:7001/health

# 返回JSON示例
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

## 🎨 综合使用示例

### 示例1: 用户登录与Token获取

**请求流程** (Desktop客户端 → WebAPI):

```csharp
// ========== Desktop客户端代码 ==========

using RestSharp;

public class AuthService
{
    private readonly RestClient _client;

    public AuthService()
    {
        _client = new RestClient("https://localhost:7001");
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var request = new RestRequest("api/v1/auth/login", Method.Post);
        request.AddJsonBody(new
        {
            Username = username,
            Password = password
        });

        var response = await _client.ExecuteAsync<ApiResponse<LoginResponse>>(request);

        if (response.IsSuccessful && response.Data.Success)
        {
            // 保存Token到本地（用于后续请求）
            SaveToken(response.Data.Data.Token);
            return response.Data.Data;
        }

        throw new Exception(response.Data?.Message ?? "登录失败");
    }

    /// <summary>
    /// 使用Token访问受保护的API
    /// </summary>
    public async Task<List<PatientDto>> GetPatientsAsync()
    {
        var request = new RestRequest("api/v1/patients", Method.Get);

        // 添加JWT Token到请求头
        request.AddHeader("Authorization", $"Bearer {GetSavedToken()}");

        var response = await _client.ExecuteAsync<ApiResponse<PagedResult<PatientDto>>>(request);

        if (response.IsSuccessful && response.Data.Success)
        {
            return response.Data.Data.Items;
        }

        throw new Exception(response.Data?.Message ?? "查询失败");
    }
}

// ========== WebAPI端代码 ==========

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    [HttpPost("login")]
    [AllowAnonymous] // 登录端点不需要认证
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // 验证用户名密码
            var user = await _authService.ValidateCredentialsAsync(
                request.Username, request.Password
            );

            if (user == null)
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "用户名或密码错误"
                });
            }

            // 生成JWT Token
            var token = _authService.GenerateJwtToken(user);

            return Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = new LoginResponse
                {
                    Token = token,
                    User = user,
                    ExpiresIn = 3600 // 1小时
                },
                Message = "登录成功"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "登录失败"
            });
        }
    }
}
```

### 示例2: CRUD操作完整流程

**创建患者完整流程** (参数验证 → Service调用 → 数据库保存 → 返回结果):

```csharp
// ========== Desktop客户端代码 ==========

public async Task CreatePatientAsync()
{
    var createDto = new CreatePatientDto
    {
        Name = "张三",
        Gender = Gender.Male,
        Age = 45,
        PhoneNumber = "13800138000",
        Address = "北京市朝阳区XX街道",
        IdCard = "110101197801011234",
        MedicalHistory = "高血压10年",
        Allergies = "青霉素过敏"
    };

    var request = new RestRequest("api/v1/patients", Method.Post);
    request.AddHeader("Authorization", $"Bearer {token}");
    request.AddJsonBody(createDto);

    var response = await _client.ExecuteAsync<ApiResponse<PatientDto>>(request);

    if (response.IsSuccessful && response.Data.Success)
    {
        Console.WriteLine($"患者创建成功，ID: {response.Data.Data.Id}");
    }
    else
    {
        Console.WriteLine($"创建失败: {response.Data?.Message}");
    }
}

// ========== WebAPI端完整流程 ==========

[HttpPost]
public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
{
    // Step 1: 模型验证（自动触发ValidateModelStateAttribute）
    if (!ModelState.IsValid)
    {
        return BadRequest(new ApiResponse
        {
            Success = false,
            Message = "参数验证失败",
            Errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList()
        });
    }

    try
    {
        // Step 2: 调用Service创建患者
        var patient = await _patientService.CreateAsync(dto);

        // Step 3: 返回201 Created响应
        return CreatedAtAction(
            nameof(GetById),
            new { id = patient.Id },
            new ApiResponse<PatientDto>
            {
                Success = true,
                Data = patient,
                Message = "创建成功"
            }
        );
    }
    catch (ArgumentException ex)
    {
        // Step 4: 业务逻辑异常（参数错误）
        return BadRequest(new ApiResponse
        {
            Success = false,
            Message = ex.Message
        });
    }
    catch (Exception ex)
    {
        // Step 5: 未预期异常（由ExceptionHandlingMiddleware捕获）
        throw; // 让全局异常中间件处理
    }
}
```

### 示例3: 全局异常处理演示

**异常捕获与统一返回格式**:

```csharp
// ========== 触发异常的Controller代码 ==========

[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    // 场景1: 参数为空（ArgumentNullException）
    if (id == Guid.Empty)
    {
        throw new ArgumentNullException(nameof(id), "患者ID不能为空");
    }

    // 场景2: 资源不存在（KeyNotFoundException）
    var patient = await _patientService.GetByIdAsync(id);
    if (patient == null)
    {
        throw new KeyNotFoundException($"患者不存在: {id}");
    }

    // 场景3: 数据库连接失败（DbUpdateException）
    // 由ExceptionHandlingMiddleware捕获并返回500

    return Ok(new ApiResponse<PatientDto>
    {
        Success = true,
        Data = patient
    });
}

// ========== 全局异常中间件捕获 ==========

// 场景1: ArgumentNullException → 400 Bad Request
{
  "Success": false,
  "Message": "参数不能为空",
  "StatusCode": 400,
  "Data": null,
  "Errors": null
}

// 场景2: KeyNotFoundException → 404 Not Found
{
  "Success": false,
  "Message": "资源不存在",
  "StatusCode": 404,
  "Data": null,
  "Errors": null
}

// 场景3: DbUpdateException → 500 Internal Server Error
{
  "Success": false,
  "Message": "服务器内部错误",
  "StatusCode": 500,
  "Data": null,
  "Errors": null
}
```

### 示例4: 健康检查与监控

**健康检查访问与监控集成**:

```bash
# ========== 访问健康检查端点 ==========

# 1. 基础健康检查
curl https://localhost:7001/health

# 返回示例（所有检查通过）
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

# 2. 数据库检查失败示例
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

# ========== 集成到监控系统 ==========

# 3. 在Azure Application Insights中配置健康检查
# appsettings.json添加配置
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key"
  }
}

# 4. 在Startup中注册Application Insights
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]
);

# 5. 配置定期健康检查（每30秒）
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<CustomHealthCheck>("custom");
```

### 示例5: 日志记录与追踪

**结构化日志记录示例** (Serilog):

```csharp
// ========== Controller中使用ILogger ==========

public class PatientsController : ControllerBase
{
    private readonly ILogger<PatientsController> _logger;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        // 记录信息日志
        _logger.LogInformation(
            "创建患者请求: {@CreatePatientDto}",
            dto
        );

        try
        {
            var patient = await _patientService.CreateAsync(dto);

            // 记录成功日志
            _logger.LogInformation(
                "患者创建成功: {PatientId}, {PatientName}",
                patient.Id,
                patient.Name
            );

            return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
        }
        catch (ArgumentException ex)
        {
            // 记录警告日志
            _logger.LogWarning(
                ex,
                "创建患者参数错误: {Message}",
                ex.Message
            );
            return BadRequest(new ApiResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            // 记录错误日志
            _logger.LogError(
                ex,
                "创建患者失败: {@CreatePatientDto}",
                dto
            );
            throw; // 让全局异常中间件处理
        }
    }
}

// ========== 日志输出示例（文件：logs/lybt-20250129.txt） ==========

2025-01-29 10:15:32.123 [Information] 创建患者请求: {"Name":"张三","Gender":1,"Age":45,"PhoneNumber":"13800138000"}
2025-01-29 10:15:32.456 [Information] 患者创建成功: 3fa85f64-5717-4562-b3fc-2c963f66afa6, 张三

2025-01-29 10:20:15.789 [Warning] 创建患者参数错误: 手机号码格式不正确
System.ArgumentException: 手机号码格式不正确
   at LYBT.Module.Patients.Services.PatientService.CreateAsync(CreatePatientDto dto)

2025-01-29 10:25:45.123 [Error] 创建患者失败: {"Name":"李四","Gender":2,"Age":38}
Microsoft.EntityFrameworkCore.DbUpdateException: 数据库更新失败
   at LYBT.Infrastructure.Repositories.BaseRepository.AddAsync(TEntity entity)
```

## 🎯 最佳实践

### 1. API设计原则

**RESTful风格**:
```csharp
//  正确：遵循RESTful规范
[HttpGet]                     // GET /api/v1/patients (查询列表)
[HttpGet("{id}")]             // GET /api/v1/patients/{id} (查询单个)
[HttpPost]                    // POST /api/v1/patients (创建)
[HttpPut("{id}")]             // PUT /api/v1/patients/{id} (完整更新)
[HttpPatch("{id}")]           // PATCH /api/v1/patients/{id} (部分更新)
[HttpDelete("{id}")]          // DELETE /api/v1/patients/{id} (删除)

// ❌ 错误：不符合RESTful规范
[HttpGet("GetPatient")]       // GET /api/v1/patients/GetPatient (命名不规范)
[HttpPost("DeletePatient")]   // POST /api/v1/patients/DeletePatient (方法错误)
```

**统一返回格式**:
```csharp
//  所有API都返回ApiResponse<T>
public async Task<IActionResult> GetById(Guid id)
{
    var patient = await _patientService.GetByIdAsync(id);

    return Ok(new ApiResponse<PatientDto>
    {
        Success = true,
        Data = patient,
        Message = "查询成功",
        StatusCode = 200
    });
}

// ❌ 错误：直接返回数据（不一致）
public async Task<IActionResult> GetById(Guid id)
{
    var patient = await _patientService.GetByIdAsync(id);
    return Ok(patient); // 缺少Success/Message等元数据
}
```

### 2. Controller规范

**依赖注入原则**:
```csharp
//  正确：通过构造函数注入
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientService patientService,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }
}

// ❌ 错误：使用ServiceLocator反模式
public class PatientsController : ControllerBase
{
    public async Task<IActionResult> GetById(Guid id)
    {
        var service = HttpContext.RequestServices.GetService<IPatientService>();
        // 违反依赖注入原则
    }
}
```

**异步编程规范**:
```csharp
//  正确：所有I/O操作使用async/await
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var patient = await _patientService.GetByIdAsync(id);
    return Ok(patient);
}

// ❌ 错误：阻塞调用（影响性能）
[HttpGet("{id}")]
public IActionResult GetById(Guid id)
{
    var patient = _patientService.GetByIdAsync(id).Result; // 阻塞
    return Ok(patient);
}
```

### 3. 异常处理最佳实践

**业务逻辑异常**:
```csharp
//  正确：明确的异常类型
public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
{
    try
    {
        var patient = await _patientService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }
    catch (ArgumentException ex) // 参数错误 → 400
    {
        return BadRequest(new ApiResponse { Message = ex.Message });
    }
    catch (UnauthorizedAccessException ex) // 未授权 → 401
    {
        return Unauthorized(new ApiResponse { Message = ex.Message });
    }
    catch (KeyNotFoundException ex) // 资源不存在 → 404
    {
        return NotFound(new ApiResponse { Message = ex.Message });
    }
    catch (Exception ex) // 未预期异常 → 500（全局中间件处理）
    {
        _logger.LogError(ex, "创建患者失败");
        throw; // 让ExceptionHandlingMiddleware处理
    }
}
```

### 4. 日志记录最佳实践

**结构化日志**:
```csharp
//  正确：使用结构化日志（可查询）
_logger.LogInformation(
    "创建患者成功: {PatientId}, {PatientName}, {Age}",
    patient.Id,
    patient.Name,
    patient.Age
);

// ❌ 错误：字符串拼接（难以查询）
_logger.LogInformation(
    $"创建患者成功: {patient.Id}, {patient.Name}, {patient.Age}"
);
```

**日志级别使用**:
```csharp
// Information: 正常业务流程
_logger.LogInformation("用户登录成功: {Username}", username);

// Warning: 可恢复的错误
_logger.LogWarning("患者手机号码格式错误: {PhoneNumber}", phoneNumber);

// Error: 需要关注的错误
_logger.LogError(ex, "数据库连接失败");

// Critical: 严重错误（需要立即处理）
_logger.LogCritical(ex, "应用程序启动失败");
```

### 5. 安全防护最佳实践

**输入验证**:
```csharp
//  正确：使用FluentValidation验证DTO
public class CreatePatientDtoValidator : AbstractValidator<CreatePatientDto>
{
    public CreatePatientDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("姓名不能为空")
            .MaximumLength(50).WithMessage("姓名长度不能超过50字符");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确");

        RuleFor(x => x.Age)
            .InclusiveBetween(0, 150).WithMessage("年龄必须在0-150之间");
    }
}

//  在Controller中自动触发验证
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
{
    // ModelState.IsValid会自动检查FluentValidation规则
    if (!ModelState.IsValid)
    {
        return BadRequest(new ApiResponse
        {
            Success = false,
            Message = "参数验证失败",
            Errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList()
        });
    }
    // ...
}
```

**授权策略**:
```csharp
//  正确：使用授权策略保护端点
[HttpDelete("{id}")]
[Authorize(Policy = "AdminOnly")] // 仅管理员可删除
public async Task<IActionResult> Delete(Guid id)
{
    // ...
}

[HttpGet]
[Authorize(Policy = "DoctorOrAdmin")] // 医生或管理员可访问
public async Task<IActionResult> GetPaged()
{
    // ...
}

// ❌ 错误：硬编码角色检查（不灵活）
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
{
    if (User.FindFirst(ClaimTypes.Role)?.Value != "Admin")
    {
        return Unauthorized();
    }
    // 违反单一职责原则
}
```

### 6. CORS配置最佳实践

**开发环境与生产环境分离**:
```csharp
// appsettings.Development.json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5001",
      "https://localhost:7001"
    ]
  }
}

// appsettings.Production.json
{
  "Cors": {
    "AllowedOrigins": [
      "https://lybtzyzs.com",
      "https://www.lybtzyzs.com"
    ]
  }
}

// Program.cs配置
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

    options.AddPolicy("AllowDesktopClient", builder =>
    {
        builder.WithOrigins(origins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

### 7. Swagger文档最佳实践

**添加XML注释**:
```csharp
//  在Controller方法上添加XML注释
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

// Program.cs配置XML注释
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
```

## 📈 性能优化

### 1. 异步编程优化

**全异步I/O操作**:
```csharp
//  正确：所有I/O操作使用async/await
public async Task<IActionResult> GetPaged(int pageIndex, int pageSize)
{
    var result = await _patientService.GetPagedAsync(pageIndex, pageSize);
    return Ok(result);
}

// ❌ 错误：阻塞调用（降低吞吐量）
public IActionResult GetPaged(int pageIndex, int pageSize)
{
    var result = _patientService.GetPagedAsync(pageIndex, pageSize).Result;
    return Ok(result);
}
```

### 2. 数据库查询优化

**分页查询**:
```csharp
//  正确：使用Skip/Take分页
public async Task<PagedResult<PatientDto>> GetPagedAsync(int pageIndex, int pageSize)
{
    var query = _dbContext.Patients
        .Where(p => !p.IsDeleted)
        .OrderByDescending(p => p.CreatedAt);

    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<PatientDto>
    {
        Items = _mapper.Map<List<PatientDto>>(items),
        TotalCount = totalCount,
        PageIndex = pageIndex,
        PageSize = pageSize
    };
}

// ❌ 错误：加载所有数据（内存溢出风险）
public async Task<List<PatientDto>> GetAllAsync()
{
    var patients = await _dbContext.Patients.ToListAsync(); // 加载全部
    return _mapper.Map<List<PatientDto>>(patients);
}
```

**Select投影优化**:
```csharp
//  正确：只查询需要的字段
public async Task<List<PatientSummaryDto>> GetSummariesAsync()
{
    return await _dbContext.Patients
        .Where(p => !p.IsDeleted)
        .Select(p => new PatientSummaryDto
        {
            Id = p.Id,
            Name = p.Name,
            Age = p.Age,
            PhoneNumber = p.PhoneNumber
        })
        .ToListAsync();
}

// ❌ 错误：查询所有字段后再投影
public async Task<List<PatientSummaryDto>> GetSummariesAsync()
{
    var patients = await _dbContext.Patients.ToListAsync();
    return patients.Select(p => new PatientSummaryDto
    {
        Id = p.Id,
        Name = p.Name,
        Age = p.Age,
        PhoneNumber = p.PhoneNumber
    }).ToList();
}
```

### 3. 响应缓存

**配置响应缓存中间件**:
```csharp
// Program.cs配置
builder.Services.AddResponseCaching();

app.UseResponseCaching();

// Controller中使用
[HttpGet]
[ResponseCache(Duration = 60)] // 缓存60秒
public async Task<IActionResult> GetPaged(int pageIndex, int pageSize)
{
    var result = await _patientService.GetPagedAsync(pageIndex, pageSize);
    return Ok(result);
}
```

### 4. 连接池优化

**DbContext生命周期管理**:
```csharp
//  正确：使用Scoped生命周期（每个请求一个实例）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped); // 默认Scoped

// ❌ 错误：使用Singleton（线程安全问题）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Singleton); // 违反DbContext使用规范
```

## 🔒 安全考虑

### 1. JWT认证安全

**Token安全存储** (Desktop客户端):
```csharp
//  正确：使用DPAPI加密存储Token
using System.Security.Cryptography;

public class TokenStorage
{
    public void SaveToken(string token)
    {
        var encryptedToken = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(token),
            null,
            DataProtectionScope.CurrentUser
        );
        File.WriteAllBytes("token.dat", encryptedToken);
    }

    public string GetToken()
    {
        var encryptedToken = File.ReadAllBytes("token.dat");
        var decryptedToken = ProtectedData.Unprotect(
            encryptedToken,
            null,
            DataProtectionScope.CurrentUser
        );
        return Encoding.UTF8.GetString(decryptedToken);
    }
}

// ❌ 错误：明文存储Token（安全风险）
public void SaveToken(string token)
{
    File.WriteAllText("token.txt", token); // 明文存储
}
```

### 2. HTTPS强制

**生产环境强制HTTPS**:
```csharp
// Program.cs配置
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // 生产环境强制HTTPS
    app.UseHsts(); // HTTP Strict Transport Security
}
```

### 3. 敏感信息保护

**配置敏感信息加密**:
```bash
# 使用User Secrets存储敏感信息（开发环境）
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-256-bit-secret-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"

# 生产环境使用环境变量
export Jwt__Key="your-256-bit-secret-key"
export ConnectionStrings__DefaultConnection="your-connection-string"
```

### 4. 输入验证与防注入

**防止SQL注入** (通过EF Core参数化查询):
```csharp
//  正确：EF Core自动参数化
public async Task<List<PatientDto>> SearchAsync(string keyword)
{
    return await _dbContext.Patients
        .Where(p => p.Name.Contains(keyword)) // 自动参数化
        .ToListAsync();
}

// ❌ 错误：使用原始SQL（SQL注入风险）
public async Task<List<PatientDto>> SearchAsync(string keyword)
{
    var sql = $"SELECT * FROM Patients WHERE Name LIKE '%{keyword}%'"; // SQL注入
    return await _dbContext.Patients.FromSqlRaw(sql).ToListAsync();
}
```

## 🧪 测试指南

### 1. Controller单元测试示例

**PatientsController单元测试** (使用xUnit + NSubstitute):

```csharp
using LYBT.WebAPI.Controllers;
using LYBT.Shared.Interfaces;
using LYBT.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.WebAPI.Tests.Controllers
{
    public class PatientsControllerTests
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientsController> _logger;
        private readonly PatientsController _controller;

        public PatientsControllerTests()
        {
            // Arrange: 创建Mock对象
            _patientService = Substitute.For<IPatientService>();
            _logger = Substitute.For<ILogger<PatientsController>>();
            _controller = new PatientsController(_patientService, _logger);
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenPatientExists()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patientDto = new PatientDto
            {
                Id = patientId,
                Name = "张三",
                Age = 45,
                Gender = Gender.Male
            };

            _patientService.GetByIdAsync(patientId)
                .Returns(Task.FromResult(patientDto));

            // Act
            var result = await _controller.GetById(patientId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PatientDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(patientDto.Id, response.Data.Id);
            Assert.Equal("张三", response.Data.Name);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenPatientDoesNotExist()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _patientService.GetByIdAsync(patientId)
                .Returns(Task.FromResult<PatientDto>(null));

            // Act
            var result = await _controller.GetById(patientId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("患者不存在", response.Message);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenValidDto()
        {
            // Arrange
            var createDto = new CreatePatientDto
            {
                Name = "李四",
                Age = 38,
                Gender = Gender.Female,
                PhoneNumber = "13800138000"
            };

            var createdPatient = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Age = createDto.Age,
                Gender = createDto.Gender,
                PhoneNumber = createDto.PhoneNumber
            };

            _patientService.CreateAsync(createDto)
                .Returns(Task.FromResult(createdPatient));

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<ApiResponse<PatientDto>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.Equal("李四", response.Data.Name);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenArgumentException()
        {
            // Arrange
            var createDto = new CreatePatientDto
            {
                Name = "王五",
                Age = 200, // 非法年龄
                Gender = Gender.Male
            };

            _patientService.CreateAsync(createDto)
                .Returns(Task.FromException<PatientDto>(
                    new ArgumentException("年龄必须在0-150之间")
                ));

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("年龄", response.Message);
        }
    }
}
```

### 2. 集成测试示例

**WebAPI集成测试** (使用WebApplicationFactory):

```csharp
using LYBT.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit;

namespace LYBT.WebAPI.IntegrationTests
{
    public class PatientsApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public PatientsApiTests(WebApplicationFactory<Program> factory)
        {
            // 配置测试数据库
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 移除原有DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // 添加In-Memory数据库
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDatabase");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetPaged_ShouldReturnOk_WithPatients()
        {
            // Arrange
            var loginRequest = new { Username = "admin", Password = "admin123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResult.Data.Token}");

            // Act
            var response = await _client.GetAsync("/api/v1/patients?pageIndex=1&pageSize=10");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PatientDto>>>();
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenValidDto()
        {
            // Arrange
            var createDto = new CreatePatientDto
            {
                Name = "集成测试患者",
                Age = 45,
                Gender = Gender.Male,
                PhoneNumber = "13800138000"
            };

            // 先登录获取Token
            var loginRequest = new { Username = "admin", Password = "admin123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResult.Data.Token}");

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/patients", createDto);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDto>>();
            Assert.True(result.Success);
            Assert.Equal("集成测试患者", result.Data.Name);
        }
    }
}
```

##  快速开始

### 开发环境启动

**前置条件**:
- .NET 8 SDK
- SQL Server 2022 Express (或更高版本)
- Visual Studio 2022 (或 VS Code)

**启动步骤**:

```bash
# 1. 克隆项目并进入WebAPI目录
cd src/Server/Services/LYBT.WebAPI

# 2. 配置环境变量（复制.env.example到.env并配置）
# 在项目根目录执行: copy .env.example .env

# 3. 还原NuGet包依赖
dotnet restore

# 4. 更新数据库到最新迁移
cd ../../Core/LYBT.Infrastructure
dotnet ef database update
cd ../../Services/LYBT.WebAPI

# 5. 配置appsettings.json（数据库连接字符串、JWT密钥）
# 确保ConnectionStrings:DefaultConnection指向正确的SQL Server实例

# 6. 启动API服务（HTTPS端口7001）
dotnet run --urls "https://localhost:7001;http://localhost:5001"

# 7. 访问Swagger API文档
# 浏览器访问: https://localhost:7001/swagger

# 8. 访问健康检查端点
# 浏览器访问: https://localhost:7001/health
```

### 生产环境部署

**IIS部署**:

```powershell
# 1. 发布Release版本
dotnet publish -c Release -o ./publish

# 2. 配置IIS应用程序池
# - .NET CLR版本: 无托管代码
# - 托管管道模式: 集成
# - 启用32位应用程序: False

# 3. 配置应用程序设置
# - 环境变量: ASPNETCORE_ENVIRONMENT=Production
# - 数据库连接字符串（通过环境变量或appsettings.Production.json）

# 4. 配置HTTPS绑定
# - 绑定类型: https
# - 端口: 443
# - SSL证书: 选择有效证书

# 5. 重启应用程序池
Restart-WebAppPool -Name "LYBTZYZS"
```

**Docker部署** (可选):

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LYBT.WebAPI.csproj", "./"]
RUN dotnet restore "LYBT.WebAPI.csproj"
COPY . .
RUN dotnet build "LYBT.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LYBT.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LYBT.WebAPI.dll"]
```

```bash
# 构建Docker镜像
docker build -t lybtzyzs-webapi:1.0 .

# 运行容器
docker run -d -p 7001:443 -p 5001:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=sqlserver;Database=LYBTZYZS;..." \
  --name lybtzyzs-api \
  lybtzyzs-webapi:1.0
```

## 🔌 API 接口

此项目是后端所有RESTful API的提供者，集成了8个业务模块，共计90+个API端点。所有API都遵循统一的`ApiResponse<T>`返回格式。

**API文档 (Swagger)**: 启动服务后，可通过 `https://localhost:7001/swagger` 访问交互式API文档。

### 8个模块Controller及端点统计

| 模块 | Controller | 端点数 | 主要功能 |
|------|-----------|--------|---------|
| **认证模块** | AuthController | 5 | 登录、登出、Token刷新、密码重置、会话管理 |
| **用户管理** | UsersController | 12 | 用户CRUD、角色管理、权限分配、用户搜索 |
| **患者管理** | PatientsController | 10 | 患者档案CRUD、搜索、病史管理 |
| **医案管理** | MedicalCasesController | 15 | 医案流程、状态管理、病案归档 |
| **诊疗模块** | ConsultationsController | 8 | 诊断记录、四诊管理、病情跟踪 |
| **处方管理** | PrescriptionsController | 18 | 处方CRUD、配药管理、处方历史 |
| **药材管理** | HerbsController | 13 | 药材信息、导入导出、价格管理 |
| **验方管理** | FormulasController | 19 | 验方模板、克隆、分享 |
| **总计** | - | **90+** | - |

### 核心API端点示例

**认证模块 (AuthController)**:
```
POST   /api/v1/auth/login              # 用户登录
POST   /api/v1/auth/logout             # 用户登出
POST   /api/v1/auth/refresh-token      # 刷新Token
POST   /api/v1/auth/change-password    # 修改密码
GET    /api/v1/auth/sessions           # 获取活跃会话
```

**患者管理 (PatientsController)**:
```
GET    /api/v1/patients                # 分页查询患者
GET    /api/v1/patients/{id}           # 按ID查询患者
POST   /api/v1/patients                # 创建患者
PUT    /api/v1/patients/{id}           # 更新患者
DELETE /api/v1/patients/{id}           # 删除患者
GET    /api/v1/patients/search         # 搜索患者
GET    /api/v1/patients/{id}/history   # 获取患者病史
```

**处方管理 (PrescriptionsController)**:
```
GET    /api/v1/prescriptions                    # 分页查询处方
GET    /api/v1/prescriptions/{id}               # 按ID查询处方
POST   /api/v1/prescriptions                    # 创建处方
PUT    /api/v1/prescriptions/{id}               # 更新处方
DELETE /api/v1/prescriptions/{id}               # 删除处方
POST   /api/v1/prescriptions/from-formula       # 从验方创建处方
POST   /api/v1/prescriptions/{id}/confirm       # 确认处方
POST   /api/v1/prescriptions/{id}/dispense      # 配药
GET    /api/v1/prescriptions/{id}/print         # 打印处方
POST   /api/v1/prescriptions/import             # 批量导入处方
GET    /api/v1/prescriptions/export             # 导出处方
```

### API统一返回格式

**成功响应**:
```json
{
  "Success": true,
  "Message": "操作成功",
  "StatusCode": 200,
  "Data": {
    "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "Name": "张三",
    "Age": 45
  },
  "Errors": null
}
```

**失败响应**:
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

**分页响应**:
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

## 📚 详细文档

- **架构设计**:[docs/explanation/architecture/server/webapi-design.md](../../../../docs/explanation/architecture/server/webapi-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/webapi-development.md](../../../../docs/how-to-guides/server/webapi-development.md) *(待创建)*
- **部署指南**:[docs/how-to-guides/server/webapi-deployment.md](../../../../docs/how-to-guides/server/webapi-deployment.md) *(待创建)*
- **健康检查文档**:[docs/reference/server/health-checks.md](../../../../docs/reference/server/health-checks.md) *(待创建)*
- **日志记录指南**:[docs/reference/server/logging.md](../../../../docs/reference/server/logging.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组
