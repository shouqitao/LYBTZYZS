# LYBT.Shared.Utilities

凌隐宝堂中医诊所系统 - 共享工具类库项目

## 项目概述

这个项目提供了系统中通用的工具类和帮助方法，包括字符串处理、数据验证、文件操作、时间转换、密码安全等常用功能。所有工具类都是静态类，可以在前后端项目中直接调用。

## 目录结构

```
LYBT.Shared.Utilities/
└── Helpers/                    # 工具类库
    ├── CommonHelper.cs         # 通用工具类
    ├── EnumHelper.cs          # 枚举操作工具类
    └── PasswordHelper.cs      # 密码安全工具类
```

## 核心功能

### 1. 通用工具类 (CommonHelper)

提供日常开发中最常用的工具方法，性能优化并支持.NET 8现代语法：

#### 网络与系统检查
- **网络可用性**: `IsNetworkAvailable()` - 检查网络连接状态

#### 数据验证与格式化
- **电话号码格式化**: `FormatPhone(string phone)` - 自动格式化为 `138-1234-5678` 格式
- **身份证号验证**: `CheckIdNumber(string idNumber)` - 完整的18位身份证校验算法
- **邮箱格式验证**: `IsValidEmail(string email)` - 基于.NET标准的邮箱验证

#### 数据脱敏
- **手机号脱敏**: `MaskPhoneNumber(string phoneNumber)` - `138****5678`
- **身份证脱敏**: `MaskIdNumber(string idNumber)` - `430421********1234`

#### 类型安全转换
```csharp
// 安全类型转换，避免异常
int result = CommonHelper.SafeToInt("123", 0);           // 成功返回123
decimal price = CommonHelper.SafeToDecimal("12.34", 0); // 成功返回12.34
bool flag = CommonHelper.SafeToBool("true", false);      // 成功返回true
```

#### ID生成器
- **唯一标识符**: `GenerateUniqueId()` - 32位不含连字符的GUID
- **短ID**: `GenerateShortId()` - 8位短标识符
- **随机字符串**: `GenerateRandomString(int length, bool includeNumbers)` - 自定义长度随机串

#### 文件操作工具
- **文件类型检测**: `IsImageFile(string fileName)` / `IsDocumentFile(string fileName)`
- **文件大小格式化**: `GetFileSizeString(long fileSize)` - `"1.23 MB"`
- **文件名清理**: `SanitizeFileName(string fileName)` - 移除非法字符

#### 时间戳工具
```csharp
// Unix时间戳转换
long timestamp = CommonHelper.GetTimestamp();                    // 秒级时间戳
long timestampMs = CommonHelper.GetTimestampMilliseconds();      // 毫秒级时间戳
DateTime dateTime = CommonHelper.FromTimestamp(timestamp);       // 时间戳转DateTime
```

#### 拼音码生成
- **拼音首字母**: `GetPinyinCode(string text)` - 中文转拼音首字母（简化实现）

### 2. 枚举工具类 (EnumHelper)

专业的枚举操作工具，支持Description特性和类型安全转换：

#### 描述文本操作
```csharp
public enum UserRole 
{
    [Description("系统管理员")]
    Admin = 1,
    
    [Description("医生")]
    Doctor = 2
}

// 获取枚举描述
string desc = EnumHelper.GetDescription(UserRole.Admin);  // "系统管理员"

// 根据描述获取枚举
UserRole role = EnumHelper.GetEnumByDescription<UserRole>("医生");  // UserRole.Doctor
```

#### 枚举-集合转换
```csharp
// 获取所有枚举值和描述
Dictionary<UserRole, string> roleDict = EnumHelper.GetEnumDescriptions<UserRole>();

// 获取下拉框用的键值对
List<KeyValuePair<UserRole, string>> roleItems = EnumHelper.GetKeyValuePairs<UserRole>();

// 获取整数值-描述键值对
List<KeyValuePair<int, string>> intRoleItems = EnumHelper.GetIntKeyValuePairs<UserRole>();
```

#### 类型转换
```csharp
// 枚举与整数转换
int roleValue = EnumHelper.ToInt(UserRole.Admin);          // 1
UserRole role = EnumHelper.FromInt<UserRole>(1);           // UserRole.Admin

// 字符串解析
UserRole role = EnumHelper.Parse<UserRole>("Admin");       // 忽略大小写
bool success = EnumHelper.TryParse<UserRole>("Doctor", out UserRole result);
```

#### 验证与查询
```csharp
// 检查枚举值是否已定义
bool isDefined = EnumHelper.IsDefined<UserRole>(1);        // true

// 获取所有枚举值
List<UserRole> allRoles = EnumHelper.GetValues<UserRole>();

// 获取所有枚举名称
List<string> roleNames = EnumHelper.GetNames<UserRole>();
```

### 3. 密码安全工具类 (PasswordHelper)

基于ASP.NET Core Identity的密码哈希工具，提供企业级密码安全：

#### 密码哈希
```csharp
// 对密码进行安全哈希
string hashedPassword = PasswordHelper.Hash("userPassword123");
// 返回: "AQAAAAEAACcQAAAAEGk7xg3..."（包含盐值的哈希）

// 验证密码
bool isValid = PasswordHelper.Verify(hashedPassword, "userPassword123");  // true
bool isInvalid = PasswordHelper.Verify(hashedPassword, "wrongPassword");   // false
```

#### 安全特性
- **自动加盐**: 每次哈希都生成唯一盐值
- **防彩虹表**: 使用PBKDF2算法增加破解难度
- **版本兼容**: 支持密码哈希格式升级
- **性能平衡**: 在安全性和性能之间取得平衡

