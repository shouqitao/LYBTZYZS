# LocalWebAPI 架构概览

> **注意**: 本文档的核心内容已合并到 [05-dual-mode.md](../05-dual-mode.md) §"LocalWebAPI 架构"。此文件保留作为详细实现参考。

## 背景

LocalWebAPI 是在 WPF 桌面进程内嵌入的 ASP.NET Core Kestrel WebAPI 服务器，使用 SQL Server 作为本地数据库。它是系统本地离线模式的实现方案。

## 设计目标

| 目标 | 说明 |
|------|------|
| 统一数据访问层 | 远程/本地模式共用同一套 Repository 接口 |
| 简化部署 | 使用 SQL Server，与远程模式保持一致 |
| 降低维护成本 | 用 HTTP Proxy Repository 替代独立的 LocalRepository 实现 |

## 架构

```
Desktop (WPF/Prism)
  ├── Remote → Refit HTTP → Server WebAPI → SQL Server
  └── LocalWebAPI → HttpRepository → Kestrel (in-process) → SQL Server
```

## 项目结构

```
src/Client/Desktop/LocalWebAPI/
├── LYBT.LocalWebAPI.csproj          # Microsoft.NET.Sdk.Web, net8.0
├── LocalWebApiProgram.cs            # WebAPI 入口
├── LocalWebApiHost.cs               # Kestrel 生命周期管理器
├── Auth/
│   └── LocalJwtConfig.cs            # JWT 认证 (HMAC-SHA256, 1年有效期)
├── Controllers/                     # 8 个控制器
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── PatientsController.cs
│   ├── HerbsController.cs
│   ├── FormulasController.cs
│   ├── RegistrationsController.cs
│   ├── MedicalCasesController.cs
│   └── HealthController.cs
├── Data/
│   ├── LocalWebApiDbContext.cs      # EF Core DbContext (SQL Server)
│   └── LocalWebApiSeedData.cs       # 种子数据
└── Repositories/                    # 6 个 HTTP Proxy Repository
    ├── HttpPatientRepository.cs
    ├── HttpUserRepository.cs
    ├── HttpHerbRepository.cs
    ├── HttpFormulaRepository.cs
    ├── HttpRegistrationRepository.cs
    └── HttpMedicalCaseRepository.cs
```

## 核心组件

### LocalWebApiHost

Kestrel 生命周期管理器，负责：
- `StartAsync()`: 构建 WebApplication → 配置动态端口 → 初始化数据库 → 启动 Kestrel → 捕获实际端口
- `StopAsync()`: 取消令牌 → 等待运行任务 → 释放资源
- 线程安全：lock 防止并发 Start/Stop
- 幂等性：重复 StartAsync 不会重复启动

### LocalWebApiDbContext

SQL Server 数据库上下文：
- 复用 Server 端实体配置 (`ApplyConfigurationsFromAssembly`)
- 自动应用全局查询过滤器 (`IsDeleted`)

### HTTP Proxy Repository

实现与 Remote 相同的 Repository 接口：
- 通过 HttpClient 调用 LocalWebAPI 端点
- System.Text.Json 序列化 (PascalCase)
- 不支持的端点返回 null 并记录警告

### 动态端口发现

使用端口 0 (OS 自动分配)，启动后通过 `IServerAddressesFeature` 获取实际端口：
```csharp
var addressFeature = _app.ServerFeatures.Get<IServerAddressesFeature>();
Port = new Uri(addressFeature.Addresses.First()).Port;
```

## 认证

简化的 JWT 认证：
- HMAC-SHA256 签名密钥 (固定 secret)
- Token 有效期 1 年
- 无 Refresh Token
- 无外部认证提供者

## 相关文档

- [双模式架构](../05-dual-mode.md)
- [LocalWebAPI API 端点](./api-endpoints.md)
- [LocalWebAPI 认证](./authentication.md)

> 架构决策详见 [decisions/](../decisions/) 目录。
