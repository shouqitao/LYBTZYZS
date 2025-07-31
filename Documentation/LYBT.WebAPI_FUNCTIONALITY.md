# LYBT.WebAPI 功能说明

## 模块概述

LYBT.WebAPI 是智能中医诊疗系统的主要 Web API 项目，作为整个系统的统一入口点，集成所有业务模块并通过 RESTful API 对外提供服务。该项目基于 ASP.NET Core 8.0 构建，采用模块化架构设计。

## 主要功能

### 1. 统一 API 网关

- **模块集成**：集成所有 LYBT.Module.* 业务模块
- **统一路由**：提供统一的 API 路由和版本管理
- **跨域支持**：配置 CORS 支持前端应用访问
- **API 文档**：集成 Swagger/OpenAPI 文档

### 2. 认证与授权

- **JWT 认证**：基于 JSON Web Token 的身份验证
- **角色授权**：支持基于角色的访问控制（RBAC）
- **安全中间件**：全局异常处理和安全防护
- **会话管理**：用户会话状态管理

### 3. 中间件管道

- **全局异常处理**：统一的异常捕获和错误响应
- **性能监控**：请求性能监控和统计
- **请求日志**：详细的API请求日志记录
- **缓存处理**：响应缓存和数据缓存

### 4. 数据验证与响应

- **模型验证**：自动的请求模型验证
- **统一响应格式**：标准化的 API 响应结构
- **分页支持**：统一的分页查询处理
- **数据转换**：DTO 与实体模型的自动映射

## 技术架构

### 项目结构

```
LYBT.WebAPI/
├── Controllers/                    # API 控制器
│   ├── BaseController.cs          # 控制器基类
│   ├── AuthController.cs          # 认证控制器
│   ├── UsersController.cs         # 用户管理
│   ├── PatientsController.cs      # 患者管理
│   ├── DoctorsController.cs       # 医生管理
│   ├── RegistrationController.cs  # 挂号管理
│   ├── QueueingController.cs      # 排队管理
│   ├── DiagnosisTreatmentController.cs # 诊疗管理
│   ├── PrescriptionsController.cs # 处方管理
│   ├── HerbsController.cs         # 药材管理
│   ├── PharmacyController.cs      # 药房管理
│   ├── BillingController.cs       # 计费管理
│   ├── RecordsController.cs       # 病历管理
│   ├── UnifiedConfigController.cs # 配置管理
│   └── UnifiedLogsController.cs   # 日志管理
├── Extensions/                     # 扩展方法
│   ├── ServiceCollectionExtension.cs # 服务注册扩展
│   ├── SwaggerExtension.cs        # Swagger 配置
│   ├── CorsExtension.cs           # CORS 配置
│   └── CacheExtensions.cs         # 缓存扩展
├── Middleware/                     # 中间件
│   ├── GlobalExceptionMiddleware.cs # 全局异常处理
│   └── PerformanceMiddleware.cs   # 性能监控
├── Properties/                     # 项目属性
│   └── launchSettings.json        # 启动配置
├── Program.cs                      # 应用入口点
├── appsettings.json               # 应用配置
├── Dockerfile                     # Docker 配置
└── LYBT.WebAPI.http              # HTTP 测试文件
```

### 依赖注入配置

- **模块服务注册**：自动注册所有业务模块服务
- **数据库上下文**：配置各模块的独立数据库上下文
- **缓存服务**：内存缓存和分布式缓存配置
- **日志服务**：结构化日志和审计日志配置

## API 控制器详解

### 1. BaseController

提供所有控制器的基础功能：

- **统一日志记录**：操作日志和审计日志
- **模型验证**：请求数据验证
- **异常处理**：标准化异常处理
- **响应格式**：统一的API响应格式

### 2. 业务控制器

每个业务模块都有对应的控制器：

#### AuthController - 认证管理

- `POST /api/v1/auth/login` - 用户登录
- `POST /api/v1/auth/logout` - 用户登出
- `POST /api/v1/auth/changeSysAdminPassword` - 修改管理员密码

#### UsersController - 用户管理

- `GET /api/v1/users` - 获取用户列表
- `GET /api/v1/users/{id}` - 获取用户详情
- `POST /api/v1/users` - 创建用户
- `PUT /api/v1/users/{id}` - 更新用户
- `DELETE /api/v1/users/{id}` - 删除用户
- `POST /api/v1/users/batch-enable` - 批量启用用户
- `POST /api/v1/users/batch-disable` - 批量禁用用户

#### PatientsController - 患者管理

