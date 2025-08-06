# LYBT项目前后端模型完整设计文档

## 文档概述

本文档详细描述了凌隐宝堂中医诊所管理系统(LYBTZYZS)的完整模型架构，包括后端领域模型、共享DTO契约、前端业务模型的设计思路、结构关系和映射规则。

---

## 一、模型架构总览

### 1.1 三层模型设计原则

```
数据库实体 (Entity)  ←→  API传输对象 (DTO)  ←→  前端业务模型 (Info)
      ↓                      ↓                      ↓
   EF Core映射           前后端API契约          WPF MVVM绑定
```

### 1.2 核心设计理念

1. **分离关注点**: 每一层模型专注于特定职责
2. **共享基础**: 通过Base模型实现属性复用
3. **类型安全**: 强类型约束和枚举定义
4. **版本控制**: 集中管理，便于API演进

---

## 二、后端领域模型架构

### 2.1 实体模型继承结构

```csharp
// 抽象基类 - 提供核心字段
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public DateTime? UpdateTime { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeleteTime { get; set; }
}

// 可审计实体 - 提供审计跟踪
public abstract class AuditableEntity : BaseEntity
{
    public Guid? CreateUserId { get; set; }
    public Guid? UpdateUserId { get; set; }
    public string? CreateUserName { get; set; }
    public string? UpdateUserName { get; set; }
}

// 可启用禁用实体
public abstract class ActivatableEntity : AuditableEntity
{
    public bool IsEnabled { get; set; } = true;
    public string? DisableReason { get; set; }
}
```

### 2.2 核心业务实体

#### 用户与权限模块

```csharp
// 用户实体 - src/Backend/Core/LYBT.Models/Entities/User.cs
public class User : ActivatableEntity
{
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime? LastLoginTime { get; set; }
    public string? Avatar { get; set; }
    
    // 导航属性
    public ICollection<Doctor> Doctors { get; set; } = [];
}

// 角色权限实体
public class Role : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Permission> Permissions { get; set; } = [];
}
```

#### 患者管理模块

```csharp
// 患者实体 - src/Backend/Core/LYBT.Models/Entities/Patient.cs
public class Patient : AuditableEntity
{
    public string PatientNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Unknown;
    public int Age { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? IdCard { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public BloodType BloodType { get; set; } = BloodType.Unknown;
    public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Unknown;
    public string? Occupation { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Allergies { get; set; }
    public string? Remark { get; set; }
    
    // 导航属性
    public ICollection<Registration> Registrations { get; set; } = [];
    public ICollection<Record> Records { get; set; } = [];
    public ICollection<Prescription> Prescriptions { get; set; } = [];
    public ICollection<Billing> Billings { get; set; } = [];
}
```

#### 医生管理模块

```csharp
// 医生实体 - src/Backend/Core/LYBT.Models/Entities/Doctor.cs
public class Doctor : ActivatableEntity
{
    public string DoctorNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Unknown;
    public int Age { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Specialty { get; set; }
    public string? Education { get; set; }
    public decimal RegistrationFee { get; set; } = 0;
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public DoctorStatus Status { get; set; } = DoctorStatus.Available;
    public TimeSpan StartWorkTime { get; set; } = new TimeSpan(8, 0, 0);
    public TimeSpan EndWorkTime { get; set; } = new TimeSpan(18, 0, 0);
    public string? WorkSchedule { get; set; }
    public string? Introduction { get; set; }
    public string? Avatar { get; set; }
    
    // 关联用户账户
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    
    // 导航属性
    public ICollection<Registration> Registrations { get; set; } = [];
    public ICollection<DiagnosisTreatment> DiagnosisTreatments { get; set; } = [];
    public ICollection<Prescription> Prescriptions { get; set; } = [];
}
```

#### 中药材管理模块

```csharp
// 中药材实体 - src/Backend/Core/LYBT.Models/Entities/Herb.cs
public class Herb : ActivatableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? PinYinCode { get; set; }
    public string? Origin { get; set; }
    public string? Spec { get; set; }
    public string Unit { get; set; } = "克";
    public decimal Price { get; set; }
    public int Stock { get; set; } = 0;
    public int MinStock { get; set; } = 10;
    public int MaxStock { get; set; } = 1000;
    public string? Supplier { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNumber { get; set; }
    public string? Effect { get; set; }
    public string? Usage { get; set; }
    public HerbStatus Status { get; set; } = HerbStatus.Normal;
    public string? Remark { get; set; }
    
    // 导航属性
    public ICollection<PrescriptionHerb> PrescriptionHerbs { get; set; } = [];
    public ICollection<FormulaTemplateHerb> FormulaTemplateHerbs { get; set; } = [];
}
```

#### 处方管理模块

```csharp
// 处方实体 - src/Backend/Core/LYBT.Models/Entities/Prescription.cs
public class Prescription : AuditableEntity
{
    public string PrescriptionNo { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? RecordId { get; set; }
    public string? Diagnosis { get; set; }
    public int DosageCount { get; set; } = 7;
    public decimal SingleDosePrice { get; set; } = 0;
    public decimal TotalPrice { get; set; } = 0;
    public decimal TotalWeight { get; set; } = 0;
    public string? Advice { get; set; }
    public string? FormulaSource { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    public string? Remark { get; set; }
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public Record? Record { get; set; }
    public ICollection<PrescriptionHerb> Herbs { get; set; } = [];
    public ICollection<Billing> Billings { get; set; } = [];
}

// 处方药材关联实体
public class PrescriptionHerb : BaseEntity
{
    public Guid PrescriptionId { get; set; }
    public Guid HerbId { get; set; }
    public decimal Dosage { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Remark { get; set; }
    
    // 导航属性
    public Prescription Prescription { get; set; } = null!;
    public Herb Herb { get; set; } = null!;
}
```

