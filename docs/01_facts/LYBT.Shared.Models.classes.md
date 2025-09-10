# LYBT.Shared.Models 类和方法文档

> **版本**: 1.0  
> **生成日期**: 2025-09-10  
> **项目路径**: src/Shared/LYBT.Shared.Models  
> **项目类型**: Shared DTO Library  
> **目标框架**: net8.0  

## ApiResponse<T> (src/Shared/LYBT.Shared.Models/Contracts/Common/ApiResponse.cs:1-120)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Common
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Unified API Response Contract

### 2) 泛型约束
- **T**: 响应数据类型，无约束

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Success | bool | 否 | [JsonPropertyName("success")] | 操作是否成功 |
| Message | string | 是 | [JsonPropertyName("message")] | 响应消息 |
| Data | T | 是 | [JsonPropertyName("data")] | 响应数据 |
| Errors | object | 是 | [JsonPropertyName("errors")] | 错误详情 |
| Timestamp | DateTime | 否 | [JsonPropertyName("timestamp")] | 响应时间戳 |
| RequestId | string | 是 | [JsonPropertyName("requestId")] | 请求链路追踪ID |

### 4) 静态工厂方法

| 可见性 | 返回类型 | 方法名(参数列表) | 源码行号 |
|--------|----------|------------------|----------|
| public static | ApiResponse<T> | CreateSuccess(T data, string message = "操作成功") | 25-35 |
| public static | ApiResponse<T> | CreateFail(string message, object? errors = null) | 37-47 |
| public static | ApiResponse<T> | Ok(T data, string message = "操作成功") | 49-52 |
| public static | ApiResponse<T> | Fail(string message, object? errors = null) | 54-57 |

#### CreateSuccess(T data, string message = "操作成功")
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/ApiResponse.cs:25-35`
- **返回类型**: `ApiResponse<T>`
- **内部调用**: 构造函数初始化
- **备注**: 创建成功响应，自动设置Success=true和当前时间戳
- **默认消息**: "操作成功"

#### CreateFail(string message, object? errors = null)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/ApiResponse.cs:37-47`
- **返回类型**: `ApiResponse<T>`
- **内部调用**: 构造函数初始化
- **备注**: 创建失败响应，自动设置Success=false，Data=default(T)
- **错误信息**: errors参数支持结构化错误详情

### 5) 非泛型版本 (ApiResponse)

| 可见性 | 返回类型 | 方法名(参数列表) | 源码行号 |
|--------|----------|------------------|----------|
| public static | ApiResponse | CreateSuccess(string message = "操作成功") | 65-75 |
| public static | ApiResponse | CreateFail(string message, object? errors = null) | 77-87 |

### 6) 设计特点
- **统一响应格式**: 所有API使用统一的响应结构
- **泛型支持**: 支持任意数据类型的响应包装
- **JSON序列化优化**: 使用JsonPropertyName确保前端兼容性
- **请求追踪**: RequestId支持分布式链路追踪
- **工厂模式**: 提供多种静态工厂方法简化创建

---

## ServiceResult<T> (src/Shared/LYBT.Shared.Models/Contracts/Common/ServiceResult.cs:1-100)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Common
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Service Layer Result Contract

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| IsSuccess | bool | 否 | - | 操作是否成功 |
| Data | T | 是 | - | 结果数据 |
| ErrorMessage | string | 是 | - | 错误消息 |
| Exception | Exception | 是 | - | 异常详情 |
| Message | string | 是 | [JsonIgnore] | 兼容性属性，映射到ErrorMessage |

### 3) 兼容性属性方法

#### Message (兼容性属性)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/ServiceResult.cs:45-55`
- **get方法**: `return ErrorMessage;`
- **set方法**: `ErrorMessage = value;`
- **特性**: `[JsonIgnore]` - JSON序列化时忽略
- **备注**: 为向后兼容提供Message属性，内部映射到ErrorMessage

### 4) 静态工厂方法

| 可见性 | 返回类型 | 方法名(参数列表) | 源码行号 |
|--------|----------|------------------|----------|
| public static | ServiceResult<T> | Success(T data) | 25-35 |
| public static | ServiceResult<T> | Failure(string errorMessage, Exception? exception = null) | 37-47 |
| public static | ServiceResult<bool> | Success() | 49-52 |
| public static | ServiceResult<bool> | Failure(string errorMessage) | 54-57 |

#### Success(T data)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/ServiceResult.cs:25-35`
- **返回类型**: `ServiceResult<T>`
- **内部调用**: 构造函数初始化
- **备注**: 创建成功结果，设置IsSuccess=true，Data=传入数据

#### Failure(string errorMessage, Exception? exception = null)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/ServiceResult.cs:37-47`
- **返回类型**: `ServiceResult<T>`
- **内部调用**: 构造函数初始化
- **备注**: 创建失败结果，设置IsSuccess=false，Data=default(T)
- **异常支持**: 可选的Exception参数用于异常详情记录

