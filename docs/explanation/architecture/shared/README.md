# 共享架构指南

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-15  
**对应代码层**：LYBT.Shared  

## 🏗️ 共享架构设计

凌隐宝堂中医诊所共享架构是连接Server端和Client端的桥梁，提供跨层共享的基础设施、数据模型、业务接口和技术标准。

```
LYBT.Shared (共享层)
├── Models/             # 数据模型和实体
├── Interfaces/         # 业务接口定义
├── Infrastructure/     # 基础设施组件
├── Utilities/          # 工具类和扩展
├── Constants/          # 常量定义
└── Enums/             # 枚举类型
```

## 📐 核心组件详解

### 1. Models - 数据模型层

> **⚠️ 架构说明**：当前MVP阶段，Models采用**按业务模块组织DTO**结构，不使用平坦的DTOs/目录。

**职责**：定义数据传输对象、枚举、常量、异常类、扩展方法

**实际目录结构**（src/Shared/LYBT.Shared.Models/）：

```
Common/              # 通用DTO和基类
  ├── BatchIdsDto.cs           # 批量ID操作DTO
  ├── EnumItem.cs              # 枚举项DTO
  ├── PagedResult.cs           # 分页结果
  └── StatusDto.cs             # 状态DTO基类

Constants/           # 常量定义
  ├── ErrorMessageKeys.cs      # 错误消息键
  └── ValidationConstants.cs   # 验证常量

Contracts/           # DTO按业务模块组织（核心架构）
  ├── Auth/                    # 认证模块DTOs
  ├── Consultation/            # 诊断模块DTOs
  ├── Patients/                # 患者模块DTOs
  │   ├── PatientDtos.cs              # PatientDto, PatientDetailDto
  │   ├── PatientOperationDtos.cs     # 操作相关DTOs
  │   └── PatientStatisticsDtos.cs    # 统计相关DTOs
  ├── Prescriptions/           # 处方模块DTOs
  ├── MedicalCase/             # 病案模块DTOs
  └── ...

Core/                # 核心基类
  └── BaseAuthSession.cs       # 认证会话基类

Enums/               # 枚举定义
  ├── Gender.cs                # 性别枚举
  ├── MedicalCaseEnums.cs      # 病案相关枚举（Status, Type等）
  ├── UserRole.cs              # 用户角色
  ├── PrescriptionStatus.cs    # 处方状态
  └── ...（共9个枚举文件）

Exceptions/          # 异常类定义
  └── BusinessException.cs     # 业务异常基类

Extensions/          # 扩展方法
  ├── Application/             # 应用初始化扩展
  └── ServiceCollection/       # 服务集合扩展
```

**设计原则**：
- ✅ **按业务模块组织**：Contracts/Patients/而不是平坦的DTOs/目录
- ✅ **按功能分组**：PatientDtos.cs（基础）、PatientOperationDtos.cs（操作）、PatientStatisticsDtos.cs（统计）
- ✅ **清晰的命名空间**：`LYBT.Shared.Models.Contracts.Patients`
- ✅ **避免过度拆分**：相关DTOs放在同一个文件中（如PatientDto和PatientDetailDto）

**实际代码示例**：

```csharp
// Contracts/Patients/PatientDtos.cs
namespace LYBT.Shared.Models.Contracts.Patients
{
    /// &lt;summary&gt;
    /// 患者信息DTO - UltraThink v2.0简化版
    /// &lt;/summary&gt;
    public class PatientDto : StatusDto
    {
        public string Name { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        
        /// &lt;summary&gt;年龄（基于出生日期的计算属性）&lt;/summary&gt;
        public int? Age
        {
            get
            {
                if (BirthDate == null) return null;
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date &gt; today.AddYears(-age)) age--;
                return age;
            }
        }
    }
}

// Common/PagedResult.cs - 通用分页结果
public class PagedResult&lt;T&gt;
{
    public List&lt;T&gt; Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages =&gt; (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage =&gt; CurrentPage &lt; TotalPages;
    public bool HasPreviousPage =&gt; CurrentPage &gt; 1;
}

// Enums/Gender.cs - 枚举定义
public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2
}
```

**关键差异说明**：
- ❌ **文档描述**：Entities/, DTOs/, Requests/, Responses/, ViewModels/（平坦结构）
- ✅ **实际实现**：Contracts/{Module}/（按业务模块组织）+ Common/（通用）+ Constants/（常量）+ Enums/（枚举）
- **原因**：实际架构更符合MVP原则（够用即好），避免过度分层

