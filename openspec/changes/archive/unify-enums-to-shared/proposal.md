# OpenSpec Proposal: unify-enums-to-shared

## 元数据
- **提案ID**: unify-enums-to-shared
- **创建日期**: 2025-12-17
- **状态**: Draft
- **优先级**: Medium
- **影响范围**: Server, Client, Shared

## 1. 问题陈述

当前项目中枚举定义分散在三个层级(Server/Client/Shared)，存在以下问题：

### 1.1 重复定义
- `ErrorCategory`: 在3个位置定义
  - `Shared/LYBT.Shared.Models/Contracts/Common/SharedCommon.cs:11`
  - `Shared/LYBT.Shared.Models/Errors/ErrorCategory.cs:7`
  - `Shared/LYBT.Shared.Models/Contracts/Common/ErrorCategory.cs:7`
- `ErrorSeverity`: 在3个位置定义
  - `Shared/LYBT.Shared.Models/Contracts/Common/SharedCommon.cs:92`
  - `Shared/LYBT.Shared.Models/Contracts/Common/ErrorSeverity.cs:7`
  - `Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ErrorHandling/ErrorContext.cs:48`

### 1.2 位置不合理
- 部分枚举仅在某一层使用，但定义位置不一致
- 缺乏统一的命名空间组织

### 1.3 难以维护
- 修改枚举值时需要同步多处
- JSON序列化/反序列化可能因枚举不一致导致错误

### 1.4 过度定义未使用
以下枚举仅有定义，从未被实际使用：
| 枚举名 | 位置 | 引用次数 |
|--------|------|----------|
| DataStatus | SystemEnums.cs | 1 (仅定义) |
| AuditStatus | SystemEnums.cs | 1 (仅定义) |
| DeleteStatus | SystemEnums.cs | 1 (仅定义) |
| TimeSlot | SystemEnums.cs | 1 (仅定义) |
| WorkDay | SystemEnums.cs | 1 (仅定义) |
| PaymentStatus | SystemEnums.cs | 1 (仅定义) |
| PaymentMethod | SystemEnums.cs | 1 (仅定义) |
| CompatibilityType | SystemEnums.cs | 1 (仅定义) |
| CompatibilitySeverity | SystemEnums.cs | 1 (仅定义) |
| PendingType | MedicalCaseEnums.cs | 1 (仅定义) |

## 2. 目标

1. **消除重复**: 每个枚举只定义一次
2. **删除未使用**: 清理10个从未使用的枚举定义
3. **集中管理**: 共享枚举统一放在Shared层
4. **清晰分类**: 按业务域组织枚举文件
5. **保持兼容**: 不破坏现有API和序列化
6. **中文显示**: 为所有枚举提供`ToChinese()`扩展方法，支持UI中文显示

## 3. 当前枚举分布

### 3.1 Shared层枚举 (正确位置，保持不变)
```
LYBT.Shared.Models/Enums/
├── SystemEnums.cs      # CommonStatus, DeleteStatus, OperationResult, DataStatus, AuditStatus, PaymentStatus, PaymentMethod, WorkDay, TimeSlot, CompatibilityType, CompatibilitySeverity
├── RecordEnums.cs      # ConsultationStatus
├── PatientStatus.cs    # PatientStatus
├── MedicalCaseEnums.cs # MedicalCaseStatus, PendingType, AuditOperationType
├── DecocteMethod.cs    # DecocteMethod
├── Gender.cs           # Gender
├── CaseStatus.cs       # CaseStatus
├── FormulaValidationStatus.cs # FormulaValidationStatus
├── AuthEnums.cs        # LoginType, AuthSessionStatus, UserRole, AuthErrorCode
└── DuplicateStrategy.cs # DuplicateStrategy
```

### 3.2 Client层枚举 (仅Client使用，保持不变)
- UI状态: `BadgeType`, `NotificationType`, `FeedbackType`
- 流程状态: `LoginFlowState`, `SessionState`, `ApplicationState`, `StartupPipelineState`
- 业务流程: `WorkspaceMode`, `EditType`, `FlowStep`, `EditState`
- 打印: `PaperSize`, `PrintOrientation`
- 其他: `ConnectionMode`, `LogoutReason`, `TripleChoiceResult`, `LeaveConsultationChoice`

### 3.3 Server层枚举 (仅Server使用，保持不变)
- 配置验证: `Severity`, `ErrorType`
- 数据安全: `SensitiveDataType`, `MaskingMode`
- 认证: `BlacklistType`
- 业务: `FormulaType`

