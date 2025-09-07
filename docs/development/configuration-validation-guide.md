# 配置验证系统使用指南

## 概述

本系统实现了全面的配置验证机制，防止因配置错误导致的系统启动失败或运行异常。配置验证在应用启动时自动执行，确保配置的完整性和正确性。

## 验证特性

### 后端配置验证

#### 启动时验证
- **ValidateOnStart**: 应用启动时立即验证所有配置
- **ValidateDataAnnotations**: 使用DataAnnotations特性验证配置项
- **自动失败**: 配置无效时阻止应用启动

#### 支持的配置类

1. **JwtOptions**: JWT认证配置验证
   ```csharp
   [Required(ErrorMessage = "JWT密钥不能为空")]
   [MinLength(32, ErrorMessage = "JWT密钥长度至少32个字符")]
   public string Secret { get; set; }
   
   [Range(1, 1440, ErrorMessage = "Token过期时间必须在1-1440分钟之间")]
   public int ExpireMinutes { get; set; }
   ```

2. **AuthOptions**: 认证选项验证
   ```csharp
   [Range(1, 100, ErrorMessage = "最大登录失败次数必须在1-100之间")]
   public int MaxFailedLoginAttempts { get; set; }
   
   [Required(ErrorMessage = "系统管理员默认密码不能为空")]
   [MinLength(6, ErrorMessage = "系统管理员默认密码长度至少6个字符")]
   public string DefaultSysAdminPassword { get; set; }
   ```

3. **CacheOptions**: 缓存配置验证
   ```csharp
   [Range(1, 1440, ErrorMessage = "默认过期时间必须在1-1440分钟之间")]
   public int DefaultExpiryMinutes { get; set; }
   
   [RegularExpression("^(Memory|Redis|Hybrid)$", ErrorMessage = "缓存类型必须是Memory、Redis或Hybrid")]
   public string CacheType { get; set; }
   ```

### 前端配置验证

#### API配置验证
```csharp
[Required(ErrorMessage = "API基础地址不能为空")]
[Url(ErrorMessage = "API基础地址格式不正确")]
public string BaseUrl { get; set; }

[Range(5, 300, ErrorMessage = "请求超时时间必须在5-300秒之间")]
public int TimeoutSeconds { get; set; }
```

## 配置验证规则

### 数据类型验证

#### 字符串验证
```csharp
[Required(ErrorMessage = "字段不能为空")]
[StringLength(50, MinimumLength = 3, ErrorMessage = "长度必须在3-50个字符之间")]
[Url(ErrorMessage = "URL格式不正确")]
[EmailAddress(ErrorMessage = "邮箱格式不正确")]
[Phone(ErrorMessage = "电话格式不正确")]
[RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "只能包含字母、数字和下划线")]
```

#### 数值验证
```csharp
[Range(1, 100, ErrorMessage = "数值必须在1-100之间")]
[Range(0.01, 0.5, ErrorMessage = "百分比必须在0.01-0.5之间")]
```

#### 集合验证
```csharp
[Required(ErrorMessage = "集合不能为空")]
[MinLength(1, ErrorMessage = "至少包含一个元素")]
[MaxLength(10, ErrorMessage = "最多包含10个元素")]
```

### 自定义验证

#### 条件验证
```csharp
public class ConditionalValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        // 自定义验证逻辑
        return true;
    }
}
```

#### 跨字段验证
```csharp
[CustomValidation(typeof(MyValidator), nameof(MyValidator.ValidatePasswords))]
public class UserOptions
{
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}
```

## 错误处理

### 启动失败处理

当配置验证失败时，应用将无法启动并显示详细错误信息：

```
System.InvalidOperationException: 配置验证失败
One or more validation errors occurred:
- JWT密钥长度至少32个字符
- 最大登录失败次数必须在1-100之间
- API基础地址格式不正确
```

### 配置修复步骤

1. **检查配置文件**: 确认 appsettings.json 中的配置项
2. **验证数据格式**: 检查URL、邮箱、电话等格式
3. **检查数值范围**: 确保数值在指定范围内
4. **填充必填项**: 确保所有必填配置项都有值

## 配置示例

