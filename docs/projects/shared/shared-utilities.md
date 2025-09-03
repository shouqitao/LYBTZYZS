# LYBT.Shared.Utilities 共享工具类项目文档

## 项目概览

**项目名称**: LYBT.Shared.Utilities  
**项目类型**: 共享工具类库  
**技术框架**: .NET 8.0 + ASP.NET Core Identity  
**业务领域**: 通用工具方法和扩展  
**更新时间**: 2025-01-01

## 项目定位

### 核心功能
LYBT.Shared.Utilities提供整个系统使用的通用工具类和扩展方法：

1. **密码安全工具**: 基于ASP.NET Core Identity的密码哈希和验证
2. **通用工具方法**: 数据验证、格式化、类型转换等常用功能
3. **枚举工具类**: 枚举类型的扩展操作和转换
4. **数据脱敏工具**: 隐私信息脱敏处理
5. **文件处理工具**: 文件类型检查、大小格式化等
6. **时间戳工具**: Unix时间戳转换和处理

### 架构角色
- **工具库中心**: 提供系统级通用工具方法
- **安全保障**: 密码安全和数据脱敏处理
- **类型安全**: 安全的类型转换和验证
- **性能优化**: 预编译正则表达式和高效算法

## 技术架构

### 核心依赖
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity" Version="2.3.1" />
<ProjectReference Include="..\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
```

### 项目配置
```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <BaseOutputPath>..\..\BIN</BaseOutputPath>
</PropertyGroup>
```

## 密码安全工具

### PasswordHelper
```csharp
[Description("密码工具类")]
public static class PasswordHelper
{
    private static readonly PasswordHasher<object> _hasher = new();

    /// <summary>对明文密码进行哈希</summary>
    /// <param name="password">明文密码</param>
    /// <returns>哈希后的密码</returns>
    public static string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    /// <summary>验证密码与存储的哈希是否匹配</summary>
    /// <param name="hash">存储的密码哈希</param>
    /// <param name="password">待验证的明文密码</param>
    /// <returns>验证结果</returns>
    public static bool Verify(string hash, string password)
    {
        var result = _hasher.VerifyHashedPassword(null!, hash, password);
        return result == PasswordVerificationResult.Success || 
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
```

#### 使用示例
```csharp
// 密码哈希
string plainPassword = "Admin@123456";
string hashedPassword = PasswordHelper.Hash(plainPassword);

// 密码验证
bool isValid = PasswordHelper.Verify(hashedPassword, plainPassword); // true
```

## 通用工具类

### CommonHelper - 全功能工具类
```csharp
public static partial class CommonHelper
{
    // 预编译正则表达式以提升性能
    [GeneratedRegex(@"\n|\r|\s|\D", RegexOptions.Compiled)]
    private static partial Regex PhoneDigitsRegex();

    [GeneratedRegex(@"^\d{17}[\dXx]$", RegexOptions.Compiled)]
    private static partial Regex IdNumberRegex();

    // 身份证校验权重和校验码（避免重复计算）
    private static readonly int[] IdWeights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
    private static readonly char[] IdCodes = "10X98765432".ToCharArray();
}
```

#### 网络检查
```csharp
/// <summary>检查网络是否可用</summary>
public static bool IsNetworkAvailable() => NetworkInterface.GetIsNetworkAvailable();
```

#### 电话号码处理
```csharp
/// <summary>格式化电话号码（性能优化版本）</summary>
public static string FormatPhone(string? phone)
{
    if (string.IsNullOrWhiteSpace(phone))
        return string.Empty;

    var digits = PhoneDigitsRegex().Replace(phone, string.Empty);

    return digits.Length switch
    {
        11 => $"{digits[..3]}-{digits[3..7]}-{digits[7..]}",
        10 => $"{digits[..3]}-{digits[3..6]}-{digits[6..]}",
        _ => digits
    };
}

/// <summary>脱敏手机号</summary>
public static string MaskPhoneNumber(string? phoneNumber)
{
    if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 7)
        return phoneNumber ?? string.Empty;

    return phoneNumber.Length == 11
        ? $"{phoneNumber[..3]}****{phoneNumber[7..]}"
        : $"{phoneNumber[..3]}****{phoneNumber[^3..]}";
}
```

#### 身份证处理
```csharp
/// <summary>验证身份证号码（性能优化版本）</summary>
public static bool CheckIdNumber(string? idNumber)
{
    if (string.IsNullOrWhiteSpace(idNumber))
        return false;

    idNumber = idNumber.Trim();

    if (!IdNumberRegex().IsMatch(idNumber))
        return false;

    // 计算校验码
    int sum = 0;
    for (int i = 0; i < 17; i++)
    {
        sum += (idNumber[i] - '0') * IdWeights[i];
    }

    char expectedCode = IdCodes[sum % 11];
    return char.ToUpperInvariant(idNumber[17]) == expectedCode;
}

/// <summary>脱敏身份证号</summary>
public static string MaskIdNumber(string? idNumber)
{
    if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length < 8)
        return idNumber ?? string.Empty;