### 2. Interfaces - 接口定义层

> **⚠️ 项目状态**：当前Shared.Interfaces项目为**空项目**（0个源文件），这是**有意的设计决策**。

**空项目原因**（MVP架构原则）：

当前v5.0架构采用**去中心化接口定义**模式，每个端定义自己的接口：

```
Server端接口定义：
  src/Server/Core/LYBT.Server.Core.Interfaces/
    ├── Services/         # 业务服务接口（IPatientService等）
    ├── Repositories/     # 仓储接口（IPatientRepository等）
    └── Common/           # 通用接口

Client端接口定义：
  src/Client/Shared/LYBT.Client.Shared.Interfaces/
    ├── Services/         # 客户端服务接口
    └── ViewModels/       # ViewModel接口

Shared.Interfaces留空：
  src/Shared/LYBT.Shared.Interfaces/
    └── (empty - 仅保留项目结构)
```

**设计优势**：
- ✅ **避免过早抽象**：Server和Client的接口需求不同，不强制共享
- ✅ **依赖方向清晰**：Server依赖Server.Core.Interfaces，Client依赖Client.Shared.Interfaces
- ✅ **职责明确**：每个端管理自己的接口定义
- ✅ **符合MVP原则**：只在真正需要跨端共享接口时才引入到Shared.Interfaces

**演进触发条件**（参见ADR-005）：
- 出现真正需要跨端共享的接口（如通用验证接口IValidationService）
- 达到接口共享阈值（>5个跨端接口）

**当前结论**：Shared.Interfaces空项目是**正确的架构选择**，不是遗漏或Bug。

### 3. Components - 跨端组件层

> **⚠️ 项目说明**：当前项目名称为**LYBT.Shared.Components**（不是Infrastructure），包含少量跨端共享组件。

**职责**：提供Desktop/Avalonia跨端共享的业务组件（当前专注于中药相关功能）

**实际目录结构**（src/Shared/LYBT.Shared.Components/）：

```
Components/
  ├── HerbCalculatorBase.cs       # 中药计算基类
  ├── HerbValidatorBase.cs        # 中药验证基类
  └── IHerbItem.cs                # 中药项接口
```

**组件说明**：

1. **HerbCalculatorBase** - 中药剂量计算抽象基类
   - 提供中药配方的剂量计算逻辑
   - 支持Desktop和Avalonia端共享

2. **HerbValidatorBase** - 中药验证抽象基类
   - 提供中药配伍禁忌验证
   - 支持Desktop和Avalonia端共享

3. **IHerbItem** - 中药项通用接口
   - 定义中药项的基本属性
   - 支持Desktop和Avalonia端共享

**设计原则**（MVP阶段）：
- ✅ **仅包含真正需要跨端共享的组件**（当前仅3个中药相关组件）
- ✅ **避免过度抽象**：不预先创建可能用不上的Infrastructure组件
- ✅ **按需演进**：当出现新的跨端共享需求时再添加新组件
- ❌ **不创建空目录**：没有Data/, Caching/, Logging/等未使用的目录

**实际代码示例**：

```csharp
// Components/IHerbItem.cs - 中药项接口
namespace LYBT.Shared.Components
{
    /// &lt;summary&gt;
    /// 中药项通用接口 - Desktop/Avalonia跨端共享
    /// &lt;/summary&gt;
    public interface IHerbItem
    {
        string Name { get; set; }           // 中药名称
        decimal Dosage { get; set; }        // 剂量（克）
        string Unit { get; set; }           // 单位
    }
}

// Components/HerbCalculatorBase.cs - 中药计算基类
public abstract class HerbCalculatorBase
{
    /// &lt;summary&gt;
    /// 计算处方总剂量
    /// &lt;/summary&gt;
    public abstract decimal CalculateTotalDosage(IEnumerable&lt;IHerbItem&gt; herbs);
    
    /// &lt;summary&gt;
    /// 计算单味药占比
    /// &lt;/summary&gt;
    public abstract decimal CalculateProportion(IHerbItem herb, IEnumerable&lt;IHerbItem&gt; herbs);
}
```

