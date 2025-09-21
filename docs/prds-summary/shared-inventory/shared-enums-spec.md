# Shared 枚举规范文档

> 版本：1.0.0
> 更新时间：2025-09-21
> 适用范围：LYBT.Shared.Models.Enums 及所有使用枚举的模块

## 📋 目录

1. [核心原则](#核心原则)
2. [命名规范](#命名规范)
3. [值定义规范](#值定义规范)
4. [显示规范](#显示规范)
5. [i18n国际化](#i18n国际化)
6. [前端字典缓存](#前端字典缓存)
7. [接口约束](#接口约束)
8. [最佳实践](#最佳实践)
9. [示例代码](#示例代码)

## 🎯 核心原则

1. **一致性**：所有枚举遵循统一的命名和编码规范
2. **可扩展**：新增枚举值不影响现有数据
3. **可维护**：清晰的文档和注释
4. **前后端统一**：确保前后端枚举值完全一致
5. **国际化支持**：支持多语言显示

## 📝 命名规范

### 枚举类型命名

```csharp
// ✅ 正确：使用单数形式，Pascal命名法
public enum UserRole { }
public enum Gender { }
public enum ConsultationStatus { }

// ❌ 错误：避免复数形式
public enum UserRoles { }  // 应为 UserRole
public enum Genders { }     // 应为 Gender
```

### 枚举值命名

```csharp
public enum UserRole
{
    // ✅ 正确：Pascal命名法，语义清晰
    Admin = 1,
    Doctor = 2,
    Nurse = 3,

    // ❌ 错误：避免缩写和不清晰的命名
    ADM = 1,    // 应为 Admin
    DOC = 2,    // 应为 Doctor
}
```

### 命名约定表

| 类型 | 命名规则 | 示例 | 说明 |
|------|----------|------|------|
| 状态类 | XxxStatus | ConsultationStatus | 表示业务状态 |
| 类型类 | XxxType | PaymentType | 表示业务类型 |
| 方法类 | XxxMethod | DiagnosisMethod | 表示操作方法 |
| 级别类 | XxxLevel | RiskLevel | 表示等级层次 |
| 角色类 | XxxRole | UserRole | 表示角色定义 |

## 🔢 值定义规范

### 基础规则

```csharp
public enum CommonStatus
{
    // 1. 显式指定值，避免隐式赋值
    Disabled = 0,    // 禁用/无效状态使用0
    Enabled = 1,     // 启用/有效状态使用1

    // 2. 预留扩展空间
    Pending = 10,    // 预留10-19给待处理相关状态
    Processing = 11,

    // 3. 错误状态使用负数
    Error = -1,
    Failed = -2,
}
```

### 值分配策略

| 值范围 | 用途 | 示例 |
|--------|------|------|
| 0 | 默认/无效/禁用 | Disabled = 0 |
| 1-9 | 基础状态 | Enabled = 1 |
| 10-99 | 业务状态 | Pending = 10 |
| 100-999 | 扩展状态 | Custom = 100 |
| 负数 | 错误/异常 | Error = -1 |

## 🎨 显示规范

### 使用 Description 特性

```csharp
using System.ComponentModel;

public enum Gender
{
    [Description("未知")]
    Unknown = 0,

    [Description("男")]
    Male = 1,

    [Description("女")]
    Female = 2,
}
```

### 显示名称规则

| 场景 | 规则 | 示例 |
|------|------|------|
| 中文显示 | 使用Description | [Description("待处理")] |
| 英文显示 | 使用枚举名 | Pending |
| 代码值 | 使用数字值 | 10 |
| API传输 | 使用枚举名或值 | "Pending" 或 10 |

## 🌍 i18n国际化

### 资源文件结构

```
Resources/
├── Enums.resx              # 默认语言（中文）
├── Enums.en-US.resx        # 英文
└── Enums.zh-TW.resx        # 繁体中文
```

### 资源键命名

```xml
<!-- Enums.resx -->
<data name="Gender_Unknown" xml:space="preserve">
    <value>未知</value>
</data>
<data name="Gender_Male" xml:space="preserve">
    <value>男</value>
</data>
<data name="Gender_Female" xml:space="preserve">
    <value>女</value>
</data>
```

### 获取本地化文本

```csharp
public static class EnumExtensions
{
    public static string GetLocalizedName(this Enum value)
    {
        var resourceKey = $"{value.GetType().Name}_{value}";
        return Resources.Enums.ResourceManager.GetString(resourceKey)
               ?? value.ToString();
    }
}
```

## 💾 前端字典缓存

### 字典数据结构

```typescript
interface EnumDictionary {
    name: string;        // 枚举类型名
    items: EnumItem[];   // 枚举项列表
    version: string;     // 版本号
    cached: Date;        // 缓存时间
}

interface EnumItem {
    value: number;       // 枚举值
    code: string;        // 枚举代码
    name: string;        // 显示名称
    description?: string; // 描述信息
}
```

### 缓存策略

```typescript
class EnumCache {
    private cache = new Map<string, EnumDictionary>();
    private readonly CACHE_DURATION = 24 * 60 * 60 * 1000; // 24小时

    async getEnum(enumType: string): Promise<EnumItem[]> {
        const cached = this.cache.get(enumType);

        if (cached && !this.isExpired(cached)) {
            return cached.items;
        }

        const data = await this.fetchEnum(enumType);
        this.cache.set(enumType, data);
        return data.items;
    }

    private isExpired(dict: EnumDictionary): boolean {
        return Date.now() - dict.cached.getTime() > this.CACHE_DURATION;
    }
}
```

### 前端使用示例

```vue
<template>
    <el-select v-model="gender">
        <el-option
            v-for="item in genderOptions"
            :key="item.value"
            :label="item.name"
            :value="item.value"
        />
    </el-select>
</template>

<script setup>
const genderOptions = await enumCache.getEnum('Gender');
</script>
```

## 🔒 接口约束

### API 请求/响应

```csharp
// 请求DTO - 使用枚举类型
public class UserCreateDto
{
    public string Username { get; set; }
    public Gender Gender { get; set; }  // 直接使用枚举
    public UserRole Role { get; set; }
}

// 响应DTO - 包含枚举值和显示名
public class UserDto
{
    public Guid Id { get; set; }
    public Gender Gender { get; set; }
    public string GenderName => Gender.GetDescription(); // 附加显示名
}
```

### JSON 序列化配置

```csharp
// Startup.cs 或 Program.cs
services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 枚举序列化为字符串
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });
```

### Swagger 文档

```csharp
// 为枚举生成文档
services.AddSwaggerGen(c =>
{
    c.SchemaFilter<EnumSchemaFilter>();
});

public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            foreach (var name in Enum.GetNames(context.Type))
            {
                schema.Enum.Add(new OpenApiString(name));
            }
        }
    }
}
```

## ✅ 最佳实践

### 1. 枚举扩展方法

```csharp
public static class EnumExtensions
{
    // 获取描述
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    // 转换为字典
    public static Dictionary<int, string> ToDictionary<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(e => (int)(object)e, e => e.GetDescription());
    }

    // 验证值是否有效
    public static bool IsValid<T>(int value) where T : Enum
    {
        return Enum.IsDefined(typeof(T), value);
    }
}
```

### 2. 枚举验证

```csharp
public class EnumValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        var type = value.GetType();
        return type.IsEnum && Enum.IsDefined(type, value);
    }
}
```

### 3. 示例枚举定义

```csharp
namespace LYBT.Shared.Models.Enums
{
    public enum ConsultationStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        [Description("待处理")]
        Pending = 10,

        /// <summary>
        /// 问诊中
        /// </summary>
        [Description("问诊中")]
        InProgress = 20,

        /// <summary>
        /// 已完成
        /// </summary>
        [Description("已完成")]
        Completed = 30,

        /// <summary>
        /// 已取消
        /// </summary>
        [Description("已取消")]
        Cancelled = 40,

        /// <summary>
        /// 已过期
        /// </summary>
        [Description("已过期")]
        Expired = 50
    }
}
```

### API 控制器示例

```csharp
[ApiController]
[Route("api/[controller]")]
public class EnumsController : ControllerBase
{
    /// <summary>
    /// 获取枚举字典
    /// </summary>
    [HttpGet("{enumType}")]
    public IActionResult GetEnumDictionary(string enumType)
    {
        var type = Type.GetType($"LYBT.Shared.Models.Enums.{enumType}");
        if (type == null || !type.IsEnum)
        {
            return NotFound();
        }

        var items = Enum.GetValues(type)
            .Cast<Enum>()
            .Select(e => new
            {
                Value = (int)(object)e,
                Code = e.ToString(),
                Name = e.GetDescription()
            });

        return Ok(new
        {
            Name = enumType,
            Items = items,
            Version = "1.0.0",
            Cached = DateTime.Now
        });
    }
}
```

### 前端 TypeScript 定义

```typescript
// enums.ts
export enum Gender {
    Unknown = 0,
    Male = 1,
    Female = 2
}

export enum UserRole {
    Admin = 1,
    Doctor = 2,
    Nurse = 3
}

export enum ConsultationStatus {
    Pending = 10,
    InProgress = 20,
    Completed = 30,
    Cancelled = 40,
    Expired = 50
}

// enum-helper.ts
export class EnumHelper {
    static getOptions<T>(enumType: any): Array<{value: T, label: string}> {
        return Object.keys(enumType)
            .filter(key => !isNaN(Number(enumType[key])))
            .map(key => ({
                value: enumType[key],
                label: key
            }));
    }
}
```

## 🔍 检查清单

使用此清单确保枚举定义符合规范：

- [ ] 枚举类型使用单数形式命名
- [ ] 枚举值使用 Pascal 命名法
- [ ] 显式指定枚举值
- [ ] 添加 Description 特性
- [ ] 添加 XML 注释文档
- [ ] 前后端枚举值保持一致
- [ ] 提供枚举字典接口
- [ ] 支持国际化（如需要）
- [ ] 编写单元测试
- [ ] 更新相关文档

## 📈 版本管理

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| 1.0.0 | 2025-09-21 | 初始版本发布 |

---

*此规范为 LYBT 项目枚举使用的权威指南，所有开发人员应严格遵循*