    return idNumber.Length == 18
        ? $"{idNumber[..6]}********{idNumber[14..]}"
        : $"{idNumber[..3]}****{idNumber[^2..]}";
}
```

#### 邮箱验证
```csharp
/// <summary>验证邮箱格式</summary>
public static bool IsValidEmail(string? email)
{
    if (string.IsNullOrWhiteSpace(email))
        return false;

    try
    {
        var mailAddress = new System.Net.Mail.MailAddress(email);
        return mailAddress.Address == email;
    }
    catch
    {
        return false;
    }
}
```

#### 随机字符串生成
```csharp
/// <summary>生成随机字符串</summary>
public static string GenerateRandomString(int length, bool includeNumbers = true)
{
    const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string numbers = "0123456789";

    string chars = letters + letters.ToLower();
    if (includeNumbers)
        chars += numbers;

    var random = new Random();
    return new string(Enumerable.Repeat(chars, length)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}
```

#### 类型安全转换
```csharp
/// <summary>安全地转换为整数</summary>
public static int SafeToInt(string? value, int defaultValue = 0)
{
    return int.TryParse(value, out var result) ? result : defaultValue;
}

/// <summary>安全地转换为小数</summary>
public static decimal SafeToDecimal(string? value, decimal defaultValue = 0)
{
    return decimal.TryParse(value, out var result) ? result : defaultValue;
}

/// <summary>安全地转换为布尔值</summary>
public static bool SafeToBool(string? value, bool defaultValue = false)
{
    return bool.TryParse(value, out var result) ? result : defaultValue;
}
```

#### 唯一标识符生成
```csharp
/// <summary>生成唯一标识符</summary>
public static string GenerateUniqueId()
{
    return Guid.NewGuid().ToString("N");
}

/// <summary>生成短ID（8位）</summary>
public static string GenerateShortId()
{
    return Guid.NewGuid().ToString("N")[..8];
}
```

#### 文件处理工具
```csharp
/// <summary>获取文件扩展名（包含点号）</summary>
public static string GetFileExtension(string fileName)
{
    if (string.IsNullOrWhiteSpace(fileName))
        return string.Empty;

    return Path.GetExtension(fileName).ToLower();
}

/// <summary>获取文件大小的友好显示</summary>
public static string GetFileSizeString(long fileSize)
{
    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
    double len = fileSize;
    int order = 0;
    while (len >= 1024 && order < sizes.Length - 1)
    {
        order++;
        len = len / 1024;
    }
    return $"{len:0.##} {sizes[order]}";
}

/// <summary>检查文件类型是否为图片</summary>
public static bool IsImageFile(string fileName)
{
    if (string.IsNullOrWhiteSpace(fileName))
        return false;

    var extension = GetFileExtension(fileName);
    string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
    return imageExtensions.Contains(extension);
}

/// <summary>检查文件类型是否为文档</summary>
public static bool IsDocumentFile(string fileName)
{
    if (string.IsNullOrWhiteSpace(fileName))
        return false;

    var extension = GetFileExtension(fileName);
    string[] docExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".rtf" };
    return docExtensions.Contains(extension);
}