### 5) UltraThink架构集成
- **服务层标准**: ServiceResult作为服务层统一返回类型
- **控制器转换**: 通过HandleServiceResult自动转换为ApiResponse
- **异常安全**: 统一的异常处理和错误消息传递

---

## PagedResult<T> (src/Shared/LYBT.Shared.Models/Contracts/Common/PagedResult.cs:1-150)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Common
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Unified Pagination Contract

### 2) 构造函数

| 可见性 | 参数列表 | 源码行号 |
|--------|----------|----------|
| public | (IEnumerable<T> items, int totalCount, int currentPage, int pageSize) | 25-35 |

### 3) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Items | IEnumerable<T> | 否 | - | 当前页数据项 |
| TotalCount | int | 否 | - | 总记录数 |
| CurrentPage | int | 否 | - | 当前页码(从1开始) |
| PageSize | int | 否 | - | 每页大小 |
| TotalPages | int | 否 | [计算属性] | 总页数 |
| HasPreviousPage | bool | 否 | [计算属性] | 是否有上一页 |
| HasNextPage | bool | 否 | [计算属性] | 是否有下一页 |
| Data | IEnumerable<T> | 否 | [兼容性属性] | 兼容性别名，映射到Items |

### 4) 计算属性方法

#### TotalPages (总页数计算)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/PagedResult.cs:65-70`
- **计算逻辑**: `(int)Math.Ceiling((double)TotalCount / PageSize)`
- **返回类型**: `int`
- **备注**: 基于总记录数和每页大小计算总页数，使用向上取整

#### HasPreviousPage (上一页判断)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/PagedResult.cs:72-75`
- **计算逻辑**: `CurrentPage > 1`
- **返回类型**: `bool`

#### HasNextPage (下一页判断)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/PagedResult.cs:77-80`
- **计算逻辑**: `CurrentPage < TotalPages`
- **返回类型**: `bool`

#### Data (兼容性属性)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/PagedResult.cs:82-85`
- **get方法**: `return Items;`
- **set方法**: `Items = value;`
- **备注**: 为兼容旧版本API提供Data属性名

### 5) 分页设计特点
- **从1开始**: 页码从1开始，符合用户习惯
- **计算属性**: 总页数、上下页判断自动计算
- **泛型支持**: 支持任意类型的分页数据
- **兼容性**: Data属性确保向后兼容

---

## UserDto (src/Shared/LYBT.Shared.Models/Contracts/Users/UserDto.cs:1-120)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Users
- **基类**: StatusDto
- **实现接口**: ICodeable
- **修饰符**: public
- **归属层角色**: User Display DTO

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Username | string | 是 | [StringLength(50), DisplayName("用户名")] | 用户名 |
| RealName | string | 是 | [StringLength(50), DisplayName("真实姓名")] | 真实姓名 |
| PinYinCode | string | 是 | [StringLength(50), DisplayName("拼音码")] | 拼音码 |
| WuBiCode | string | 是 | [StringLength(50), DisplayName("五笔码")] | 五笔码 |
| PhoneNumber | string | 是 | [StringLength(20), DisplayName("电话号码")] | 电话号码 |
| Email | string | 是 | [StringLength(100), DisplayName("邮箱地址")] | 邮箱地址 |
| Role | UserRole | 否 | [DisplayName("用户角色")] | 用户角色 |
| Specialty | string | 是 | [StringLength(200), DisplayName("专长")] | 专长 |
| RegistrationFee | decimal | 是 | [DisplayName("挂号费")] | 挂号费 |
| LicenseNumber | string | 是 | [StringLength(50), DisplayName("执业证书号")] | 执业证书号 |
| Introduction | string | 是 | [StringLength(1000), DisplayName("简介")] | 简介 |
| LastLoginTime | DateTime | 是 | [DisplayName("最后登录时间")] | 最后登录时间 |
| UserName | string | 是 | [兼容性属性] | 兼容性别名，映射到RealName |

### 3) 兼容性属性方法

#### UserName (兼容性属性)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Users/UserDto.cs:85-95`
- **get方法**: `return RealName;`
- **set方法**: `RealName = value;`
- **备注**: 为兼容旧版本API提供UserName属性，映射到RealName

### 4) 继承特性
- **StatusDto**: 继承Status状态管理和时间戳字段
- **ICodeable**: 实现拼音码和五笔码接口
- **BaseDto**: 包含Guid ID基础字段

### 5) 角色枚举支持
- **UserRole.Admin**: 管理员
- **UserRole.Doctor**: 医生
- **UserRole.Pharmacist**: 药师
- **UserRole.Nurse**: 护士
- **UserRole.Receptionist**: 接待员
- **UserRole.Operator**: 操作员

---

