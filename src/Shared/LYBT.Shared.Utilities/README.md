# LYBT.Shared.Utilities

凌隐宝堂中医诊所系统 - 共享工具类库

## 📋 项目概述

这是凌隐宝堂中医诊所系统的共享工具类库，专注于密码安全管理等核心工具功能，为小型中医诊所系统提供简单可靠的工具支持。

**技术栈**: .NET 8.0 + C# 12 + Microsoft.AspNetCore.Identity + System.Text.Json

## 🏗️ 技术栈

- **.NET 8.0** - 目标框架
- **C# 12** - 编程语言 (现代语法特性)
- **Microsoft.AspNetCore.Identity** - 密码哈希与验证
- **System.Text.Json** - JSON 序列化支持
- **System.ComponentModel.Annotations** - 数据注解支持

## 📦 项目结构

```
LYBT.Shared.Utilities/
├── LYBT.Shared.Utilities.csproj    # 项目配置
├── README.md                       # 项目说明
└── Helpers/
    └── PasswordHelper.cs           # 密码安全工具类
```

## 🔐 PasswordHelper 密码安全工具

### 核心功能

PasswordHelper 提供密码哈希、验证、生成和强度检查等安全功能，专为小型中医诊所系统设计。

### 主要方法 (8个核心方法)

#### 1. 密码哈希与验证
```csharp
// 生成密码哈希 (使用 AspNetCore Identity)
string hash = PasswordHelper.Hash("myPassword");

// 验证密码
bool isValid = PasswordHelper.Verify(hash, "myPassword");

// 安全字符串比较 (防时间攻击)
bool areEqual = PasswordHelper.SecureEquals(pwd1, pwd2);
```

#### 2. 密码验证与强度检查
```csharp
// 综合密码验证
var result = PasswordHelper.ValidatePassword("myPassword", 
    minLength: 8, requireUppercase: true, requireLowercase: true, 
    requireDigits: true, requireSpecialChars: true);

// 检查密码强度
PasswordStrength strength = PasswordHelper.CheckPasswordStrength("myPassword");

// 检查是否为常见弱密码
bool isWeak = PasswordHelper.IsCommonPassword("123456");

// 检查最小长度要求
bool hasMinLength = PasswordHelper.HasMinimumLength("myPassword", 8);
```

#### 3. 安全密码生成
```csharp
// 生成安全密码 (默认12位)
string securePassword = PasswordHelper.GenerateSecurePassword();

// 自定义长度和字符集
string customPassword = PasswordHelper.GenerateSecurePassword(
    length: 16, 
    includeUppercase: true, 
    includeLowercase: true, 
    includeDigits: true, 
    includeSpecialChars: true
);
```

### 密码强度级别

```csharp
public enum PasswordStrength
{
    Weak,        // 弱密码
    Fair,        // 一般
    Good,        // 良好
    Strong,      // 强密码
    VeryStrong   // 非常强
}
```

### 验证结果类型

```csharp
public class PasswordValidationResult
{
    public bool IsValid { get; set; }           // 验证是否通过
    public PasswordStrength Strength { get; set; }  // 密码强度
    public List<string> Errors { get; set; }    // 错误信息列表
    public string Suggestions { get; set; }     // 改进建议
}
```

## 🛡️ 安全特性

1. **ASP.NET Core Identity 集成** - 使用标准的密码哈希算法 (PBKDF2)
2. **时间攻击防护** - SecureEquals 方法防止时间攻击
3. **弱密码检测** - 内置23个常见弱密码黑名单
4. **加密安全随机** - 使用 System.Security.Cryptography 生成安全密码
5. **现代 C# 语法** - 使用生成正则表达式等 C# 12 特性优化性能

## 🚀 性能优化

- **生成正则表达式** - 使用 `[GeneratedRegex]` 提升匹配性能
- **StringBuilder 优化** - 高效字符串构建
- **模式匹配** - 现代 C# 语法提升代码性能

## 📝 使用示例

### 用户注册场景
```csharp
public async Task<bool> RegisterUser(string username, string password)
{
    // 1. 验证密码强度
    var validation = PasswordHelper.ValidatePassword(password, 
        minLength: 8, requireUppercase: true, requireLowercase: true, 
        requireDigits: true, requireSpecialChars: true);
    
    if (!validation.IsValid)
    {
        throw new ArgumentException($"密码不符合要求: {string.Join(", ", validation.Errors)}");
    }
    
    // 2. 生成密码哈希
    string passwordHash = PasswordHelper.Hash(password);
    
    // 3. 保存用户信息
    var user = new User 
    { 
        Username = username, 
        PasswordHash = passwordHash 
    };
    
    return await SaveUserAsync(user);
}
```

### 用户登录场景
```csharp
public async Task<bool> AuthenticateUser(string username, string password)
{
    // 1. 获取用户信息
    var user = await GetUserByUsernameAsync(username);
    if (user == null) return false;
    
    // 2. 验证密码
    return PasswordHelper.Verify(user.PasswordHash, password);
}
```

### 密码重置场景
```csharp
public string GenerateTemporaryPassword()
{
    // 生成临时密码 (16位，包含所有字符类型)
    return PasswordHelper.GenerateSecurePassword(
        length: 16, 
        includeUppercase: true, 
        includeLowercase: true, 
        includeDigits: true, 
        includeSpecialChars: true
    );
}
```

## 🎯 适用场景

此工具库专为**小型中医诊所系统**设计，适合：

- 👨‍⚕️ 2-5名医生的小型诊所
- 👥 少于20人的用户规模
- 🔐 基础但完整的密码安全需求
- 🚀 追求简单高效的系统架构

## 🔧 依赖安装

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity" />
<PackageReference Include="System.Text.Json" />
<PackageReference Include="System.ComponentModel.Annotations" />
```

## 📊 项目统计

- **Helper 类数量**: 1个
- **总方法数**: 8个核心方法
- **代码行数**: 约200行 (精简后)
- **支持的 .NET 版本**: .NET 8.0
- **最后更新**: 2025-01-31

## 📈 版本历史

- **v1.0.0** (2025-01-31) - 初始发布，专注密码安全核心功能

---

> 💡 **设计理念**: 专注核心功能，避免过度工程化，为小型诊所系统提供简单可靠的工具支持。