/// <summary>清理文件名中的非法字符</summary>
public static string SanitizeFileName(string fileName)
{
    if (string.IsNullOrWhiteSpace(fileName))
        return string.Empty;

    var invalidChars = Path.GetInvalidFileNameChars();
    return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
}
```

#### 时间戳工具
```csharp
/// <summary>生成Unix时间戳（秒）</summary>
public static long GetTimestamp()
{
    return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

/// <summary>生成Unix时间戳（毫秒）</summary>
public static long GetTimestampMilliseconds()
{
    return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>从Unix时间戳转换为DateTime</summary>
public static DateTime FromTimestamp(long timestamp)
{
    return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
}

/// <summary>从Unix时间戳（毫秒）转换为DateTime</summary>
public static DateTime FromTimestampMilliseconds(long timestamp)
{
    return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
}
```

#### 拼音码工具
```csharp
/// <summary>根据中文名称生成拼音码（简化实现）</summary>
/// <param name="text">中文文本</param>
/// <returns>拼音首字母缩写</returns>
public static string GetPinyinCode(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
        return string.Empty;

    // 简化实现：返回空字符串，避免编译错误
    // 注：实际项目中可以集成专业的拼音转换库
    return string.Empty;
}
```

## 枚举工具类

### EnumHelper
```csharp
[Description("枚举工具类")]
public static class EnumHelper
{
    /// <summary>获取枚举值的显示名称</summary>
    public static string GetDescription<T>(T enumValue) where T : Enum
    {
        return enumValue.GetDescription();
    }

    /// <summary>获取枚举类型的所有值和描述</summary>
    public static Dictionary<T, string> GetEnumDescriptions<T>() where T : Enum
    {
        var result = new Dictionary<T, string>();

        foreach (T value in Enum.GetValues(typeof(T)))
        {
            result[value] = value.GetDescription();
        }

        return result;
    }

    /// <summary>根据描述获取枚举值</summary>
    public static T GetEnumByDescription<T>(string description) where T : Enum
    {
        foreach (T value in Enum.GetValues(typeof(T)))
        {
            if (value.GetDescription().Equals(description, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return default(T)!;
    }

    /// <summary>枚举值转换为整数</summary>
    public static int ToInt<T>(T enumValue) where T : Enum
    {
        return Convert.ToInt32(enumValue);
    }

    /// <summary>整数转换为枚举值</summary>
    public static T FromInt<T>(int value) where T : Enum
    {
        return (T)Enum.ToObject(typeof(T), value);
    }

    /// <summary>字符串转换为枚举值</summary>
    public static T Parse<T>(string value, bool ignoreCase = true) where T : Enum
    {
        return (T)Enum.Parse(typeof(T), value, ignoreCase);
    }

    /// <summary>尝试将字符串转换为枚举值</summary>
    public static bool TryParse<T>(string value, out T result, bool ignoreCase = true) where T : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase, out result);
    }

    /// <summary>检查枚举值是否已定义</summary>
    public static bool IsDefined<T>(object value) where T : Enum
    {
        return Enum.IsDefined(typeof(T), value);
    }

    /// <summary>获取枚举类型的所有值</summary>
    public static List<T> GetValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }

    /// <summary>获取枚举类型的所有名称</summary>
    public static List<string> GetNames<T>() where T : Enum
    {
        return Enum.GetNames(typeof(T)).ToList();
    }

    /// <summary>获取枚举的键值对列表（用于下拉框等）</summary>
    public static List<KeyValuePair<T, string>> GetKeyValuePairs<T>() where T : Enum
    {
        var result = new List<KeyValuePair<T, string>>();

        foreach (T value in Enum.GetValues(typeof(T)))
        {
            result.Add(new KeyValuePair<T, string>(value, value.GetDescription()));
        }

        return result;
    }

    /// <summary>获取枚举的整数值和描述的键值对列表</summary>
    public static List<KeyValuePair<int, string>> GetIntKeyValuePairs<T>() where T : Enum
    {
        var result = new List<KeyValuePair<int, string>>();

        foreach (T value in Enum.GetValues(typeof(T)))
        {
            result.Add(new KeyValuePair<int, string>(ToInt(value), value.GetDescription()));
        }

        return result;
    }
}
```

#### 枚举工具使用示例
```csharp
// 假设有枚举类型
public enum UserRole
{
    [Description("管理员")]
    Admin = 1,
    
    [Description("医生")]
    Doctor = 2
}

// 使用示例
var description = EnumHelper.GetDescription(UserRole.Admin); // "管理员"
var allDescriptions = EnumHelper.GetEnumDescriptions<UserRole>(); // Dictionary<UserRole, string>
var roleByDesc = EnumHelper.GetEnumByDescription<UserRole>("医生"); // UserRole.Doctor
var intValue = EnumHelper.ToInt(UserRole.Doctor); // 2
var enumValue = EnumHelper.FromInt<UserRole>(1); // UserRole.Admin
var keyValuePairs = EnumHelper.GetKeyValuePairs<UserRole>(); // 用于下拉框绑定
```

## 性能优化特性

### 1. 预编译正则表达式
使用C# 11的Source Generator功能生成高性能正则表达式：

```csharp
[GeneratedRegex(@"\n|\r|\s|\D", RegexOptions.Compiled)]
private static partial Regex PhoneDigitsRegex();
```

### 2. 静态缓存
预计算常用数据，避免重复计算：

```csharp
private static readonly int[] IdWeights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
private static readonly char[] IdCodes = "10X98765432".ToCharArray();
```

### 3. 现代C#语法
使用C# 8+的新特性提升性能和可读性：

```csharp
// 范围表达式
return digits.Length switch
{
    11 => $"{digits[..3]}-{digits[3..7]}-{digits[7..]}",
    10 => $"{digits[..3]}-{digits[3..6]}-{digits[6..]}",
    _ => digits
};

// 索引表达式
return $"{phoneNumber[..3]}****{phoneNumber[^3..]}";
```

## 使用指南

### 项目引用
```xml
<ItemGroup>
    <ProjectReference Include="..\LYBT.Shared.Utilities\LYBT.Shared.Utilities.csproj" />
</ItemGroup>
```

### 命名空间引用
```csharp
using LYBT.Shared.Utilities.Helpers;
```

### 常用场景示例

#### 用户注册
```csharp
// 密码哈希
string hashedPassword = PasswordHelper.Hash(userInput.Password);

// 邮箱验证
if (!CommonHelper.IsValidEmail(userInput.Email))
{
    throw new ValidationException("邮箱格式不正确");
}

// 身份证验证
if (!CommonHelper.CheckIdNumber(userInput.IdCard))
{
    throw new ValidationException("身份证号码不正确");
}
```

#### 数据脱敏显示
```csharp
// 显示脱敏的用户信息
var displayPhone = CommonHelper.MaskPhoneNumber(user.Phone);
var displayIdCard = CommonHelper.MaskIdNumber(user.IdCard);
```

#### 枚举下拉框绑定
```csharp
// WPF/WinUI下拉框数据绑定
var roleOptions = EnumHelper.GetKeyValuePairs<UserRole>();
cmbRole.ItemsSource = roleOptions;
cmbRole.DisplayMemberPath = "Value";
cmbRole.SelectedValuePath = "Key";
```

#### 文件上传处理
```csharp
// 文件类型检查
if (!CommonHelper.IsImageFile(uploadedFile.FileName))
{
    throw new BusinessException("只允许上传图片文件");
}

// 文件名安全处理
string safeFileName = CommonHelper.SanitizeFileName(uploadedFile.FileName);

// 文件大小显示
string fileSizeDisplay = CommonHelper.GetFileSizeString(uploadedFile.Length);
```

## 扩展指南

### 添加新的工具方法

#### 1. 在CommonHelper中添加
```csharp
public static class CommonHelper
{
    /// <summary>新的工具方法</summary>
    public static string NewUtilityMethod(string input)
    {
        // 实现逻辑
        return processedInput;
    }
}
```

#### 2. 创建专门的工具类
```csharp
[Description("专门功能工具类")]
public static class SpecializedHelper
{
    /// <summary>专门功能方法</summary>
    public static void DoSpecializedTask()
    {
        // 专门功能实现
    }
}
```

### 性能优化建议

#### 1. 使用预编译正则表达式
```csharp
[GeneratedRegex(@"your-pattern", RegexOptions.Compiled)]
private static partial Regex YourRegex();
```

#### 2. 缓存计算结果
```csharp
private static readonly Dictionary<string, string> _cache = new();

public static string ExpensiveOperation(string input)
{
    if (_cache.TryGetValue(input, out var cached))
        return cached;
        
    var result = DoExpensiveCalculation(input);
    _cache[input] = result;
    return result;
}
```

#### 3. 使用Span&lt;T&gt;减少内存分配
```csharp
public static string ProcessString(ReadOnlySpan<char> input)
{
    // 使用Span处理字符串，减少内存分配
}
```

## 单元测试支持

### 测试结构
```
tests/
├── PasswordHelperTests.cs
├── CommonHelperTests.cs
├── EnumHelperTests.cs
└── TestData/
    ├── ValidPhoneNumbers.txt
    ├── ValidIdNumbers.txt
    └── TestEnums.cs
```

### 测试示例
```csharp
[TestClass]
public class PasswordHelperTests
{
    [TestMethod]
    public void Hash_ValidPassword_ReturnsHashedString()
    {
        // Arrange
        string password = "TestPassword123";
        
        // Act
        string hash = PasswordHelper.Hash(password);
        
        // Assert
        Assert.IsNotNull(hash);
        Assert.AreNotEqual(password, hash);
        Assert.IsTrue(hash.Length > 50); // BCrypt哈希长度检查
    }

    [TestMethod]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        string password = "TestPassword123";
        string hash = PasswordHelper.Hash(password);
        
        // Act
        bool result = PasswordHelper.Verify(hash, password);
        
        // Assert
        Assert.IsTrue(result);
    }
}
```

## 维护与更新

### 版本管理
- 主版本号：重大架构变更
- 次版本号：添加新功能
- 修订版本号：Bug修复和性能改进

### 性能监控
- 正则表达式执行时间
- 缓存命中率
- 内存分配统计

### 安全更新
- 定期更新密码哈希算法
- 审查数据脱敏效果
- 验证输入sanitization

---

**版本**: v1.0  
**维护**: 开发团队  
**更新**: 2025-01-01