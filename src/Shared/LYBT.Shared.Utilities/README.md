# LYBT.Shared.Utilities

> 共享工具类库 | 配置管理/安全工具/扩展方法

## 项目定位

- **层级**: Shared层
- **职责**: 提供配置管理、安全工具、扩展方法、中间件配置等核心功能

## 目录结构

```
LYBT.Shared.Utilities/
├── Configuration/            # 配置管理(2类)
│   ├── ConfigurationHelper.cs
│   └── EnvironmentHelper.cs
├── Extensions/               # 扩展方法(5类)
│   ├── Application/
│   │   ├── ApplicationInitializationExtensions.cs
│   │   └── MiddlewareConfigurationExtensions.cs
│   └── ServiceCollection/
│       ├── AuthenticationExtensions.cs
│       ├── AuthorizationExtensions.cs
│       └── CacheExtensions.cs
├── Helpers/                  # 帮助类(1类)
│   └── PasswordHelper.cs
└── Security/                 # 安全工具(2类)
    ├── ClaimsHelper.cs
    └── RoleHelper.cs
```

## 核心组件

| 工具类 | 方法数 | 说明 |
|--------|--------|------|
| ConfigurationHelper | 3 | 连接字符串/配置节/必需值读取 |
| EnvironmentHelper | 3 | 环境检测(开发/生产/当前) |
| PasswordHelper | 4 | Hash/Verify/CheckStrength/Generate |
| ClaimsHelper | 4 | GetUserId/GetUserName/GetRoles/CreateJwtClaims |
| RoleHelper | 4 | IsAdmin/IsDoctor/HasAccess/GetDisplayName |

## 扩展方法

| 扩展方法 | 说明 |
|----------|------|
| AddJwtAuthentication | JWT认证配置 |
| AddRoleBasedAuthorization | RBAC授权策略(4个Policy) |
| AddMemoryCacheService | 内存缓存服务 |
| AddDistributedCacheService | 分布式缓存(Redis) |
| UseGlobalExceptionHandler | 全局异常处理中间件 |
| UseConfiguredCors | CORS配置中间件 |

## 密码安全

| 特性 | 说明 |
|------|------|
| 哈希算法 | ASP.NET Core Identity PasswordHasher(BCrypt) |
| 强度评分 | 7个标准(长度3分+字符类型4分) |
| 强度级别 | Weak/Fair/Good/Strong/VeryStrong |

## 依赖关系

### 依赖
- LYBT.Shared.Models (UserDto、ApiResponse等)

### 被依赖
- LYBT.WebAPI (JWT认证、授权、异常处理)
- LYBT.Infrastructure (配置管理、缓存扩展)
- LYBT.Desktop.Infrastructure (配置和安全工具)

### NuGet包
- Microsoft.Extensions.Configuration (8.0.x)
- Microsoft.AspNetCore.Identity (8.0.x)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.x)
- Microsoft.Extensions.Caching.Memory (8.0.x)
- Microsoft.Extensions.Caching.StackExchangeRedis (8.0.x)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-09-20 | 工具集完善 |