#### 账单管理模块

```csharp
// 账单实体 - src/Backend/Core/LYBT.Models/Entities/Billing.cs
public class Billing : AuditableEntity
{
    public string BillingNo { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid? RegistrationId { get; set; }
    public Guid? RecordId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public BillingStatus Status { get; set; } = BillingStatus.Pending;
    public string? PaymentMethod { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? CashierId { get; set; }
    public DateTime? PaidTime { get; set; }
    public DateTime? RefundTime { get; set; }
    public string? RefundReason { get; set; }
    public Guid? RefundOperatorId { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool IsInvoiced { get; set; } = false;
    public string? Remark { get; set; }
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public Registration? Registration { get; set; }
    public Record? Record { get; set; }
    public Prescription? Prescription { get; set; }
    public Doctor Doctor { get; set; } = null!;
}
```

### 2.3 枚举定义

```csharp
// 通用枚举 - src/Backend/Core/LYBT.Models/Enums/CommonEnums.cs

/// <summary>性别枚举</summary>
public enum Gender
{
    Unknown = 0,    // 未知
    Male = 1,       // 男性
    Female = 2      // 女性
}

/// <summary>血型枚举</summary>
public enum BloodType
{
    Unknown = 0,    // 未知
    A = 1,          // A型
    B = 2,          // B型
    AB = 3,         // AB型
    O = 4           // O型
}

/// <summary>婚姻状况枚举</summary>
public enum MaritalStatus
{
    Unknown = 0,    // 未知
    Single = 1,     // 单身
    Married = 2,    // 已婚
    Divorced = 3,   // 离异
    Widowed = 4     // 丧偶
}

/// <summary>用户角色枚举</summary>
public enum UserRole
{
    User = 0,           // 普通用户
    Doctor = 1,         // 医生
    Nurse = 2,          // 护士
    Cashier = 3,        // 收费员
    Pharmacist = 4,     // 药师
    Admin = 999         // 系统管理员
}

/// <summary>医生状态枚举</summary>
public enum DoctorStatus
{
    Available = 0,      // 可预约
    Busy = 1,          // 忙碌中
    OnLeave = 2,       // 请假
    Offline = 3        // 离线
}

/// <summary>中药材状态枚举</summary>
public enum HerbStatus
{
    Normal = 0,        // 正常
    LowStock = 1,      // 库存不足
    OutOfStock = 2,    // 缺货
    Expired = 3,       // 过期
    Discontinued = 4   // 停用
}

/// <summary>处方状态枚举</summary>
public enum PrescriptionStatus
{
    Draft = 0,         // 草稿
    Confirmed = 1,     // 已确认
    Dispensing = 2,    // 配药中
    Completed = 3,     // 已完成
    Cancelled = 4      // 已取消
}

/// <summary>账单状态枚举</summary>
public enum BillingStatus
{
    Pending = 0,       // 待付款
    Paid = 1,         // 已付款
    PartiallyPaid = 2, // 部分付款
    Refunded = -1,     // 已退款
    Cancelled = -2     // 已取消
}
```

---

## 三、共享DTO契约模型

### 3.1 DTO设计原则

1. **API契约**: 定义前后端通信接口
2. **数据验证**: 包含完整的验证特性
3. **版本控制**: 支持API版本演进
4. **类型安全**: 强类型约束

### 3.2 DTO命名规范

| DTO类型 | 命名模式 | 用途说明 | 示例 |
|---------|----------|----------|------|
| 基础展示 | `{Entity}Dto` | 列表展示和基础信息 | `PatientDto` |
| 创建请求 | `{Entity}CreateDto` | 新增记录请求 | `PatientCreateDto` |
| 更新请求 | `{Entity}UpdateDto` | 修改记录请求 | `PatientUpdateDto` |
| 编辑请求 | `{Entity}EditDto` | 编辑操作请求 | `BillingEditDto` |
| 详细信息 | `{Entity}DetailDto` | 详情页面展示 | `HerbDetailDto` |
| 查询条件 | `{Entity}QueryDto` | 查询参数 | `UserQueryDto` |
| 分页查询 | `{Entity}PagedQueryDto` | 分页查询参数 | `PatientPagedQueryDto` |
| 导入数据 | `{Entity}ImportDto` | 数据导入 | `HerbImportDto` |
| 统计信息 | `{Entity}StatisticsDto` | 统计分析 | `RecordStatisticsDto` |

### 3.3 DTO基础结构

```csharp
// DTO基类 - src/Shared/LYBT.Shared.Models/Core/BaseDto.cs
public abstract class BaseDto
{
    [DisplayName("唯一标识")]
    public Guid Id { get; set; }
    
    [DisplayName("创建时间")]
    public DateTime CreateTime { get; set; }
    
    [DisplayName("更新时间")]
    public DateTime? UpdateTime { get; set; }
}

// 可审计DTO基类
public abstract class AuditableDto : BaseDto
{
    [DisplayName("创建人")]
    public string? CreateUserName { get; set; }
    
    [DisplayName("更新人")]
    public string? UpdateUserName { get; set; }
}
```