**关键差异说明**：
- ❌ **文档描述**：Infrastructure/（Data/, Caching/, Logging/, Security/, Validation/）
- ✅ **实际实现**：Components/（仅3个中药相关组件）
- **原因**：MVP阶段避免过早抽象，仅实现真正需要的跨端共享功能

**演进触发条件**（参见ADR-005）：
- 出现更多跨端共享需求（>5个组件）
- 需要通用的Data/Caching/Logging组件时
- 当前Components/可能演进为Infrastructure/的子目录之一

### 4. Utilities - 工具类层

> **⚠️ 项目说明**：当前Utilities包含**少量跨端共享的工具类**，主要是启动初始化和缓存扩展。

**实际目录结构**（src/Shared/LYBT.Shared.Utilities/）：

```
Utilities/
  ├── Configuration/            # 配置相关（空目录，保留结构）
  ├── Extensions/               # 扩展方法
  │   ├── Application/
  │   │   └── ApplicationInitializationExtensions.cs  # 应用启动初始化扩展
  │   └── ServiceCollection/
  │       └── CacheExtensions.cs                      # 缓存服务注册扩展
  ├── Helpers/                  # 辅助类（空目录，保留结构）
  └── Security/                 # 安全相关（空目录，保留结构）
```

**现有工具类**（仅2个）：

**4.1 ApplicationInitializationExtensions.cs** - 应用启动初始化扩展：
```csharp
// 用途：提供应用启动时的初始化扩展方法
// 位置：Extensions/Application/ApplicationInitializationExtensions.cs
public static class ApplicationInitializationExtensions
{
    // 初始化应用（具体实现见代码）
}
```

**4.2 CacheExtensions.cs** - 缓存服务注册扩展：
```csharp
// 用途：提供IServiceCollection的缓存服务注册扩展
// 位置：Extensions/ServiceCollection/CacheExtensions.cs
public static class CacheExtensions
{
    public static IServiceCollection AddMemoryCacheServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        return services;
    }
}
```

**设计原则**（MVP阶段）：

- ✅ **仅包含真正需要跨端共享的工具类**（当前仅2个扩展类）
- ✅ **避免过度设计**：不预先创建StringExtensions、DateTimeExtensions等可能用不上的工具类
- ✅ **按需添加**：未来如有真正需要跨端共享的工具方法，再添加对应类

**注意事项**：

1. **空目录保留原因**：Configuration/、Helpers/、Security/目录当前为空，但保留目录结构以便未来扩展
2. **工具类最小化**：当前仅ApplicationInitializationExtensions和CacheExtensions，符合"够用即好"原则
3. **端特定工具类**：
   - Server端特定工具类 → 放在Server端项目
   - Client端特定工具类 → 放在Client端项目
   - 仅真正跨端共享的 → 放在Shared.Utilities

**演进触发条件**（参见ADR-005）：
- 当出现3个以上端都需要使用的相同工具方法时 → 提取到Shared.Utilities
- 当前MVP阶段不主动创建"可能未来会用到"的工具类

### 5. Constants - 常量定义层

> **⚠️ 项目说明**：当前Constants包含**少量验证和错误消息相关的常量**，不包含文档中描述的SystemConstants和BusinessConstants。

**职责**：定义验证规则常量、错误消息键

**实际目录结构**（src/Shared/LYBT.Shared.Models/Constants/）：

```
Constants/
  ├── ErrorMessageKeys.cs      # 错误消息键定义
  └── ValidationConstants.cs   # 验证常量定义
```

**实际代码示例**：

**5.1 ValidationConstants.cs** - 验证规则常量：
```csharp
// 用途：定义统一的验证规则常量
// 位置：Constants/ValidationConstants.cs
namespace LYBT.Shared.Models.Constants
{
    public static class ValidationConstants
    {
        // 患者验证
        public const int PATIENT_NAME_MAX_LENGTH = 50;
        public const int PATIENT_PHONE_LENGTH = 11;
        public const int PATIENT_IDCARD_LENGTH = 18;
        
        // 处方验证
        public const int PRESCRIPTION_NAME_MAX_LENGTH = 100;
        public const decimal MIN_HERB_DOSAGE = 0.1m;
        public const decimal MAX_HERB_DOSAGE = 1000m;
        
        // 分页验证
        public const int MIN_PAGE_SIZE = 1;
        public const int MAX_PAGE_SIZE = 100;
        public const int DEFAULT_PAGE_SIZE = 20;
    }
}
```

