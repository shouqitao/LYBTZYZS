# LYBT.Shared.Utilities 代码知识

共享工具类库，提供密码安全处理、拼音码生成等基础设施工具。

## 代码文件结构

```
Extensions/
└── ServiceCollection/
    └── CacheExtensions.cs              # IMemoryCache 扩展 (RemoveByPrefix/Clear)
Security/
├── PasswordHelper.cs                   # 统一密码处理 (BCrypt 哈希/验证/强度检查)
└── PasswordPolicyValidator.cs          # 密码策略验证器 (复杂度/强度评分)
Text/
└── PinYinHelper.cs                     # 拼音码生成 (hyjiacan.pinyin4net)
```

### Security/PasswordHelper.cs
**PasswordHelper** (static class) | 统一密码处理 (BCrypt 算法)

| 方法 | 说明 |
|------|------|
| HashPassword(string, UserRole, ILogger?) | BCrypt 哈希密码 |
| VerifyPassword(string, string, UserRole, ILogger?) | 验证密码 (含重新哈希检测) |
| VerifyAndRehashIfNeeded(string, string, UserRole, ILogger?) | 验证并自动重新哈希 |
| GenerateTemporaryPassword() | 生成 8 位临时密码 (1大写+4小写+3数字) |
| GenerateSalt(int) | 生成安全随机盐值 |
| UpdateWorkFactor(int) | 更新 BCrypt 工作因子 (10-15) |
| GetConfiguration() | 获取当前配置信息 |
| ValidatePassword(string, int, bool, bool, bool, bool) | 验证密码强度和合规性 |
| CheckPasswordStrength(string) | 检查密码强度等级 |
| IsCommonPassword(string) | 检查是否为常见弱密码 |
| GenerateSecurePassword(int, bool, bool, bool, bool) | 生成安全随机密码 |
| SecureEquals(string?, string?) | 防时间攻击的字符串比较 |

嵌套类型: PasswordVerificationResult / PasswordHelperConfiguration / PasswordValidationResult

### Security/PasswordPolicyValidator.cs
**PasswordPolicyValidator** (static partial class) | 企业级密码策略验证器 (GeneratedRegex)

| 方法 | 说明 |
|------|------|
| Validate(string, out List\<string\>) | 验证密码复杂度 (长度/大小写/数字/特殊字符/连续字符/弱密码) |
| CalculateStrength(string) | 计算密码强度评分 (0-100) |
| GetStrengthLevel(string) | 获取密码强度等级 (PasswordStrength 枚举) |
| GenerateSecurePassword(int) | 生成符合策略的随机密码 |

**Policy** (nested static class) | 密码策略配置常量: MinLength(8)/MaxLength(128)/RequireUppercase/RequireDigit 等

### Text/PinYinHelper.cs
**PinYinHelper** (static class) | 拼音码生成工具 (基于 hyjiacan.pinyin4net v4.1.1)

| 方法 | 说明 |
|------|------|
| GetPinYinCode(string?) | 生成拼音首字母码 (如 "张韶涵" -> "ZSH")，含降级方案 |

## 死代码与废弃标记

| 类型/方法 | 状态 | 说明 |
|-----------|------|------|
| ConfigurationHelper | [已清理] | 2026-03-01 删除，0 业务引用 |
| EnvironmentHelper | [已清理] | 2026-03-01 删除，0 业务引用 |
| ApplicationInitializationExtensions | [已清理] | 2026-03-01 删除，0 业务引用 |
| CacheExtensions (GetOrSet/GetOrSetAsync/RemoveByPattern) | [已清理] | 2026-03-01 删除未使用方法，保留 RemoveByPrefix + Clear (被 DesktopCacheManager + CacheInvalidationService 使用) |
| ClaimsHelper | [已清理] | 2026-03-01 删除，0 业务引用 |
| RoleHelper | [已清理] | 2026-03-01 删除，仅被 ClaimsHelper 引用 (同为死代码) |

## 设计分析

| 文件 | 问题 | 建议 |
|------|------|------|
| PasswordHelper + PasswordPolicyValidator | 两个类都含密码强度检查和生成功能 | 统一: PasswordPolicyValidator 专注策略验证，PasswordHelper 专注哈希操作 |
| PasswordHelper.GenerateTemporaryPassword | 使用 new Random() 而非 RandomNumberGenerator | 使用 Random.Shared 或 RandomNumberGenerator |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| PasswordHelper.WorkFactor 是静态可变状态 | UpdateWorkFactor 修改全局静态属性 | 仅在启动时配置，避免运行时调用 |
| PasswordPolicyValidator.GenerateSecurePassword 随机种子 | Guid.GetHashCode 不是加密安全随机数 | 安全敏感场景用 PasswordHelper.GenerateSecurePassword 替代 |
| PinYinHelper 多音字处理 | multiFirstLetter: false 仅取第一个读音 | 搜索支持拼音模糊匹配弥补 |