### 3.4 核心业务DTO

#### 患者DTO

```csharp
// 患者基础DTO - src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientDto.cs
public class PatientDto : AuditableDto
{
    [DisplayName("患者编号")]
    public string PatientNo { get; set; } = string.Empty;
    
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;
    
    [DisplayName("性别")]
    public Gender Gender { get; set; }
    
    [DisplayName("年龄")]
    public int Age { get; set; }
    
    [DisplayName("联系电话")]
    public string? PhoneNumber { get; set; }
    
    [DisplayName("地址")]
    public string? Address { get; set; }
}

// 患者创建DTO
public class PatientCreateDto
{
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "性别不能为空")]
    [DisplayName("性别")]
    public Gender Gender { get; set; } = Gender.Unknown;
    
    [Range(0, 150, ErrorMessage = "年龄必须在0-150之间")]
    [DisplayName("年龄")]
    public int Age { get; set; }
    
    [StringLength(11, ErrorMessage = "手机号码长度不正确")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号码格式不正确")]
    [DisplayName("联系电话")]
    public string? PhoneNumber { get; set; }
    
    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    [DisplayName("地址")]
    public string? Address { get; set; }
    
    [StringLength(500, ErrorMessage = "病史长度不能超过500个字符")]
    [DisplayName("病史")]
    public string? MedicalHistory { get; set; }
    
    [StringLength(200, ErrorMessage = "过敏史长度不能超过200个字符")]
    [DisplayName("过敏史")]
    public string? Allergies { get; set; }
}

// 患者分页查询DTO
public class PatientPagedQueryDto : PaginationRequest
{
    [DisplayName("姓名关键词")]
    public string? Name { get; set; }
    
    [DisplayName("性别")]
    public Gender? Gender { get; set; }
    
    [DisplayName("最小年龄")]
    [Range(0, 150)]
    public int? MinAge { get; set; }
    
    [DisplayName("最大年龄")]
    [Range(0, 150)]
    public int? MaxAge { get; set; }
    
    [DisplayName("电话号码")]
    public string? PhoneNumber { get; set; }
    
    [DisplayName("开始时间")]
    public DateTime? StartDate { get; set; }
    
    [DisplayName("结束时间")]
    public DateTime? EndDate { get; set; }
}
```

#### 中药材DTO

```csharp
// 中药材基础DTO - src/Shared/LYBT.Shared.Models/Contracts/Herbs/HerbDto.cs
public class HerbDto : AuditableDto
{
    [DisplayName("药材名称")]
    public string Name { get; set; } = string.Empty;
    
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }
    
    [DisplayName("单位")]
    public string Unit { get; set; } = string.Empty;
    
    [DisplayName("单价")]
    public decimal Price { get; set; }
    
    [DisplayName("库存")]
    public int Stock { get; set; }
    
    [DisplayName("状态")]
    public HerbStatus Status { get; set; }
    
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; }
}

// 中药材创建DTO
public class HerbCreateDto
{
    [Required(ErrorMessage = "药材名称不能为空")]
    [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
    [DisplayName("药材名称")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }
    
    [StringLength(50, ErrorMessage = "产地长度不能超过50个字符")]
    [DisplayName("产地")]
    public string? Origin { get; set; }
    
    [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
    [DisplayName("规格")]
    public string? Spec { get; set; }
    
    [Required(ErrorMessage = "单位不能为空")]
    [StringLength(10, ErrorMessage = "单位长度不能超过10个字符")]
    [DisplayName("单位")]
    public string Unit { get; set; } = "克";
    
    [Required(ErrorMessage = "单价不能为空")]
    [Range(0.01, 99999.99, ErrorMessage = "单价必须大于0且小于100000")]
    [DisplayName("单价")]
    public decimal Price { get; set; }
    
    [Range(0, 999999, ErrorMessage = "库存必须在0-999999之间")]
    [DisplayName("初始库存")]
    public int Stock { get; set; } = 0;
}

// 批量状态更新DTO
public class BatchStatusUpdateDto
{
    [Required(ErrorMessage = "ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一项")]
    [DisplayName("ID列表")]
    public List<Guid> Ids { get; set; } = new();
    
    [DisplayName("状态")]
    public int Status { get; set; }
    
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; }
    
    [StringLength(200, ErrorMessage = "原因长度不能超过200个字符")]
    [DisplayName("操作原因")]
    public string? Reason { get; set; }
}
```

### 3.5 通用DTO