## UserMutationDto (src/Shared/LYBT.Shared.Models/Contracts/Users/UserMutationDto.cs:1-180)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Users
- **基类**: CreateDtoBase
- **实现接口**: ICodeable, IRemarkable
- **修饰符**: public
- **归属层角色**: Unified User Mutation DTO (UltraThink创新设计)

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Username | string | 否 | [Required, StringLength(50), RegularExpression(@"^[a-zA-Z0-9_]+$")] | 用户名 |
| RealName | string | 否 | [Required, StringLength(50), DisplayName("真实姓名")] | 真实姓名 |
| Password | string | 是 | [StringLength(128, MinimumLength = 6), DisplayName("密码")] | 密码(创建必须，更新可选) |
| PinYinCode | string | 是 | [StringLength(50), DisplayName("拼音码")] | 拼音码 |
| WuBiCode | string | 是 | [StringLength(50), DisplayName("五笔码")] | 五笔码 |
| PhoneNumber | string | 是 | [StringLength(20), Phone, DisplayName("电话号码")] | 电话号码 |
| Email | string | 是 | [StringLength(100), EmailAddress, DisplayName("邮箱地址")] | 邮箱地址 |
| Role | UserRole | 否 | [DisplayName("用户角色")] | 用户角色 |
| Status | CommonStatus | 否 | [DisplayName("用户状态")] | 用户状态 |
| Specialty | string | 是 | [StringLength(200), DisplayName("专长")] | 专长 |
| RegistrationFee | decimal | 是 | [Range(0, 999999.99), DisplayName("挂号费")] | 挂号费 |
| LicenseNumber | string | 是 | [StringLength(50), DisplayName("执业证书号")] | 执业证书号 |
| Introduction | string | 是 | [StringLength(1000), DisplayName("简介")] | 简介 |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |

### 3) 验证规则详解

#### Username验证
- **正则表达式**: `@"^[a-zA-Z0-9_]+$"`
- **错误消息**: "用户名只能包含字母、数字和下划线"
- **长度限制**: 最大50字符

#### Password验证
- **长度范围**: 6-128字符
- **可空设计**: 创建时必须，更新时可选
- **错误消息**: "密码长度必须在6-128个字符之间"

#### Email验证
- **EmailAddress特性**: 自动邮箱格式验证
- **可选字段**: 支持不填写邮箱

#### RegistrationFee验证
- **范围限制**: 0-999999.99
- **decimal类型**: 确保价格精度

### 4) UltraThink创新设计特点
- **统一变更模型**: 消除95%的CreateDto/UpdateDto代码重复
- **智能验证**: 密码字段在创建和更新场景下的不同验证逻辑
- **接口组合**: 通过ICodeable和IRemarkable实现功能组合
- **业务适应**: 一个DTO适应多种业务场景

---

## PatientDto (src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientDto.cs:1-180)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Patients
- **基类**: StatusDto
- **实现接口**: ICodeable, IRemarkable
- **修饰符**: public
- **归属层角色**: Patient Display DTO

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Name | string | 否 | [Required, StringLength(100), DisplayName("患者姓名")] | 患者姓名 |
| PinYinCode | string | 是 | [StringLength(20), DisplayName("拼音码")] | 拼音码 |
| WuBiCode | string | 是 | [StringLength(20), DisplayName("五笔码")] | 五笔码 |
| Gender | Gender | 否 | [DisplayName("性别")] | 性别 |
| BirthDate | DateTime | 是 | [DisplayName("出生日期")] | 出生日期 |
| Age | int | 否 | [计算属性, DisplayName("年龄")] | 年龄(基于出生日期计算) |
| IdNumber | string | 是 | [StringLength(50), DisplayName("证件号码")] | 证件号码 |
| PhoneNumber | string | 是 | [StringLength(20), DisplayName("手机号码")] | 手机号码 |
| Address | string | 是 | [StringLength(256), DisplayName("地址")] | 地址 |
| AllergyHistory | string | 是 | [StringLength(500), DisplayName("过敏史")] | 过敏史 |
| MedicalHistory | string | 是 | [StringLength(1000), DisplayName("既往病史")] | 既往病史 |
| FamilyHistory | string | 是 | [StringLength(1000), DisplayName("家族病史")] | 家族病史 |
| EmergencyContactName | string | 是 | [StringLength(50), DisplayName("紧急联系人姓名")] | 紧急联系人姓名 |
| EmergencyContactPhone | string | 是 | [StringLength(20), DisplayName("紧急联系人电话")] | 紧急联系人电话 |
| EmergencyContactRelation | string | 是 | [StringLength(30), DisplayName("紧急联系人关系")] | 紧急联系人关系 |
| LastVisitTime | DateTime | 是 | [DisplayName("最后就诊时间")] | 最后就诊时间 |
| VisitCount | int | 否 | [DisplayName("就诊次数")] | 就诊次数 |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |
| IsActive | bool | 否 | [兼容性属性] | 兼容性属性，基于Status计算 |

### 3) 计算属性方法

#### Age (年龄计算属性)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientDto.cs:95-115`
- **计算逻辑**: 
  ```csharp
  get
  {
      if (BirthDate == null) return 0;
      var today = DateTime.Today;
      var age = today.Year - BirthDate.Value.Year;
      if (BirthDate.Value.Date > today.AddYears(-age)) age--;
      return Math.Max(0, age);
  }
  ```
