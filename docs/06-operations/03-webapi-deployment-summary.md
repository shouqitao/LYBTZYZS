# LYBT WebAPI 部署总结

> 记录时间: 2026-04-22
> 环境: **开发环境**（非生产）

---

## 环境说明

| 角色 | 主机 | IP | 系统 | 说明 |
|------|------|-----|------|------|
| 开发主机 | 开发者 PC | - | Ubuntu | 代码编写、编译、Git |
| 服务器 | WIN-URSB5I68VL5 | 192.168.190.248 | Windows Server 2012 R2 | WebAPI 运行 |
| 桌面端 | Desktop PC | 192.168.190.6 | Windows | WPF 客户端 |

> 248 和 6 是为配合 Ubuntu 开发主机而创建的开发环境。

---

## 当前部署状态

| 项目 | 值 |
|------|-----|
| 部署方式 | 框架依赖发布（Framework-Dependent） |
| 启动方式 | Windows 计划任务（`schtasks`） |
| 任务名 | `LYBT-API` |
| 运行身份 | SYSTEM |
| 启动触发 | 系统启动时自动拉起 |
| 进程 | `dotnet LYBT.WebAPI.dll` |
| 监听地址 | `http://0.0.0.0:5000` |
| 数据库 | SQL Server (localhost) - Windows Authentication |
| 运行时 | .NET 8 (已安装 SDK 8.0.420) |

---

## 部署路径

```
C:\Services\LYBT-API\          # 主部署目录
├── LYBT.WebAPI.dll
├── appsettings.json            # 基础配置
├── appsettings.Production.json # 生产配置（ASPNETCORE_ENVIRONMENT=Production）
├── start-service.bat           # 计划任务入口
├── logs\                       # 日志目录
└── ...

C:\LYBTZYZS\                   # 源码目录（服务器上）
└── src\Server\Services\LYBT.WebAPI\
```

---

## 配置要点

### 连接字符串
- Server: `localhost`
- 数据库: `LYBTDB`
- 认证: `Trusted_Connection=True`（Windows Authentication）

### 密码配置
```json
{
  "DefaultPasswords": {
    "SysAdminPassword": "<REDACTED>",
    "NewUserPassword": "<REDACTED>"
  },
  "SystemAdmin": {
    "Email": "admin@lybt.com"
  }
}
```

### Kestrel 监听
- 基础配置: `http://localhost:5000`
- Production 覆盖: `http://0.0.0.0:5000`

---

## 启动方式选型

### 尝试过的方案

| 方案 | 结果 | 原因 |
|------|------|------|
| `sc.exe create` + bat | ❌ 1053 错误 | bat 无法传递 SCM 生命周期信号 |
| `sc.exe create` + dotnet.exe host | ❌ 1053 错误 | Server 2012 R2 默认 SCM 超时 30s |
| `sc.exe create` + 自包含 exe | ❌ 不兼容 | .NET 8 自包含 exe 不支持 Server 2012 R2 |
| `wmic process call create` | ✅ 可用 | 但不随系统启动 |
| **计划任务 `schtasks`** | ✅ **采用** | 开机自启 + 无超时问题 |

### 最终方案: 计划任务

```powershell
# 创建开机启动任务
schtasks /create /tn "LYBT-API" `
    /tr "C:\Services\LYBT-API\start-service.bat" `
    /sc onstart /ru SYSTEM /rl HIGHEST /f

# 手动启动
schtasks /run /tn "LYBT-API"
```

**start-service.bat:**
```bat
@echo off
cd /d C:\Services\LYBT-API
set ASPNETCORE_ENVIRONMENT=Production
"C:\Program Files\dotnet\dotnet.exe" LYBT.WebAPI.dll >> logs\service.log 2>&1
```

---

## 遇到的问题与解决

### 1. 中文字符编码丢失
**现象**: `fix-config.ps1` 含中文注释，经 base64 传输到 Windows 后编码损坏，导致 JSON 文件解析失败。

**解决**: 将中文替换为 ASCII 字符，或使用 SCP 二进制传输（`scp` 优于 base64 管道）。

### 2. 密码策略校验失败
**现象**: `DefaultPasswords` 含环境变量占位符 `${SYSADMIN_PASSWORD}`，未被替换。

**解决**: 用 Python 读取 JSON → 修改值 → SCP 上传覆盖（PowerShell 的 `$` 转义太复杂）。

### 3. SSH 子进程被回收
**现象**: 通过 SSH 执行 `Start-Process` 启动的 dotnet 进程，在 SSH 会话结束后被系统回收。

**解决**: 使用 `wmic process call create` 或计划任务启动独立进程。

### 4. SC 1053 错误
**现象**: `sc.exe start LYBT-API` 报 1053 "服务没有及时响应启动或控制请求"。

**根本原因**: Server 2012 R2 SCM 默认超时 30s，.NET 应用初始化时间超过此限制。

**解决**: 升级到 Server 2016（SCM 超时更长 + .NET 8 原生支持）可彻底解决。当前用计划任务绕过。

### 5. 自包含 exe 不兼容
**现象**: Server 2012 R2 上运行 .NET 8 自包含 exe 报 "不是有效的 Win32 应用程序"。

**解决**: 保持框架依赖发布 + dotnet runtime 模式。

---

## 常用运维命令

```powershell
# 查看任务状态
schtasks /query /tn LYBT-API /fo LIST

# 手动启动
schtasks /run /tn "LYBT-API"

# 停止
Stop-Process -Name dotnet -Force

# 查看进程
Get-Process dotnet

# 查看端口
Get-NetTCPConnection -LocalPort 5000

# 健康检查
(New-Object Net.WebClient).DownloadString("http://127.0.0.1:5000/health")

# 查看日志
Get-Content C:\Services\LYBT-API\logs\lybt-web-api-*.log -Tail 50
```

---

## 后续优化

- [ ] 升级到 Windows Server 2016 → 改用原生 Windows Service 模式
- [ ] 升级到 SQL Server 2016 SP2（与 Server 2016 原生配对）
- [ ] `fix-config.ps1` 去除中文注释，避免编码问题
- [ ] 添加日志轮转监控（Serilog 已配置，但 log 目录需定期清理）