### 3.4 需要处理的重复枚举
| 枚举名 | 当前位置 | 目标位置 |
|--------|----------|----------|
| ErrorCategory | SharedCommon.cs, ErrorCategory.cs (x2) | Shared/Enums/ErrorEnums.cs |
| ErrorSeverity | SharedCommon.cs, ErrorSeverity.cs, ErrorContext.cs | Shared/Enums/ErrorEnums.cs |
| BusinessOperation | ValidationContext.cs | Shared/Enums/ValidationEnums.cs |
| PasswordStrength | PasswordHelper.cs | Shared/Enums/SecurityEnums.cs |
| MedicalCaseUpdateMode | MedicalCaseDtos.cs | Shared/Enums/MedicalCaseEnums.cs |
| ErrorCode | ErrorCode.cs | Shared/Enums/ErrorEnums.cs |

## 4. 方案设计

### 4.1 目标目录结构
```
LYBT.Shared.Models/Enums/
├── AuthEnums.cs           # LoginType, AuthSessionStatus, UserRole, AuthErrorCode
├── CaseStatus.cs          # CaseStatus
├── DecocteMethod.cs       # DecocteMethod
├── DuplicateStrategy.cs   # DuplicateStrategy
├── ErrorEnums.cs          # ErrorCode, ErrorCategory, ErrorSeverity (合并)
├── FormulaValidationStatus.cs
├── Gender.cs
├── MedicalCaseEnums.cs    # MedicalCaseStatus, PendingType, AuditOperationType, MedicalCaseUpdateMode (合并)
├── PatientStatus.cs
├── RecordEnums.cs         # ConsultationStatus
├── SecurityEnums.cs       # PasswordStrength (新建)
├── SystemEnums.cs         # CommonStatus, DeleteStatus, OperationResult, DataStatus, AuditStatus, PaymentStatus, PaymentMethod, WorkDay, TimeSlot, CompatibilityType, CompatibilitySeverity
└── ValidationEnums.cs     # BusinessOperation (新建)
```

### 4.2 枚举中文扩展方法设计

为每个枚举创建对应的扩展方法类，统一放在`LYBT.Shared.Models/Extensions/`目录。

**设计模式:**
```csharp
// LYBT.Shared.Models/Extensions/EnumExtensions.cs
namespace LYBT.Shared.Models.Extensions;

public static class GenderExtensions
{
    public static string ToChinese(this Gender gender) => gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        Gender.Unknown => "未知",
        _ => gender.ToString()
    };
}

public static class CommonStatusExtensions
{
    public static string ToChinese(this CommonStatus status) => status switch
    {
        CommonStatus.Active => "启用",
        CommonStatus.Inactive => "停用",
        CommonStatus.Deleted => "已删除",
        _ => status.ToString()
    };
}

public static class MedicalCaseStatusExtensions
{
    public static string ToChinese(this MedicalCaseStatus status) => status switch
    {
        MedicalCaseStatus.Draft => "草稿",
        MedicalCaseStatus.InProgress => "进行中",
        MedicalCaseStatus.Completed => "已完成",
        MedicalCaseStatus.Cancelled => "已取消",
        _ => status.ToString()
    };
}

public static class DecocteMethodExtensions
{
    public static string ToChinese(this DecocteMethod method) => method switch
    {
        DecocteMethod.Default => "常规",
        DecocteMethod.FirstDecoct => "先煎",
        DecocteMethod.LaterDecoct => "后下",
        DecocteMethod.WrapDecoct => "包煎",
        DecocteMethod.SeparateDecoct => "另煎",
        DecocteMethod.Dissolve => "烊化",
        DecocteMethod.Swallow => "冲服",
        _ => method.ToString()
    };
}

// ... 其他枚举扩展方法
```

**目标目录结构:**
```
LYBT.Shared.Models/Extensions/
├── EnumExtensions.cs           # 所有枚举的ToChinese()扩展方法
└── (现有扩展方法文件...)
```

**UI绑定示例:**
```xml
<!-- XAML中使用 -->
<TextBlock Text="{Binding Gender, Converter={StaticResource EnumToChineseConverter}}" />

<!-- 或在ViewModel中直接调用 -->
public string GenderDisplay => Patient.Gender.ToChinese();
```

### 4.3 枚举JSON序列化规范

**设计决策: 统一使用字符串方式传递枚举**

