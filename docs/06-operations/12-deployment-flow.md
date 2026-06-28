# 部署流程

## 远程模式部署

```
1. 服务器准备
   ├─ 安装 .NET 8 Runtime
   ├─ 安装 SQL Server 2019+
   └─ 配置防火墙（端口 5000）

2. 数据库初始化
   ├─ 创建数据库 LYBTDB_Dev
   └─ 运行 EF Core 迁移

3. 部署 WebAPI
   ├─ 发布框架依赖部署（Framework-Dependent，非自包含）
   ├─ 配置 appsettings.json
   ├─ 注册为 Windows 服务或计划任务
   └─ 启动服务

4. 部署 Desktop
   ├─ 安装 .NET 8 Desktop Runtime
   ├─ 复制客户端文件
   └─ 配置连接地址（指向服务器 IP:5000）

5. 验证
   ├─ 访问 /health 健康检查
   ├─ 以 sysadmin 登录测试
   └─ 创建首个 admin 账户
```

## 本地模式部署

> LocalWebAPI 嵌入端口 **5300**（`EmbeddedLocalWebApiService` + `appsettings.json:OfflineMode:LocalApiBaseUrl`）。独立调试端口 5290 仅独立运行生效，嵌入模式不监听。

```
1. 安装 Desktop
   ├─ 运行安装包
   └─ 自动安装 LocalDB

2. 首次启动
   ├─ 检测到无远程连接
   ├─ 自动切换本地模式
   ├─ 初始化 LocalDB
   └─ 走 sysadmin 登录流程

3. 验证
   ├─ 登录 sysadmin
   ├─ 创建 admin 账户
   └─ 测试基本功能
```

## 更新流程

```
1. 服务端更新
   ├─ 停止旧服务
   ├─ 备份数据库
   ├─ 部署新版本
   ├─ 运行迁移
   └─ 启动新服务

2. 客户端更新
   ├─ 检测新版本（Velopack）
   ├─ 下载更新包
   ├─ 自动重启应用
   └─ 验证版本号
```

## 回滚流程

```
1. 服务端回滚
   ├─ 停止新服务
   ├─ 恢复数据库备份
   ├─ 部署旧版本
   └─ 启动旧服务

2. 客户端回滚
   ├─ 卸载新版本
   └─ 安装旧版本
```

## 健康检查端点

| 端点 | 方法 | 认证 | 用途 |
|------|------|:---:|------|
| `/health` | GET | 否 | 存活探针（中间件 MapHealthChecks） |
| `/health/database` | GET | 否 | 数据库连接检查（中间件层，**仅 Server WebAPI**） |
| `/api/v1/health/ping` | GET | 否 | Ping 测试（控制器） |
| `/api/v1/health/details` | GET | 是 | 详细健康状态（控制器，需 Bearer Token） |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-28 | v1.1 | 自包含 exe 改为框架依赖部署；补 LocalWebAPI 嵌入端口 5300；健康表 /ping→/api/v1/health/ping，/health/details 标注需认证，补 /health/database（仅 Server） |
| 2026-06-12 | v1.0 | 初始版本 |