```csharp
// 分页请求基类 - src/Shared/LYBT.Shared.Models/Core/PaginationRequest.cs
public class PaginationRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "当前页必须大于0")]
    [DisplayName("当前页")]
    public int CurrentPage { get; set; } = 1;
    
    [Range(1, 100, ErrorMessage = "每页数量必须在1-100之间")]
    [DisplayName("每页数量")]
    public int PageSize { get; set; } = 10;
    
    [StringLength(100, ErrorMessage = "搜索关键词长度不能超过100个字符")]
    [DisplayName("搜索关键词")]
    public string? SearchKeyword { get; set; }
    
    [DisplayName("排序字段")]
    public string? SortField { get; set; }
    
    [DisplayName("升序排列")]
    public bool SortAscending { get; set; } = true;
}

// 分页结果 - src/Shared/LYBT.Shared.Models/Core/PaginatedResult.cs
public class PaginatedResult<T>
{
    [DisplayName("数据项")]
    public IList<T> Items { get; set; } = new List<T>();
    
    [DisplayName("总数量")]
    public int TotalCount { get; set; }
    
    [DisplayName("当前页")]
    public int CurrentPage { get; set; }
    
    [DisplayName("每页数量")]
    public int PageSize { get; set; }
    
    [DisplayName("总页数")]
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    [DisplayName("是否有上一页")]
    public bool HasPreviousPage => CurrentPage > 1;
    
    [DisplayName("是否有下一页")]
    public bool HasNextPage => CurrentPage < TotalPages;
}

// API响应包装类 - src/Shared/LYBT.Shared.Models/Core/ApiResponse.cs
public class ApiResponse<T>
{
    [DisplayName("是否成功")]
    public bool Success { get; set; }
    
    [DisplayName("响应数据")]
    public T? Data { get; set; }
    
    [DisplayName("错误消息")]
    public string? Message { get; set; }
    
    [DisplayName("错误代码")]
    public string? ErrorCode { get; set; }
    
    [DisplayName("时间戳")]
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    // 静态工厂方法
    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Error(string message, string? errorCode = null) => 
        new() { Success = false, Message = message, ErrorCode = errorCode };
}
```

---

## 四、前端业务模型架构

### 4.1 前端模型设计原则

1. **MVVM支持**: 实现INotifyPropertyChanged接口
2. **UI友好**: 包含UI绑定所需的属性和方法
3. **业务扩展**: 基于共享模型扩展前端特有功能
4. **命令支持**: 提供UI交互命令

### 4.2 前端模型继承结构

```csharp
// 前端模型基类 - src/Frontend/Desktop/Core/Models/Common/BaseModel.cs
public abstract class BaseModel : BindableBase, IDisposable
{
    private bool _isSelected;
    private bool _isEnabled = true;
    private string? _statusMessage;
    
    [DisplayName("是否选中")]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    [DisplayName("是否启用")]
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
    
    [DisplayName("状态消息")]
    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    [DisplayName("唯一标识")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [DisplayName("创建时间")]
    public DateTime CreateTime { get; set; } = DateTime.Now;
    
    public virtual void Dispose() { }
}

// 可选择项包装类
public class SelectableItem<T> : BindableBase where T : class
{
    private bool _isSelected;
    private T _data;
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public T Data
    {
        get => _data;
        set => SetProperty(ref _data, value);
    }
    
    public SelectableItem(T data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }
}
```

### 4.3 核心业务模型

#### 患者信息模型

```csharp
// 患者信息模型 - src/Frontend/Desktop/Core/Models/Patients/PatientInfo.cs
public class PatientInfo : BaseModel
{
    private string _patientNo = string.Empty;
    private string _name = string.Empty;
    private Gender _gender = Gender.Unknown;
    private int _age;
    private string? _phoneNumber;
    private string? _address;
    private string? _emergencyContact;
    private string? _emergencyPhone;
    
    [DisplayName("患者编号")]
    public string PatientNo
    {
        get => _patientNo;
        set => SetProperty(ref _patientNo, value);
    }
    
    [DisplayName("姓名")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    [DisplayName("性别")]
    public Gender Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }
    
    [DisplayName("年龄")]
    public int Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }
    
    [DisplayName("联系电话")]
    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }
    
    [DisplayName("地址")]
    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }
    
    [DisplayName("紧急联系人")]
    public string? EmergencyContact
    {
        get => _emergencyContact;
        set => SetProperty(ref _emergencyContact, value);
    }
    
    [DisplayName("紧急联系电话")]
    public string? EmergencyPhone
    {
        get => _emergencyPhone;
        set => SetProperty(ref _emergencyPhone, value);
    }
    
    // 计算属性 - UI显示用
    public string GenderText => Gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };
    
    public string AgeDescription => Age > 0 ? $"{Age}岁" : "未知";
    
    public string DisplayName => $"{Name} ({GenderText}, {AgeDescription})";
    
    public string ContactInfo => !string.IsNullOrWhiteSpace(PhoneNumber) ? PhoneNumber : "无";
    
    // UI状态属性
    public bool HasEmergencyContact => !string.IsNullOrWhiteSpace(EmergencyContact);
    
    public string EmergencyContactInfo => HasEmergencyContact 
        ? $"{EmergencyContact} ({EmergencyPhone ?? "无电话"})"
        : "未设置";
}
```

#### 中药材信息模型