| 方式 | 示例 | 优点 | 缺点 |
|------|------|------|------|
| 整数(int) | `{"gender": 0}` | 传输小、性能高 | 不可读、值变更风险 |
| 字符串(string) | `{"gender": "Male"}` | 可读性强、调试友好 | 传输略大 |

**选择字符串方式的原因:**
1. API调试时可直接看懂枚举值
2. 前后端解耦，枚举值顺序变更不影响已有数据
3. Swagger文档更清晰
4. 符合RESTful API最佳实践

**业界调研结论 (2024-2025):**

根据Stack Overflow、Medium、Code Maze等技术社区的调研，字符串序列化是.NET REST API的主流做法：

| 来源 | 结论 |
|------|------|
| Stack Overflow | 80%+的回答推荐`JsonStringEnumConverter`全局配置 |
| Jason Watmore Blog | .NET 7/8官方推荐字符串序列化 |
| Code Maze | 推荐全局配置，避免每个枚举单独标注 |
| Zalando RESTful Guidelines | 企业级API设计规范推荐字符串枚举 |
| Medium (Ted Spence) | 字符串枚举提高API可维护性和可读性 |

**主流配置方式:**
```csharp
// ✅ 推荐：全局配置（一处配置，全局生效）
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ❌ 不推荐：每个枚举单独标注（冗余、难维护）
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Status { ... }
```

**当前配置现状 (需清理冗余):**
```csharp
// Server: WebAPI/Extensions/ServiceCollectionExtensions.cs:152
options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

// Client: ApiService.cs:68
_jsonOptions.Converters.Add(new JsonStringEnumConverter());

// 枚举定义 (冗余! 全局已配置)
[JsonConverter(typeof(JsonStringEnumConverter))]  // 可删除
public enum Gender { ... }
```

**目标配置 (统一规范):**
```csharp
// 1. Server端全局配置 (保留)
// WebAPI/Extensions/ServiceCollectionExtensions.cs
options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

// 2. Client端全局配置 (保留)
// ApiService.cs
_jsonOptions.Converters.Add(new JsonStringEnumConverter());

// 3. 枚举定义 (删除冗余属性)
public enum Gender  // 无需[JsonConverter]属性
{
    [Description("未知")]
    Unknown = 0,

    [Description("男")]
    Male = 1,

    [Description("女")]
    Female = 2
}
```

**序列化示例:**
```json
// 请求
POST /api/patients
{
    "name": "张三",
    "gender": "Male",           // 字符串枚举
    "status": "Active"          // 字符串枚举
}

// 响应
{
    "id": "...",
    "gender": "Male",
    "caseStatus": "Draft",
    "decocteMethod": "FirstDecoct"
}
```

### 4.4 迁移策略

**Phase 1: 合并重复枚举**
1. 创建统一的`ErrorEnums.cs`，合并所有Error相关枚举
2. 删除重复定义
3. 更新所有引用

**Phase 2: 迁移分散枚举**
1. 将`MedicalCaseUpdateMode`从DTO移至`MedicalCaseEnums.cs`
2. 将`BusinessOperation`移至新建`ValidationEnums.cs`
3. 将`PasswordStrength`移至新建`SecurityEnums.cs`

**Phase 3: Client层清理**
1. Client层`ErrorSeverity`改为使用Shared层定义
2. 保留Client专用枚举不变

## 5. 已知问题

> **[BUG] 医案保存Unit字段验证失败(HTTP 400)**
> - 根因: `HerbDto.Unit`默认值"克"与数据库"g"不一致
> - 状态: 调查中，待修复
> - 此提案不涉及此bug修复

## 6. 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 命名空间变更导致编译错误 | 高 | 分阶段迁移，每阶段验证编译 |
| JSON序列化兼容性 | 中 | 保持枚举值不变，仅移动位置 |
| 遗漏引用更新 | 中 | 使用IDE重构功能，全局搜索验证 |

## 7. 验收标准

- [ ] 所有重复枚举消除，每个枚举只有一处定义
- [ ] 共享枚举统一在`LYBT.Shared.Models/Enums/`目录
- [ ] Client/Server专用枚举保留在各自层级
- [ ] 所有共享枚举都有`ToChinese()`扩展方法
- [ ] UI显示使用中文而非英文枚举值
- [ ] 编译通过，无警告
- [ ] 单元测试全部通过
- [ ] API序列化/反序列化兼容

## 8. 相关文档

- [项目架构文档](../../../docs/architecture.md)
- [命名规范](../../../docs/coding-standards.md)
