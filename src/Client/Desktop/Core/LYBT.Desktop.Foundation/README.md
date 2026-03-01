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

## 设计依据

- 与Infrastructure分离，保持平台无关性(无WPF依赖)，为未来跨平台(MAUI等)复用奠定基础
- BaseApiRepository提供统一的CRUD+分页基类，各模块仓储继承后只需关注业务差异
- 集成Polly弹性策略(重试/熔断/超时)，将网络不稳定性的处理统一在基础层，业务层无需关心
- DPAPI加密凭证存储，确保Token等敏感数据不以明文形式保存在本地

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