- **返回类型**: `int`
- **备注**: 基于出生日期和当前日期精确计算年龄，考虑生日未到的情况

#### IsActive (兼容性属性)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientDto.cs:165-175`
- **get方法**: `return Status == CommonStatus.Enabled;`
- **set方法**: `Status = value ? CommonStatus.Enabled : CommonStatus.Disabled;`
- **备注**: 为兼容旧版本API提供IsActive布尔属性

### 4) 业务特点
- **完整病历**: 包含过敏史、既往病史、家族病史
- **紧急联系人**: 完整的紧急联系人信息
- **就诊统计**: 自动维护就诊次数和最后就诊时间
- **年龄智能**: 基于出生日期的精确年龄计算

---

## MedicalCaseDto (src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDto.cs:1-160)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.MedicalCase
- **基类**: StatusDto
- **实现接口**: IRemarkable
- **修饰符**: public
- **归属层角色**: Medical Case Aggregate Root DTO

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| PatientId | Guid | 否 | [Required, DisplayName("患者ID")] | 患者ID |
| PatientName | string | 是 | [StringLength(50), DisplayName("患者姓名")] | 患者姓名(显示用) |
| DoctorId | Guid | 否 | [Required, DisplayName("医生ID")] | 医生ID |
| DoctorName | string | 是 | [StringLength(50), DisplayName("医生姓名")] | 医生姓名(显示用) |
| ConsultationDate | DateTime | 否 | [DisplayName("看诊时间")] | 看诊时间 |
| MedicalCaseStatus | MedicalCaseStatus | 否 | [DisplayName("医案状态")] | 医案状态 |
| PrescriptionId | Guid | 是 | [DisplayName("处方ID")] | 处方ID(可为空) |
| Priority | int | 否 | [计算属性] | 优先级(基于时间计算) |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |

### 3) 计算属性方法

#### Priority (优先级计算)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDto.cs:85-105`
- **计算逻辑**: 
  ```csharp
  get
  {
      var hoursElapsed = (DateTime.Now - ConsultationDate).TotalHours;
      if (hoursElapsed > 48) return 3; // 高优先级
      if (hoursElapsed > 24) return 2; // 中优先级
      return 1; // 低优先级
  }
  ```
- **返回类型**: `int`
- **备注**: 基于看诊时间自动计算优先级，时间越长优先级越高

### 4) 业务逻辑方法

| 可见性 | 返回类型 | 方法名(参数列表) | 源码行号 |
|--------|----------|------------------|----------|
| public | bool | CanStartConsultation() | 115-125 |
| public | bool | CanComplete() | 127-137 |
| public | bool | IsUrgent() | 139-149 |

#### CanStartConsultation()
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDto.cs:115-125`
- **返回类型**: `bool`
- **业务逻辑**: `MedicalCaseStatus == MedicalCaseStatus.Registered`
- **备注**: 判断医案是否可以开始看诊，只有已挂号状态才能开始

#### CanComplete()
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDto.cs:127-137`
- **返回类型**: `bool`
- **业务逻辑**: `MedicalCaseStatus == MedicalCaseStatus.InProgress`
- **备注**: 判断医案是否可以完成，只有诊疗中状态才能完成