## 技术栈

### 依赖项
- **.NET 8.0**: 现代.NET平台，支持最新C#语法
- **Microsoft.AspNetCore.Identity 2.3.1**: 密码哈希算法支持
- **LYBT.Shared.Models**: 共享数据模型引用

### 现代化特性
- **生成正则表达式**: 使用 `[GeneratedRegex]` 提升性能
- **范围运算符**: `phoneNumber[..3]` / `phoneNumber[^3..]` 现代语法
- **模式匹配**: `digits.Length switch { 11 => ..., 10 => ..., _ => ... }`
- **静态导入**: 简化常用方法调用

## 使用示例

### 患者信息处理
```csharp
// 验证和格式化患者基础信息
var patient = new PatientDto
{
    Phone = CommonHelper.FormatPhone("13812345678"),           // "138-1234-5678"
    IdNumber = request.IdNumber,
    Email = request.Email
};

// 验证数据有效性
if (!CommonHelper.CheckIdNumber(patient.IdNumber))
{
    throw new ValidationException("身份证号码格式不正确");
}

if (!CommonHelper.IsValidEmail(patient.Email))
{
    throw new ValidationException("邮箱格式不正确"); 
}

// 生成患者编号
patient.PatientCode = CommonHelper.GenerateShortId();  // "a1b2c3d4"
```

### 用户角色管理
```csharp
// 获取所有用户角色用于下拉框
var roleOptions = EnumHelper.GetKeyValuePairs<UserRole>();
roleComboBox.ItemsSource = roleOptions;

// 角色权限验证
if (EnumHelper.ToInt(currentUser.Role) >= EnumHelper.ToInt(UserRole.Doctor))
{
    // 医生级别及以上权限
    EnableMedicalFeatures();
}

// 角色描述显示
userRoleLabel.Content = EnumHelper.GetDescription(user.Role);  // "系统管理员"
```

### 密码安全管理
```csharp
// 用户注册时哈希密码
var user = new User
{
    Username = request.Username,
    PasswordHash = PasswordHelper.Hash(request.Password),  // 安全哈希存储
    CreateTime = DateTime.Now
};

// 用户登录时验证密码
var user = await _userRepository.GetByUsernameAsync(loginRequest.Username);
if (user != null && PasswordHelper.Verify(user.PasswordHash, loginRequest.Password))
{
    // 登录成功
    await CreateLoginSessionAsync(user);
}
```

### 文件上传处理
```csharp
// 文件类型和大小验证
if (!CommonHelper.IsImageFile(uploadFile.FileName))
{
    throw new ValidationException("请上传图片文件");
}

var fileSizeText = CommonHelper.GetFileSizeString(uploadFile.Length);
if (uploadFile.Length > 5 * 1024 * 1024)  // 5MB限制
{
    throw new ValidationException($"文件过大：{fileSizeText}，请上传小于5MB的文件");
}

// 生成安全的文件名
string safeFileName = CommonHelper.SanitizeFileName(uploadFile.FileName);
string uniqueFileName = $"{CommonHelper.GenerateUniqueId()}{Path.GetExtension(safeFileName)}";
```

### 数据脱敏展示
```csharp
// 患者隐私信息脱敏显示
var patientDisplay = new PatientDisplayDto
{
    Name = patient.Name,
    PhoneDisplay = CommonHelper.MaskPhoneNumber(patient.Phone),      // "138****5678"
    IdNumberDisplay = CommonHelper.MaskIdNumber(patient.IdNumber),   // "430421********1234"
    CreateTime = patient.CreateTime
};
```

## 性能优化

### 编译时优化
- **预编译正则表达式**: 使用 `[GeneratedRegex]` 避免运行时编译开销
- **静态只读数组**: 身份证权重等常量数组避免重复创建
- **字符串池化**: 高频字符串操作优化内存使用

### 算法优化
- **身份证验证**: 一次遍历完成权重计算和校验码验证
- **安全转换**: 避免异常处理的性能开销
- **文件类型检测**: 基于扩展名的快速判断

## 扩展指南

### 添加新工具方法
1. **选择合适的Helper类**: 根据功能归类
2. **添加XML注释**: 详细描述用途、参数、返回值
3. **编写单元测试**: 覆盖正常和边界情况
4. **性能考虑**: 对高频调用的方法进行性能优化

### 最佳实践
- **静态方法**: 所有Helper方法都应该是静态的
- **线程安全**: 确保多线程环境下的安全性
- **参数验证**: 对输入参数进行必要的验证
- **异常处理**: 提供明确的异常信息

## 质量保证

### 代码规范
- **命名规范**: 方法名明确表达功能，参数名具有描述性
- **文档覆盖**: 所有公共方法都有完整的XML注释
- **类型安全**: 使用泛型和强类型，避免object类型
- **null安全**: 支持可空引用类型，提供默认值处理

### 测试覆盖
- **单元测试**: 每个公共方法都有对应的单元测试
- **边界测试**: 测试空值、边界值、异常情况
- **性能测试**: 确保关键方法的性能表现

## 相关文档

- [LYBT.Shared.Models](../LYBT.Shared.Models/README.md) - 共享数据模型
- [开发规范](../../docs/开发规范.md) - 项目开发标准
- [密码安全指南](../../docs/guides/password-security-guide.md) - 密码安全最佳实践

---

**项目状态**: ✅ 生产就绪 | **最后更新**: 2025-01-01