**5.2 ErrorMessageKeys.cs** - 错误消息键：
```csharp
// 用途：定义统一的错误消息键（用于国际化）
// 位置：Constants/ErrorMessageKeys.cs
namespace LYBT.Shared.Models.Constants
{
    public static class ErrorMessageKeys
    {
        // 通用错误
        public const string VALIDATION_FAILED = "validation.failed";
        public const string NOT_FOUND = "not.found";
        public const string UNAUTHORIZED = "unauthorized";
        
        // 患者相关
        public const string PATIENT_NAME_REQUIRED = "patient.name.required";
        public const string PATIENT_PHONE_INVALID = "patient.phone.invalid";
        
        // 处方相关
        public const string PRESCRIPTION_EMPTY = "prescription.empty";
        public const string HERB_DOSAGE_INVALID = "herb.dosage.invalid";
    }
}
```

**设计原则**（MVP阶段）：
- ✅ **仅包含真正需要的常量**：验证规则和错误消息键
- ✅ **避免过度设计**：不预先创建SystemConstants、BusinessConstants等大而全的常量类
- ✅ **按需添加**：未来如需其他常量类型，再添加对应文件

**注意事项**：
1. **业务枚举值**：使用Enums/目录定义（如Gender、MedicalCaseStatus等），不使用字符串常量
2. **配置值**：使用appsettings.json或环境变量，不硬编码在Constants中
3. **最小化原则**：当前仅2个常量文件，符合MVP"够用即好"原则

### 6. Enums - 枚举类型层
**职责**：定义业务枚举类型、系统枚举类型

**代码示例**：
```csharp
// Enums/Gender.cs
public enum Gender
{
    [Description("男")]
    Male = 1,
    
    [Description("女")]
    Female = 2
}

// Enums/MedicalCaseStatus.cs
public enum MedicalCaseStatus
{
    [Description("新建")]
    New = 1,
    
    [Description("进行中")]
    InProgress = 2,
    
    [Description("已完成")]
    Completed = 3,
    
    [Description("已取消")]
    Cancelled = 4
}

// Enums/PrescriptionStatus.cs
public enum PrescriptionStatus
{
    [Description("草稿")]
    Draft = 1,
    
    [Description("已确认")]
    Confirmed = 2,
    
    [Description("已发药")]
    Dispensed = 3,
    
    [Description("已完成")]
    Completed = 4,
    
    [Description("已取消")]
    Cancelled = 5
}

// Enums/ConsultationType.cs
public enum ConsultationType
{
    [Description("初诊")]
    FirstVisit = 1,
    
    [Description("复诊")]
    FollowUp = 2,
    
    [Description("急诊")]
    Emergency = 3
}

// Enums/PaymentMethod.cs
public enum PaymentMethod
{
    [Description("现金")]
    Cash = 1,
    
    [Description("银行卡")]
    CreditCard = 2,
    
    [Description("支付宝")]
    Alipay = 3,
    
    [Description("微信支付")]
    WechatPay = 4,
    
    [Description("医保")]
    Insurance = 5
}

// Enums/Permission.cs
public enum Permission
{
    [Description("患者管理")]
    PatientManage = 1,
    
    [Description("医案管理")]
    MedicalCaseManage = 2,
    
    [Description("诊疗管理")]
    ConsultationManage = 3,
    
    [Description("处方管理")]
    PrescriptionManage = 4,
    
    [Description("药材管理")]
    HerbManage = 5,
    
    [Description("验方管理")]
    FormulaManage = 6,
    
    [Description("用户管理")]
    UserManage = 7,
    
    [Description("系统管理")]
    SystemManage = 8
}
```

## 🔧 跨层数据传输

### 1. 统一响应格式
```csharp
// Models/Responses/ApiResult.cs
public class ApiResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public int Code { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> Errors { get; set; }
    
    public static ApiResult<T> Success(T data, string message = "操作成功")
    {
        return new ApiResult<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Code = 200,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public static ApiResult<T> Error(string message, int code = 400, List<string> errors = null)
    {
        return new ApiResult<T>
        {
            Success = false,
            Message = message,
            Code = code,
            Timestamp = DateTime.UtcNow,
            Errors = errors ?? new List<string>()
        };
    }
    
    public static ApiResult<T> ValidationError(List<ValidationError> validationErrors)
    {
        var errors = validationErrors.Select(e => e.ErrorMessage).ToList();
        return new ApiResult<T>
        {
            Success = false,
            Message = "数据验证失败",
            Code = 422,
            Timestamp = DateTime.UtcNow,
            Errors = errors
        };
    }
}
```

