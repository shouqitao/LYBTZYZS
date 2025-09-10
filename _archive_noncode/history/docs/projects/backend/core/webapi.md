# LYBT.WebAPI 项目文档

## 📋 项目概述

**LYBT.WebAPI**是凌隐宝堂中医诊所系统的Web API入口项目，作为整个后端系统的统一网关和服务协调器。它负责统筹8个业务模块的服务注册、提供RESTful API接口、处理HTTP请求响应，以及管理身份认证、授权和监控等横切关注点。

### 项目职责
- **API网关**: 为前端WPF客户端提供统一的RESTful API接口
- **服务协调**: 统筹8个业务模块的依赖注入注册和生命周期管理
- **中间件管道**: 配置认证、授权、异常处理、CORS等中间件
- **健康监控**: 提供系统健康检查和性能监控端点
- **API文档**: 集成Swagger自动生成API文档和测试界面
- **配置管理**: 管理应用程序配置和环境变量

### 在系统中的位置
WebAPI作为整个后端系统的入口点和协调中心，位于架构的最上层。它依赖Infrastructure提供的基础服务，协调8个业务模块，为前端Desktop客户端提供API服务。

### 关键业务价值
- **统一入口**: 为前端提供一致的API接口和响应格式
- **服务治理**: 统一管理所有业务服务的注册和配置
- **安全网关**: 集中处理认证授权，确保API安全访问
- **运维支撑**: 提供监控、日志、健康检查等运维必需功能

## 🏗️ 技术架构

### 项目架构设计
WebAPI采用分层中间件管道架构：

```
HTTP请求
    ↓
中间件管道 (认证、异常处理、CORS等)
    ↓
控制器层 (UltraThink三层控制器架构)
    ↓
业务服务层 (8个模块的UltraThink双层服务)
    ↓
数据访问层 (Infrastructure)
    ↓
HTTP响应
```

### 核心技术栈
- **ASP.NET Core 8.0**: Web API框架，支持高性能HTTP处理
- **Swashbuckle.AspNetCore 9.0.1**: Swagger API文档自动生成
- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT Bearer认证中间件
- **Microsoft.AspNetCore.Cors**: 跨域资源共享支持
- **Microsoft.Extensions.Diagnostics.HealthChecks**: 健康检查框架
- **Microsoft.AspNetCore.ResponseCompression**: HTTP响应压缩
- **Serilog.AspNetCore**: 结构化日志记录框架

### 依赖项目列表
**直接依赖**:
- `LYBT.Infrastructure` - 基础设施服务(数据访问、认证、异常处理)
- `LYBT.Entities` - 实体模型定义
- `LYBT.Shared.Models` - 数据传输对象
- `LYBT.Shared.Interfaces` - 服务接口契约
- `LYBT.Shared.Utilities` - 通用工具类

**业务模块依赖**:
- `LYBT.Module.Auth` - 认证授权模块
- `LYBT.Module.Users` - 用户管理模块
- `LYBT.Module.Patients` - 患者档案模块
- `LYBT.Module.MedicalCase` - 医疗案例模块
- `LYBT.Module.Consultation` - 看诊诊断模块
- `LYBT.Module.Prescriptions` - 处方管理模块
- `LYBT.Module.Herbs` - 中药材管理模块
- `LYBT.Module.Formula` - 验方管理模块

### 设计模式采用
- **MVC Pattern**: 标准的Model-View-Controller架构
- **Middleware Pattern**: ASP.NET Core中间件管道
- **Dependency Injection**: 构造函数依赖注入
- **Options Pattern**: 强类型配置管理
- **Factory Pattern**: 服务工厂和健康检查工厂

## 🎯 功能规范

### 必须实现的功能清单

#### 1. 控制器API端点
- ✅ **AuthController**: 身份认证API (`/api/v1/auth`)
- ✅ **UsersController**: 用户管理API (`/api/v1/users`)
- ✅ **PatientsController**: 患者档案API (`/api/v1/patients`)
- ✅ **MedicalCaseController**: 医疗案例API (`/api/v1/medicalcase`)
- ✅ **ConsultationController**: 看诊诊断API (`/api/v1/consultation`)
- ✅ **PrescriptionsController**: 处方管理API (`/api/v1/prescriptions`)
- ✅ **HerbsController**: 中药材管理API (`/api/v1/herbs`)
- ✅ **FormulasController**: 验方管理API (`/api/v1/formulas`)

