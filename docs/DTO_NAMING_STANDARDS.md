# DTO命名规范标准 - 前后端契约统一化

> 版本：1.0  
> 更新：2025-01-08  
> 目标：建立统一的数据传输对象命名规范，确保前后端契约一致性

## 🎯 总体原则

### 命名一致性
- 所有DTO类名必须以用途后缀结尾
- 使用PascalCase（首字母大写）命名
- 名称应清晰描述数据传输的用途和场景

### 命名模式
```
{实体名称}{用途后缀}Dto
```

---

## 📋 标准DTO后缀定义

### 查询相关
| 后缀 | 用途 | 示例 | 说明 |
|------|------|------|------|
| `Dto` | 基础数据展示 | `UserDto` | 用于API返回的标准数据格式 |
| `DetailDto` | 详细信息展示 | `UserDetailDto` | 包含完整信息的详情数据 |
| `ListDto` | 列表项展示 | `UserListDto` | 用于列表显示的精简数据 |
| `SummaryDto` | 摘要信息 | `UserSummaryDto` | 用于统计、摘要的简化数据 |

### 操作相关
| 后缀 | 用途 | 示例 | 说明 |
|------|------|------|------|
| `CreateDto` | 创建操作 | `UserCreateDto` | 用于创建新记录的数据 |
| `UpdateDto` | 更新操作 | `UserUpdateDto` | 用于更新现有记录的数据 |
| `PatchDto` | 部分更新 | `UserPatchDto` | 用于PATCH操作的部分更新数据 |

### 查询参数相关
| 后缀 | 用途 | 示例 | 说明 |
|------|------|------|------|
| `QueryDto` | 通用查询 | `UserQueryDto` | 用于查询筛选的参数 |
| `PagedQueryDto` | 分页查询 | `UserPagedQueryDto` | 包含分页参数的查询条件 |
| `SearchDto` | 搜索查询 | `UserSearchDto` | 用于搜索功能的参数 |

### 特殊操作相关
| 后缀 | 用途 | 示例 | 说明 |
|------|------|------|------|
| `ImportDto` | 导入数据 | `UserImportDto` | 用于批量导入的数据格式 |
| `ExportDto` | 导出数据 | `UserExportDto` | 用于数据导出的格式 |
| `ValidationDto` | 验证数据 | `UserValidationDto` | 用于数据验证的结构 |

---

## 🏗️ 实体模块DTO结构标准

### 用户模块示例
```
LYBT.Shared.Models.Contracts.Users/
├── UserDto.cs                    # 基础用户数据
├── UserDetailDto.cs              # 用户详细信息
├── UserListDto.cs                # 用户列表项
├── UserCreateDto.cs              # 创建用户
├── UserUpdateDto.cs              # 更新用户
├── UserQueryDto.cs               # 用户查询参数
├── UserPagedQueryDto.cs          # 分页查询参数
└── UserSummaryDto.cs             # 用户统计摘要
```

### 患者模块示例
```
LYBT.Shared.Models.Contracts.Patients/
├── PatientDto.cs
├── PatientDetailDto.cs
├── PatientCreateDto.cs
├── PatientUpdateDto.cs
├── PatientQueryDto.cs
├── PatientPagedQueryDto.cs
├── PatientSearchDto.cs           # 患者搜索（支持姓名、身份证、手机号）
└── PatientValidationDto.cs       # 患者信息验证
```

---

## 📝 DTO设计原则

### 1. 单一职责原则
每个DTO只负责一种数据传输场景，不混用用途：

```csharp
// ✅ 正确：职责明确
public class UserCreateDto
{
    public string Username { get; set; }
    public string RealName { get; set; }
    public string? PhoneNumber { get; set; }
    // 只包含创建时需要的字段
}

// ❌ 错误：混合用途
public class UserDto
{
    public Guid Id { get; set; }        // 查询时需要
    public string Username { get; set; }
    public string RealName { get; set; }
    public string Password { get; set; } // 创建时需要，查询时不应该包含
}
```

### 2. 数据最小化原则
每个DTO只包含该场景必需的字段：

```csharp
// ✅ 正确：列表DTO只包含列表显示必需信息
public class UserListDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string RealName { get; set; }
    public CommonStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
}

// ✅ 正确：详情DTO包含完整信息
public class UserDetailDto : UserListDto
{
    public string? PhoneNumber { get; set; }
    public string? PinYinCode { get; set; }
    public DateTime? LastLoginTime { get; set; }
    // 更多详细信息...
}
```

### 3. 前后端一致性原则
DTO字段命名应与前端预期一致：

```csharp
// ✅ 正确：使用JSON属性名映射
public class UserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("realName")]
    public string RealName { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}
```

---

## 🔧 验证和约束规范

### 数据注解标准
```csharp
public class UserCreateDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度应在3-50个字符之间")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(20, ErrorMessage = "真实姓名不能超过20个字符")]
    [JsonPropertyName("realName")]
    public string RealName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "手机号格式不正确")]
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [EnumDataType(typeof(CommonStatus), ErrorMessage = "状态值无效")]
    [JsonPropertyName("status")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}
```

---

## 📊 分页查询DTO标准

### 标准分页查询基类
```csharp
public abstract class BasePagedQueryDto
{
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "页大小必须在1-100之间")]
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;

    [JsonPropertyName("searchKeyword")]
    public string? SearchKeyword { get; set; }
}

public class UserPagedQueryDto : BasePagedQueryDto
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("realName")]
    public string? RealName { get; set; }

    [JsonPropertyName("status")]
    public CommonStatus? Status { get; set; }

    [JsonPropertyName("createStartDate")]
    public DateTime? CreateStartDate { get; set; }

    [JsonPropertyName("createEndDate")]
    public DateTime? CreateEndDate { get; set; }
}
```

---

## 🎨 JSON序列化规范

### 统一JSON命名策略
- 使用camelCase（驼峰命名）
- 避免下划线和连字符
- 保持与前端JavaScript命名习惯一致

### 示例配置
```csharp
// Startup.cs 或 Program.cs 中配置
services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
```

---

## ✅ 检查清单

在实现DTO时，请确保：

- [ ] DTO名称遵循`{实体名称}{用途后缀}Dto`格式
- [ ] 每个DTO职责单一，不混合用途
- [ ] 包含必要的数据注解验证
- [ ] 使用JsonPropertyName确保序列化一致性
- [ ] 继承适当的基类（如BasePagedQueryDto）
- [ ] 提供合适的默认值
- [ ] 添加完整的XML注释文档

---

*"统一的DTO命名规范是前后端契约一致性的基石，确保团队协作效率和代码可维护性。"*