### 2. 分页响应格式
```csharp
// Models/Responses/PagedResult.cs
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    
    public static PagedResult<T> Create(IEnumerable<T> data, int pageIndex, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        
        return new PagedResult<T>
        {
            Data = data,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = pageIndex > 1,
            HasNextPage = pageIndex < totalPages
        };
    }
}
```

### 3. 统一异常处理
```csharp
// Infrastructure/Exceptions/BusinessException.cs
public class BusinessException : Exception
{
    public int ErrorCode { get; }
    public string ErrorDetails { get; }
    
    public BusinessException(string message, int errorCode = 400, string errorDetails = null)
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }
    
    public BusinessException(string message, Exception innerException, int errorCode = 400, string errorDetails = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }
}

// Infrastructure/Exceptions/ValidationException.cs
public class ValidationException : BusinessException
{
    public List<ValidationError> ValidationErrors { get; }
    
    public ValidationException(List<ValidationError> validationErrors)
        : base("数据验证失败", 422)
    {
        ValidationErrors = validationErrors;
    }
}

// Infrastructure/Exceptions/NotFoundException.cs
public class NotFoundException : BusinessException
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id {id} not found", 404)
    {
    }
    
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}
```

## 🔗 技术决策记录 (ADR)

### ADR-001: 使用FluentValidation进行数据验证
**状态**: 已接受  
**日期**: 2025-10-15  

**决策**：使用FluentValidation库进行数据验证，而不是DataAnnotations。

**理由**：
- 更灵活的验证规则定义
- 更好的性能
- 支持复杂的验证逻辑
- 更清晰的错误消息

**后果**：
- 需要额外的依赖
- 验证规则需要单独维护
- 学习成本较高

### ADR-002: 使用AutoMapper进行对象映射
**状态**: 已接受  
**日期**: 2025-10-15  

**决策**：使用AutoMapper库进行对象映射，而不是手动映射。

**理由**：
- 减少样板代码
- 提高开发效率
- 减少映射错误
- 支持复杂映射逻辑

**后果**：
- 运行时性能开销
- 需要配置映射规则
- 调试复杂度增加

### ADR-003: 使用MediatR实现命令查询分离
**状态**: 已拒绝  
**日期**: 2025-10-15  

**决策**：不使用MediatR，保持传统的服务层架构。

**理由**：
- 项目规模相对较小
- 避免过度设计
- 减少学习成本
- 保持代码简洁

**后果**：
- 代码耦合度可能较高
- 扩展性受限
- 测试复杂度较高

## 📋 最佳实践

### 1. 命名约定
- **接口**: 以I开头，如IPatientService
- **实现类**: 以具体名称开头，如PatientService
- **实体类**: 使用业务名词，如Patient、MedicalCase
- **DTO类**: 以Dto结尾，如PatientDto
- **请求类**: 以Request结尾，如PatientCreateRequest
- **响应类**: 以Response结尾，如PatientResponse

### 2. 代码组织
- **单一职责**: 每个类只负责一个功能
- **开闭原则**: 对扩展开放，对修改封闭
- **依赖倒置**: 依赖抽象，不依赖具体实现
- **接口隔离**: 使用小而专一的接口

### 3. 性能优化
- **延迟加载**: 使用延迟加载减少内存占用
- **缓存策略**: 合理使用缓存提高性能
- **异步编程**: I/O操作使用async/await
- **批量操作**: 减少数据库访问次数

### 4. 安全考虑
- **输入验证**: 所有输入都必须验证
- **SQL注入防护**: 使用参数化查询
- **敏感数据**: 敏感数据加密存储
- **权限控制**: 实现细粒度权限控制

## 🔗 相关文档

- **[架构总览](../README.md)** - 三层对齐架构设计原理
- **[Server端架构](../server/README.md)** - 服务端三层架构实现
- **[Client端架构](../client/README.md)** - WPF五层架构实现
- **[共享开发指南](../../development/shared/README.md)** - 共享层开发规范
- **[模块设计指南](../module-design-guide.md)** - 业务模块化设计标准

---

**文档维护**：架构组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核