#### 2. 系统管理端点
- ✅ **HealthController**: 健康检查API (`/api/v1/health`)
- ✅ **MonitoringController**: 性能监控API (`/api/v1/monitoring`)
- ✅ **SecurityController**: 安全管理API (`/api/v1/security`)
- ✅ **CacheController**: 缓存管理API (`/api/v1/cache`)

#### 3. 中间件管道功能
- ✅ **JWT认证中间件**: 自动验证Bearer Token
- ✅ **全局异常处理**: ExceptionMiddleware统一错误处理
- ✅ **CORS支持**: 跨域请求处理
- ✅ **响应压缩**: Gzip压缩减少传输大小
- ✅ **请求日志**: 结构化HTTP请求日志记录

#### 4. 服务注册管理
- ✅ **模块化注册**: AddAllModules()统一注册8个业务模块
- ✅ **基础设施注册**: 数据库、缓存、认证等基础服务
- ✅ **健康检查注册**: 数据库、内存、磁盘等检查项
- ✅ **Swagger集成**: API文档自动生成和UI界面

### API端点定义规范

#### RESTful URL设计标准
```http
# 业务API端点
GET    /api/v1/users                    # 获取用户列表
GET    /api/v1/users/{id}               # 获取单个用户
POST   /api/v1/users                    # 创建新用户
PUT    /api/v1/users/{id}               # 更新用户
DELETE /api/v1/users/{id}               # 删除用户

GET    /api/v1/users/search             # 搜索用户
POST   /api/v1/users/batch              # 批量操作
GET    /api/v1/users/{id}/medicalcases  # 获取用户相关医案
```

#### HTTP状态码规范
```http
200 OK                    # 成功获取资源
201 Created               # 成功创建资源
204 No Content            # 成功删除资源
400 Bad Request           # 请求参数错误
401 Unauthorized          # 未认证
403 Forbidden             # 权限不足
404 Not Found             # 资源不存在
409 Conflict              # 资源冲突
422 Unprocessable Entity  # 验证失败
500 Internal Server Error # 服务器内部错误
```

### 数据模型定义

#### API响应格式
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RequestId { get; set; } = string.Empty;
    public object? Meta { get; set; }
}
```

#### 分页响应格式
```csharp
public class PagedResponse<T> : ApiResponse<PagedResult<T>>
{
    public PagedResponse(PagedResult<T> data, string message = "查询成功")
    {
        Success = true;
        Message = message;
        Data = data;
        Meta = new
        {
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalRecords = data.TotalRecords,
            TotalPages = data.TotalPages
        };
    }
}
```

#### 错误响应格式
```csharp
public class ErrorResponse : ApiResponse<object>
{
    public string ErrorCode { get; set; } = string.Empty;
    public string[] Details { get; set; } = Array.Empty<string>();
    public string? StackTrace { get; set; }
    
