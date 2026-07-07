# 测试环境部署文档

> 更新时间: 2026-07-07 | 状态: 测试阶段

## [S1] 服务器清单

| 服务器 | IP | 用途 | 端口 | 操作系统 |
|--------|-----|------|------|----------|
| WebAPI | 192.168.190.246 | WebAPI 服务 | 5000 | Ubuntu |
| WebAPI (公网) | 60.190.215.86 | WebAPI 公网访问 | 5000 | - |
| 数据库 | 192.168.190.243 | SQL Server | 1433 | Windows |

## [S2] 服务账号

### WebAPI 服务器 (192.168.190.246)

| 项目 | 值 |
|------|-----|
| SSH 用户 | player |
| SSH 密码 | 123456 |
| SSH 免密登录 | 已配置 |
| WebAPI 安装路径 | ~/lybt-api |
| 日志路径 | ~/lybt-api/logs/webapi.log |
| 启动方式 | `dotnet LYBT.WebAPI.dll` (nohup) |

### 数据库服务器 (192.168.190.243)

| 项目 | 值 |
|------|-----|
| 数据库类型 | SQL Server |
| 数据库名 | LYBTDB_Dev |
| SA 用户名 | sa |
| SA 密码 | Shou@850528 |

## [S3] 应用配置 (appsettings.Production.json)

### 数据库连接

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=192.168.190.243;Database=LYBTDB_Dev;User ID=sa;Password=Shou@850528;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}
```

### JWT 配置

```json
"Jwt": {
  "SecretKey": "LyBTZYZS2026!SecretKeyForJWT@Production",
  "AccessTokenExpirationMinutes": 30
}
```

### 默认密码

```json
"DefaultPasswords": {
  "SysAdminPassword": "SysAdmin@2026!",
  "NewUserPassword": "User@2026!Qwx",
  "ForceChangeOnFirstLogin": true
}
```

> ⚠️ 密码复杂度要求：8+ 位、含大小写、数字、特殊字符、无连续序列（如 123、abc）

### API 路由

- 远程 WebAPI: `http://192.168.190.246:5000/api/v1/[controller]`
- 公网访问: `http://60.190.215.86:5000/api/v1/[controller]`

## [S4] 测试账号

| 用户名 | 密码 | 角色 | 用途 |
|--------|------|------|------|
| sysadmin | SysAdmin@2026! | SuperAdmin | 系统运维测试 |
| admin | Admin@123456 | Admin | 管理员测试 |
| doctor1 | Doctor@123 | Doctor | 医生测试 (需创建) |
| reception1 | Recep@123 | Receptionist | 前台测试 (需创建) |

## [S5] 部署步骤

### 5.1 本地构建

```powershell
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj -c Release -o "$env:TEMP\lybt-publish" --self-contained false
```

### 5.2 打包

```powershell
Compress-Archive -Path "$env:TEMP\lybt-publish\*" -DestinationPath "$env:TEMP\lybt-webapi-update.zip" -Force
```

### 5.3 上传到服务器

```powershell
scp "$env:TEMP\lybt-webapi-update.zip" player@192.168.190.246:/tmp/lybt-webapi-update.zip
```

### 5.4 部署到服务器

```bash
ssh player@192.168.190.246 "
  pkill -f 'dotnet LYBT.WebAPI.dll' 2>/dev/null;
  sleep 2;
  cd ~/lybt-api && unzip -o /tmp/lybt-webapi-update.zip -d . > /dev/null 2>&1;
  nohup dotnet LYBT.WebAPI.dll > logs/webapi.log 2>&1 &
  sleep 5;
  ps aux | grep 'dotnet LYBT.WebAPI' | grep -v grep
"
```

### 5.5 验证部署

```powershell
Invoke-RestMethod -Uri "http://60.190.215.86:5000/api/v1/auth/login" -Method POST -ContentType "application/json" -Body '{"username":"sysadmin","password":"SysAdmin@2026!"}'
```

## [S6] Newman 测试执行

### 6.1 测试集合位置

```
tests/newman/lybt-full-api-collection.json
tests/newman/env-full-test.json
```

### 6.2 执行命令

```powershell
npx newman run tests/newman/lybt-full-api-collection.json -e tests/newman/env-full-test.json --reporters cli --timeout-request 15000 --delay-request 100
```

### 6.3 按角色执行

```powershell
# 仅 SysAdmin
npx newman run tests/newman/lybt-full-api-collection.json -e tests/newman/env-full-test.json --folder "Phase 1: SysAdmin"

# 仅 Admin
npx newman run tests/newman/lybt-full-api-collection.json -e tests/newman/env-full-test.json --folder "Phase 2: Admin"

# 仅 Receptionist
npx newman run tests/newman/lybt-full-api-collection.json -e tests/newman/env-full-test.json --folder "Phase 3: Receptionist"

# 仅 Doctor
npx newman run tests/newman/lybt-full-api-collection.json -e tests/newman/env-full-test.json --folder "Phase 4: Doctor"
```

## [S7] 已知问题

### 7.1 IdentitySeedData Role 属性未设置

**问题描述**: `IdentitySeedData` 创建用户时未设置 `Role` 属性，默认值为 `UserRole.Doctor`

**影响**: sysadmin 登录后返回的 role 是 "Doctor" 而非 "SuperAdmin"

**修复**: 已在 `src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs` 中添加 Role 设置

**状态**: 修复已提交，待部署到 246

### 7.2 密码策略验证

**问题描述**: Production 环境密码需满足严格复杂度要求

**要求**:
- 最少 8 位
- 包含大写字母
- 包含小写字母
- 包含数字
- 包含特殊字符
- 无连续数字序列（如 123、456）
- 无连续字母序列（如 abc、xyz）

**解决方案**: 使用 `SysAdmin@2026!` 和 `User@2026!Qwx`

### 7.3 环境变量未设置

**问题描述**: appsettings.Production.json 使用 `${VAR}` 格式的环境变量，但服务器未设置

**解决方案**: 直接修改 appsettings.Production.json，将环境变量替换为实际值

## [S8] 测试结果记录

| 测试日期 | 测试轮次 | 通过数 | 失败数 | 备注 |
|----------|----------|--------|--------|------|
| 2026-07-07 | v1 | - | - | 待执行 |

---

## 变更日志

| 日期 | 变更内容 |
|------|----------|
| 2026-07-07 | 初始版本，记录测试环境配置 |
