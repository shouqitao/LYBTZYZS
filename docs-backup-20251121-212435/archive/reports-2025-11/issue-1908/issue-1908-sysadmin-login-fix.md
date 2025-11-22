# Issue #1908: SysAdmin 登录问题诊断与修复

## 问题概述

**问题描述**: 医生账户可以正常登录，但 sysadmin 无法登录

**影响范围**: 开发环境下的超级管理员登录功能

**问题严重性**: 🔴 高（阻止系统管理员访问）

---

## 诊断过程

### 1. 初步诊断工具

创建了三个诊断工具来定位问题：

#### 工具1: `scripts/BcryptGenerator/Program.cs`
- **功能**: 验证BCrypt哈希算法是否正常工作
- **测试结果**: ✅ BCrypt算法工作正常，能够正确验证密码

```bash
旧哈希验证: ✓ 成功 - 旧哈希可以验证明文密码
新哈希验证: ✓ 成功 - 新哈希可以验证明文密码
```

#### 工具2: `scripts/DiagnoseAdminHash/Program.cs`
- **功能**: 查询数据库AdminSecrets表，对比实际哈希与配置文件期望哈希
- **测试结果**: ⚠️ 发现数据库哈希与配置文件不一致，但两者都能验证密码

```
数据库哈希: $2a$11$afPwqPi6lpQr22fqoaRol.u9ktXMg.nVftjMBfGvpot.gs2NAlaT2
配置哈希:   $2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C

哈希对比: ✗ 不一致
实际哈希验证期望密码: ✓ 成功
期望哈希验证期望密码: ✓ 成功
```

**初步结论**: 哈希不一致不是问题根源，因为两个哈希都能正确验证密码

#### 工具3: SQL查询脚本 `scripts/CheckAdminHash.sql`
- **功能**: 快速查询AdminSecrets表内容
- **测试结果**: 确认数据库记录存在

---

### 2. 深度代码审查

#### 2.1 认证流程分析

跟踪认证流程代码路径：

```
LoginAsync (AuthService.cs:252)
  ↓
VerifyCredentialsAsync (AuthService.cs:114)
  ↓
IsSuperAdminCredentials (AuthService.cs:58)
  ↓
读取配置: _configuration["Lybt:SystemAdmin:UserName"]  ⬅️ 关键点
  ↓
对比用户名（大小写不敏感）
  ↓
BCrypt.Verify(password, adminSecret.PasswordHash)
```

#### 2.2 配置读取逻辑（AuthService.cs:65）

```csharp
// 从配置获取超级管理员用户名
var configUserName = _configuration["Lybt:SystemAdmin:UserName"];
if (string.IsNullOrEmpty(configUserName))
{
    _logger.LogWarning("配置中未找到超级管理员用户名");
    return false;  // ⬅️ 如果配置为空，直接返回false
}

// 验证用户名是否匹配
if (!string.Equals(username, configUserName, StringComparison.OrdinalIgnoreCase))
{
    return false;  // ⬅️ 用户名不匹配，返回false
}
```

**关键发现**: 如果配置中读取不到 `UserName`，直接返回认证失败

---

### 3. 配置文件审查

检查两个配置文件：

#### appsettings.json (主配置)
```json
"SystemAdmin": {
  "UserName": "sysadmin",  // ✅ 键名正确
  "Email": "admin@lybt.com",
  "DisplayName": "系统管理员",
  ...
}
```

#### appsettings.Development.json (开发环境覆盖配置)
```json
"SystemAdmin": {
    "Username": "sysadmin",  // ❌ 键名错误（小写u）
    "Email": "admin@lybt.com",
    "DefaultPassword": "LybtAdmin2025@SecurePass!",
    ...
}
```

**问题根源**:
1. 代码读取的配置键是 `"UserName"`（大写U）
2. appsettings.Development.json 使用的键名是 `"Username"`（小写u）
3. .NET配置系统的键名是**大小写敏感**的
4. 在开发环境下，appsettings.Development.json 会覆盖 appsettings.json 的配置
5. 因为键名不一致，导致配置读取为空，认证流程在用户名验证阶段就失败了

---

## 问题根因

**根本原因**: appsettings.Development.json 配置键名拼写错误

- **预期**: `"UserName": "sysadmin"`
- **实际**: `"Username": "sysadmin"`
- **影响**: 配置读取失败 → 用户名验证失败 → 认证失败 → 登录失败

---

## 修复方案

### 修复内容

修改 `src/Server/Services/LYBT.WebAPI/appsettings.Development.json`

**修复前** (第85行):
```json
"SystemAdmin": {
    "Username": "sysadmin",  // ❌ 错误
    ...
}
```

