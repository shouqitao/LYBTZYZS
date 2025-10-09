# 超级管理员架构边界设计

## 概述

本文档定义了系统超级管理员(sysadmin)与业务用户之间的架构边界，确保安全隔离和职责分离。

**Issue #1074**: 修复超级管理员架构违规问题

## 核心原则

### 1. 完全隔离原则
- **超级管理员**：系统级管理账户，独立于业务用户体系
- **业务用户**：所有业务相关用户，包括医生、管理员等
- **隔离要求**：两者必须在存储、认证、管理上完全分离

### 2. 存储架构

#### 超级管理员存储
- **表**: `AdminSecrets`
- **字段**:
  - `Id`: 固定ID (00000000-0000-0000-0000-000000000001)
  - `PasswordHash`: BCrypt格式密码哈希
- **用户名**: 从配置文件读取，不存储在数据库中

#### 业务用户存储
- **表**: `Users`
- **管理**: 通过UserRepository和UserService
- **认证**: 标准业务流程

### 3. 认证流程

#### 超级管理员认证
```csharp
// AuthService.IsSuperAdminCredentials()
1. 从配置读取用户名: Lybt:Business:SystemAdmin:Username
2. 验证用户名匹配
3. 从AdminSecrets表获取密码哈希
4. 使用BCrypt验证密码
```

#### 业务用户认证
```csharp
// AuthService.VerifyCredentialsAsync()
1. 通过UserRepository查询用户
2. 使用BCrypt验证密码
3. 返回用户信息
```

## 配置规范

### appsettings.json
```json
{
  "Lybt": {
    "Business": {
      "SystemAdmin": {
        "Username": "sysadmin",
        "Email": "admin@lybt.com",
        "DefaultPassword": "仅用于开发环境初始化"
      }
    }
  }
}
```

### 配置路径统一
- **正确**: `Lybt:Business:SystemAdmin:*`
- **错误**: ~~`Lybt:Users:SysAdmin:*`~~ (已废弃)

## 实现要求

### EF Core配置

#### AdminSecretConfiguration.cs
```csharp
// 正确：使用BCrypt哈希
entity.HasData(new AdminSecretModel
{
    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
    PasswordHash = "$2a$11$..." // BCrypt格式
});
```

#### UserConfiguration.cs
```csharp
// 错误示例 - 绝不能这样做！
// entity.HasData(new User { UserName = "sysadmin", ... });
// sysadmin不能出现在Users表种子数据中
```

## 安全考虑

### 1. 防止SQL注入
- 超级管理员用户名从配置文件读取，不从用户输入获取
- 减少数据库暴露风险

### 2. 密码哈希
- 统一使用BCrypt (cost factor = 11)
- 格式: `$2a$11$...`
- 不使用Identity哈希格式

### 3. 权限隔离
- 超级管理员：系统配置、用户管理、系统维护
- 业务用户：业务操作、数据管理

## 迁移历史

### 2025-10-09 修复架构违规
- **迁移**: FixSysAdminArchitecture_RemoveSysAdminFromUsersTable
- **内容**:
  1. 从Users表删除sysadmin记录
  2. 更新AdminSecrets表使用BCrypt哈希
  3. 统一配置路径

## 检查清单

开发时请确认：
- [ ] sysadmin账户**不在**Users表中
- [ ] AdminSecrets表使用BCrypt哈希
- [ ] 配置路径为`Lybt:Business:SystemAdmin`
- [ ] AuthService正确区分超级管理员和业务用户
- [ ] 新的种子数据不包含sysadmin

## 测试验证

### 超级管理员登录测试
```bash
POST /api/auth/login
{
  "username": "sysadmin",
  "password": "LybtAdmin2025@SecurePass!"
}
```

预期结果：
- 通过AdminSecrets表认证
- 返回JWT token
- 角色为Admin

### 业务用户登录测试
```bash
POST /api/auth/login
{
  "username": "doctor1",
  "password": "Pass123!"
}
```

预期结果：
- 通过Users表认证
- 返回JWT token
- 角色为Doctor

## 维护指南

### 修改超级管理员密码
1. 生成新的BCrypt哈希
2. 更新AdminSecrets表
3. 不要更新Users表

### 添加新的系统管理员
- 考虑扩展AdminSecrets表支持多管理员
- 或使用角色基础的权限系统
- 绝不在Users表中添加系统管理员

## 参考文档
- [认证模块设计](../modules/auth/README.md)
- [用户管理设计](../modules/patients/README.md)
- [开发标准](../../development/standards.md)