```csharp
// 中药材信息模型 - src/Frontend/Desktop/Core/Models/Herbs/HerbInfo.cs
public class HerbInfo : BaseModel
{
    private string _name = string.Empty;
    private string? _pinYinCode;
    private string _unit = "克";
    private decimal _price;
    private int _stock;
    private int _minStock = 10;
    private HerbStatus _status = HerbStatus.Normal;
    private string? _supplier;
    private DateTime? _expiryDate;
    
    [DisplayName("药材名称")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    [DisplayName("拼音码")]
    public string? PinYinCode
    {
        get => _pinYinCode;
        set => SetProperty(ref _pinYinCode, value);
    }
    
    [DisplayName("单位")]
    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }
    
    [DisplayName("单价")]
    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }
    
    [DisplayName("库存")]
    public int Stock
    {
        get => _stock;
        set
        {
            SetProperty(ref _stock, value);
            RaisePropertyChanged(nameof(StockLevel));
            RaisePropertyChanged(nameof(StockStatusText));
            RaisePropertyChanged(nameof(StockStatusColor));
        }
    }
    
    [DisplayName("最低库存")]
    public int MinStock
    {
        get => _minStock;
        set
        {
            SetProperty(ref _minStock, value);
            RaisePropertyChanged(nameof(StockLevel));
        }
    }
    
    [DisplayName("状态")]
    public HerbStatus Status
    {
        get => _status;
        set
        {
            SetProperty(ref _status, value);
            RaisePropertyChanged(nameof(StatusText));
            RaisePropertyChanged(nameof(StatusColor));
        }
    }
    
    [DisplayName("供应商")]
    public string? Supplier
    {
        get => _supplier;
        set => SetProperty(ref _supplier, value);
    }
    
    [DisplayName("过期时间")]
    public DateTime? ExpiryDate
    {
        get => _expiryDate;
        set
        {
            SetProperty(ref _expiryDate, value);
            RaisePropertyChanged(nameof(IsExpired));
            RaisePropertyChanged(nameof(ExpiryWarning));
        }
    }
    
    // UI计算属性
    public StockLevel StockLevel
    {
        get
        {
            if (Stock == 0) return StockLevel.OutOfStock;
            if (Stock <= MinStock) return StockLevel.Low;
            return StockLevel.Normal;
        }
    }
    
    public string StockStatusText => StockLevel switch
    {
        StockLevel.OutOfStock => "缺货",
        StockLevel.Low => "库存不足",
        _ => "正常"
    };
    
    public string StockStatusColor => StockLevel switch
    {
        StockLevel.OutOfStock => "#FF6B6B",
        StockLevel.Low => "#FFB347",
        _ => "#51CF66"
    };
    
    public string StatusText => Status switch
    {
        HerbStatus.Normal => "正常",
        HerbStatus.LowStock => "库存不足",
        HerbStatus.OutOfStock => "缺货",
        HerbStatus.Expired => "过期",
        HerbStatus.Discontinued => "停用",
        _ => "未知"
    };
    
    public string StatusColor => Status switch
    {
        HerbStatus.Normal => "#51CF66",
        HerbStatus.LowStock => "#FFB347",
        HerbStatus.OutOfStock => "#FF6B6B",
        HerbStatus.Expired => "#FF6B6B",
        HerbStatus.Discontinued => "#ADB5BD",
        _ => "#6C757D"
    };
    
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Today;
    
    public string ExpiryWarning
    {
        get
        {
            if (!ExpiryDate.HasValue) return string.Empty;
            
            var daysToExpiry = (ExpiryDate.Value - DateTime.Today).Days;
            if (daysToExpiry <= 0) return "已过期";
            if (daysToExpiry <= 30) return $"{daysToExpiry}天后过期";
            
            return string.Empty;
        }
    }
    
    public string PriceDisplay => $"¥{Price:F2}/{Unit}";
    
    public string StockDisplay => $"{Stock} {Unit}";
}

// 库存级别枚举
public enum StockLevel
{
    Normal = 0,     // 正常
    Low = 1,        // 库存不足
    OutOfStock = 2  // 缺货
}
```

#### 医生信息模型

```csharp
// 医生信息模型 - src/Frontend/Desktop/Core/Models/Doctors/DoctorInfo.cs
public class DoctorInfo : BaseModel
{
    private string _doctorNo = string.Empty;
    private string _name = string.Empty;
    private Gender _gender = Gender.Unknown;
    private string? _title;
    private string? _department;
    private string? _specialty;
    private decimal _registrationFee;
    private DoctorStatus _status = DoctorStatus.Available;
    private string? _avatar;
    
    [DisplayName("医生编号")]
    public string DoctorNo
    {
        get => _doctorNo;
        set => SetProperty(ref _doctorNo, value);
    }
    
    [DisplayName("姓名")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    [DisplayName("性别")]
    public Gender Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }
    
    [DisplayName("职称")]
    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
    
    [DisplayName("科室")]
    public string? Department
    {
        get => _department;
        set => SetProperty(ref _department, value);
    }
    
    [DisplayName("专长")]
    public string? Specialty
    {
        get => _specialty;
        set => SetProperty(ref _specialty, value);
    }
    
    [DisplayName("挂号费")]
    public decimal RegistrationFee
    {
        get => _registrationFee;
        set => SetProperty(ref _registrationFee, value);
    }
    
    [DisplayName("状态")]
    public DoctorStatus Status
    {
        get => _status;
        set
        {
            SetProperty(ref _status, value);
            RaisePropertyChanged(nameof(StatusText));
            RaisePropertyChanged(nameof(StatusColor));
            RaisePropertyChanged(nameof(IsAvailable));
        }
    }
    
    [DisplayName("头像")]
    public string? Avatar
    {
        get => _avatar;
        set => SetProperty(ref _avatar, value);
    }
    
    // UI计算属性
    public string GenderText => Gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };
    
    public string StatusText => Status switch
    {
        DoctorStatus.Available => "可预约",
        DoctorStatus.Busy => "忙碌中",
        DoctorStatus.OnLeave => "请假",
        DoctorStatus.Offline => "离线",
        _ => "未知"
    };
    
    public string StatusColor => Status switch
    {
        DoctorStatus.Available => "#51CF66",
        DoctorStatus.Busy => "#FFB347",
        DoctorStatus.OnLeave => "#FF6B6B",
        DoctorStatus.Offline => "#ADB5BD",
        _ => "#6C757D"
    };
    
    public bool IsAvailable => Status == DoctorStatus.Available && IsEnabled;
    
    public string DisplayName => !string.IsNullOrWhiteSpace(Title) ? $"{Title} {Name}" : Name;
    
    public string DepartmentAndTitle => !string.IsNullOrWhiteSpace(Department) && !string.IsNullOrWhiteSpace(Title)
        ? $"{Department} - {Title}"
        : Department ?? Title ?? "无";
    
    public string RegistrationFeeText => $"¥{RegistrationFee:F2}";
    
    public string SpecialtyText => !string.IsNullOrWhiteSpace(Specialty) ? Specialty : "暂无专长信息";
}
```