    public ErrorResponse(string message, string errorCode = "INTERNAL_ERROR")
    {
        Success = false;
        Message = message;
        ErrorCode = errorCode;
        Data = null;
    }
}
```

### 业务规则约束
1. **API版本控制**: 所有API使用v1版本前缀，支持版本演进
2. **认证要求**: 除登录端点外，所有API需要JWT Bearer认证
3. **权限控制**: Admin角色可访问所有API，Doctor角色仅限业务API
4. **响应格式**: 所有API必须返回标准化的ApiResponse<T>格式
5. **错误处理**: 业务异常返回400系列，系统异常返回500系列
6. **日志记录**: 所有API请求响应必须记录结构化日志
7. **性能要求**: API响应时间<2秒，健康检查<500ms

## 📋 开发规范

### 代码结构要求
```
src/Server/Services/LYBT.WebAPI/
├── Controllers/
│   ├── Business/                    # 业务API控制器
│   │   ├── AuthController.cs
│   │   ├── UsersController.cs
│   │   ├── PatientsController.cs
│   │   ├── MedicalCaseController.cs
│   │   ├── ConsultationController.cs
│   │   ├── PrescriptionsController.cs
│   │   ├── HerbsController.cs
│   │   └── FormulasController.cs
│   └── System/                      # 系统管理控制器
│       ├── HealthController.cs
│       ├── MonitoringController.cs
│       ├── SecurityController.cs
│       └── CacheController.cs
├── Configuration/
│   ├── ServiceCollectionExtensions.cs  # 服务注册扩展
│   ├── MiddlewareExtensions.cs         # 中间件扩展
│   └── SwaggerConfiguration.cs         # Swagger配置
├── Properties/
│   └── launchSettings.json            # 启动配置
├── appsettings.json                   # 应用配置
├── appsettings.Development.json       # 开发环境配置
├── appsettings.Production.json        # 生产环境配置
├── Program.cs                         # 应用程序入口
└── LYBT.WebAPI.csproj                # 项目文件
```

### 控制器开发规范

#### UltraThink控制器架构标准
所有业务控制器必须继承`BaseApiController`：

```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService, ILogger<UsersController> logger, IMemoryCache cache)
        : base(logger, cache)
    {
        _userService = userService;
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] UserSearchDto searchDto)
    {
        try
        {
            var result = await _userService.SearchUsersAsync(searchDto);
            return HandleServiceResult(result, "获取用户列表成功");
        }
        catch (Exception ex)
        {
            return HandleException<PagedResult<UserDto>>(ex, "获取用户列表", searchDto);
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var validation = ValidateGuid<UserDto>(id, "用户ID");
            if (validation != null) return validation;
            
            var result = await _userService.GetByIdAsync(id);
            return HandleServiceResult(result, "获取用户详情成功");
        }
        catch (Exception ex)
        {
            return HandleException<UserDto>(ex, "获取用户详情", id);
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] UserCreateDto dto)
    {
        try
        {
            var validation = ValidateModel<UserDto>(dto);
            if (validation != null) return validation;
            
            var result = await _userService.CreateAsync(dto);
            return HandleServiceResult(result, "创建用户成功", StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            return HandleException<UserDto>(ex, "创建用户", dto);
        }
    }
}
```

#### 系统控制器规范
系统管理控制器继承`BaseSystemController`：

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class HealthController : BaseSystemController
{
    public HealthController(ILogger<HealthController> logger)
        : base(logger) { }
    
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            var checks = await PerformReadinessChecks();
            return SystemOk(checks, "系统就绪检查完成");
        }
        catch (Exception ex)
        {
            return HandleSystemException(ex, "系统就绪检查");
        }
    }
}
```

### 命名规范
- **控制器名**: PascalCase + Controller后缀 (UsersController)
- **Action方法**: PascalCase，符合HTTP动词语义 (GetUsers, CreateUser)
- **路由模板**: 小写kebab-case (`/api/v1/medical-case`)
- **参数名**: camelCase (searchDto, id)
- **响应模型**: PascalCase + Dto/Response后缀

### 质量标准
- **异常处理**: 所有Action必须有try-catch异常处理
- **参数验证**: 使用ModelState和自定义验证逻辑
- **响应统一**: 使用BaseController提供的响应方法
- **日志记录**: 关键操作使用LogOperation记录日志
- **性能监控**: 长时间操作使用性能计数器
- **API文档**: 所有Action添加完整的XML注释

### 测试要求
- **控制器单元测试**: 使用Mock测试Action方法逻辑
- **API集成测试**: 使用TestServer测试完整HTTP管道
- **认证授权测试**: 验证JWT认证和角色权限
- **异常处理测试**: 验证各种异常情况的响应

## 🔌 集成接口

### 对外提供的接口

#### RESTful API端点
WebAPI为前端WPF客户端提供完整的REST API：

```http
# 认证相关
POST /api/v1/auth/login           # 用户登录
POST /api/v1/auth/refresh         # 刷新令牌
POST /api/v1/auth/logout          # 用户登出
GET  /api/v1/auth/profile         # 获取当前用户信息

# 业务API（以用户管理为例）
GET    /api/v1/users              # 分页查询用户
GET    /api/v1/users/{id}         # 获取用户详情  
POST   /api/v1/users              # 创建新用户
PUT    /api/v1/users/{id}         # 更新用户信息
DELETE /api/v1/users/{id}         # 删除用户
GET    /api/v1/users/search       # 搜索用户
POST   /api/v1/users/batch        # 批量操作
```