### 正确的配置示例

#### 后端配置 (appsettings.json)
```json
{
  "JwtOptions": {
    "Secret": "这是一个至少32个字符长的JWT密钥用于签名和验证Token",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "ExpireMinutes": 480,
    "RememberMeExpireMinutes": 43200,
    "ClockSkewSeconds": 300
  },
  "AuthOptions": {
    "MaxFailedLoginAttempts": 5,
    "AccountLockoutDuration": "00:15:00",
    "DefaultSysAdminPassword": "Admin@123456",
    "EnableDetailedLoginLogging": true
  },
  "CacheOptions": {
    "DefaultExpiryMinutes": 30,
    "CacheType": "Memory",
    "MemoryCache": {
      "SizeLimit": 200,
      "CompactionPercentage": 0.10,
      "ExpirationScanFrequency": 30
    }
  }
}
```

#### 前端配置 (appsettings.json)
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001/",
    "TimeoutSeconds": 60
  }
}
```

### 常见配置错误

#### 错误示例 1: JWT密钥过短
```json
{
  "JwtOptions": {
    "Secret": "短密钥"  // ❌ 错误：长度不足32个字符
  }
}
```
**修复**: 使用至少32个字符的密钥

#### 错误示例 2: 无效的URL格式
```json
{
  "ApiSettings": {
    "BaseUrl": "invalid-url"  // ❌ 错误：不是有效的URL格式
  }
}
```
**修复**: 使用完整的URL格式，如 "https://localhost:7001/"

#### 错误示例 3: 数值超出范围
```json
{
  "AuthOptions": {
    "MaxFailedLoginAttempts": 0  // ❌ 错误：必须在1-100之间
  }
}
```
**修复**: 设置为1-100之间的数值

## 开发建议

### 添加新配置项

1. **定义配置类**:
   ```csharp
   public class MyConfigOptions
   {
       public const string SectionName = "MyConfig";
       
       [Required(ErrorMessage = "配置项不能为空")]
       public string MyProperty { get; set; }
   }
   ```

2. **注册配置验证**:
   ```csharp
   services.AddOptions<MyConfigOptions>()
       .Bind(configuration.GetSection(MyConfigOptions.SectionName))
       .ValidateDataAnnotations()
       .ValidateOnStart();
   ```

### 测试配置验证

1. **单元测试配置类**:
   ```csharp
   [Test]
   public void ConfigOptions_Should_ValidateCorrectly()
   {
       var options = new MyConfigOptions { MyProperty = "valid-value" };
       var context = new ValidationContext(options);
       var results = new List<ValidationResult>();
       
       var isValid = Validator.TryValidateObject(options, context, results, true);
       
       Assert.IsTrue(isValid);
   }
   ```

2. **集成测试启动验证**:
   ```csharp
   [Test]
   public void Application_Should_StartWith_ValidConfig()
   {
       var host = CreateTestHost(validConfiguration);
       
       // 应该能够成功启动
       Assert.DoesNotThrow(() => host.Start());
   }
   ```

## 监控和维护

### 日志记录

配置验证失败会自动记录到系统日志：

```
[ERROR] Configuration validation failed for JwtOptions: JWT密钥长度至少32个字符
[ERROR] Configuration validation failed for AuthOptions: 最大登录失败次数必须在1-100之间
```

### 定期检查

建议定期检查配置文件的完整性：

1. **开发环境**: 每次启动都会进行验证
2. **测试环境**: CI/CD管道中包含配置验证测试
3. **生产环境**: 部署前进行配置验证检查

### 故障排除

1. **收集错误信息**: 查看启动日志中的详细验证错误
2. **对比配置模板**: 使用正确的配置示例进行对比
3. **逐项验证**: 按照验证规则逐一检查每个配置项
4. **环境变量检查**: 确认环境变量是否正确覆盖配置文件

## 安全注意事项

1. **敏感信息保护**: 验证错误日志中不包含敏感配置值
2. **生产环境配置**: 生产环境使用环境变量覆盖敏感配置
3. **配置文件权限**: 确保配置文件具有适当的访问权限
4. **默认值安全**: 确保默认配置值符合安全要求