# LYBT WebAPI 配置体系重构迁移指南

## 背景

本次重构解决了配置体系混乱导致的优先级覆盖问题：
- **问题**：`.env.development` 和 `appsettings.Development.json` 同时定义连接字符串
- **影响**：DotNetEnv 加载的环境变量优先级更高，导致 `appsettings.Development.json` 的配置被覆盖
- **风险**：敏感信息（JWT密钥、密码）被提交到 Git

## 新的配置架构

### 职责分离

| 配置文件 | 用途 | 提交到 Git |
|---------|------|-----------|
| `.env.development` | 敏感配置：连接字符串、JWT密钥、密码 | ❌ 否 |
| `appsettings.Development.json` | 行为配置：日志级别、功能开关、业务参数 | ✅ 是 |
| `appsettings.json` | 生产环境默认值 | ✅ 是 |
| `.env.example` | 环境变量模板（供开发者复制） | ✅ 是 |

### 配置加载优先级（ASP.NET Core）

```
高 ←─────────────────────────────────────────────────────→ 低
│                                                         │
├─ 环境变量（.env.development 注入） ← 优先级最高           │
├─ appsettings.Development.json                          │
├─ appsettings.json ← 优先级最低                         │
```

### 为什么要这样设计？

1. **安全性**：敏感信息不提交到版本控制
2. **团队协作**：行为配置共享，敏感配置独立
3. **灵活性**：不同环境（Dev/Test/Prod）使用独立的数据库和密钥
4. **可追溯**：模板文件 `.env.example` 说明需要哪些配置

---

## 迁移步骤

### 如果你是新开发者

1. 复制模板文件：
   ```bash
   cd src/Server/Services/LYBT.WebAPI
   cp .env.example .env.development
   ```

2. 编辑 `.env.development`，填入实际值：
   - 连接字符串（改为你的本地数据库）
   - JWT SecretKey（生成随机字符串）
   - 系统管理员密码

3. 启动应用：
   ```bash
   dotnet run
   ```

### 如果你是现有开发者（已有本地配置）

1. **备份当前配置**（重要）：
   ```bash
   cp .env.development .env.development.backup
   ```

2. **更新 `.env.development`** 添加之前可能在 `appsettings.Development.json` 中的配置：

   | 配置项 | 原位置 | 新位置 |
   |-------|--------|--------|
   | `ConnectionStrings__DefaultConnection` | `appsettings.Development.json` | `.env.development` |
   | `Lybt__Jwt__SecretKey` | `appsettings.Development.json` | `.env.development` |
   | `Lybt__DefaultPasswords__SysAdminPassword` | `appsettings.Development.json` | `.env.development` |
   | `Lybt__DefaultPasswords__NewUserPassword` | `appsettings.Development.json` | `.env.development` |

3. **验证配置**（确保连接字符串指向开发数据库 `LYBTDB_Dev`）：
   ```bash
   # 检查环境变量是否正确加载
   dotnet run --urls "http://localhost:5000"
   ```

4. **清理旧配置**（可选）：
   - 删除 `appsettings.Development.json` 中的敏感值（现在只保留行为配置）

---

## 验证配置是否正确加载

### 方法 1：查看启动日志

启动应用时，观察控制台输出：

```
[INF] 已加载环境变量文件: .env.development
[INF] 使用数据库: LYBTDB_Dev  ✓ 正确
[ERR] 使用数据库: LYBTDB      ✗ 错误（检查 .env.development）
```

### 方法 2：通过 API 检查环境

```bash
curl http://localhost:5000/api/system/environment
```

### 方法 3：调试检查

在代码中添加临时检查：

```csharp
var connectionString = Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"连接字符串: {connectionString}");
```

---

## 重要注意事项

### 1. 不要提交 `.env.development`

`.gitignore` 已更新为忽略 `.env.development`，请确保不要强制添加：

```bash
# ❌ 不要这样做
git add -f .env.development

# ✅ 正确的敏感信息管理方式
# 使用环境变量、密钥管理服务或 CI/CD secrets
```

### 2. 环境特定的数据库

| 环境 | 推荐数据库 | 用途 |
|-----|-----------|------|
| Development | `LYBTDB_Dev` | 本地开发，可随意修改 |
| Test | `LYBTDB_Test` | 自动化测试 |
| Production | `LYBTDB` | 生产数据 |

### 3. 生成 JWT SecretKey

PowerShell:
```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 } | ForEach-Object { [byte]$_ }))
```

### 4. 生产环境配置

生产环境应使用：
- 环境变量（服务器配置）
- Azure Key Vault / AWS Secrets Manager
- Kubernetes Secrets

**不要**在生产服务器上使用 `.env` 文件。

---

## 故障排查

### 问题：连接字符串仍指向生产数据库

**症状**：启动时显示使用 `LYBTDB` 而非 `LYBTDB_Dev`

**排查步骤**：
1. 检查 `.env.development` 是否存在且包含正确的连接字符串
2. 检查 `ASPNETCORE_ENVIRONMENT` 是否设置为 `Development`
3. 检查系统环境变量是否覆盖了本地配置

**解决方案**：
```bash
# 检查当前环境变量
Get-ChildItem Env: | Where-Object { $_.Name -like "*Connection*" }

# 临时清除环境变量（PowerShell）
Remove-Item Env:ConnectionStrings__DefaultConnection
```

### 问题：`.env.development` 未加载

**症状**：控制台没有显示 "已加载环境变量文件"

**排查步骤**：
1. 确认文件位于 `src/Server/Services/LYBT.WebAPI/.env.development`
2. 确认文件编码为 UTF-8（不带 BOM）
3. 检查文件权限

---

## 相关文件

| 文件 | 说明 |
|-----|------|
| `.env.example` | 环境变量模板，供复制使用 |
| `appsettings.Development.json` | 开发环境行为配置 |
| `appsettings.json` | 默认配置 |
| `.gitignore` | 忽略 `.env.development` |

---

## 变更记录

| 日期 | 变更 | 作者 |
|-----|------|------|
| 2026-01-15 | 重构配置体系，分离敏感配置与行为配置 | Sisyphus |
| 2026-01-15 | 更新 `.gitignore` 阻止 `.env.development` 提交 | Sisyphus |
| 2026-01-15 | 清理 `appsettings.Development.json` 中的敏感信息 | Sisyphus |