#### 系统管理API
```http
# 健康检查
GET /api/v1/health/ready          # 应用就绪检查
GET /api/v1/health/live           # 应用存活检查
GET /api/v1/health/database       # 数据库连接检查
GET /api/v1/health/cache          # 缓存服务检查

# 性能监控
GET /api/v1/monitoring/metrics    # 系统性能指标
GET /api/v1/monitoring/logs       # 系统日志信息
GET /api/v1/cache/statistics      # 缓存统计信息
```

#### Swagger API文档
```http
GET /swagger                      # Swagger UI界面
GET /swagger/v1/swagger.json      # OpenAPI规范文档
```

### 依赖的外部接口
- **数据库服务**: SQL Server连接和查询
- **缓存服务**: IMemoryCache内存缓存
- **日志服务**: ILogger结构化日志
- **配置服务**: IConfiguration应用配置
- **健康检查**: IHealthCheck健康检查服务

### 数据传输格式

#### 标准请求格式
```json
// POST/PUT请求体
{
    "username": "doctor01",
    "email": "doctor01@lybt.com",
    "fullName": "张医生",
    "role": "Doctor"
}

// 查询参数
GET /api/v1/users?pageNumber=1&pageSize=10&keyword=张&status=Active
```

#### 标准响应格式
```json
// 成功响应
{
    "success": true,
    "message": "操作成功",
    "data": {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "username": "doctor01",
        "fullName": "张医生"
    },
    "timestamp": "2025-09-01T10:30:00Z",
    "requestId": "req-123456"
}

// 分页响应
{
    "success": true,
    "message": "查询成功",
    "data": {
        "items": [...],
        "pageNumber": 1,
        "pageSize": 10,
        "totalRecords": 25,
        "totalPages": 3
    },
    "meta": {
        "pageNumber": 1,
        "pageSize": 10,
        "totalRecords": 25,
        "totalPages": 3
    }
}

// 错误响应
{
    "success": false,
    "message": "用户名已存在",
    "data": null,
    "errorCode": "USER_EXISTS",
    "details": ["Username 'doctor01' is already taken"],
    "timestamp": "2025-09-01T10:30:00Z"
}
```

### 错误处理规范
- **参数验证错误**: 400 Bad Request + 详细验证消息
- **认证失败**: 401 Unauthorized + 认证错误信息
- **权限不足**: 403 Forbidden + 权限要求说明
- **资源不存在**: 404 Not Found + 资源标识信息
- **业务冲突**: 409 Conflict + 冲突原因说明
- **服务器错误**: 500 Internal Server Error + 错误追踪ID

## ⚙️ 配置管理

### 配置项定义

#### appsettings.json主配置
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;TrustServerCertificate=true"
  },
  "JwtOptions": {
    "Key": "YourSuperSecureKeyHere_MustBe256BitsOrMore_ForProductionUse",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client", 
    "ExpireMinutes": 480,
    "RefreshTokenExpireDays": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "CorsOptions": {
    "PolicyName": "LYBTCorsPolicy",
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://localhost:3001"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"],
    "AllowCredentials": true
  },
  "SwaggerOptions": {
    "Title": "凌隐宝堂中医诊所API",
    "Version": "v1",
    "Description": "凌隐宝堂中医诊所诊疗系统RESTful API文档",
    "ContactName": "UltraThink项目组",
    "ContactEmail": "support@lybt.com"
  },
  "HealthCheckOptions": {
    "DatabaseTimeoutSeconds": 10,
    "CacheTimeoutSeconds": 5,
    "DiskSpaceThresholdGB": 1,
    "MemoryThresholdPercent": 85
  }
}
```

#### appsettings.Development.json开发配置
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "SwaggerOptions": {
    "EnableUI": true,
    "EnableAnnotations": true
  },
  "DetailedErrors": true,
  "DeveloperExceptionPage": true
}
```