### 4.4 ViewModel基础架构

```csharp
// ViewModel基类 - src/Frontend/Desktop/Core/ViewModels/BaseViewModel.cs
public abstract class BaseViewModel : BindableBase, IDisposable
{
    private bool _isLoading;
    private bool _hasError;
    private string? _errorMessage;
    private string? _statusMessage;
    
    protected readonly IEventAggregator EventAggregator;
    protected readonly ILogger Logger;
    
    protected BaseViewModel(IEventAggregator eventAggregator, ILogger logger)
    {
        EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        ClearErrorCommand = new DelegateCommand(ClearError);
    }
    
    [DisplayName("加载中")]
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    [DisplayName("有错误")]
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }
    
    [DisplayName("错误消息")]
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
    
    [DisplayName("状态消息")]
    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    [DisplayName("清除错误命令")]
    public DelegateCommand ClearErrorCommand { get; }
    
    // 错误处理
    protected virtual void HandleError(Exception exception)
    {
        Logger.LogError(exception, "ViewModel错误: {Message}", exception.Message);
        
        HasError = true;
        ErrorMessage = exception.Message;
        IsLoading = false;
    }
    
    protected virtual void ClearError()
    {
        HasError = false;
        ErrorMessage = null;
    }
    
    // 状态管理
    protected virtual void SetSuccess(string message)
    {
        HasError = false;
        ErrorMessage = null;
        StatusMessage = message;
        IsLoading = false;
    }
    
    protected virtual void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
        IsLoading = false;
    }
    
    // 异步初始化支持
    public virtual async Task InitializeAsync()
    {
        // 子类可重写此方法进行异步初始化
        await Task.CompletedTask;
    }
    
    public virtual void Dispose()
    {
        // 子类可重写进行资源清理
    }
}

// 列表ViewModel基类 - src/Frontend/Desktop/Core/ViewModels/BaseListViewModel.cs
public abstract class BaseListViewModel<T> : BaseViewModel where T : class
{
    private ObservableCollection<SelectableItem<T>> _items = new();
    private SelectableItem<T>? _selectedItem;
    private string? _searchKeyword;
    private int _currentPage = 1;
    private int _pageSize = 20;
    private int _totalCount;
    private bool _isAllSelected;
    
    protected BaseListViewModel(IEventAggregator eventAggregator, ILogger logger) 
        : base(eventAggregator, logger)
    {
        RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsLoading);
        SearchCommand = new DelegateCommand(async () => await SearchAsync(), () => !IsLoading);
        SelectAllCommand = new DelegateCommand(SelectAll);
        ClearSelectionCommand = new DelegateCommand(ClearSelection);
        PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), CanGoPreviousPage);
        NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), CanGoNextPage);
    }
    
    // 数据集合
    [DisplayName("数据项")]
    public ObservableCollection<SelectableItem<T>> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }
    
    [DisplayName("选中项")]
    public SelectableItem<T>? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }
    
    // 搜索和分页
    [DisplayName("搜索关键词")]
    public string? SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }
    
    [DisplayName("当前页")]
    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }
    
    [DisplayName("每页数量")]
    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }
    
    [DisplayName("总数量")]
    public int TotalCount
    {
        get => _totalCount;
        set
        {
            SetProperty(ref _totalCount, value);
            RaisePropertyChanged(nameof(TotalPages));
        }
    }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    // 选择相关
    [DisplayName("全选")]
    public bool IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            SetProperty(ref _isAllSelected, value);
            if (value != GetIsAllSelected())
            {
                foreach (var item in Items)
                {
                    item.IsSelected = value;
                }
            }
        }
    }
    
    public List<T> SelectedItems => Items.Where(x => x.IsSelected).Select(x => x.Data).ToList();
    
    // 命令
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand SelectAllCommand { get; }
    public DelegateCommand ClearSelectionCommand { get; }
    public DelegateCommand PreviousPageCommand { get; }
    public DelegateCommand NextPageCommand { get; }
    
    // 抽象方法 - 子类需要实现
    protected abstract Task<PaginatedResult<T>> LoadDataAsync(int page, int pageSize, string? searchKeyword);
    
    // 虚方法 - 子类可重写
    protected virtual void OnItemSelectionChanged(SelectableItem<T> item)
    {
        RaisePropertyChanged(nameof(IsAllSelected));
    }
    
    // 基础操作
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();
            
            var result = await LoadDataAsync(CurrentPage, PageSize, SearchKeyword);
            
            Items.Clear();
            foreach (var item in result.Items)
            {
                var selectableItem = new SelectableItem<T>(item);
                selectableItem.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SelectableItem<T>.IsSelected))
                    {
                        OnItemSelectionChanged(selectableItem);
                    }
                };
                Items.Add(selectableItem);
            }
            
            TotalCount = result.TotalCount;
            
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await RefreshAsync();
    }
    
    private void SelectAll()
    {
        IsAllSelected = true;
    }
    
    private void ClearSelection()
    {
        IsAllSelected = false;
    }
    
    private bool GetIsAllSelected()
    {
        return Items.Count > 0 && Items.All(x => x.IsSelected);
    }
    
    private async Task PreviousPageAsync()
    {
        if (CanGoPreviousPage())
        {
            CurrentPage--;
            await RefreshAsync();
        }
    }
    
    private async Task NextPageAsync()
    {
        if (CanGoNextPage())
        {
            CurrentPage++;
            await RefreshAsync();
        }
    }
    
    private bool CanGoPreviousPage() => CurrentPage > 1 && !IsLoading;
    
    private bool CanGoNextPage() => CurrentPage < TotalPages && !IsLoading;
}
```