#### IsUrgent()
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDto.cs:139-149`
- **返回类型**: `bool`
- **业务逻辑**: `Priority >= 3 || (DateTime.Now - ConsultationDate).TotalHours > 72`
- **备注**: 判断是否为紧急医案，高优先级或超过72小时未处理

### 5) 医案状态流转
- **Registered** (挂号完成): 初始状态，可以开始看诊
- **InProgress** (诊疗中): 正在看诊，可以完成或取消
- **Completed** (诊疗完成): 终态，诊疗结束
- **Cancelled** (已取消): 终态，医案取消

---

## ConsultationDetailDto (src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDetailDto.cs:1-457)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Consultation
- **基类**: BaseDto
- **实现接口**: IRemarkable
- **修饰符**: public
- **归属层角色**: Complete TCM Diagnosis DTO (最详细的中医四诊模型)

### 2) 基础关联属性

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| MedicalCaseId | Guid | 否 | [Required, DisplayName("医疗案例ID")] | 医疗案例ID |
| PatientId | Guid | 否 | [Required, DisplayName("患者ID")] | 患者ID |
| UserId | Guid | 否 | [Required, DisplayName("医生ID")] | 医生ID |
| StartTime | DateTime | 是 | [DisplayName("开始时间")] | 看诊开始时间 |
| EndTime | DateTime | 是 | [DisplayName("结束时间")] | 看诊结束时间 |
| ConsultationTime | int | 是 | [计算属性] | 看诊时长(分钟) |

### 3) 病史采集属性

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| ChiefComplaint | string | 是 | [StringLength(500), DisplayName("主诉")] | 主诉 |
| PresentIllness | string | 是 | [StringLength(1000), DisplayName("现病史")] | 现病史 |
| PastHistory | string | 是 | [StringLength(1000), DisplayName("既往史")] | 既往史 |
| FamilyHistory | string | 是 | [StringLength(1000), DisplayName("家族史")] | 家族史 |
| PersonalHistory | string | 是 | [StringLength(1000), DisplayName("个人史")] | 个人史 |

### 4) 中医四诊详细属性

#### 望诊 (Inspection) - 12个专业字段

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Inspection | string | 是 | [StringLength(500), DisplayName("望诊")] | 望诊总述 |
| Spirit | string | 是 | [StringLength(200), DisplayName("神态")] | 神态 |
| Complexion | string | 是 | [StringLength(200), DisplayName("面色")] | 面色 |
| BodyBuild | string | 是 | [StringLength(200), DisplayName("体型")] | 体型 |
| SkinColor | string | 是 | [StringLength(200), DisplayName("肤色")] | 肤色 |
| TongueBody | string | 是 | [StringLength(200), DisplayName("舌质")] | 舌质 |
| TongueCoating | string | 是 | [StringLength(200), DisplayName("舌苔")] | 舌苔 |
| TongueShape | string | 是 | [StringLength(200), DisplayName("舌形")] | 舌形 |
| Eyes | string | 是 | [StringLength(200), DisplayName("目诊")] | 目诊 |
| Nails | string | 是 | [StringLength(200), DisplayName("爪甲")] | 爪甲 |
| Hair | string | 是 | [StringLength(200), DisplayName("毛发")] | 毛发 |
| Excretions | string | 是 | [StringLength(500), DisplayName("排泄物")] | 排泄物 |

#### 闻诊 (Auscultation & Olfaction) - 5个专业字段

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| AuscultationOlfaction | string | 是 | [StringLength(500), DisplayName("闻诊")] | 闻诊总述 |
| Voice | string | 是 | [StringLength(200), DisplayName("声音")] | 声音 |
| Breathing | string | 是 | [StringLength(200), DisplayName("呼吸")] | 呼吸 |
| Cough | string | 是 | [StringLength(200), DisplayName("咳嗽")] | 咳嗽 |
| BodyOdor | string | 是 | [StringLength(200), DisplayName("气味")] | 气味 |

#### 问诊 (Inquiry) - 18个系统性问诊字段

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Inquiry | string | 是 | [StringLength(500), DisplayName("问诊")] | 问诊总述 |
| Chills | string | 是 | [StringLength(200), DisplayName("寒热")] | 寒热 |
| Sweating | string | 是 | [StringLength(200), DisplayName("汗出")] | 汗出 |
| AppetiteAndTaste | string | 是 | [StringLength(200), DisplayName("饮食口味")] | 饮食口味 |
| Urination | string | 是 | [StringLength(200), DisplayName("小便")] | 小便 |
| Defecation | string | 是 | [StringLength(200), DisplayName("大便")] | 大便 |
| Sleep | string | 是 | [StringLength(200), DisplayName("睡眠")] | 睡眠 |
| Emotion | string | 是 | [StringLength(200), DisplayName("情志")] | 情志 |
| HeadAndNeck | string | 是 | [StringLength(200), DisplayName("头颈")] | 头颈 |
| ChestAndRibs | string | 是 | [StringLength(200), DisplayName("胸胁")] | 胸胁 |
| AbdomenAndBack | string | 是 | [StringLength(200), DisplayName("脘腹腰背")] | 脘腹腰背 |
| Limbs | string | 是 | [StringLength(200), DisplayName("四肢")] | 四肢 |
| Ears | string | 是 | [StringLength(200), DisplayName("耳")] | 耳 |
| Eyes2 | string | 是 | [StringLength(200), DisplayName("目")] | 目 |
| MouthAndTeeth | string | 是 | [StringLength(200), DisplayName("口齿")] | 口齿 |
| Throat | string | 是 | [StringLength(200), DisplayName("咽喉")] | 咽喉 |
| WomenMenstruation | string | 是 | [StringLength(200), DisplayName("妇女月经")] | 妇女月经 |
| WomenLeukorrhea | string | 是 | [StringLength(200), DisplayName("妇女带下")] | 妇女带下 |

#### 切诊 (Palpation) - 8个触诊字段

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Palpation | string | 是 | [StringLength(500), DisplayName("切诊")] | 切诊总述 |
| Pulse | string | 是 | [StringLength(200), DisplayName("脉象")] | 脉象 |
| PulseRate | string | 是 | [StringLength(100), DisplayName("脉率")] | 脉率 |
| PulseRhythm | string | 是 | [StringLength(100), DisplayName("脉律")] | 脉律 |
| PulseStrength | string | 是 | [StringLength(100), DisplayName("脉力")] | 脉力 |
| PulseQuality | string | 是 | [StringLength(200), DisplayName("脉质")] | 脉质 |
| AbdominalPalpation | string | 是 | [StringLength(200), DisplayName("腹诊")] | 腹诊 |
| SkinTemperature | string | 是 | [StringLength(200), DisplayName("肌肤温度")] | 肌肤温度 |

### 5) 中医诊断结果属性

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| PatternDifferentiation | string | 是 | [StringLength(500), DisplayName("辨证分析")] | 辨证分析 |
| TCMDiagnosis | string | 是 | [StringLength(500), DisplayName("中医诊断")] | 中医诊断 |
| TCMSyndrome | string | 是 | [StringLength(500), DisplayName("中医证候")] | 中医证候 |
| TreatmentPrinciple | string | 是 | [StringLength(500), DisplayName("治疗原则")] | 治疗原则 |
| MedicalAdvice | string | 是 | [StringLength(1000), DisplayName("医嘱")] | 医嘱 |
| FollowUpAdvice | string | 是 | [StringLength(500), DisplayName("随访建议")] | 随访建议 |

### 6) 计算属性方法

#### ConsultationTime (看诊时长计算)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDetailDto.cs:445-457`
- **计算逻辑**: 
  ```csharp
  get
  {
      if (StartTime.HasValue && EndTime.HasValue)
      {
          return (int)(EndTime.Value - StartTime.Value).TotalMinutes;
      }
      return 0;
  }
  ```
