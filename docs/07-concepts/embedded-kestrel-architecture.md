---
type: concept
title: 嵌入式 LocalWebAPI 架构
tags: [architecture, localwebapi, kestrel, offline]
related: [localwebapi-extensions, dynamic-port-discovery, http-proxy-repository-pattern, sql-server-localdb]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/03-architecture/dual-mode.md"]
---

# 嵌入式 LocalWebAPI 架构

## 定义

嵌入式 LocalWebAPI 是指在 WPF 桌面客户端进程内托管的 ASP.NET Core Kestrel WebAPI 服务器。它为离线模式提供标准化的 HTTP 数据访问接口，使用 SQL Server LocalDB 作为本地数据存储。

## 核心设计目标

1.  **统一数据访问**：远程和本地模式共用同一套 Repository 接口（通过 [[http-proxy-repository-pattern]] 实现）。
2.  **简化部署与维护**：使用 SQL Server 保持与远程环境数据结构一致，避免 SQLite 带来的同步转换成本。
3.  **进程内托管**：避免独立进程管理的复杂性，通过 `LocalWebApiHost` 统一管理生命周期。

## 关键机制

### 动态端口发现 (Dynamic Port Discovery)

LocalWebAPI 启动时监听端口 0（由操作系统自动分配可用端口）。
*   **获取端口**：启动后通过 `IServerAddressesFeature` 获取实际绑定的端口号。
*   **客户端连接**：`HttpClientApiClient` 通过 `LocalWebApiHost.Port` 动态构建 BaseAddress，确保连接正确。

### 生命周期管理 (LocalWebApiHost)

*   **StartAsync**: 创建数据库目录 -> 构建 WebApplication -> 配置动态端口 -> 初始化数据库 -> 启动 Kestrel -> 捕获端口。
*   **StopAsync**: 优雅停止服务，设置超时保护。
*   **线程安全**：使用锁机制防止并发启动/停止。

### 简化认证

*   **JWT 配置**：使用 HMAC-SHA256 签名，Token 有效期设为 1 年。
*   **无刷新机制**：离线场景下无需复杂的 Refresh Token 轮换，简化了认证流程。

## 项目结构

```
src/Client/Desktop/LocalWebAPI/
├── LocalWebApiProgram.cs      # 入口点
├── LocalWebApiHost.cs         # 生命周期管理器
├── Controllers/               # API 控制器 (Auth, Users, Patients, etc.)
├── Data/                      # EF Core DbContext & SeedData
└── Repositories/              # HTTP Proxy Repository 实现
```

## 限制与排除

根据 **TBD-01** 决策，以下功能在本地模式下不可用：
*   Token 刷新
*   系统级审计日志查询
*   自动登录令牌同步
*   用户数据同步