---

## 五、模型映射关系与规则

### 5.1 三层映射架构

```
数据库实体 (Entity) ←→ AutoMapper ←→ API传输对象 (DTO) ←→ 手动转换 ←→ 前端业务模型 (Info)
```

### 5.2 AutoMapper配置

```csharp
// AutoMapper配置示例 - src/Backend/Core/LYBT.Infrastructure/Mapping/PatientMappingProfile.cs
public class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
        // Entity → DTO 映射
        CreateMap<Patient, PatientDto>()
            .ForMember(dest => dest.CreateUserName, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateUserName, opt => opt.Ignore());
            
        CreateMap<Patient, PatientDetailDto>()
            .IncludeMembers(src => src.Registrations)
            .IncludeMembers(src => src.Records);
        
        // DTO → Entity 映射
        CreateMap<PatientCreateDto, Patient>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PatientNo, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeleteTime, opt => opt.Ignore());
            
        CreateMap<PatientUpdateDto, Patient>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PatientNo, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreateUserId, opt => opt.Ignore());
    }
}
```

### 5.3 前端模型转换

```csharp
// 前端服务层转换示例 - src/Frontend/Desktop/Services/PatientService.cs
public class PatientService : IPatientService
{
    private readonly IPatientApiService _apiService;
    
    public PatientService(IPatientApiService apiService)
    {
        _apiService = apiService;
    }
    
    public async Task<ServiceResult<List<PatientInfo>>> GetPatientsAsync()
    {
        try
        {
            var response = await _apiService.GetPatientsAsync();
            
            if (!response.Success || response.Data == null)
            {
                return ServiceResult<List<PatientInfo>>.Error(response.Message ?? "获取患者列表失败");
            }
            
            // DTO → Info 转换
            var patientInfos = response.Data.Select(ConvertToPatientInfo).ToList();
            
            return ServiceResult<List<PatientInfo>>.Ok(patientInfos);
        }
        catch (Exception ex)
        {
            return ServiceResult<List<PatientInfo>>.Error($"网络错误: {ex.Message}");
        }
    }
    
    private static PatientInfo ConvertToPatientInfo(PatientDto dto)
    {
        return new PatientInfo
        {
            Id = dto.Id,
            PatientNo = dto.PatientNo,
            Name = dto.Name,
            Gender = dto.Gender,
            Age = dto.Age,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            CreateTime = dto.CreateTime
        };
    }
    
    private static PatientCreateDto ConvertToCreateDto(PatientInfo info)
    {
        return new PatientCreateDto
        {
            Name = info.Name,
            Gender = info.Gender,
            Age = info.Age,
            PhoneNumber = info.PhoneNumber,
            Address = info.Address
        };
    }
}
```

### 5.4 映射规则总结

#### Entity → DTO 映射规则
1. **包含关系**: 基础DTO只包含展示必需字段
2. **详情映射**: DetailDto包含完整信息和关联数据
3. **忽略字段**: 敏感字段（如密码哈希）不映射到DTO
4. **计算字段**: 通过AutoMapper配置实现复杂计算

#### DTO → Info 映射规则
1. **手动转换**: 前端服务层手动转换，确保类型安全
2. **UI扩展**: Info模型可包含DTO没有的UI专用字段
3. **属性通知**: Info模型实现INotifyPropertyChanged接口
4. **默认值**: 前端模型可设置UI友好的默认值

#### 命名一致性
1. **基础字段**: 三层模型保持相同字段名
2. **扩展字段**: 前端特有字段使用明确命名
3. **显示属性**: 前端计算属性使用***Text、***Display后缀
4. **状态属性**: 使用Is***、Has***、Can***前缀

---

## 六、模型使用最佳实践

### 6.1 Entity层最佳实践

