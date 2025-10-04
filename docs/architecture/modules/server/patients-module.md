# Patients模块设计 - Server端

## 📋 模块概述
**职责**：患者档案管理、基本信息维护、病历关联
**命名空间**：`LYBT.Module.Patients`
**API路径**：`/api/v1/patients/*`

## 🏗️ 架构设计

### 分层结构（实际实现）
```
├── Services/             # 业务服务
│   └── PatientService.cs           # 患者业务逻辑实现
├── Repositories/         # 数据访问
│   └── PatientRepository.cs        # 患者数据访问
├── Interfaces/           # 服务接口
│   ├── IPatientService.cs          # 患者服务接口
│   └── IPatientRepository.cs       # 患者仓储接口
├── Mapping/             # 对象映射
│   └── PatientMappingProfile.cs    # AutoMapper配置
└── PatientsModule.cs               # 模块依赖注册
```

## 🔌 API接口设计

### GET /api/v1/patients
**功能**：分页查询患者列表
```csharp
// Query Parameters
?page=1&pageSize=20&keyword=张三&gender=M&ageMin=18&ageMax=65

// Response 200
{
  "items": [
    {
      "id": "guid",
      "name": "张三",
      "gender": "Male",
      "age": 35,
      "phoneNumber": "13800138000",
      "idCard": "330106198801011234",
      "address": "杭州市西湖区",
      "lastVisitTime": "2024-01-15T10:30:00Z",
      "visitCount": 5,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "totalCount": 150,
  "currentPage": 1,
  "pageSize": 20
}
```

### GET /api/v1/patients/{id}
**功能**：获取患者详细信息
```csharp
// Response 200
{
  "id": "guid",
  "name": "张三",
  "gender": "Male",
  "birthDate": "1988-01-01",
  "age": 35,
  "phoneNumber": "13800138000",
  "email": "zhangsan@email.com",
  "idCard": "330106198801011234",
  "address": "浙江省杭州市西湖区文三路138号",
  "emergencyContact": "李四",
  "emergencyPhone": "13900139000",
  "occupation": "软件工程师",
  "maritalStatus": "Married",
  "allergyHistory": "青霉素过敏",
  "chronicDiseases": "高血压",
  "familyHistory": "父亲有糖尿病史",
  "lastVisitTime": "2024-01-15T10:30:00Z",
  "visitCount": 5,
  "totalSpent": 2500.00,
  "status": "Active",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

### POST /api/v1/patients
**功能**：新建患者档案
```csharp
// Request
{
  "name": "王五",
  "gender": "Female",
  "birthDate": "1990-05-15",
  "phoneNumber": "13700137000",
  "email": "wangwu@email.com",
  "idCard": "330106199005151234",
  "address": "浙江省杭州市拱墅区",
  "emergencyContact": "王六",
  "emergencyPhone": "13600136000",
  "occupation": "教师",
  "maritalStatus": "Single",
  "allergyHistory": "无",
  "chronicDiseases": "无",
  "familyHistory": "无特殊病史"
}

// Response 201
{
  "id": "new-guid",
  "name": "王五",
  // ... other fields
  "createdAt": "2024-01-20T14:30:00Z"
}
```

### PUT /api/v1/patients/{id}
**功能**：更新患者信息
```csharp
// Request
{
  "phoneNumber": "13800138001",
  "address": "新地址",
  "occupation": "高级工程师",
  "allergyHistory": "新增药物过敏"
}