- **返回类型**: `int`
- **备注**: 基于开始和结束时间计算看诊时长，以分钟为单位

### 7) 中医特色设计
- **四诊合参**: 完整的中医四诊记录体系，总计43个专业字段
- **辨证论治**: 从症状收集到诊断确立的完整流程
- **系统性问诊**: 按中医理论系统性收集患者信息
- **脉象详细**: 脉率、脉律、脉力、脉质全面记录
- **专业术语**: 使用标准中医术语和诊断规范

---

## PrescriptionDto (src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionDto.cs:1-140)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Prescriptions
- **基类**: StatusDto
- **实现接口**: IRemarkable
- **修饰符**: public
- **归属层角色**: Prescription Management DTO

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| MedicalCaseId | Guid | 否 | [Required, DisplayName("医疗案例ID")] | 医疗案例ID |
| PatientId | Guid | 否 | [Required, DisplayName("患者ID")] | 患者ID |
| UserId | Guid | 否 | [Required, DisplayName("医生ID")] | 医生ID |
| Indication | string | 是 | [StringLength(500), DisplayName("主治")] | 主治(适应症) |
| DosageCount | int | 否 | [Range(1, 100), DisplayName("处方帖数")] | 处方帖数 |
| Discount | decimal | 否 | [Range(0.01, 1.0), DisplayName("折扣")] | 折扣 |
| Advice | string | 是 | [StringLength(500), DisplayName("医嘱")] | 医嘱 |
| FormulaSource | string | 是 | [StringLength(200), DisplayName("验方来源")] | 验方来源 |
| PrescriptionStatus | PrescriptionStatus | 否 | [DisplayName("处方状态")] | 处方状态 |
| Items | List<PrescriptionItemDto> | 是 | - | 处方项目明细 |
| SingleDosePrice | decimal | 否 | [计算属性] | 单帖价格 |
| TotalPrice | decimal | 否 | [计算属性] | 总价格 |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |

### 3) 计算属性方法

#### SingleDosePrice (单帖价格计算)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionDto.cs:95-115`
- **计算逻辑**: 
  ```csharp
  get
  {
      if (Items?.Any() != true) return 0m;
      var subtotal = Items.Sum(item => item.UnitPrice * item.Quantity);
      return subtotal * Discount;
  }
  ```
- **返回类型**: `decimal`
- **备注**: 计算单帖价格 = (所有药材小计) × 折扣

#### TotalPrice (总价格计算)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionDto.cs:117-125`
- **计算逻辑**: `SingleDosePrice * DosageCount`
- **返回类型**: `decimal`
- **备注**: 总价格 = 单帖价格 × 帖数

### 4) 处方状态流转
- **Draft** (草稿): 初始状态，可以编辑
- **Confirmed** (已确认): 处方确认，不可编辑
- **Dispensed** (已配药): 药房配药完成
- **Completed** (已完成): 患者取药完成

### 5) 业务特点
- **验方集成**: 支持从验方库引用，自动填写FormulaSource
- **价格自动计算**: 基于药材明细自动计算价格
- **折扣支持**: 支持0.01-1.0范围的折扣设置
- **帖数管理**: 支持1-100帖的处方开具

---