```csharp
// ✅ 好的实践
public class Patient : AuditableEntity
{
    // 使用强类型约束
    public Gender Gender { get; set; } = Gender.Unknown;
    
    // 包含数据验证约束
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    // 使用导航属性建立关联
    public ICollection<Registration> Registrations { get; set; } = [];
    
    // 避免循环引用，只在必要时配置反向导航
}

// ❌ 避免的实践
public class Patient
{
    // 避免使用弱类型
    public string Gender { get; set; }
    
    // 避免在实体中包含UI逻辑
    public string DisplayName => $"{Name} ({Age}岁)";
    
    // 避免在实体中处理业务规则
    public bool CanRegister() => true;
}
```

### 6.2 DTO层最佳实践

```csharp
// ✅ 好的实践
public class PatientCreateDto
{
    // 完整的数据验证
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;
    
    // 使用枚举提供类型安全
    [Required(ErrorMessage = "性别不能为空")]
    [DisplayName("性别")]
    public Gender Gender { get; set; } = Gender.Unknown;
    
    // 合理的范围约束
    [Range(0, 150, ErrorMessage = "年龄必须在0-150之间")]
    [DisplayName("年龄")]
    public int Age { get; set; }
}

// ❌ 避免的实践
public class PatientCreateDto
{
    // 避免缺少验证
    public string Name { get; set; }
    
    // 避免使用弱类型
    public string Gender { get; set; }
    
    // 避免包含实体ID
    public Guid Id { get; set; }
    
    // 避免包含审计字段
    public DateTime CreateTime { get; set; }
}
```

### 6.3 Info层最佳实践

```csharp
// ✅ 好的实践
public class PatientInfo : BaseModel
{
    private string _name = string.Empty;
    
    [DisplayName("姓名")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    // UI计算属性
    public string DisplayName => $"{Name} ({GenderText}, {AgeDescription})";
    
    // UI状态属性
    public bool HasEmergencyContact => !string.IsNullOrWhiteSpace(EmergencyContact);
    
    // UI友好的文本转换
    public string GenderText => Gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };
}

// ❌ 避免的实践
public class PatientInfo
{
    // 避免不支持属性通知
    public string Name { get; set; }
    
    // 避免在Info中包含业务逻辑
    public async Task SaveAsync() { }
    
    // 避免直接包含其他实体对象
    public List<Registration> Registrations { get; set; }
}
```

### 6.4 映射配置最佳实践

```csharp
// ✅ 好的实践
public class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
        // 明确忽略不需要的字段
        CreateMap<Patient, PatientDto>()
            .ForMember(dest => dest.CreateUserName, opt => opt.Ignore());
        
        // 使用条件映射处理复杂场景
        CreateMap<PatientCreateDto, Patient>()
            .ForMember(dest => dest.PatientNo, opt => opt.MapFrom(src => GeneratePatientNo()))
            .ForMember(dest => dest.Id, opt => opt.Ignore());
        
        // 配置值转换器处理特殊类型
        CreateMap<Patient, PatientExportDto>()
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.ToString()));
    }
    
    private string GeneratePatientNo()
    {
        return $"P{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
    }
}
```

### 6.5 性能优化建议

#### 查询优化
```csharp
// ✅ 使用投影查询减少数据传输
var patients = await _context.Patients
    .Where(p => !p.IsDeleted)
    .Select(p => new PatientDto
    {
        Id = p.Id,
        Name = p.Name,
        Gender = p.Gender,
        Age = p.Age
    })
    .ToListAsync();

// ❌ 避免查询完整实体后映射
var patients = await _context.Patients.ToListAsync();
var patientDtos = _mapper.Map<List<PatientDto>>(patients);
```

#### 分页处理
```csharp
// ✅ 数据库层面分页
var result = await _context.Patients
    .Where(p => p.Name.Contains(keyword))
    .OrderBy(p => p.Name)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(p => new PatientDto { ... })
    .ToListAsync();

// ❌ 内存分页
var allPatients = await _context.Patients.ToListAsync();
var pagedPatients = allPatients.Skip((page - 1) * pageSize).Take(pageSize);
```

---

## 七、总结与展望

### 7.1 模型架构优势

1. **清晰分离**: 三层模型分离关注点，职责明确
2. **类型安全**: 强类型约束，编译时检查
3. **可维护**: 统一规范，便于团队协作
4. **可扩展**: 分层设计支持功能扩展
5. **高性能**: 合理的映射和查询策略

### 7.2 当前实现状态

- ✅ 后端实体模型设计完整
- ✅ 共享DTO契约规范统一
- ✅ 前端Info模型支持MVVM
- ✅ AutoMapper配置覆盖全面
- ✅ 分页和查询优化到位

### 7.3 后续优化方向

1. **缓存策略**: 为常用模型添加缓存支持
2. **验证增强**: 完善跨字段验证和业务规则验证
3. **国际化**: 支持多语言DisplayName
4. **审计日志**: 完善实体变更跟踪
5. **性能监控**: 添加模型转换性能监控

### 7.4 开发建议

1. **新增模型**: 严格遵循三层架构和命名规范
2. **映射维护**: 及时更新AutoMapper配置
3. **测试覆盖**: 为模型转换添加单元测试
4. **文档更新**: 保持模型文档与代码同步
5. **代码审查**: 重点关注模型设计和映射配置

---

*本文档版本: 1.0*  
*最后更新: 2025年8月6日*  
*维护人员: 开发团队*