**修复后**:
```json
"SystemAdmin": {
    "UserName": "sysadmin",  // ✅ 修复
    ...
}
```

### 验证步骤

编译验证：
```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

结果: ✅ 编译成功（0 errors, 1 warning-文件锁定）

---

## 影响分析

### 影响范围
- **环境**: 仅影响开发环境（使用 appsettings.Development.json）
- **功能**: 超级管理员登录功能
- **用户**: 系统管理员账户（sysadmin）

### 为什么医生账户能登录？
医生账户使用普通用户认证流程，走的是 `Users` 表验证，不依赖 `SystemAdmin` 配置，因此不受影响。

### 配置覆盖机制说明

ASP.NET Core 配置加载顺序：
1. appsettings.json（基础配置）
2. appsettings.{Environment}.json（环境特定配置，**覆盖**基础配置）

在开发环境（ASPNETCORE_ENVIRONMENT=Development）：
- 加载 appsettings.json
- **覆盖**加载 appsettings.Development.json

因为 `"Username"` 和 `"UserName"` 是不同的键名，覆盖时：
- `"UserName"` 不会被覆盖（因为没有匹配的键）
- 但整个 `"SystemAdmin"` section 可能被重新创建，导致旧的 `"UserName"` 丢失

---

## 预防措施

### 1. 配置键名规范
- 使用 **PascalCase** 命名配置键（与C#属性命名一致）
- 保持所有配置文件中的键名一致

### 2. 强类型配置建议

**当前方式** (字符串键名):
```csharp
var configUserName = _configuration["Lybt:SystemAdmin:UserName"];
```

**推荐方式** (强类型配置类):
```csharp
public class SystemAdminOptions
{
    public string UserName { get; set; }  // 编译时检查
    public string Email { get; set; }
    ...
}

// 注册
services.Configure<SystemAdminOptions>(
    configuration.GetSection("Lybt:SystemAdmin"));

// 使用
private readonly SystemAdminOptions _systemAdminOptions;
public AuthService(IOptions<SystemAdminOptions> options)
{
    _systemAdminOptions = options.Value;
}
```

**优势**:
- 编译时类型检查
- IntelliSense支持
- 配置验证
- 重构友好

### 3. 配置验证

在 `Program.cs` 启动时验证关键配置：
```csharp
var sysAdminUserName = builder.Configuration["Lybt:SystemAdmin:UserName"];
if (string.IsNullOrEmpty(sysAdminUserName))
{
    throw new InvalidOperationException(
        "SystemAdmin.UserName 配置缺失！请检查 appsettings.json");
}
```

---

## 测试计划

### 运行时验证清单

- [ ] 启动 WebAPI 服务器
- [ ] 启动 Desktop 客户端
- [ ] 使用 sysadmin 账户登录
  - 用户名: `sysadmin`
  - 密码: `LybtAdmin2025@SecurePass!`
- [ ] 验证登录成功
- [ ] 验证医生账户登录仍然正常
- [ ] 检查服务器日志，确认认证成功日志

### 日志验证

**预期日志** (AuthService.cs):
```
[Information] 超级管理员认证成功 [用户名: sysadmin] [时间: ...]
[Information] 超级管理员登录成功（用户名已隐藏）
```

**失败日志** (修复前):
```
[Warning] 配置中未找到超级管理员用户名
[Warning] 超级管理员认证失败：密码错误
```

---

## 相关文件

### 修改文件
- `src/Server/Services/LYBT.WebAPI/appsettings.Development.json` - 修复配置键名

### 诊断工具
- `scripts/BcryptGenerator/Program.cs` - BCrypt哈希验证工具
- `scripts/DiagnoseAdminHash/Program.cs` - 数据库哈希诊断工具
- `scripts/CheckAdminHash.sql` - SQL快速查询脚本

### 相关代码
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
  - `IsSuperAdminCredentials` (58-104行) - 超级管理员认证
  - `VerifyCredentialsAsync` (114-152行) - 凭证验证
  - `LoginAsync` (252-398行) - 登录流程
- `src/Server/Core/LYBT.Infrastructure/Data/Configurations/AdminSecretConfiguration.cs` - 种子数据配置

---

## 总结

1. **问题**: 配置键名拼写错误（`"Username"` vs `"UserName"`）
2. **原因**: .NET配置系统键名大小写敏感
3. **影响**: 开发环境下 sysadmin 无法登录
4. **修复**: 统一配置键名为 `"UserName"`
5. **预防**: 使用强类型配置类 + 启动时配置验证

---

**诊断完成时间**: 2025-11-08
**修复完成时间**: 2025-11-08
**Issue**: #1908
