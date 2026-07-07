# WebAPI 部署失败根因分析

## [S1] 问题现象

本地 `dotnet run` 正常启动，部署到 246 服务器后频繁崩溃。

## [S2] 根因分析

### 2.1 核心原因：环境差异导致验证路径不同

```
Program.cs 第 156 行: ValidateDefaultPasswordConfiguration()
  ↓
Development 模式: 仅检查存在 + 长度>=8 + 非弱密码 → 通过
Production 模式:  完整复杂度验证(大小写+数字+特殊字符+无连续序列) → 可能失败

Program.cs 第 196 行: if (builder.Environment.IsProduction())
  ↓
Development 模式: 跳过 ProductionConfigurationValidator → 启动成功
Production 模式:  调用 ValidateOrThrow() → 缺失任何 Important 配置都终止
```

### 2.2 部署流程缺陷

```
deploy 流程:
1. dotnet publish → 生成 appsettings*.json（含占位符 ${VAR}）
2. SCP 上传 zip
3. unzip -o → 覆盖服务器上已修好的配置文件
4. 配置回到占位符状态 → 启动失败
```

**关键问题**: `unzip -o` 无差别覆盖所有文件，包括配置文件。

### 2.3 本地为何正常

| 配置项 | 本地 Development | 服务器 Production |
|--------|------------------|-------------------|
| DefaultPasswords:SysAdminPassword | appsettings.Development.json 有值 | 被 unzip 覆盖为 ${SYSADMIN_PASSWORD} |
| DefaultPasswords:NewUserPassword | appsettings.Development.json 有值 | 被 unzip 覆盖为 ${NEWUSER_PASSWORD} |
| SystemAdmin:Email | appsettings.Development.json 有值 | 不存在 → 启动失败 |
| SystemAdmin:DisplayName | 不检查 | Important 级别 → 终止启动 |
| Jwt:SecretKey | DevOnly-NotSecure... (45字符) | 被覆盖为 13 字符占位符 |
| TrustServerCertificate | 默认 True | False → 数据库连接失败 |
| HTTPS 端点 | 开发证书自动信任 | 无证书 → Kestrel 崩溃 |

### 2.4 被覆盖的配置文件

服务器上有 4 个配置文件，加载优先级: Production > Test > Development > Base

```
appsettings.json              ← 被 unzip 覆盖
appsettings.Development.json  ← 被 unzip 覆盖
appsettings.Production.json   ← 被 unzip 覆盖
appsettings.Test.json         ← 被 unzip 覆盖
```

publish 产物中的配置文件全部是**模板值**（占位符），不是实际值。

## [S3] 修复方案

### 3.1 短期：部署脚本排除配置文件

打包时排除 `appsettings*.json`，只部署二进制文件。

### 3.2 中期：配置外置

将敏感配置移到环境变量或独立配置目录，不随代码部署。

### 3.3 长期：CI/CD 流程

- 本地验证增加 `ASPNETCORE_ENVIRONMENT=Production` 测试
- 部署前自动验证配置完整性
- 配置版本管理（与代码分离）

## [S4] 已应用的修复

1. **UsersDbContext.cs**: `HasConversion<string>()` → `HasConversion<int>()`（修复 toggle-status 类型转换）
2. **IdentitySeedData.cs**: 创建用户时显式设置 Role 属性
3. **fix_both_configs.py**: 服务器端配置修复脚本
4. **deploy-fixed.ps1**: 改进的部署脚本（排除配置文件）