## PrescriptionItemDto (src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionItemDto.cs:1-80)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Prescriptions
- **基类**: BaseDto
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Prescription Item Detail DTO

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| PrescriptionId | Guid | 否 | [Required, DisplayName("处方ID")] | 处方ID |
| HerbId | Guid | 否 | [Required, DisplayName("药材ID")] | 药材ID |
| HerbName | string | 否 | [Required, StringLength(100), DisplayName("药材名称")] | 药材名称 |
| Quantity | decimal | 否 | [Range(0.001, 9999.999), DisplayName("用量")] | 用量 |
| Unit | string | 否 | [Required, StringLength(16), DisplayName("单位")] | 单位 |
| UnitPrice | decimal | 否 | [Range(0.01, 999999.99), DisplayName("单价")] | 单价 |
| Amount | decimal | 否 | [计算属性] | 小计金额 |
| Usage | string | 是 | [StringLength(200), DisplayName("用法说明")] | 用法说明 |
| ProcessingMethod | string | 是 | [StringLength(100), DisplayName("炮制方法")] | 炮制方法 |

### 3) 计算属性方法

#### Amount (小计金额计算)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionItemDto.cs:65-70`
- **计算逻辑**: `UnitPrice * Quantity`
- **返回类型**: `decimal`
- **备注**: 自动计算药材小计金额

### 4) 验证规则
- **Quantity范围**: 0.001-9999.999，支持精确到毫克级用量
- **UnitPrice范围**: 0.01-999999.99，支持各种价位药材
- **药材名称**: 必填，最大100字符
- **用法说明**: 可选，最大200字符描述

### 5) 中医药特色
- **炮制方法**: 支持生用、炮制、蜜炙等中药炮制方法
- **用法说明**: 支持先煎、后下、包煎、冲服等特殊用法
- **单位灵活**: 支持克、钱、两等传统中医计量单位

---

## HerbDto (src/Shared/LYBT.Shared.Models/Contracts/Herbs/HerbDto.cs:1-100)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Herbs
- **基类**: StatusDto
- **实现接口**: ICodeable, IRemarkable
- **修饰符**: public
- **归属层角色**: Herb Management DTO (简化版，删除库存管理)

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Name | string | 否 | [Required, StringLength(100), DisplayName("药材名称")] | 药材名称 |
| PinYinCode | string | 是 | [StringLength(50), DisplayName("拼音码")] | 拼音码 |
| WuBiCode | string | 是 | [StringLength(50), DisplayName("五笔码")] | 五笔码 |
| Origin | string | 是 | [StringLength(100), DisplayName("产地")] | 产地 |
| Spec | string | 是 | [StringLength(100), DisplayName("规格")] | 规格 |
| Unit | string | 否 | [Required, StringLength(10), DisplayName("单位")] | 单位 |
| Price | decimal | 否 | [Range(0.01, 999999.99), DisplayName("单价")] | 单价 |
| CostPrice | decimal | 是 | [Range(0.01, 999999.99), DisplayName("成本价")] | 成本价 |
| Effect | string | 是 | [StringLength(500), DisplayName("功效说明")] | 功效说明 |
| Usage | string | 是 | [StringLength(500), DisplayName("用法用量")] | 用法用量 |
| Property | string | 是 | [StringLength(200), DisplayName("性味归经")] | 性味归经 |
| Contraindications | string | 是 | [StringLength(500), DisplayName("禁忌")] | 禁忌 |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |

### 3) 业务定位
- **处方专用**: 仅用于处方开具，不含库存管理功能
- **价格管理**: 支持成本价和售价双轨制
- **中医属性**: 包含功效、用法用量、性味归经等中医信息
- **快速检索**: 通过拼音码和五笔码支持快速搜索

### 4) 中医药特色字段
- **功效说明**: 详细记录药材的中医功效
- **用法用量**: 标准的中医用法用量指导
- **性味归经**: 中药的性味和归经理论
- **禁忌**: 用药禁忌和注意事项
- **产地规格**: 支持不同产地和规格的药材管理

### 5) 验证规则
- **价格范围**: 0.01-999999.99，支持各种价位药材
- **名称唯一**: 药材名称在系统中应保持唯一性
- **编码快速**: 拼音码和五笔码用于快速检索

---

## FormulaDto (src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaDto.cs:1-120)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.Shared.Models.Contracts.Formula
- **基类**: StatusDto
- **实现接口**: IRemarkable
- **修饰符**: public
- **归属层角色**: Formula Template Management DTO

### 2) 属性清单

| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Name | string | 否 | [Required, StringLength(100), DisplayName("验方名称")] | 验方名称 |
| Effect | string | 是 | [StringLength(500), DisplayName("功效")] | 功效 |
| Usage | string | 是 | [StringLength(500), DisplayName("用法")] | 用法 |
| Property | string | 是 | [StringLength(200), DisplayName("性味归经")] | 性味归经 |
| Source | string | 是 | [StringLength(200), DisplayName("出处")] | 出处 |
| Category | string | 否 | [计算属性] | 分类(智能判断) |
| IsShared | bool | 否 | [DisplayName("是否共享")] | 是否共享 |
| UsageCount | int | 否 | [DisplayName("使用次数")] | 使用次数 |
| Herbs | List<FormulaHerbItemDto> | 是 | - | 药材组成 |
| Remark | string | 是 | [StringLength(500), DisplayName("备注")] | 备注 |

### 3) 计算属性方法

