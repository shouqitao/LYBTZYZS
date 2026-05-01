# LocalWebAPI 架构概览

## 背景

LocalWebAPI 是在 WPF 桌面进程内嵌入的 ASP.NET Core Kestrel WebAPI 服务器，使用 SQLite 作为本地数据库。它是三模式架构（Remote / Local / LocalWebAPI）中的推荐本地离线方案。

## 设计目标

| 目标 | 说明 |
|------|------|
| 统一数据访问层 | 远程/本地模式共用同一套 Repository 接口 |
| 简化部署 | SQLite 单文件数据库，无需安装 SQL Server LocalDB |
| 降低维护成本 | 减少 6 对 LocalRepository 实现，改为 6 个 HTTP Proxy Repository |

## 架构

```
Desktop (WPF/Prism)
  └── ConnectionModeProvider
       ├── Remote → Refit HTTP → Server WebAPI → SQL Server
       ├── Local → LocalRepository → LocalDbContext → LocalDB (遗留)
       └── LocalWebAPI → HttpRepository → Kestrel (in-process) → SQLite
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
│   ├── LocalWebApiDbContext.cs      # EF Core DbContext (SQLite)
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

SQLite 数据库上下文：
- 复用 Server 端实体配置 (`ApplyConfigurationsFromAssembly`)
- 自动应用全局查询过滤器 (`IsDeleted`)
- 数据库文件位置：`%LOCALAPPDATA%\LYBT\localwebapi.db`

### HTTP Proxy Repository

实现与 Remote/Local 相同的 Repository 接口：
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

## 模式切换

`ConnectionModeProvider.SwitchModeAsync()` 七步流程：
1. ActiveConsultation 检查
2. ModeSwitchValidator 验证
3. Region 清理
4. 切换模式
5. 数据库初始化 (Local 模式)
6. LocalWebAPI 生命周期管理 (启动/停止 Kestrel)
7. 通知 + 导航首页

## 相关文档

- [三模式架构](../three-mode.md)
- [LocalWebAPI API 端点](./api-endpoints.md)
- [LocalWebAPI 认证](./authentication.md)
- [LocalWebAPI 部署](./deployment.md)
- [ADR-0009: 嵌入式 LocalWebAPI](../decisions/0009-localwebapi-embedded.md)