// Response 200 - Updated patient object
```

### GET /api/v1/patients/{id}/medical-records
**功能**：获取患者病历记录
```csharp
// Response 200
{
  "patientId": "guid",
  "records": [
    {
      "id": "record-guid",
      "recordDate": "2024-01-15T10:30:00Z",
      "diagnosis": "感冒",
      "symptoms": "咳嗽、发热",
      "treatment": "中药调理",
      "doctorId": "doctor-guid",
      "doctorName": "李医生"
    }
  ]
}
```

## 🔧 核心服务

### PatientService (业务服务)
**职责**：患者业务逻辑，统一的读写操作
```csharp
public interface IPatientService
{
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

### PatientQueryService (查询服务)
**职责**：患者查询优化，只读操作
```csharp
public interface IPatientQueryService
{
    Task<PagedResult<PatientDto>> GetPagedPatientsAsync(PatientSearchDto searchDto);
    Task<PatientDto?> GetPatientByIdAsync(Guid patientId);
    Task<List<PatientDto>> SearchPatientsByNameAsync(string name);
    Task<List<PatientDto>> GetRecentPatientsAsync(int count = 10);
}
```

### MedicalRecordService (病历服务)
**职责**：患者病历管理
```csharp
public interface IMedicalRecordService
{
    Task<ServiceResult<MedicalRecordDto>> CreateRecordAsync(MedicalRecordCreateDto createDto);
    Task<ServiceResult<MedicalRecordDto>> UpdateRecordAsync(Guid id, MedicalRecordUpdateDto updateDto);
    Task<ServiceResult<List<MedicalRecordDto>>> GetPatientRecordsAsync(Guid patientId);
}
```

## 📊 数据模型

### 核心实体 (Patient)
```csharp
[Table("Patients")]
public class Patient : BaseEntity
{
    // 基本信息
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;        // 姓名
    
    [StringLength(20)]
    public string? PinYinCode { get; set; }                 // 拼音码（快速搜索）
    
    public Gender Gender { get; set; } = Gender.Unknown;    // 性别
    
    public int MaritalStatus { get; set; } = 0;             // 婚姻状态（数值存储）
    
    public DateTime? BirthDate { get; set; }                // 出生日期
    
    // 证件信息
    public int IdType { get; set; } = 0;                    // 证件类型（数值存储）
    
    [StringLength(50)]
    [SensitiveData(SensitiveDataType.IdentityInfo)]         // Epic 05-P0-03: 敏感数据加密
    public string? IdNumber { get; set; }                   // 证件号码
    
    // 联系方式
    [StringLength(20)]
    [SensitiveData(SensitiveDataType.ContactInfo)]          // Epic 05-P0-03: 敏感数据加密
    public string? PhoneNumber { get; set; }                // 手机号码
    
    [StringLength(256)]
    [SensitiveData(SensitiveDataType.PersonalInfo)]         // Epic 05-P0-03: 敏感数据加密
    public string? Address { get; set; }                    // 地址
    
    // 医疗信息
    [StringLength(500)]
    [SensitiveData(SensitiveDataType.MedicalInfo)]          // Epic 05-P0-03: 敏感数据加密
    public string? AllergyHistory { get; set; }             // 过敏史
    
    public int BloodType { get; set; } = 0;                 // 血型（数值存储）
    
    // 紧急联系人
    public string? EmergencyContactName { get; set; }       // 紧急联系人姓名
    public string? EmergencyContactPhone { get; set; }      // 紧急联系人电话
    public string? EmergencyContactRelation { get; set; }   // 紧急联系人关系
    
    // 状态管理
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;  // 患者状态
    
    [StringLength(128)]
    public string? DisableReason { get; set; }              // 禁用原因
    
    // 就诊统计
    public DateTime? LastVisitTime { get; set; }            // 最后就诊时间
    public int VisitCount { get; set; } = 0;                // 就诊次数
    
    // 计算属性
    [NotMapped]
    public int? Age => BirthDate.HasValue                   // 年龄（根据出生日期计算）
        ? DateTime.Today.Year - BirthDate.Value.Year - 
          (BirthDate.Value.Date > DateTime.Today.AddYears(-(DateTime.Today.Year - BirthDate.Value.Year)) ? 1 : 0)
        : null;
}
```

### 病历实体 (MedicalRecord)
```csharp
public class MedicalRecord : BaseEntity
{
    public Guid PatientId { get; set; }
    public DateTime RecordDate { get; set; }
    public string ChiefComplaint { get; set; }       // 主诉
    public string PresentIllness { get; set; }       // 现病史
    public string PhysicalExam { get; set; }         // 体格检查
    public string Diagnosis { get; set; }            // 诊断
    public string Treatment { get; set; }            // 治疗方案
    public string? Prescription { get; set; }        // 处方
    public string? Remarks { get; set; }             // 备注
    public Guid DoctorId { get; set; }               // 医生ID
    
    // 导航属性
    public Patient Patient { get; set; }
    public User Doctor { get; set; }
}
```

### 枚举定义

```csharp
public enum Gender
{
    [Description("未知")]
    Unknown = 0,
    
    [Description("男")]
    Male = 1,
    
    [Description("女")] 
    Female = 2
}

public enum MaritalStatus
{
    Single = 1,      // 未婚
    Married = 2,     // 已婚
    Divorced = 3,    // 离异
    Widowed = 4      // 丧偶
}

public enum PatientStatus
{
    Active = 1,      // 活跃
    Inactive = 2,    // 非活跃
    Archived = 3     // 归档
}
```

## 🛡️ 验证与安全

### 数据验证
```csharp
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    public PatientCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("姓名不能为空")
            .MaximumLength(50).WithMessage("姓名长度不能超过50个字符");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.IdCard)
            .Matches(@"^\d{15}|\d{18}|\d{17}[xX]$").WithMessage("身份证号格式不正确")
            .When(x => !string.IsNullOrEmpty(x.IdCard));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.Today).WithMessage("出生日期不能晚于今天")
            .GreaterThan(DateTime.Today.AddYears(-150)).WithMessage("出生日期不合理");
    }
}
```

### 隐私保护
- 身份证号部分脱敏显示
- 电话号码中间四位脱敏
- 病历信息访问权限控制
- 患者信息修改日志记录

## 📝 配置管理

### PatientModuleOptions
```csharp
public class PatientModuleOptions
{
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
    public bool EnableCache { get; set; } = false;
    public int CacheExpirationMinutes { get; set; } = 10;
    public bool EnablePinYinSearch { get; set; } = true;
    public bool RequireIdCard { get; set; } = false;
    public bool EnablePhotoUpload { get; set; } = false;
}
```

## 📋 实现状态

### ✅ 已实现
- 患者基础CRUD
- 分页查询功能
- 数据验证器
- AutoMapper映射
- 病历关联结构

### 🔄 待完善
- 拼音码自动生成
- 患者照片上传
- 复杂查询条件
- 数据导入导出

## 🧪 测试覆盖

### 单元测试
- PatientService业务逻辑
- 数据验证规则
- 映射配置正确性
- 查询性能测试

### 集成测试
- 患者API完整流程
- 病历关联操作
- 并发安全测试

## 🔗 依赖关系

### 依赖组件
- **Infrastructure** - 数据库上下文
- **Shared.Models** - DTO定义
- **Users模块** - 创建人信息

### 被依赖模块
- **Consultation模块** - 诊疗记录
- **Prescriptions模块** - 处方开具
- **MedicalCase模块** - 病历管理

## 📈 性能优化

### 查询优化
- 姓名字段索引
- 拼音码快速检索
- 电话号码索引
- 身份证号索引

### 缓存策略
- 热门患者信息缓存
- 近期就诊患者缓存
- 统计数据缓存

## 🔍 监控指标

### 业务指标
- 日新增患者数
- 活跃患者比例
- 平均就诊频次
- 信息完整度统计

### 技术指标
- 查询响应时间
- 数据库连接池使用率
- 缓存命中率