#### appsettings.Production.json生产配置
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "SwaggerOptions": {
    "EnableUI": false,
    "EnableAnnotations": false
  },
  "DetailedErrors": false,
  "DeveloperExceptionPage": false,
  "UseHttpsRedirection": true,
  "UseHsts": true
}
```

### 环境变量要求
生产环境敏感配置通过环境变量覆盖：

```bash
# 数据库连接
CONNECTIONSTRINGS__DEFAULTCONNECTION="Server=prod-sql;Database=LYBTDB;..."

# JWT密钥（生产环境必须设置）
JWTOPTIONS__KEY="ProductionSuperSecureKey256BitsOrMore_ChangeThis"

# 日志级别
LOGGING__LOGLEVEL__DEFAULT="Warning"

# CORS配置
CORSOPTIONS__ALLOWEDORIGINS__0="https://prod-domain.com"

# 应用环境
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_URLS="http://+:80;https://+:443"
```

### 部署配置说明

#### IIS部署配置
```xml
<!-- web.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\LYBT.WebAPI.dll" 
                  stdoutLogEnabled="false" 
                  stdoutLogFile=".\logs\stdout" />
    </system.webServer>
  </location>
</configuration>
```

#### Docker部署配置
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LYBT.WebAPI.csproj", "."]
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

## 🧪 测试规范

### 单元测试要求

#### 控制器单元测试
```csharp
public class UsersControllerTests : IDisposable
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly UsersController _controller;
    
    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _mockCache = new Mock<IMemoryCache>();
        _controller = new UsersController(_mockUserService.Object, _mockLogger.Object, _mockCache.Object);
    }
    
    [Fact]
    public async Task GetUser_ValidId_ReturnsOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserDto { Id = userId, Username = "testuser" };
        var serviceResult = ServiceResult<UserDto>.Success(userDto);
        
        _mockUserService.Setup(s => s.GetByIdAsync(userId))
                       .ReturnsAsync(serviceResult);
        
        // Act
        var result = await _controller.GetUser(userId);
        
        // Assert
        var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(userId);
    }
    
    [Fact]
    public async Task GetUser_InvalidGuid_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetUser(Guid.Empty);
        
        // Assert
        var actionResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = actionResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("无效");
    }
}
```

### 集成测试要求

#### WebAPI集成测试
```csharp
public class UsersApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public UsersApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task GetUsers_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task GetUsers_WithValidAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        // Act
        var response = await _client.GetAsync("/api/v1/users");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<UserDto>>>(content);
        apiResponse!.Success.Should().BeTrue();
    }
}
```

### 测试覆盖率目标
- **控制器类覆盖率**: >85%
- **Action方法覆盖率**: >90%
- **异常处理覆盖率**: >80%
- **认证授权覆盖率**: >95%

### 测试数据准备
```csharp
public static class ApiTestDataBuilder
{
    public static async Task<string> GetValidJwtTokenAsync(HttpClient client, 
        string username = "testadmin", string password = "TestPass123!")
    {
        var loginRequest = new { Username = username, Password = password };
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync("/api/v1/auth/login", content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponseDto>>(responseContent);
        
        return loginResponse!.Data!.AccessToken;
    }
}
```

## 🚀 部署说明

### 构建要求
- **.NET 8.0 SDK**: 编译和发布应用程序
- **SQL Server**: 2019或更高版本数据库服务
- **IIS 10+**: Windows服务器部署(推荐)
- **内存**: 最少1GB可用内存(推荐2GB)
- **磁盘**: 至少2GB可用空间

### 部署步骤

#### 1. 发布应用程序
```bash
# 清理和恢复依赖
dotnet clean
dotnet restore

# 发布生产版本
dotnet publish -c Release -o ./publish --self-contained false --runtime win-x64

# 验证发布文件
ls ./publish/LYBT.WebAPI.exe  # 确认可执行文件存在
```

#### 2. IIS站点配置
```powershell
# 创建应用程序池
New-WebAppPool -Name "LYBTWebAPI" -Force
Set-ItemProperty -Path "IIS:\AppPools\LYBTWebAPI" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\LYBTWebAPI" -Name "enable32BitAppOnWin64" -Value $false

# 创建网站
New-Website -Name "LYBT WebAPI" -Port 8080 -PhysicalPath "C:\inetpub\wwwroot\lybt-api" -ApplicationPool "LYBTWebAPI"

# 复制发布文件
Copy-Item -Path ".\publish\*" -Destination "C:\inetpub\wwwroot\lybt-api" -Recurse -Force
```

#### 3. 数据库初始化
```bash
# 更新数据库架构
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 验证数据库连接
dotnet run --urls="https://localhost:7001" --environment=Production
curl https://localhost:7001/api/v1/health/database
```

#### 4. SSL证书配置
```bash
# 绑定SSL证书到IIS站点
netsh http add sslcert ipport=0.0.0.0:443 certhash=<证书指纹> appid={GUID}

# 或使用Let's Encrypt自动化证书
certbot --iis -d api.lybt.com
```

### 环境依赖
- **操作系统**: Windows Server 2019+ (推荐) 或 Linux
- **运行时**: ASP.NET Core 8.0 Runtime
- **数据库**: SQL Server 2019+ 或 SQL Server Express
- **Web服务器**: IIS 10+ 或 Nginx 反向代理
- **防火墙**: 开放HTTP(80)和HTTPS(443)端口

### 运行监控

#### 应用程序监控
```http
# 基础健康检查
GET https://api.lybt.com/api/v1/health/ready
{
    "success": true,
    "status": "Healthy",
    "checks": {
        "database": "Healthy",
        "cache": "Healthy", 
        "disk": "Healthy"
    }
}

# 性能指标监控
GET https://api.lybt.com/api/v1/monitoring/metrics
{
    "success": true,
    "data": {
        "requestsPerSecond": 12.5,
        "averageResponseTime": 150,
        "activeConnections": 8,
        "memoryUsage": 67.3,
        "cpuUsage": 23.1
    }
}
```

#### 日志监控
```bash
# 查看应用日志 (Windows)
Get-EventLog -LogName Application -Source "LYBT.WebAPI" -Newest 50

# 查看IIS日志
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Tail 100

# 查看应用程序性能计数器
Get-Counter "\ASP.NET Apps v4.0.30319(_LM_W3SVC_1_ROOT_lybt-api)\Requests/Sec"
```

#### 告警规则配置
- **响应时间**: 平均响应时间>5秒触发警告，>10秒触发告警
- **错误率**: 5分钟内错误率>5%触发警告，>10%触发告警
- **数据库**: 数据库连接失败或响应时间>3秒触发告警
- **内存使用**: 内存使用率>85%触发警告，>95%触发告警
- **磁盘空间**: 可用磁盘空间<2GB触发警告，<1GB触发告警

## 📚 相关文档

### 相关项目文档链接
- [LYBT.Infrastructure项目文档](./infrastructure.md) - 基础设施服务和中间件
- [LYBT.Entities项目文档](./entities.md) - 实体模型定义和数据结构
- [后端业务模块文档](../modules/) - 8个业务模块的详细实现

### API文档链接
- [认证API规范](../../../api/auth-api.md) - JWT认证和授权接口
- [用户管理API](../../../api/users-api.md) - 用户CRUD操作接口
- [健康检查API](../../../api/health-api.md) - 系统监控和诊断接口
- [API响应格式规范](../../../api/api-response-format.md) - 统一响应格式标准

### 技术规范引用
- [UltraThink控制器架构](../../../ultrathink/ultrathink-comprehensive-refactoring-complete-20250831.md) - 控制器设计标准
- [ASP.NET Core最佳实践](../../../development/aspnetcore-best-practices.md) - Web API开发规范
- [RESTful API设计指南](../../../api/restful-api-design.md) - API设计原则和规范
- [JWT认证实施指南](../../../security/jwt-implementation-guide.md) - 认证安全最佳实践
- [API性能优化指南](../../../performance/api-optimization.md) - 响应时间和吞吐量优化

---

**文档版本**: v1.0  
**创建日期**: 2025-09-01  
**最后更新**: 2025-09-01  
**维护者**: UltraThink项目组  
**审核状态**: ✅ 已审核通过