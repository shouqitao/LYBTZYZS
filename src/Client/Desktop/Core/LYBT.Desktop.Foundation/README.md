# LYBT.Desktop.Foundation

> Desktop端技术基础层 | HTTP通信/缓存/配置/安全/性能

## 项目定位

- **层级**: Client Core层
- **职责**: 提供平台无关的技术基础能力(无WPF依赖)，支持跨平台复用

## 目录结构

```
LYBT.Desktop.Foundation/
├── Api/Managers/                 # API管理
├── Caching/                      # 缓存服务
│   └── CacheService.cs
├── Configuration/                # 配置管理
│   └── ConfigurationService.cs
├── Diagnostics/                  # 诊断服务
├── Exceptions/                   # 异常处理(5文件)
├── Extensions/                   # 扩展方法(3文件)
├── HealthCheck/                  # 健康检查(2文件)
├── Http/                         # HTTP客户端(3文件)
├── Modules/                      # 模块加载(2文件)
├── Performance/                  # 性能优化(2文件)
├── Repositories/                 # 仓储基类
│   └── BaseApiRepository.cs
├── Security/                     # 安全服务(9文件)
└── Settings/                     # 设置管理
```

## 核心服务

| 服务 | 方法数 | 说明 |
|------|--------|------|
| IAuthenticationService | 8 | 登录/登出/Token管理/密码修改 |
| ICacheService | 7 | 缓存CRUD/GetOrCreate |
| IConfigurationService | 10 | 配置读写/用户设置 |
| IApiService | 15 | RESTful CRUD/文件操作 |
| BaseApiRepository<T> | 8 | API仓储基类(CRUD+分页+搜索) |
| IApiHealthCheckService | 2 | API健康状态检查 |
| IStartupOptimizationService | 7 | 启动优化/预加载/预热 |
| IExceptionHandler | 4 | 异常处理/SafeExecute |

## 设计特点

| 特点 | 说明 |
|------|------|
| 平台无关 | 无WPF依赖，纯.NET 8技术栈 |
| Polly集成 | 重试/熔断/超时弹性策略 |
| DPAPI加密 | 安全凭证存储 |
| 三层HTTP抽象 | IApiService → ApiService → BaseApiRepository |

## 依赖关系

### 依赖
- LYBT.Shared.Models (共享DTO)
- LYBT.Shared.Utilities (共享工具)
- Microsoft.Extensions.Http (8.0.x)
- Microsoft.Extensions.Caching.Memory (8.0.x)
- Polly (8.x)

### 被依赖
- LYBT.Desktop.Infrastructure (WPF基础设施)
- LYBT.Desktop.Shell (DI注册)
- 所有Desktop业务模块

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-01-29 | Foundation与Infrastructure职责分离 |
