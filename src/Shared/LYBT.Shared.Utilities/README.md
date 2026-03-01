# LYBT.Shared.Utilities

> 共享工具类库 | 密码安全/拼音码生成

## 项目定位

- **层级**: Shared层
- **职责**: 提供密码处理、密码策略验证、拼音码生成等核心安全和文本工具

## 目录结构

```
LYBT.Shared.Utilities/
├── Security/                    # 安全工具(2类)
│   ├── PasswordHelper.cs        # BCrypt 密码哈希/验证/强度检查
│   └── PasswordPolicyValidator.cs # 密码策略验证器 (GeneratedRegex)
└── Text/                        # 文本工具(1类)
    └── PinYinHelper.cs          # 拼音首字母码生成
```

## 核心组件

| 工具类 | 方法数 | 说明 |
|--------|--------|------|
| PasswordHelper | 11 | Hash/Verify/RehashIfNeeded/Generate/CheckStrength/SecureEquals |
| PasswordPolicyValidator | 4 | Validate/CalculateStrength/GetStrengthLevel/GenerateSecurePassword |
| PinYinHelper | 1 | GetPinYinCode (中文 -> 拼音首字母) |

## 密码安全

| 特性 | 说明 |
|------|------|
| 哈希算法 | ASP.NET Core Identity PasswordHasher (BCrypt) |
| 策略验证 | 长度/大小写/数字/特殊字符/连续字符/弱密码检测 |
| 强度评分 | 0-100 分 (Weak/Fair/Good/Strong/VeryStrong) |

## 设计依据

- PasswordHelper 集中于 Shared 层，Server 端和 Desktop 端共享统一的密码哈希策略
- PasswordPolicyValidator 使用 GeneratedRegex 提高正则匹配性能
- PinYinHelper 基于 hyjiacan.pinyin4net，支持搜索拼音首字母匹配
- 与 Shared.Components 的区别: Utilities 是通用基础设施工具，Components 是领域业务逻辑

## 依赖关系

### 依赖
- LYBT.Shared.Models (UserRole 枚举)

### 被依赖
- LYBT.WebAPI (密码哈希)
- LYBT.Module.Auth / LYBT.Module.Users (密码处理)
- LYBT.Desktop.Auth (密码验证)

### NuGet包
- Microsoft.AspNetCore.Identity (8.0.x) -- PasswordHasher
- hyjiacan.pinyin4net (4.1.1) -- 拼音转换

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 死代码清理: 移除 ConfigurationHelper/EnvironmentHelper/CacheExtensions/ClaimsHelper/RoleHelper/ApplicationInitializationExtensions (6类0引用) |
| 2025-12-04 | 按README规范重写文档 |
