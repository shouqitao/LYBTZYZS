# Configuration Governance P1 - Step ① Split Report

生成时间: 2025-09-13 23:59:00  
执行分支: `webapi/config-governance-p1`  

## 📋 配置拆分目标

将开发特有的配置从基础配置中分离，确保环境配置的清晰分工：
- **appsettings.json**: 通用基础配置
- **appsettings.Development.json**: 开发环境专属配置  
- **appsettings.Production.json**: 生产环境配置模板 (环境变量占位符)

## ✅ 执行结果

### 🔄 appsettings.Development.json 重构

**迁入的开发专属配置**:

1. **CORS配置** - 开发环境端口支持
   ```json
   "Cors": {
     "AllowedOrigins": [
       "http://localhost:3000", "http://localhost:4200", 
       "http://localhost:5173", "https://localhost:5001", 
       "http://127.0.0.1:3000"
     ]
   }
   ```

2. **安全配置** - 开发环境宽松设置
   ```json
   "Security": {
     "Https": { "RequireHttps": false },
     "Environment": {
       "HideServerInfo": false,
       "HideDetailedErrors": false, 
       "EnableSensitiveDataLogging": true
     }
   }
   ```

3. **日志配置** - 详细调试日志
   ```json
   "Logging": { "LogLevel": { "Default": "Information" } },
   "Serilog": { 
     "MinimumLevel": { "Default": "Debug" },
     "WriteTo": [{ "path": "logs/lybt-web-api-dev-.log" }]
   }
   ```

4. **数据库配置** - 开发调试选项
   ```json
   "DatabaseOptions": {
     "EnableSensitiveDataLogging": true,
     "EnableDetailedErrors": true,
     "EnableQueryTracing": true
   }
   ```

5. **审计日志** - 开发环境详细审计
   ```json
   "AuthOptions": { "EnableDetailedLoginLogging": true },
   "UserOptions": { "EnableDetailedAuditLogging": true }
   ```

### 📝 appsettings.json 精简

**移除的配置** (已迁移到Development.json):
- 敏感的默认密码 (`DefaultPasswords.SystemAdmin`, `DefaultPasswords.NewUser`)
- JWT Secret密钥 (`JwtOptions.Secret`)
- 详细的UserOptions默认密码 (`UserOptions.DefaultUserPassword`)
- SysAdminOptions默认密码 (`SysAdminOptions.DefaultPassword`)
- 开发专属的日志和调试配置

**保留的通用配置**:
- 数据库连接字符串模板 (开发环境)
- JWT基础配置 (不含Secret)
- Serilog基础配置
- 缓存配置
- 通用选项配置

### 📊 配置分工概览

| 配置文件 | 用途 | 包含配置类型 |
|---------|------|-------------|
| appsettings.json | 通用基础 | 连接字符串、JWT基础配置、缓存、日志基础 |
| appsettings.Development.json | 开发专属 | CORS、调试日志、详细错误、开发安全设置 |
| appsettings.Production.json | 生产模板 | 环境变量占位符、生产安全设置 |
| appsettings.Security.json | 安全策略 | 密码策略、速率限制、安全头 |

## ⚠️ 需要注意的事项

### 敏感配置移除清单

以下敏感配置已从JSON文件移除，需要通过环境变量或UserSecrets提供：

1. **DefaultPasswords.SystemAdmin**: `LybtAdmin2025@SecurePass!` → 环境变量
2. **DefaultPasswords.NewUser**: `LybtUser2025#InitPass!` → 环境变量  
3. **JwtOptions.Secret**: `4/HC5fsPxo8xwjDyWCRZ96ZjD...` → 环境变量
4. **UserOptions.DefaultUserPassword**: `LybtUser2025#InitPass!` → 环境变量
5. **SysAdminOptions.DefaultPassword**: `LybtAdmin2025@SecurePass!` → 环境变量

### 配置加载顺序

```
appsettings.json (基础)
    ↓
appsettings.{Environment}.json (环境覆盖)
    ↓  
UserSecrets (开发环境)
    ↓
Environment Variables (最高优先级)
```

## 🎯 下一步行动

Step ②需要完成的任务:
- [ ] 创建UserSecrets设置脚本
- [ ] 设置环境变量映射
- [ ] 验证敏感信息完全外置
- [ ] 更新Production配置确保无真实密钥

## 📋 文件变更统计

**修改文件**: 2个
- `src/Server/Services/LYBT.WebAPI/appsettings.json` - 精简至基础配置
- `src/Server/Services/LYBT.WebAPI/appsettings.Development.json` - 重构为开发专属配置

**敏感信息移除**: 5个关键密钥和密码  
**配置组织**: 开发/通用配置完全分离