- `GET /api/v1/patients` - 获取患者列表
- `GET /api/v1/patients/{id}` - 获取患者详情
- `POST /api/v1/patients` - 创建患者档案
- `PUT /api/v1/patients/{id}` - 更新患者信息
- `POST /api/v1/patients/assign-doctor` - 分配医生
- `GET /api/v1/patients/export` - 导出患者数据

#### 其他业务控制器

- **DoctorsController**：医生信息管理
- **RegistrationController**：挂号预约管理  
- **QueueingController**：诊疗排队管理
- **DiagnosisTreatmentController**：诊疗记录管理
- **PrescriptionsController**：处方开具管理
- **HerbsController**：中药材库存管理
- **PharmacyController**：药房调剂管理
- **BillingController**：费用结算管理
- **RecordsController**：电子病历管理

## 配置管理

### 应用配置 (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "数据库连接字符串"
  },
  "JwtOptions": {
    "SecretKey": "JWT密钥",
    "Issuer": "令牌发行者",
    "Audience": "令牌受众",
    "ExpiryInMinutes": 120
  },
  "UserDefaults": {
    "DefaultUserPassword": "默认用户密码"
  }
}
```

### 环境配置

- **Development**：开发环境配置，启用详细日志和异常页面
- **Production**：生产环境配置，优化性能和安全性
- **Docker**：容器化部署配置

## 中间件管道

### 请求处理流程

1. **CORS 处理**：跨域请求处理
2. **认证中间件**：JWT 令牌验证
3. **性能监控**：请求性能统计
4. **路由中间件**：URL 路由解析
5. **全局异常处理**：统一异常捕获
6. **控制器执行**：业务逻辑处理
7. **响应格式化**：统一响应格式

### 安全特性

- **HTTPS 重定向**：强制使用 HTTPS
- **安全头设置**：设置安全相关的 HTTP 头
- **请求限流**：防止恶意请求攻击
- **输入验证**：防止 SQL 注入和 XSS 攻击

## 性能优化

### 缓存策略

- **内存缓存**：热点数据的内存缓存
- **分布式缓存**：支持 Redis 分布式缓存
- **响应缓存**：API 响应结果缓存
- **数据库缓存**：EF Core 查询缓存

### 数据库优化

- **连接池**：数据库连接池配置
- **查询优化**：高效的 LINQ 查询
- **延迟加载**：合理的数据加载策略
- **批量操作**：批量数据处理优化

## 部署与运维

### Docker 容器化

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LYBT.WebAPI/LYBT.WebAPI.csproj", "LYBT.WebAPI/"]
RUN dotnet restore "LYBT.WebAPI/LYBT.WebAPI.csproj"
```

### 健康检查

- **数据库连接检查**：验证数据库连接状态
- **外部服务检查**：检查依赖的外部服务
- **内存使用监控**：监控应用内存使用情况
- **响应时间监控**：监控API响应性能

### 日志监控

- **结构化日志**：使用 Serilog 进行结构化日志记录
- **日志级别**：不同环境使用不同的日志级别
- **日志聚合**：支持日志聚合和分析平台
- **性能指标**：记录关键性能指标

## API 版本控制

### 版本策略

- **URL 版本控制**：通过 URL 路径指定版本 (`/api/v1/`)
- **向后兼容**：保持 API 向后兼容性
- **废弃通知**：合理的 API 废弃通知机制
- **文档版本**：每个版本的独立文档

### Swagger 文档

- **自动生成**：基于控制器和模型自动生成 API 文档
- **交互式测试**：支持在线 API 测试
- **示例数据**：提供完整的请求和响应示例
- **认证配置**：集成 JWT 认证的测试支持

## 开发指南

### 新增控制器

1. 继承 `BaseController` 基类
2. 实现统一的错误处理
3. 添加适当的授权特性
4. 编写 XML 文档注释

### API 设计原则

- **RESTful 设计**：遵循 REST 设计原则
- **统一命名**：使用一致的命名约定
- **状态码规范**：正确使用 HTTP 状态码
- **错误信息**：提供清晰的错误信息

### 测试策略

- **单元测试**：控制器和服务的单元测试
- **集成测试**：API 端到端集成测试
- **性能测试**：API 性能和负载测试
- **安全测试**：安全漏洞和渗透测试

## 注意事项

1. **数据安全**：严格保护患者医疗数据的隐私和安全
2. **合规要求**：确保符合医疗行业的相关法规和标准
3. **性能监控**：持续监控 API 性能和可用性
4. **错误处理**：提供友好的错误信息和恢复建议
5. **文档维护**：保持 API 文档的及时更新

LYBT.WebAPI 作为整个智能中医诊疗系统的核心接口层，为前端应用和第三方系统提供稳定、安全、高效的 API 服务。