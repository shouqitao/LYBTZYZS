# LYBT.Shared

> **前后端共享组件**  
> 统一的接口、模型和工具类库

## 🎯 项目概述

LYBT.Shared 是系统的共享组件库，提供前后端统一的数据传输对象(DTO)、服务接口和通用工具类，确保前后端数据结构和业务逻辑的一致性。

## 📦 项目结构

### LYBT.Shared.Models
数据传输对象和响应模型

```
LYBT.Shared.Models/
├── Common/                    # 通用模型
│   ├── ApiResponse.cs         # 统一API响应格式
│   ├── PagedResult.cs          # 分页数据模型
│   ├── BaseDto.cs            # DTO基类
│   └── ServiceResult.cs      # 服务结果模型
├── Contracts/                # 业务契约模型
│   ├── Auth/                 # 认证相关DTO
│   ├── Users/                # 用户相关DTO
│   ├── Patients/             # 患者相关DTO
│   ├── MedicalCase/          # 医疗案例DTO
│   ├── Consultation/         # 看诊相关DTO
│   ├── Prescriptions/        # 处方相关DTO
│   ├── Herbs/                # 中药材DTO
│   └── Formula/              # 验方相关DTO
└── Enums/                    # 枚举定义
    ├── UserRole.cs           # 用户角色枚举
    ├── MedicalCaseStatus.cs  # 医疗案例状态
    ├── PrescriptionStatus.cs # 处方状态
    └── ConsultationStatus.cs # 看诊状态
```

### LYBT.Shared.Interfaces  
业务服务接口定义

```
LYBT.Shared.Interfaces/
├── Services/                 # 服务接口
│   ├── IUserService.cs       # 用户服务接口
│   ├── IPatientService.cs    # 患者服务接口
│   ├── IConsultationService.cs # 看诊服务接口
│   ├── IPrescriptionService.cs # 处方服务接口
│   ├── IHerbService.cs       # 中药材服务接口
│   └── IFormulaService.cs    # 验方服务接口
└── Repositories/             # 仓储接口
    ├── IBaseRepository.cs    # 基础仓储接口
    └── [各业务仓储接口]
```

### LYBT.Shared.Utilities
通用工具类和扩展方法

```
LYBT.Shared.Utilities/
├── Extensions/               # 扩展方法
├── Helpers/                  # 帮助类
├── Validators/               # 数据验证器
└── Constants/                # 常量定义
```

## 🏗️ 技术特性

### 统一响应格式
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
    
    // 成功响应
    public static ApiResponse<T> Ok(T data, string message = "操作成功")
    
    // 失败响应  
    public static ApiResponse<T> Fail(string message, T? data = default)
}
```

### 分页数据模型
```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
```

### 服务结果模型
```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
    
    public static ServiceResult<T> Success(T data, string message = "")
    public static ServiceResult<T> Failure(string error)
    public static ServiceResult<T> Failure(List<string> errors)
}
```

## 🎯 核心DTO模型

### 用户相关
```csharp
// 用户信息DTO
public class UserDto : BaseDto
{
    public string UserName { get; set; }
    public string RealName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

// 用户创建DTO
public class UserCreateDto
{
    public string UserName { get; set; }
    public string RealName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public UserRole Role { get; set; }
}
```

### 患者相关
```csharp
// 患者信息DTO
public class PatientDto : BaseDto
{
    public string Name { get; set; }
    public string Gender { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string IdCardNumber { get; set; }
}
```

### 诊疗相关
```csharp
// 医疗案例DTO
public class MedicalCaseDto : BaseDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; }
    public MedicalCaseStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
}

// 看诊详情DTO
public class ConsultationDetailDto : BaseDto
{
    public Guid MedicalCaseId { get; set; }
    public string ChiefComplaint { get; set; }        // 主诉
    public string PresentIllness { get; set; }        // 现病史
    public string TCMObservation { get; set; }        // 望诊
    public string TCMAuscultation { get; set; }       // 闻诊
    public string TCMInquiry { get; set; }            // 问诊
    public string TCMPalpation { get; set; }          // 切诊
    public string Diagnosis { get; set; }             // 诊断
    public string Treatment { get; set; }             // 治疗方案
}
```

## 🔧 使用指南

### 在前端项目中使用
```csharp
// 注册服务接口实现
services.AddScoped<IUserService, UserModuleService>();

// 使用DTO进行数据传输
var createDto = new UserCreateDto 
{
    UserName = "doctor01",
    RealName = "张医生",
    Role = UserRole.Doctor
};
```

### 在后端项目中使用
```csharp
// Controller返回统一响应格式
public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([Body] UserCreateDto dto)
{
    var result = await _userService.CreateAsync(dto);
    return HandleServiceResult(result, "用户创建成功");
}
```

## 📊 设计原则

- **统一性**: 前后端使用相同的DTO模型，确保数据一致性
- **类型安全**: 强类型模型，编译时检查数据结构
- **版本控制**: 支持API版本管理和向后兼容
- **可扩展性**: 模块化设计，便于新增业务模型
- **验证友好**: 支持数据注解和FluentValidation

## 📈 性能优化

- **轻量级**: 仅包含数据传输所需属性
- **序列化优化**: JSON序列化性能优化
- **内存友好**: 避免循环引用和大对象
- **缓存友好**: 支持DTO级别的缓存策略

---

> 📌 **开发提醒**: 修改共享模型时请确保前后端同步更新，避免版本不一致问题