# 服务器配置快速参考

> 创建时间: 2026-07-13

---

## 生产服务器 (60.190.215.86)

### 连接信息

| 项目 | 值 |
|------|-----|
| IP | 60.190.215.86 |
| SSH 端口 | 5555 |
| 用户名 | player |
| 认证 | SSH Key（免密） |
| 系统 | Linux Ubuntu |

### 连接命令

```bash
ssh -p 5555 player@60.190.215.86
scp -P 5555 <file> player@60.190.215.86:<path>
```

### 应用配置

| 项目 | 值 |
|------|-----|
| 部署路径 | /home/player/lybt-api |
| dotnet 路径 | /home/player/.dotnet/dotnet |
| API 端口 | 5000 |
| 环境 | Production |
| 日志路径 | /home/player/lybt-api/logs/ |

### 数据库配置

| 项目 | 值 |
|------|-----|
| 服务器 | 192.168.190.243 |
| 数据库 | LYBTDB_Dev |
| 用户 | sa |
| 加密 | Encrypt=True;TrustServerCertificate=True |

### JWT 配置

| 项目 | 值 |
|------|-----|
| SecretKey | jin39uYqW840gYkGyxlHozWYwyTO/hjpM2ylVbbIniU= |
| Issuer | LYBT.WebAPI |
| Audience | LYBT.Client |
| Token 有效期 | 30 分钟 |

### 密码配置

| 用户 | 密码 |
|------|------|
| SysAdmin | SysAdmin@2026! |
| Admin | Admin@123456 |
| NewUser | User@2026!Qwx |

---

## 同步脚本

### 位置
`D:\source\repos\LYBTZYZS\sync-to-server.ps1`

### 用法

```powershell
# 完整部署: 构建 + 同步 + 重启
.\sync-to-server.ps1 -Build -Restart

# 仅更新配置文件
.\sync-to-server.ps1 -ConfigOnly -Restart

# 仅重启服务
.\sync-to-server.ps1 -Restart

# 仅同步不重启
.\sync-to-server.ps1 -Build
```

---

## 故障排查

### 服务无法启动

1. **检查端口占用**: `ss -tlnp | grep 5000`
2. **检查进程**: `ps aux | grep dotnet`
3. **查看日志**: `tail -50 /home/player/lybt-api/logs/lybt-web-api-*.log`
4. **杀掉旧进程**: `pkill -9 -f 'dotnet.*LYBT'`

### 常见错误

| 错误 | 原因 | 解决 |
|------|------|------|
| HTTPS endpoint 配置失败 | appsettings.json 含 HTTPS 配置 | 移除 Kestrel.Endpoints.Https |
| 密码不符合安全策略 | Production 使用环境变量占位符 | 写入实际密码 |
| Address already in use | 端口被占用 | 杀掉旧进程 |
| dotnet: command not found | PATH 未设置 | 使用完整路径 /home/player/.dotnet/dotnet |

---

## 变更记录

| 日期 | 变更内容 |
|------|----------|
| 2026-07-13 | 创建文档，记录生产服务器配置 |
