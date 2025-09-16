# Backend — P3-Fix Batch1: API契约快照

## 📊 控制器方法签名快照

**快照时间**: 2025-09-15 21:45:00  
**范围**: Patients/Users/Consultation三个Create端点

### 1. PatientsController.Add

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:85`

```csharp
/// <summary>
/// 添加患者 - 统一API响应格式  
/// </summary>
[HttpPost]
public async Task<ActionResult<ApiResponse<PatientDto>>> Add([FromBody] PatientCreateDto dto)
{
    try
    {
        var validation = ValidateModel<PatientDto>();
        if (validation != null)
        {
            return validation;
        }

        var result = await _patientService.AddAsync(dto);
        if (result.IsSuccess && result.Data != null)
        {
            LogOperation("添加患者", result.Data, result.Data.Id);
        }

        return HandleServiceResult(result, "患者添加成功");
    }
    catch (Exception ex)
    {
        return HandleException<PatientDto>(ex, "添加患者", dto);
    }
}
```

**DTO定义**: `PatientCreateDto` (src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientDtos.cs:119-194)

### 2. UsersController.CreateUser

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:86`

```csharp
/// <summary>
/// 创建用户 - 统一API响应格式
/// </summary>
[HttpPost]
public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] UserMutationDto dto)
{
    try
    {
        var validation = ValidateModel<UserDto>();
        if (validation != null)
        {
            return validation;
        }

        var result = await _userService.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
        {
            LogOperation("创建用户", result.Data, result.Data.Id);
        }

        return HandleServiceResult(result, "用户创建成功");
    }
    catch (Exception ex)
    {
        return HandleException<UserDto>(ex, "创建用户", dto);
    }
}
```

**DTO定义**: `UserMutationDto` (src/Shared/LYBT.Shared.Models/Contracts/Users/UserDtos.cs:56-100+)

### 3. ConsultationController.StartConsultation

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs:91`

```csharp
/// <summary>
/// 开始看诊 - 统一API响应格式
/// </summary>
[HttpPost("start")]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> StartConsultation([FromBody] ConsultationStartDto dto)
{
    try
    {
        var validation = ValidateModel<ConsultationDto>();
        if (validation != null)
        {
            return validation;
        }

        var (operatorId, operatorName, _) = GetOperator();
        var result = await _consultationService.StartAsync(dto);

        if (result.IsSuccess && result.Data != null)
        {
            LogOperation("开始看诊", result.Data, result.Data.Id);
        }

        return HandleServiceResult(result, "看诊开始成功");
    }
    catch (Exception ex)
    {
        return HandleException<ConsultationDto>(ex, "开始看诊", dto);
    }
}
```

**DTO定义**: `ConsultationStartDto` (src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationOperationDtos.cs:13-52)

## 📋 DTO定义快照

### 1. PatientCreateDto

```csharp
public class PatientCreateDto
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("性别")]
    public Gender Gender { get; set; } = Gender.Unknown;

    [DisplayName("出生日期")]
    public DateTime? BirthDate { get; set; }

    [Range(0, 200, ErrorMessage = "年龄必须在0-200之间")]
    [DisplayName("年龄")]
    public int Age { get; set; }

    [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
    [DisplayName("身份证号")]
    public string? IdNumber { get; set; }

    [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
    [DisplayName("手机号")]
    public string? PhoneNumber { get; set; }

    // ... 其他字段
}
```

### 2. UserMutationDto

```csharp
public class UserMutationDto : BaseDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
    [DisplayName("用户名")]
    public string Username { get; set; } = string.Empty;

    [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
    [DisplayName("密码")]
    public string? Password { get; set; }

    [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
    [DisplayName("确认密码")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;

    // ... 其他字段
}
```

### 3. ConsultationStartDto

```csharp
public class ConsultationStartDto
{
    [Required(ErrorMessage = "医疗案例ID不能为空")]
    [DisplayName("医疗案例ID")]
    public Guid MedicalCaseId { get; set; }

    [Required(ErrorMessage = "患者ID不能为空")]
    [DisplayName("患者ID")]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "医生ID不能为空")]
    [DisplayName("医生ID")]
    public Guid DoctorId { get; set; }

    [Range(5, 480, ErrorMessage = "预计看诊时长必须在5-480分钟之间")]
    [DisplayName("预计时长")]
    public int EstimatedDuration { get; set; } = 30;

    [DisplayName("看诊类型")]
    public string? ConsultationType { get; set; }

    [StringLength(500, ErrorMessage = "初步主诉长度不能超过500个字符")]
    [DisplayName("初步主诉")]
    public string? InitialComplaint { get; set; }

    // ... 其他字段
}
```

## 🔍 共同特征分析

### 控制器层共同特征
1. **基类**: 所有控制器都继承 `BaseApiController`
2. **参数绑定**: 全部使用 `[FromBody]` 绑定
3. **参数名称**: 统一使用 `dto` 作为参数名
4. **验证模式**: 使用 `ValidateModel<T>()` 进行验证
5. **响应格式**: 统一使用 `ApiResponse<T>` 包装
6. **异常处理**: 使用 `HandleException<T>()` 处理异常

### DTO层共同特征
1. **验证注解**: 大量使用DataAnnotations进行验证
2. **字段要求**: 核心字段都有 `[Required]` 注解
3. **长度限制**: 字符串字段都有 `[StringLength]` 限制
4. **显示名称**: 统一使用 `[DisplayName]` 中文标记
5. **继承关系**: UserMutationDto继承BaseDto，其他直接定义

### BaseApiController关键信息
- **验证方法**: `ValidateModel<T>()` 方法可能影响模型绑定
- **响应包装**: `HandleServiceResult()` 处理服务结果
- **异常处理**: `HandleException<T>()` 统一异常响应

## 🎯 潜在问题点

1. **ValidateModel<T>()** - 验证方法可能期望特定的请求结构
2. **BaseDto继承** - UserMutationDto继承的BaseDto可能有特殊要求
3. **JSON序列化** - 系统可能有自定义的JSON处理配置
4. **模型绑定器** - 可能存在自定义的模型绑定器配置

---

**契约快照完成时间**: 2025-09-15 21:45:00  
**文档状态**: 步骤①证据收集完成，准备进入步骤②DTO契约修复