#### Category (智能分类)
- **源码位置**: `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaDto.cs:85-105`
- **计算逻辑**: 
  ```csharp
  get
  {
      if (string.IsNullOrEmpty(Name)) return "验方";
      
      // 基于验方名称智能判断分类
      if (Name.Contains("感冒") || Name.Contains("发热")) return "内科方";
      if (Name.Contains("外伤") || Name.Contains("创面")) return "外科方";
      if (Name.Contains("妇科") || Name.Contains("月经")) return "妇科方";
      if (Name.Contains("小儿") || Name.Contains("儿科")) return "儿科方";
      if (Name.Contains("眼科") || Name.Contains("目疾")) return "眼科方";
      
      return "验方"; // 默认分类
  }
  ```
- **返回类型**: `string`
- **备注**: 基于验方名称自动智能分类

### 4) 验方类型
- **个人验方**: `IsShared = false` - 医生个人经验方
- **共享验方**: `IsShared = true` - 科室或医院共享方
- **经典验方**: 传统中医经典方剂
- **现代验方**: 现代中医临床验方

### 5) 智能分类系统
- **内科方**: 感冒、发热等内科疾病验方
- **外科方**: 外伤、创面等外科验方
- **妇科方**: 妇科疾病专用验方
- **儿科方**: 小儿疾病专用验方
- **眼科方**: 眼科疾病专用验方
- **验方**: 默认通用分类

---

## 基础DTO架构体系

### BaseDto (src/Shared/LYBT.Shared.Models/Contracts/Common/BaseDto.cs:1-30)

#### 属性清单
| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Id | Guid | 否 | [Key] | 唯一标识符 |

### StatusDto (src/Shared/LYBT.Shared.Models/Contracts/Common/StatusDto.cs:1-50)

#### 继承关系
- **基类**: TimestampDto
- **实现接口**: IStatusManageable

#### 属性清单
| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Status | CommonStatus | 否 | [DisplayName("状态")] | 实体状态 |

### TimestampDto (src/Shared/LYBT.Shared.Models/Contracts/Common/TimestampDto.cs:1-40)

#### 继承关系
- **基类**: BaseDto
- **实现接口**: IAuditable

#### 属性清单
| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| CreateTime | DateTime | 否 | [DisplayName("创建时间")] | 创建时间 |
| UpdateTime | DateTime | 是 | [DisplayName("更新时间")] | 更新时间 |

### ExtendedQueryDto (src/Shared/LYBT.Shared.Models/Contracts/Common/ExtendedQueryDto.cs:1-80)

#### 继承关系
- **基类**: PagedQueryBaseDto
- **实现接口**: IStatusManageable

#### 属性清单
| 属性名 | 类型 | 可空 | 特性注解 | 说明 |
|--------|------|------|----------|------|
| Status | CommonStatus | 是 | [DisplayName("状态筛选")] | 状态筛选条件 |
| StartDate | DateTime | 是 | [DisplayName("开始日期")] | 时间范围开始 |
| EndDate | DateTime | 是 | [DisplayName("结束日期")] | 时间范围结束 |
| SortBy | string | 是 | [DisplayName("排序字段")] | 排序字段 |
| SortOrder | string | 是 | [DisplayName("排序方向")] | 排序方向(asc/desc) |

---

## 全局统计

### 项目统计
- **DTO类数量**: 80+个业务DTO类
- **基础设施类**: 15个通用基础类
- **验证特性**: 完整的数据验证体系
- **支持模块**: 8个核心业务模块完整覆盖

### 架构特点
- **UltraThink极简化**: 统一变更DTO消除95%代码重复
- **统一响应格式**: ApiResponse<T>和ServiceResult<T>完美配合
- **分页标准化**: PagedResult<T>统一分页处理
- **计算属性设计**: 智能计算减少数据冗余
- **兼容性支持**: 完整的向后兼容属性设计

### 业务覆盖
- ✅ 认证授权：完整的JWT认证DTO体系
- ✅ 用户管理：统一变更模型UltraThink创新设计
- ✅ 患者档案：完整病历DTO，年龄智能计算
- ✅ 诊疗流程：医案聚合根DTO，业务逻辑方法
- ✅ 中医诊断：457行最详细四诊DTO模型
- ✅ 处方管理：价格自动计算，验方集成
- ✅ 药材管理：简化设计，专注处方用药
- ✅ 验方管理：智能分类，导入导出体系

### 设计原则体现
- **单一职责**：每个DTO专注特定数据传输场景
- **开闭原则**：计算属性和接口支持功能扩展
- **里氏替换**：基础DTO类体系的继承设计
- **接口隔离**：ICodeable、IRemarkable等小接口设计
- **依赖倒置**：基于接口的DTO设计模式

### 质量保证
- **数据验证**: 完整的DataAnnotations验证体系
- **类型安全**: 强类型枚举和Guid标识符
- **计算精度**: decimal类型确保价格计算精度
- **兼容性**: 完整的向后兼容属性支持
- **文档完整**: DisplayName确保UI显示友好