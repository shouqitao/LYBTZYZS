# LYBTZYZS WebAPI - Windows Server 2012 IIS 一键部署

本目录提供 **IIS 反向代理 + Kestrel 服务** 的生产部署模板，适用于 .NET 8 WebAPI。

## 文件说明

- `deploy.ps1`：主部署脚本（发布、环境变量、Windows Service、调用 IIS 配置）
- `setup-iis.ps1`：IIS/URL Rewrite/ARR 自动配置脚本
- `install-sqlserver.ps1`：SQL Server Standard/Express 安装脚本（仅本地访问，无外部 TCP）
- `web.config`：IIS 反向代理配置模板（转发到 `127.0.0.1:5000`）
- `appsettings.Override.json`：生产环境配置覆盖模板
- `.env.example`：环境变量模板

## 前置条件

1. 以管理员身份运行 PowerShell
2. Windows Server 已联网（用于自动下载安装 .NET Hosting Bundle / URL Rewrite / ARR）
3. 确保端口 80、5000、5001 未被占用

## 部署架构

```
外部设备/客户端
       │
       │ HTTP (80/5000)
       ▼
  ┌──────────┐      ┌──────────────┐      ┌─────────────┐
  │   IIS    │──────▶│  Kestrel     │──────▶│ SQL Server  │
  │  (ARR)   │ 本地  │  (WebAPI)    │ 本地  │  (仅本地)    │
  └──────────┘      └──────────────┘      └─────────────┘
   :80               :5000                  (Shared Memory)
```

**安全设计：**
- **WebAPI** (`:5000`)：监听 `0.0.0.0`，允许外部设备访问
- **SQL Server**：仅本地访问，TCP/IP 已禁用，无防火墙规则
- **IIS** (`:80`)：反向代理到 Kestrel，提供统一入口

## 一键部署

在仓库根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\iis\deploy.ps1 \
  -JwtSecret "请替换为至少32位随机密钥" \
  -SysAdminPassword "请替换为强密码" \
  -NewUserPassword "请替换为强密码"
```

> 默认行为：
>
> - 发布目录：`C:\Services\LYBT-WebAPI\app`
> - Kestrel Service：`LYBT-WebAPI-Kestrel`（监听 `0.0.0.0:5000`，允许外部访问）
> - IIS Site：`LYBT-WebAPI`（HTTP 80，反向代理到 Kestrel 5000）
> - SQL Server：`MSSQLSERVER`（默认实例，Standard 版本，仅本地访问）

## 常用参数

```powershell
-PublishRoot "C:\Services\LYBT-WebAPI"
-ServiceName "LYBT-WebAPI-Kestrel"
-SiteName "LYBT-WebAPI"
-KestrelHttpPort 5000
-KestrelHttpsPort 5001
-IISHttpPort 80
-IISHttpsPort 443
-HostHeader "api.yourdomain.com"
-SqlEdition "Standard"                    # Standard 或 Express
-SqlInstallerPath "D:\SQL2022\setup.exe"  # Standard 版本需要提供安装程序路径
```

## HTTPS 说明

- 本方案默认完成 HTTP 反向代理（IIS -> Kestrel 5000）
- 如需 IIS HTTPS，请在 `setup-iis.ps1` 里传入 `-CertThumbprint`
- Kestrel 5001 需要额外证书配置后再启用实际 HTTPS 监听

## 验证

```powershell
# 检查服务状态
Get-Service LYBT-WebAPI-Kestrel

# 本地测试 WebAPI
Invoke-WebRequest http://127.0.0.1:5000/health
Invoke-WebRequest http://localhost/health

# 从外部设备访问（替换为服务器 IP）
# http://<服务器IP>:5000/health
```

## 网络访问说明

| 组件 | 端口 | 绑定地址 | 外部访问 | 说明 |
|------|------|----------|----------|------|
| IIS | 80 | 0.0.0.0 | ✅ 是 | 反向代理入口 |
| Kestrel | 5000 | 0.0.0.0 | ✅ 是 | WebAPI 服务 |
| Kestrel | 5001 | 0.0.0.0 | ⚠️ 需证书 | HTTPS（需额外配置） |
| SQL Server | - | 本地 | ❌ 否 | 仅本地进程访问 |

**注意：** SQL Server 不对外提供服务，仅供本地 WebAPI 进程通过 Shared Memory/Named Pipes 访问。

## SQL Server 版本说明

脚本支持两种 SQL Server 版本：

### Standard 版本（默认）
适用于生产环境，需要自行提供安装介质：

```powershell
# 使用 ISO 镜像
powershell -ExecutionPolicy Bypass -File .\deploy\iis\deploy.ps1 `
  -SqlEdition "Standard" `
  -SqlInstallerPath "D:\en-us_sql_server_2022_standard_edition.iso" `
  -JwtSecret "your-jwt-secret"

# 使用本地安装程序
powershell -ExecutionPolicy Bypass -File .\deploy\iis\deploy.ps1 `
  -SqlEdition "Standard" `
  -SqlInstallerPath "D:\SQL2022\setup.exe" `
  -JwtSecret "your-jwt-secret"
```

### Express 版本
适用于测试环境，自动下载安装：

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\iis\deploy.ps1 `
  -SqlEdition "Express" `
  -SqlInstanceName "SQLEXPRESS" `
  -JwtSecret "your-jwt-secret"
```

| 参数 | Standard | Express |
|------|----------|---------|
| `-SqlEdition` | `"Standard"` | `"Express"` |
| `-SqlInstallerPath` | **必需**（ISO 或 setup.exe） | 可选（自动下载） |
| `-SqlInstanceName` | 可选（默认 `MSSQLSERVER`） | 可选（默认 `SQLEXPRESS`） |

## 回滚/重部署建议

1. 停止服务：`Stop-Service LYBT-WebAPI-Kestrel`
2. 重新执行 `deploy.ps1`
3. 若需清理 IIS 站点，可在 IIS 管理器删除对应 Site 与 AppPool

## 日志

- 主部署日志：`deploy\iis\logs\deploy-*.log`
- 应用日志：`C:\Services\LYBT-WebAPI\app\logs\`
