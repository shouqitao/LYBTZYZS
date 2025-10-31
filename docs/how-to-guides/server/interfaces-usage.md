# Server端接口层使用指南

> **版本**: v1.0
> **最后更新**: 2025-10-30
> **维护负责**: Server端开发组

---

## 📋 目录

1. [开发流程总览](#1-开发流程总览)
2. [环境准备](#2-环境准备)
3. [创建Service接口](#3-创建service接口)
4. [ServiceResult封装使用](#4-serviceresult封装使用)
5. [实现Service类](#5-实现service类)
6. [依赖注入注册](#6-依赖注入注册)
7. [Controller集成](#7-controller集成)
8. [Repository接口扩展](#8-repository接口扩展)
9. [Mock测试支持](#9-mock测试支持)
10. [接口版本演进](#10-接口版本演进)
11. [常见问题与陷阱](#11-常见问题与陷阱)
12. [检查清单](#12-检查清单)
13. [参考资料](#13-参考资料)
14. [更新历史](#14-更新历史)

---

## 1. 开发流程总览

### 1.1 完整开发流程(5个步骤)

```
┌────────────────────────────────────────────────────────────────┐
│ Step 1: 定义Service接口                                         │
│  ┌──────────────────────────────────────────┐                  │
│  │ LYBT.Server.Interfaces/Services/         │                  │
│  │  IPatientService.cs                      │                  │
│  │  - Task<ServiceResult<T>> GetByIdAsync() │                  │
│  │  - Task<ServiceResult<T>> CreateAsync()  │                  │
│  └──────────────────────────────────────────┘                  │
└────────────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────────────┐
│ Step 2: 实现Service类                                          │
│  ┌──────────────────────────────────────────┐                  │
│  │ LYBT.Module.Patients/Services/           │                  │
│  │  PatientService : IPatientService        │                  │
│  │  - 注入Repository、AutoMapper、Logger    │                  │
│  │  - 实现业务逻辑验证                       │                  │
│  │  - 返回ServiceResult封装                  │                  │
│  └──────────────────────────────────────────┘                  │
└────────────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────────────┐
│ Step 3: 注册依赖注入                                            │
│  ┌──────────────────────────────────────────┐                  │
│  │ LYBT.Module.Patients/PatientsModule.cs   │                  │
│  │  services.AddScoped<IPatientService,     │                  │
│  │                      PatientService>();  │                  │
│  └──────────────────────────────────────────┘                  │
└────────────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────────────┐
│ Step 4: Controller集成                                         │
│  ┌──────────────────────────────────────────┐                  │
│  │ LYBT.WebAPI/Controllers/                 │                  │
│  │  PatientsController                      │                  │
│  │  - 构造函数注入IPatientService           │                  │
│  │  - 解包ServiceResult返回HTTP响应         │                  │
│  └──────────────────────────────────────────┘                  │
└────────────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────────────┐
│ Step 5: 测试验证                                               │
│  ┌──────────────────────────────────────────┐                  │
│  │ 单元测试: Mock IPatientService           │                  │
│  │ 集成测试: 真实Service + InMemoryDb       │                  │
│  │ API测试: Postman/Swagger验证端点         │                  │
│  └──────────────────────────────────────────┘                  │
└────────────────────────────────────────────────────────────────┘
```

### 1.2 快速决策矩阵

| 场景 | 选择方案 | 说明 |
|------|---------|------|
| **创建新业务模块** | 在`LYBT.Server.Interfaces/Services/`定义接口 | 所有Service接口统一定义 |
| **需要特殊查询** | 在模块中定义`IXxxRepository`扩展接口 | Repository接口在模块中 |
| **返回单个实体** | `Task<ServiceResult<PatientDto>>` | ServiceResult封装DTO |
| **返回列表** | `Task<ServiceResult<List<PatientDto>>>` | ServiceResult封装List |
| **返回分页** | `Task<ServiceResult<PagedResult<T>>>` | ServiceResult封装分页 |
| **无返回值** | `Task<ServiceResult>` | 仅返回成功/失败 |
| **文件流** | `Task<MemoryStream>` | 不包装ServiceResult |
| **业务规则验证失败** | `ServiceResult.Fail("错误信息")` | 不抛异常 |
| **系统级错误** | `throw new Exception(...)` | 抛异常,全局捕获 |

---

## 2. 环境准备

### 2.1 项目结构

```
LYBTZYZS/
├── src/
│   ├── Server/
│   │   ├── Core/
│   │   │   ├── LYBT.Server.Interfaces/        ← Service接口定义
│   │   │   │   └── Services/
│   │   │   │       ├── IPatientService.cs
│   │   │   │       ├── IHerbService.cs
│   │   │   │       └── IMedicalCaseService.cs
│   │   │   └── LYBT.Infrastructure/           ← Repository基类
│   │   │       └── Repositories/
│   │   │           └── IBaseRepository.cs
│   │   ├── Modules/
│   │   │   └── LYBT.Module.Patients/          ← Service实现
│   │   │       ├── Services/
│   │   │       │   └── PatientService.cs
│   │   │       ├── Repositories/
│   │   │       │   └── PatientRepository.cs
│   │   │       ├── Interfaces/                ← Repository接口扩展
│   │   │       │   └── IPatientRepository.cs
│   │   │       └── PatientsModule.cs          ← DI注册
│   │   └── Services/
│   │       └── LYBT.WebAPI/                   ← API Controller
│   │           └── Controllers/
│   │               └── PatientsController.cs
│   └── Shared/
│       └── LYBT.Shared.Models/                ← DTO模型
│           ├── DTOs/
│           └── Common/ServiceResult.cs
└── tests/
    └── UnitTests/
        └── Server/
            └── Modules/
                └── LYBT.Module.Patients.Tests/
```

### 2.2 依赖关系配置

**LYBT.Server.Interfaces.csproj** (接口层):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 仅依赖Shared.Models(DTO模型) -->
    <ProjectReference Include="..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>

  <!-- ⚠️ 禁止依赖任何具体实现 -->
  <!-- ❌ 不能依赖: Infrastructure, Entities, Module.* -->
</Project>
```

**LYBT.Module.Patients.csproj** (模块实现):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 依赖接口层 -->
    <ProjectReference Include="..\..\Core\LYBT.Server.Interfaces\LYBT.Server.Interfaces.csproj" />

    <!-- 依赖基础设施 -->
    <ProjectReference Include="..\..\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />

    <!-- 依赖实体层 -->
    <ProjectReference Include="..\..\Core\LYBT.Entities\LYBT.Entities.csproj" />

    <!-- 依赖Shared层 -->
    <ProjectReference Include="..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- NuGet包 -->
    <PackageReference Include="AutoMapper" Version="13.0.1" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 2.3 必需的using语句

**接口定义文件** (IPatientService.cs):

```csharp
using LYBT.Shared.Models.Common;        // ServiceResult
using LYBT.Shared.Models.DTOs.Patients; // PatientDto, PatientCreateDto
using LYBT.Shared.Models.Pagination;    // PagedResult
```

**实现文件** (PatientService.cs):

```csharp
using AutoMapper;                                    // IMapper
using LYBT.Entities.Models;                         // Patient实体
using LYBT.Infrastructure.Repositories;             // IBaseRepository
using LYBT.Module.Patients.Interfaces;              // IPatientRepository
using LYBT.Shared.Models.Common;                    // ServiceResult
using LYBT.Shared.Models.DTOs.Patients;             // PatientDto
using Microsoft.Extensions.Logging;                 // ILogger
```

**Controller文件** (PatientsController.cs):

```csharp
using LYBT.Module.Patients.Interfaces;              // IPatientService
using LYBT.Shared.Models.DTOs.Patients;             // PatientCreateDto
using Microsoft.AspNetCore.Mvc;                     // [ApiController], [HttpGet]
```

---

## 3. 创建Service接口

### 3.1 基础CRUD接口模板

**场景**: 创建患者管理服务接口

**位置**: `src/Server/Core/LYBT.Server.Interfaces/Services/IPatientService.cs`

```csharp
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.DTOs.Patients;
using LYBT.Shared.Models.Pagination;

namespace LYBT.Server.Interfaces.Services;

/// <summary>
/// 患者服务接口
/// </summary>
public interface IPatientService
{
    // ========== 基础CRUD(5个核心方法) ==========

    /// <summary>
    /// 分页查询患者
    /// </summary>
    /// <param name="page">页码(从1开始)</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="keyword">搜索关键字(可选,支持姓名/电话/身份证号)</param>
    /// <returns>分页结果</returns>
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null);

    /// <summary>
    /// 根据ID获取患者详情
    /// </summary>
    /// <param name="id">患者ID</param>
    /// <returns>患者DTO,不存在返回Fail</returns>
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建新患者
    /// </summary>
    /// <param name="dto">患者创建DTO</param>
    /// <returns>创建成功的患者DTO</returns>
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

    /// <summary>
    /// 更新患者信息
    /// </summary>
    /// <param name="id">患者ID</param>
    /// <param name="dto">患者更新DTO</param>
    /// <returns>更新后的患者DTO</returns>
    Task<ServiceResult<PatientDto>> UpdateAsync(
        Guid id,
        PatientUpdateDto dto);

    /// <summary>
    /// 删除患者(软删除)
    /// </summary>
    /// <param name="id">患者ID</param>
    /// <returns>删除结果</returns>
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

**要点说明**：
- ✅ **接口名称**: `I{Entity}Service`模式(如`IPatientService`)
- ✅ **方法命名**: `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`统一模式
- ✅ **返回类型**: 所有方法返回`Task<ServiceResult<T>>`
- ✅ **参数顺序**: 必需参数 → 可选参数 → CancellationToken(如需)
- ✅ **XML注释**: 必须提供完整的summary和param说明

### 3.2 扩展业务方法

**场景**: 添加搜索和导入导出功能

```csharp
public interface IPatientService
{
    // ... 基础CRUD方法 ...

    // ========== 搜索与查询 ==========

    /// <summary>
    /// 搜索患者(按姓名、电话、身份证号)
    /// </summary>
    /// <param name="keyword">搜索关键字</param>
    /// <returns>匹配的患者列表</returns>
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);

    // ========== 批量操作 ==========

    /// <summary>
    /// 从Excel文件导入患者数据
    /// </summary>
    /// <param name="stream">Excel文件流</param>
    /// <param name="fileName">文件名(可选,用于日志记录)</param>
    /// <returns>导入结果,包含成功/失败数量和详细错误信息</returns>
    Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(
        Stream stream,
        string? fileName = null);

    /// <summary>
    /// 生成患者导入模板
    /// </summary>
    /// <returns>包含示例数据的Excel模板流</returns>
    MemoryStream GenerateImportTemplate();
    // ⚠️ 注意: 同步方法(生成模板不涉及I/O),直接返回MemoryStream
}
```

**扩展方法设计规范**:

| 方法类型 | 命名模式 | 返回类型 | 示例 |
|---------|---------|---------|------|
| **搜索** | `SearchAsync` | `Task<ServiceResult<List<T>>>` | `SearchAsync(string keyword)` |
| **复杂查询** | `QueryAsync` | `Task<ServiceResult<List<T>>>` | `QueryAsync(filter, startDate, endDate)` |
| **批量删除** | `BatchDeleteAsync` | `Task<ServiceResult<BatchResultDto>>` | `BatchDeleteAsync(List<Guid> ids)` |
| **导入** | `ImportFromExcelAsync` | `Task<ServiceResult<ImportResultDto<T>>>` | `ImportFromExcelAsync(Stream)` |
| **导出** | `ExportAsync` | `Task<MemoryStream>` | `ExportAsync(string? filter)` |
| **生成模板** | `GenerateImportTemplate` | `MemoryStream` | `GenerateImportTemplate()` |

### 3.3 聚合根服务接口(复杂场景)

**场景**: 病案聚合根管理(MedicalCase聚合Consultation + Prescription)

```csharp
/// <summary>
/// 病案服务接口(聚合根模式)
/// </summary>
public interface IMedicalCaseService
{
    // ========== 基础CRUD ==========
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(int page = 1, int pageSize = 20);
    Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);
    Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);

    // ========== 聚合根统一管理(核心亮点) ==========

    /// <summary>
    /// 创建完整的医疗案例(包含诊疗记录和可选的处方)
    /// 作为聚合根统一管理整个诊疗流程
    /// </summary>
    /// <param name="caseDto">病案创建DTO</param>
    /// <param name="consultationDto">诊疗记录创建DTO</param>
    /// <param name="prescriptionDto">处方创建DTO(可选)</param>
    /// <returns>完整的病案数据</returns>
    Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(
        MedicalCaseCreateDto caseDto,
        ConsultationCreateDto consultationDto,
        PrescriptionCreateDto? prescriptionDto = null);

    /// <summary>
    /// 根据ID获取完整的医疗案例(包含所有关联数据)
    /// </summary>
    /// <param name="id">病案ID</param>
    /// <returns>完整的病案详情</returns>
    Task<ServiceResult<MedicalCaseDetailDto>> GetByIdWithDetailsAsync(Guid id);

    /// <summary>
    /// 更新病案的诊断信息
    /// </summary>
    /// <param name="medicalCaseId">病案ID</param>
    /// <param name="dto">诊断更新DTO</param>
    /// <returns>更新后的诊断信息</returns>
    Task<ServiceResult<ConsultationDto>> UpdateConsultationAsync(
        Guid medicalCaseId,
        ConsultationUpdateDto dto);

    /// <summary>
    /// 更新病案的处方信息
    /// </summary>
    /// <param name="medicalCaseId">病案ID</param>
    /// <param name="dto">处方更新DTO</param>
    /// <returns>更新后的处方信息</returns>
    Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(
        Guid medicalCaseId,
        PrescriptionUpdateDto dto);

    /// <summary>
    /// 为已存在的医案创建处方
    /// 前置条件: MedicalCase和Consultation已存在
    /// </summary>
    /// <param name="medicalCaseId">病案ID</param>
    /// <param name="dto">处方创建DTO</param>
    /// <returns>创建的处方信息</returns>
    Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(
        Guid medicalCaseId,
        PrescriptionCreateDto dto);

    /// <summary>
    /// 删除医案的处方
    /// 支持单独删除Prescription,保留MedicalCase和Consultation
    /// </summary>
    /// <param name="medicalCaseId">病案ID</param>
    /// <returns>删除结果</returns>
    Task<ServiceResult> DeletePrescriptionAsync(Guid medicalCaseId);

    // ========== 复杂查询 ==========

    /// <summary>
    /// 根据患者ID获取医疗案例列表
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// 获取待看诊医案列表(Status=Active)
    /// </summary>
    Task<ServiceResult<List<PendingMedicalCaseDto>>> GetPendingCasesAsync();

    /// <summary>
    /// 查询病案列表(支持多条件组合查询)
    /// </summary>
    /// <param name="patientName">患者姓名关键字(模糊匹配)</param>
    /// <param name="startDate">开始日期(过滤CreatedAt)</param>
    /// <param name="endDate">结束日期(过滤CreatedAt)</param>
    /// <param name="diagnosisKeyword">诊断关键字(搜索TCMDiagnosis)</param>
    Task<ServiceResult<List<MedicalCaseDto>>> QueryAsync(
        string? patientName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? diagnosisKeyword = null);
}
```

**聚合根设计亮点**:
- ✅ **级联创建**: `CreateWithDetailsAsync`一次性创建完整病案(事务保证)
- ✅ **细粒度控制**: 支持单独更新/删除子实体(通过聚合根协调)
- ✅ **边界清晰**: 所有子实体操作必须通过聚合根(MedicalCase)进行
- ❌ **禁止**: 不允许直接暴露`IConsultationService`或`IPrescriptionService`

### 3.4 CancellationToken支持(长时间操作)

**场景**: 认证服务需要支持请求取消

```csharp
/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户登录验证(返回JWT Token)
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="cancellationToken">取消令牌(支持客户端取消)</param>
    /// <returns>登录响应(包含Token)</returns>
    Task<ServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新Token(使用RefreshToken获取新AccessToken)
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的登录响应</returns>
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
```

**何时使用CancellationToken**:
- ✅ **长时间I/O操作**: 文件上传、Excel导入、外部API调用
- ✅ **认证操作**: 登录、Token刷新(可能涉及网络验证)
- ✅ **复杂查询**: 大数据量搜索、报表生成
- ❌ **简单CRUD**: `GetByIdAsync`, `CreateAsync`等快速操作可省略

---

## 4. ServiceResult封装使用

### 4.1 ServiceResult结构理解

**定义位置**: `src/Shared/LYBT.Shared.Models/Common/ServiceResult.cs`

```csharp
/// <summary>
/// 服务结果封装(无数据)
/// </summary>
public class ServiceResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细错误列表(用于验证错误等)
    /// </summary>
    public List<string>? Errors { get; set; }

    // ========== 工厂方法(快速创建) ==========

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ServiceResult Success()
        => new() { IsSuccess = true };

    /// <summary>
    /// 创建失败结果(单个错误消息)
    /// </summary>
    public static ServiceResult Fail(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };

    /// <summary>
    /// 创建失败结果(多个错误)
    /// </summary>
    public static ServiceResult Fail(List<string> errors)
        => new()
        {
            IsSuccess = false,
            Errors = errors,
            ErrorMessage = string.Join(", ", errors)
        };
}

/// <summary>
/// 服务结果封装(带数据)
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ServiceResult<T> : ServiceResult
{
    /// <summary>
    /// 返回数据
    /// </summary>
    public T? Data { get; set; }

    // ========== 工厂方法(快速创建) ==========

    /// <summary>
    /// 创建成功结果(带数据)
    /// </summary>
    public static ServiceResult<T> Success(T data)
        => new() { IsSuccess = true, Data = data };

    /// <summary>
    /// 创建失败结果(单个错误消息)
    /// </summary>
    public new static ServiceResult<T> Fail(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };

    /// <summary>
    /// 创建失败结果(多个错误)
    /// </summary>
    public new static ServiceResult<T> Fail(List<string> errors)
        => new()
        {
            IsSuccess = false,
            Errors = errors,
            ErrorMessage = string.Join(", ", errors)
        };
}
```

### 4.2 Service层使用ServiceResult

#### 示例1: 简单查询(返回单个实体)

```csharp
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientService> _logger;

    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        try
        {
            // 从Repository获取实体
            var patient = await _repository.GetByIdAsync(id);

            // ❌ 错误示例: 返回null
            // if (patient == null) return null;

            // ✅ 正确示例: 使用ServiceResult.Fail()
            if (patient == null)
            {
                _logger.LogWarning("患者不存在: {Id}", id);
                return ServiceResult<PatientDto>.Fail("患者不存在");
            }

            // 映射为DTO
            var dto = _mapper.Map<PatientDto>(patient);

            // 返回成功结果
            return ServiceResult<PatientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者失败: {Id}", id);
            return ServiceResult<PatientDto>.Fail($"获取患者失败: {ex.Message}");
        }
    }
}
```

#### 示例2: 创建操作(带业务规则验证)

```csharp
public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
{
    try
    {
        // ========== 业务规则验证 ==========

        // 1. 检查必填字段
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return ServiceResult<PatientDto>.Fail("患者姓名不能为空");
        }

        // 2. 检查身份证号重复
        var exists = await _repository.ExistsAsync(p => p.IdCard == dto.IdCard);
        if (exists)
        {
            _logger.LogWarning("身份证号已存在: {IdCard}", dto.IdCard);
            return ServiceResult<PatientDto>.Fail("身份证号已存在");
        }

        // 3. 复杂验证(使用FluentValidation)
        var validator = new PatientCreateDtoValidator();
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => e.ErrorMessage)
                .ToList();
            return ServiceResult<PatientDto>.Fail(errors);
        }

        // ========== 创建实体 ==========

        var patient = _mapper.Map<Patient>(dto);
        patient.Id = Guid.NewGuid();
        patient.CreatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(patient);
        var patientDto = _mapper.Map<PatientDto>(created);

        _logger.LogInformation("患者创建成功: {Id}", created.Id);
        return ServiceResult<PatientDto>.Success(patientDto);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "数据库写入失败");
        return ServiceResult<PatientDto>.Fail("数据库写入失败");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建患者失败");
        return ServiceResult<PatientDto>.Fail($"创建患者失败: {ex.Message}");
    }
}
```

#### 示例3: 批量操作(返回详细结果)

```csharp
public async Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(
    Stream stream,
    string? fileName = null)
{
    try
    {
        var result = new ImportResultDto<PatientDto>();
        var patients = ParseExcelData(stream); // 解析Excel

        foreach (var (rowNumber, patientDto) in patients.WithIndex())
        {
            try
            {
                // 验证单条数据
                var validator = new PatientCreateDtoValidator();
                var validationResult = await validator.ValidateAsync(patientDto);
                if (!validationResult.IsValid)
                {
                    result.FailedRows.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                        Data = patientDto
                    });
                    continue;
                }

                // 检查重复
                var exists = await _repository.ExistsAsync(p => p.IdCard == patientDto.IdCard);
                if (exists)
                {
                    result.FailedRows.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = "身份证号已存在",
                        Data = patientDto
                    });
                    continue;
                }

                // 保存实体
                var patient = _mapper.Map<Patient>(patientDto);
                await _repository.AddAsync(patient);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入第{RowNumber}行失败", rowNumber);
                result.FailedRows.Add(new ImportErrorDto
                {
                    RowNumber = rowNumber,
                    ErrorMessage = ex.Message,
                    Data = patientDto
                });
            }
        }

        _logger.LogInformation("导入完成: 成功{Success}, 失败{Failed}",
            result.SuccessCount, result.FailedRows.Count);

        return ServiceResult<ImportResultDto<PatientDto>>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Excel导入失败");
        return ServiceResult<ImportResultDto<PatientDto>>.Fail($"导入失败: {ex.Message}");
    }
}
```

### 4.3 Controller层解包ServiceResult

#### 示例1: 简单查询端点

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>
    /// 根据ID获取患者详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _patientService.GetByIdAsync(id);

        // ✅ 推荐方式: 统一处理ServiceResult
        if (!result.IsSuccess)
        {
            // 如果是"不存在"错误,返回404
            if (result.ErrorMessage?.Contains("不存在") == true)
            {
                return NotFound(new { error = result.ErrorMessage });
            }

            // 其他错误返回400
            return BadRequest(new { error = result.ErrorMessage });
        }

        // 成功返回200 + 数据
        return Ok(result.Data);
    }
}
```

#### 示例2: 创建端点(带验证错误处理)

```csharp
/// <summary>
/// 创建新患者
/// </summary>
[HttpPost]
[ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Create([FromBody] PatientCreateDto dto)
{
    var result = await _patientService.CreateAsync(dto);

    if (!result.IsSuccess)
    {
        // 返回详细错误(如验证错误)
        if (result.Errors != null && result.Errors.Any())
        {
            return BadRequest(new
            {
                message = result.ErrorMessage,
                errors = result.Errors // 返回详细错误列表
            });
        }

        return BadRequest(new { error = result.ErrorMessage });
    }

    // 成功返回201 + Location Header
    return CreatedAtAction(
        nameof(GetById),
        new { id = result.Data!.Id },
        result.Data);
}
```

#### 示例3: 批量导入端点(详细结果)

```csharp
/// <summary>
/// 从Excel导入患者数据
/// </summary>
[HttpPost("import")]
[ProducesResponseType(typeof(ImportResultDto<PatientDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ImportFromExcel(IFormFile file)
{
    if (file == null || file.Length == 0)
    {
        return BadRequest(new { error = "文件不能为空" });
    }

    using var stream = file.OpenReadStream();
    var result = await _patientService.ImportFromExcelAsync(stream, file.FileName);

    if (!result.IsSuccess)
    {
        return BadRequest(new { error = result.ErrorMessage });
    }

    // 返回详细导入结果
    return Ok(new
    {
        successCount = result.Data!.SuccessCount,
        failedCount = result.Data.FailedRows.Count,
        failedRows = result.Data.FailedRows.Select(f => new
        {
            f.RowNumber,
            f.ErrorMessage,
            patientName = ((PatientCreateDto)f.Data).Name
        })
    });
}
```

### 4.4 ServiceResult vs 异常抛出决策表

| 场景 | ServiceResult | 异常抛出 | 推荐方案 | 理由 |
|------|--------------|---------|---------|------|
| **数据不存在** | ✅ `Fail("不存在")` | ❌ | ServiceResult | 预期的业务场景 |
| **业务规则不满足** | ✅ `Fail("规则错误")` | ❌ | ServiceResult | 预期的业务逻辑 |
| **参数验证失败** | ✅ `Fail(errors)` | ✅ `ValidationException` | ServiceResult(推荐) | 统一错误处理 |
| **重复数据** | ✅ `Fail("已存在")` | ❌ | ServiceResult | 预期的业务场景 |
| **权限不足** | ✅ `Fail("无权限")` | ✅ `UnauthorizedException` | 两者皆可 | 看团队约定 |
| **数据库连接失败** | ❌ | ✅ `throw` | 异常抛出 | 系统级错误 |
| **内存溢出** | ❌ | ✅ `throw` | 异常抛出 | 系统级错误 |
| **第三方API失败** | ❌ | ✅ `throw` | 异常抛出 | 外部系统错误 |
| **配置文件缺失** | ❌ | ✅ `throw` | 异常抛出 | 启动时错误 |

**核心原则**:
- ✅ **预期的业务错误**: 使用`ServiceResult.Fail()`
- ✅ **系统级错误**: 抛出异常,由全局异常处理器捕获

---

## 5. 实现Service类

### 5.1 Service类标准结构

**位置**: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`

```csharp
using AutoMapper;
using LYBT.Entities.Models;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.DTOs.Patients;
using LYBT.Shared.Models.Pagination;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services;

/// <summary>
/// 患者服务实现
/// </summary>
public class PatientService : IPatientService
{
    // ========== 依赖注入(3个核心依赖) ==========

    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientService> _logger;

    /// <summary>
    /// 构造函数(依赖注入)
    /// </summary>
    public PatientService(
        IPatientRepository repository,
        IMapper mapper,
        ILogger<PatientService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    // ========== IPatientService接口实现 ==========

    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null)
    {
        // 实现逻辑...
    }

    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        // 实现逻辑...
    }

    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
    {
        // 实现逻辑...
    }

    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
    {
        // 实现逻辑...
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        // 实现逻辑...
    }

    // ========== 私有辅助方法 ==========

    /// <summary>
    /// 验证患者数据
    /// </summary>
    private async Task<ServiceResult> ValidatePatientAsync(PatientCreateDto dto)
    {
        // 验证逻辑...
    }

    /// <summary>
    /// 检查身份证号是否重复
    /// </summary>
    private async Task<bool> IsIdCardDuplicateAsync(string idCard, Guid? excludeId = null)
    {
        // 检查逻辑...
    }
}
```

**结构说明**:
- ✅ **依赖注入**: 构造函数注入Repository、AutoMapper、Logger
- ✅ **接口实现**: 实现IPatientService的所有方法
- ✅ **私有方法**: 辅助方法(验证、检查重复等)放在最后
- ✅ **分段注释**: 使用`========== 段落名称 ==========`分隔逻辑段

### 5.2 完整实现示例(GetPagedAsync)

```csharp
public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
    int page = 1,
    int pageSize = 20,
    string? keyword = null)
{
    try
    {
        // ========== 参数验证 ==========

        if (page < 1)
        {
            return ServiceResult<PagedResult<PatientDto>>.Fail("页码必须大于0");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return ServiceResult<PagedResult<PatientDto>>.Fail("每页数量必须在1-100之间");
        }

        // ========== 构建查询条件 ==========

        Expression<Func<Patient, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // 多字段模糊搜索(姓名、电话、身份证号)
            predicate = p =>
                p.Name.Contains(keyword) ||
                p.Phone.Contains(keyword) ||
                (p.IdCard != null && p.IdCard.Contains(keyword));
        }

        // ========== 执行分页查询 ==========

        var (items, totalCount) = await _repository.GetPagedAsync(
            pageNumber: page,
            pageSize: pageSize,
            predicate: predicate,
            orderBy: p => p.CreatedAt,
            descending: true); // 最新创建的排最前

        // ========== 映射为DTO ==========

        var patientDtos = _mapper.Map<List<PatientDto>>(items);

        // ========== 构建分页结果 ==========

        var pagedResult = new PagedResult<PatientDto>
        {
            Items = patientDtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        _logger.LogInformation("分页查询患者成功: Page={Page}, Size={Size}, Total={Total}",
            page, pageSize, totalCount);

        return ServiceResult<PagedResult<PatientDto>>.Success(pagedResult);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "分页查询患者失败: Page={Page}, Size={Size}, Keyword={Keyword}",
            page, pageSize, keyword);
        return ServiceResult<PagedResult<PatientDto>>.Fail($"查询失败: {ex.Message}");
    }
}
```

### 5.3 完整实现示例(CreateAsync)

```csharp
public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
{
    try
    {
        // ========== 业务规则验证 ==========

        // 1. FluentValidation验证
        var validator = new PatientCreateDtoValidator();
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => e.ErrorMessage)
                .ToList();
            _logger.LogWarning("患者数据验证失败: {Errors}", string.Join("; ", errors));
            return ServiceResult<PatientDto>.Fail(errors);
        }

        // 2. 检查身份证号重复
        if (!string.IsNullOrWhiteSpace(dto.IdCard))
        {
            var exists = await _repository.ExistsAsync(p => p.IdCard == dto.IdCard);
            if (exists)
            {
                _logger.LogWarning("身份证号已存在: {IdCard}", dto.IdCard);
                return ServiceResult<PatientDto>.Fail("身份证号已存在");
            }
        }

        // 3. 检查手机号重复
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var exists = await _repository.ExistsAsync(p => p.Phone == dto.Phone);
            if (exists)
            {
                _logger.LogWarning("手机号已存在: {Phone}", dto.Phone);
                return ServiceResult<PatientDto>.Fail("手机号已存在");
            }
        }

        // ========== 映射并创建实体 ==========

        var patient = _mapper.Map<Patient>(dto);
        patient.Id = Guid.NewGuid();
        patient.CreatedAt = DateTime.UtcNow;
        patient.UpdatedAt = DateTime.UtcNow;
        patient.IsDeleted = false;

        // ========== 持久化到数据库 ==========

        var created = await _repository.AddAsync(patient);

        // ========== 映射为DTO返回 ==========

        var patientDto = _mapper.Map<PatientDto>(created);

        _logger.LogInformation("患者创建成功: {Id} - {Name}", created.Id, created.Name);
        return ServiceResult<PatientDto>.Success(patientDto);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "数据库写入失败: {Name}", dto.Name);
        return ServiceResult<PatientDto>.Fail("数据库写入失败");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建患者失败: {Name}", dto.Name);
        return ServiceResult<PatientDto>.Fail($"创建失败: {ex.Message}");
    }
}
```

### 5.4 完整实现示例(UpdateAsync)

```csharp
public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
{
    try
    {
        // ========== 验证实体存在 ==========

        var patient = await _repository.GetByIdAsync(id);
        if (patient == null)
        {
            _logger.LogWarning("患者不存在: {Id}", id);
            return ServiceResult<PatientDto>.Fail("患者不存在");
        }

        // ========== 业务规则验证 ==========

        // 1. FluentValidation验证
        var validator = new PatientUpdateDtoValidator();
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => e.ErrorMessage)
                .ToList();
            return ServiceResult<PatientDto>.Fail(errors);
        }

        // 2. 检查身份证号重复(排除自己)
        if (!string.IsNullOrWhiteSpace(dto.IdCard) && dto.IdCard != patient.IdCard)
        {
            var exists = await _repository.ExistsAsync(p => p.IdCard == dto.IdCard && p.Id != id);
            if (exists)
            {
                return ServiceResult<PatientDto>.Fail("身份证号已被其他患者使用");
            }
        }

        // ========== 更新实体字段 ==========

        // 使用AutoMapper更新(保留Id、CreatedAt等审计字段)
        _mapper.Map(dto, patient);
        patient.UpdatedAt = DateTime.UtcNow;

        // ========== 持久化到数据库 ==========

        var updated = await _repository.UpdateAsync(patient);

        // ========== 映射为DTO返回 ==========

        var patientDto = _mapper.Map<PatientDto>(updated);

        _logger.LogInformation("患者更新成功: {Id}", id);
        return ServiceResult<PatientDto>.Success(patientDto);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "数据库更新失败: {Id}", id);
        return ServiceResult<PatientDto>.Fail("数据库更新失败");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新患者失败: {Id}", id);
        return ServiceResult<PatientDto>.Fail($"更新失败: {ex.Message}");
    }
}
```

### 5.5 完整实现示例(DeleteAsync - 软删除)

```csharp
public async Task<ServiceResult> DeleteAsync(Guid id)
{
    try
    {
        // ========== 验证实体存在 ==========

        var patient = await _repository.GetByIdAsync(id);
        if (patient == null)
        {
            _logger.LogWarning("患者不存在: {Id}", id);
            return ServiceResult.Fail("患者不存在");
        }

        // ========== 业务规则验证 ==========

        // 检查是否有关联的医案(如果有,不允许删除)
        var hasMedicalCases = await _medicalCaseRepository.ExistsAsync(m => m.PatientId == id);
        if (hasMedicalCases)
        {
            _logger.LogWarning("患者有关联医案,不允许删除: {Id}", id);
            return ServiceResult.Fail("患者有关联医案,不允许删除");
        }

        // ========== 执行软删除 ==========

        var success = await _repository.DeleteAsync(id);
        if (!success)
        {
            _logger.LogWarning("软删除患者失败: {Id}", id);
            return ServiceResult.Fail("删除失败");
        }

        _logger.LogInformation("患者删除成功: {Id}", id);
        return ServiceResult.Success();
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "数据库删除失败: {Id}", id);
        return ServiceResult.Fail("数据库删除失败");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "删除患者失败: {Id}", id);
        return ServiceResult.Fail($"删除失败: {ex.Message}");
    }
}
```

---

## 6. 依赖注入注册

### 6.1 模块扩展方法(推荐模式)

**位置**: `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs`

```csharp
using FluentValidation;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Patients;

/// <summary>
/// 患者模块依赖注入扩展
/// </summary>
public static class PatientsModuleExtensions
{
    /// <summary>
    /// 注册患者模块的所有服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象(可选)</param>
    /// <returns>服务集合(支持链式调用)</returns>
    public static IServiceCollection AddPatientsModule(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // ========== 注册服务接口 ==========

        services.AddScoped<IPatientService, PatientService>();

        // ========== 注册仓储接口 ==========

        services.AddScoped<IPatientRepository, PatientRepository>();

        // ========== 注册验证器 ==========

        // 自动注册当前程序集中的所有FluentValidation验证器
        services.AddValidatorsFromAssemblyContaining<PatientCreateDtoValidator>();

        // ========== 注册AutoMapper配置 ==========

        services.AddAutoMapper(typeof(PatientMappingProfile));

        return services;
    }
}
```

**设计亮点**:
- ✅ **统一注册**: 一个扩展方法注册整个模块
- ✅ **链式调用**: 返回`IServiceCollection`支持链式调用
- ✅ **自动注册**: 使用`AddValidatorsFromAssemblyContaining`和`AddAutoMapper`自动注册
- ✅ **可选配置**: `IConfiguration`参数用于模块特定配置(如开关、选项)

### 6.2 Startup.cs统一注册

**位置**: `src/Server/Services/LYBT.WebAPI/Startup.cs`

```csharp
using LYBT.Module.Auth;
using LYBT.Module.Consultation;
using LYBT.Module.Formula;
using LYBT.Module.Herbs;
using LYBT.Module.MedicalCase;
using LYBT.Module.Patients;
using LYBT.Module.Prescriptions;
using LYBT.Module.Users;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.WebAPI;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // ========== 注册所有业务模块(8个) ==========

        services.AddAuthModule(Configuration);
        services.AddPatientsModule(Configuration);
        services.AddHerbsModule(Configuration);
        services.AddMedicalCaseModule(Configuration);
        services.AddConsultationModule(Configuration);
        services.AddPrescriptionModule(Configuration);
        services.AddFormulaModule(Configuration);
        services.AddUsersModule(Configuration);

        // ========== 注册基础设施 ==========

        // 数据库上下文
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

        // ========== 注册通用服务 ==========

        // 泛型仓储(所有实体共享)
        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

        // 工作单元模式
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ========== 注册ASP.NET Core服务 ==========

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}
```

**注册顺序说明**:
1. **业务模块**: 先注册8个业务模块(按字母顺序)
2. **基础设施**: 注册DbContext、UnitOfWork等
3. **通用服务**: 注册泛型仓储等
4. **ASP.NET服务**: 最后注册Controllers、Swagger等

### 6.3 生命周期选择指南

| 生命周期 | 适用场景 | 示例 | 注册方式 | 线程安全 |
|---------|---------|------|---------|---------|
| **Scoped** | 服务、仓储、DbContext | `IPatientService`, `IPatientRepository`, `AppDbContext` | `AddScoped` | ❌ 每次请求独立 |
| **Singleton** | 无状态服务、配置、缓存 | `IConfiguration`, `IMemoryCache` | `AddSingleton` | ✅ 必须线程安全 |
| **Transient** | 轻量级无状态服务 | `IMapper`, `IValidator<T>` | `AddTransient` | ✅ 每次创建新实例 |

**推荐约定**:
- ✅ **所有Service接口**: 注册为`Scoped`(与DbContext生命周期一致)
- ✅ **所有Repository接口**: 注册为`Scoped`(避免跨请求数据污染)
- ✅ **DbContext**: 必须注册为`Scoped`(避免并发问题)
- ❌ **禁止**: 将DbContext注册为Singleton(会导致并发冲突)

---

## 7. Controller集成

### 7.1 Controller标准结构

**位置**: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`

```csharp
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.DTOs.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 患者管理API
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 需要认证
public class PatientsController : ControllerBase
{
    // ========== 依赖注入 ==========

    private readonly IPatientService _patientService;
    private readonly ILogger<PatientsController> _logger;

    /// <summary>
    /// 构造函数(依赖注入)
    /// </summary>
    public PatientsController(
        IPatientService patientService,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    // ========== API端点实现 ==========

    // GET, POST, PUT, DELETE方法...
}
```

### 7.2 完整API端点示例(5个核心端点)

#### 端点1: 分页查询

```csharp
/// <summary>
/// 分页查询患者列表
/// </summary>
/// <param name="page">页码(从1开始)</param>
/// <param name="pageSize">每页数量(1-100)</param>
/// <param name="keyword">搜索关键字(可选,支持姓名/电话/身份证号)</param>
/// <returns>分页结果</returns>
[HttpGet]
[ProducesResponseType(typeof(PagedResult<PatientDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> GetPaged(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? keyword = null)
{
    _logger.LogInformation("分页查询患者: Page={Page}, Size={Size}, Keyword={Keyword}",
        page, pageSize, keyword);

    var result = await _patientService.GetPagedAsync(page, pageSize, keyword);

    if (!result.IsSuccess)
    {
        _logger.LogWarning("分页查询失败: {Error}", result.ErrorMessage);
        return BadRequest(new { error = result.ErrorMessage });
    }

    return Ok(result.Data);
}
```

#### 端点2: 根据ID查询

```csharp
/// <summary>
/// 根据ID获取患者详情
/// </summary>
/// <param name="id">患者ID</param>
/// <returns>患者详情</returns>
[HttpGet("{id}")]
[ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetById(Guid id)
{
    _logger.LogInformation("获取患者详情: {Id}", id);

    var result = await _patientService.GetByIdAsync(id);

    if (!result.IsSuccess)
    {
        // 如果是"不存在"错误,返回404
        if (result.ErrorMessage?.Contains("不存在") == true)
        {
            _logger.LogWarning("患者不存在: {Id}", id);
            return NotFound(new { error = result.ErrorMessage });
        }

        // 其他错误返回400
        return BadRequest(new { error = result.ErrorMessage });
    }

    return Ok(result.Data);
}
```

#### 端点3: 创建患者

```csharp
/// <summary>
/// 创建新患者
/// </summary>
/// <param name="dto">患者创建DTO</param>
/// <returns>创建成功的患者信息</returns>
[HttpPost]
[ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Create([FromBody] PatientCreateDto dto)
{
    _logger.LogInformation("创建患者: {Name}", dto.Name);

    var result = await _patientService.CreateAsync(dto);

    if (!result.IsSuccess)
    {
        // 返回详细错误(如验证错误)
        if (result.Errors != null && result.Errors.Any())
        {
            _logger.LogWarning("患者数据验证失败: {Errors}",
                string.Join("; ", result.Errors));
            return BadRequest(new
            {
                message = result.ErrorMessage,
                errors = result.Errors
            });
        }

        _logger.LogWarning("创建患者失败: {Error}", result.ErrorMessage);
        return BadRequest(new { error = result.ErrorMessage });
    }

    // 成功返回201 + Location Header
    return CreatedAtAction(
        nameof(GetById),
        new { id = result.Data!.Id },
        result.Data);
}
```

#### 端点4: 更新患者

```csharp
/// <summary>
/// 更新患者信息
/// </summary>
/// <param name="id">患者ID</param>
/// <param name="dto">患者更新DTO</param>
/// <returns>更新后的患者信息</returns>
[HttpPut("{id}")]
[ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Update(Guid id, [FromBody] PatientUpdateDto dto)
{
    _logger.LogInformation("更新患者: {Id}", id);

    var result = await _patientService.UpdateAsync(id, dto);

    if (!result.IsSuccess)
    {
        // 如果是"不存在"错误,返回404
        if (result.ErrorMessage?.Contains("不存在") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        // 验证错误返回详细信息
        if (result.Errors != null && result.Errors.Any())
        {
            return BadRequest(new
            {
                message = result.ErrorMessage,
                errors = result.Errors
            });
        }

        return BadRequest(new { error = result.ErrorMessage });
    }

    return Ok(result.Data);
}
```

#### 端点5: 删除患者

```csharp
/// <summary>
/// 删除患者(软删除)
/// </summary>
/// <param name="id">患者ID</param>
/// <returns>删除结果</returns>
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(Guid id)
{
    _logger.LogInformation("删除患者: {Id}", id);

    var result = await _patientService.DeleteAsync(id);

    if (!result.IsSuccess)
    {
        // 如果是"不存在"错误,返回404
        if (result.ErrorMessage?.Contains("不存在") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        return BadRequest(new { error = result.ErrorMessage });
    }

    // 成功返回204 No Content
    return NoContent();
}
```

### 7.3 Swagger文档配置

**位置**: `src/Server/Services/LYBT.WebAPI/Program.cs`

```csharp
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 配置Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LYBT WebAPI",
        Version = "v1",
        Description = "LYBTZYZS项目Server端API文档"
    });

    // 启用XML注释
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // JWT认证配置
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

---

## 8. Repository接口扩展

### 8.1 何时扩展Repository接口

| 场景 | 是否扩展 | 说明 |
|------|---------|------|
| **基础CRUD** | ❌ | 使用`IBaseRepository<T>`已有方法 |
| **特殊查询(拼音检索)** | ✅ | 扩展`IHerbRepository.GetByNameOrPinyinAsync` |
| **特殊查询(名称精确查询)** | ✅ | 扩展`IPatientRepository.GetByIdCardAsync` |
| **复杂业务逻辑** | ❌ | 应在Service层实现 |
| **跨表查询** | ⚠️ | 简单关联可扩展,复杂关联用LINQ |

### 8.2 Repository接口扩展示例

**场景**: 中药材仓储扩展拼音检索

**位置**: `src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbRepository.cs`

```csharp
using LYBT.Entities.Models;
using LYBT.Infrastructure.Repositories;

namespace LYBT.Module.Herbs.Interfaces;

/// <summary>
/// 中药材仓储接口(扩展2个业务查询方法)
/// </summary>
public interface IHerbRepository : IRepository<Herb>
{
    /// <summary>
    /// 根据名称获取药材(精确匹配)
    /// </summary>
    /// <param name="name">药材名称</param>
    /// <returns>药材实体,不存在返回null</returns>
    Task<Herb?> GetByNameAsync(string name);

    /// <summary>
    /// 按名称或拼音码查询药材
    /// 优先精确匹配名称,其次模糊匹配拼音码
    /// </summary>
    /// <param name="searchTerm">搜索词(药材名称或拼音码)</param>
    /// <returns>匹配的药材实体,不存在返回null</returns>
    Task<Herb?> GetByNameOrPinyinAsync(string searchTerm);
}
```

**实现** (HerbRepository.cs):

```csharp
using LYBT.Entities.Models;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Repositories;

public class HerbRepository : BaseRepository<Herb>, IHerbRepository
{
    public HerbRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Herb?> GetByNameAsync(string name)
    {
        return await _dbSet
            .Where(h => h.Name == name && !h.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<Herb?> GetByNameOrPinyinAsync(string searchTerm)
    {
        // 1. 优先精确匹配名称
        var herb = await _dbSet
            .Where(h => h.Name == searchTerm && !h.IsDeleted)
            .FirstOrDefaultAsync();

        if (herb != null)
        {
            return herb;
        }

        // 2. 模糊匹配拼音首字母(如"dg"匹配"当归")
        return await _dbSet
            .Where(h => h.PinyinAbbreviation.Contains(searchTerm) && !h.IsDeleted)
            .FirstOrDefaultAsync();
    }
}
```

### 8.3 Repository接口 vs Service接口职责分离

| 对比维度 | Repository接口 | Service接口 |
|---------|---------------|-------------|
| **职责** | 数据访问(查询、持久化) | 业务逻辑(验证、编排) |
| **返回类型** | `Task<TEntity>`, `Task<List<TEntity>>` | `Task<ServiceResult<TDto>>` |
| **依赖方向** | 被Service依赖 | 被Controller依赖 |
| **扩展内容** | 特殊查询方法 | 业务方法 |
| **事务管理** | 支持显式事务 | 通过UnitOfWork管理 |

**正确示例**:

```csharp
// ✅ Repository接口: 仅包含数据查询
public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByIdCardAsync(string idCard); // 数据查询
}

// ✅ Service接口: 包含业务逻辑
public interface IPatientService
{
    Task<ServiceResult<bool>> ValidatePatientEligibilityAsync(Guid patientId); // 业务逻辑
}
```

**错误示例**:

```csharp
// ❌ Repository接口: 不应包含业务逻辑
public interface IPatientRepository
{
    Task<bool> ValidatePatientEligibilityAsync(Guid patientId); // 业务逻辑(错误)
}
```

---

## 9. Mock测试支持

### 9.1 单元测试中Mock Service接口

**工具**: NSubstitute (推荐) 或 Moq

**示例**: 测试PatientsController

**位置**: `tests/UnitTests/Server/Services/LYBT.WebAPI.Tests/Controllers/PatientsControllerTests.cs`

```csharp
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.DTOs.Patients;
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.WebAPI.Tests.Controllers;

public class PatientsControllerTests
{
    private readonly IPatientService _mockPatientService;
    private readonly ILogger<PatientsController> _mockLogger;
    private readonly PatientsController _controller;

    public PatientsControllerTests()
    {
        // Arrange - Mock依赖
        _mockPatientService = Substitute.For<IPatientService>();
        _mockLogger = Substitute.For<ILogger<PatientsController>>();

        _controller = new PatientsController(_mockPatientService, _mockLogger);
    }

    [Fact]
    public async Task GetById_Should_Return_OkResult_When_Patient_Exists()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedPatient = new PatientDto
        {
            Id = patientId,
            Name = "张三",
            Phone = "13800138000"
        };

        // Mock IPatientService.GetByIdAsync
        _mockPatientService.GetByIdAsync(patientId)
            .Returns(ServiceResult<PatientDto>.Success(expectedPatient));

        // Act
        var result = await _controller.GetById(patientId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualPatient = Assert.IsType<PatientDto>(okResult.Value);
        Assert.Equal("张三", actualPatient.Name);
        Assert.Equal("13800138000", actualPatient.Phone);

        // 验证Service方法被调用
        await _mockPatientService.Received(1).GetByIdAsync(patientId);
    }

    [Fact]
    public async Task GetById_Should_Return_NotFound_When_Patient_Does_Not_Exist()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        // Mock IPatientService.GetByIdAsync返回失败
        _mockPatientService.GetByIdAsync(patientId)
            .Returns(ServiceResult<PatientDto>.Fail("患者不存在"));

        // Act
        var result = await _controller.GetById(patientId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_Should_Return_CreatedAtAction_When_Successful()
    {
        // Arrange
        var createDto = new PatientCreateDto
        {
            Name = "李四",
            Phone = "13900139000",
            IdCard = "110101199001011234"
        };

        var createdPatient = new PatientDto
        {
            Id = Guid.NewGuid(),
            Name = "李四",
            Phone = "13900139000"
        };

        // Mock IPatientService.CreateAsync
        _mockPatientService.CreateAsync(Arg.Any<PatientCreateDto>())
            .Returns(ServiceResult<PatientDto>.Success(createdPatient));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var actualPatient = Assert.IsType<PatientDto>(createdResult.Value);
        Assert.Equal("李四", actualPatient.Name);

        // 验证Service方法被调用
        await _mockPatientService.Received(1).CreateAsync(Arg.Any<PatientCreateDto>());
    }

    [Fact]
    public async Task Create_Should_Return_BadRequest_When_Validation_Fails()
    {
        // Arrange
        var createDto = new PatientCreateDto
        {
            Name = "", // 空名称(验证失败)
            Phone = "13900139000"
        };

        var errors = new List<string> { "患者姓名不能为空" };

        // Mock IPatientService.CreateAsync返回验证错误
        _mockPatientService.CreateAsync(Arg.Any<PatientCreateDto>())
            .Returns(ServiceResult<PatientDto>.Fail(errors));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        // 可以进一步验证返回的错误信息
    }
}
```

### 9.2 集成测试中使用真实Service实现

**示例**: 测试PatientService(使用InMemory数据库)

**位置**: `tests/IntegrationTests/Server/Modules/LYBT.Module.Patients.IntegrationTests/Services/PatientServiceTests.cs`

```csharp
using AutoMapper;
using LYBT.Entities.Models;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Mapping;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.DTOs.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Module.Patients.IntegrationTests.Services;

public class PatientServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IPatientService _patientService;

    public PatientServiceTests()
    {
        // 使用InMemory数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // 使用真实的Repository和AutoMapper
        var repository = new PatientRepository(_context);
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PatientMappingProfile>();
        });
        var mapper = mapperConfig.CreateMapper();
        var logger = Substitute.For<ILogger<PatientService>>();

        _patientService = new PatientService(repository, mapper, logger);
    }

    [Fact]
    public async Task CreateAsync_Should_Persist_To_Database()
    {
        // Arrange
        var dto = new PatientCreateDto
        {
            Name = "王五",
            Phone = "13700137000",
            IdCard = "110101199101011234",
            Gender = "男",
            BirthDate = new DateTime(1991, 1, 1)
        };

        // Act
        var result = await _patientService.CreateAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("王五", result.Data.Name);

        // 验证数据库持久化
        var savedPatient = await _context.Patients.FindAsync(result.Data.Id);
        Assert.NotNull(savedPatient);
        Assert.Equal("王五", savedPatient.Name);
        Assert.Equal("13700137000", savedPatient.Phone);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Correct_Patient()
    {
        // Arrange - 先创建一个患者
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "赵六",
            Phone = "13600136000",
            IdCard = "110101199201011234",
            Gender = "女",
            BirthDate = new DateTime(1992, 1, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _patientService.GetByIdAsync(patient.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("赵六", result.Data.Name);
        Assert.Equal(patient.Id, result.Data.Id);
    }

    [Fact]
    public async Task CreateAsync_Should_Fail_When_IdCard_Duplicate()
    {
        // Arrange - 先创建一个患者
        var existingPatient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "孙七",
            Phone = "13500135000",
            IdCard = "110101199301011234",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        _context.Patients.Add(existingPatient);
        await _context.SaveChangesAsync();

        // Act - 尝试创建相同身份证号的患者
        var dto = new PatientCreateDto
        {
            Name = "周八",
            Phone = "13400134000",
            IdCard = "110101199301011234" // 重复身份证号
        };
        var result = await _patientService.CreateAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("身份证号已存在", result.ErrorMessage);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

### 9.3 测试覆盖率清单

| 测试类型 | 覆盖内容 | 工具 | 覆盖率目标 |
|---------|---------|------|-----------|
| **单元测试** | Service接口方法(Mock Repository) | xUnit + NSubstitute | >80% |
| **集成测试** | Service接口方法(真实Repository + InMemoryDb) | xUnit + EF Core InMemory | >60% |
| **API测试** | Controller端点(真实Service + 真实DB) | xUnit + WebApplicationFactory | >50% |

---

## 10. 接口版本演进

### 10.1 兼容性变更(添加新方法)

**场景**: 在IPatientService中添加搜索功能

```csharp
// ========== v1.0 接口 ==========
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
}

// ========== v1.1 接口(兼容性变更:添加新方法) ==========
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

    // ✅ 新增方法(不影响现有调用)
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
}
```

**影响分析**:
- ✅ **现有调用**: 不受影响(向后兼容)
- ✅ **新功能**: 新Controller可以使用`SearchAsync`
- ✅ **实现类**: 需要实现新方法

### 10.2 破坏性变更(修改方法签名)

**场景**: GetByIdAsync需要支持includeDetails参数

```csharp
// ========== v1.0 接口 ==========
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
}

// ========== v2.0 接口(破坏性变更:修改方法签名) ==========
public interface IPatientService_V2
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id, bool includeDetails); // 新增参数
}
```

**处理方案**:
- ❌ **不推荐**: 直接修改现有接口(破坏现有调用)
- ✅ **推荐**: 创建新版本接口`IPatientService_V2`
- ✅ **替代方案**: 添加新方法`GetByIdWithDetailsAsync`(保持原方法不变)

```csharp
// ✅ 推荐方案: 添加新方法(保持兼容)
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

    // 新增方法(不破坏现有代码)
    Task<ServiceResult<PatientDetailDto>> GetByIdWithDetailsAsync(Guid id);
}
```

### 10.3 接口继承(渐进式扩展)

**场景**: 扩展只读接口为完整接口

```csharp
// ========== 基础接口(只读) ==========
public interface IReadOnlyPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<PatientDto>>> GetAllAsync();
}

// ========== 扩展接口(继承基础接口) ==========
public interface IPatientService : IReadOnlyPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

**优势**:
- ✅ **灵活性**: 某些场景只需要只读接口
- ✅ **权限控制**: 只读服务可以注入到只读Controller
- ✅ **测试隔离**: 可以单独测试只读功能

---

## 11. 常见问题与陷阱

### 11.1 反模式1: 接口依赖具体实现

#### ❌ 错误示例:

```csharp
// ❌ 接口方法参数依赖具体类型(Entity)
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(Patient entity); // Patient是Entity
}
```

#### ✅ 正确示例:

```csharp
// ✅ 接口方法参数使用DTO
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto); // DTO
}
```

### 11.2 反模式2: 接口方法抛出特定异常

#### ❌ 错误示例:

```csharp
/// <summary>
/// 创建患者
/// </summary>
/// <exception cref="DuplicateIdCardException">身份证号重复时抛出</exception>
Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
```

**问题**: 接口定义中承诺抛出特定异常,破坏了ServiceResult封装的初衷

#### ✅ 正确示例:

```csharp
/// <summary>
/// 创建患者
/// </summary>
/// <returns>
/// 成功返回创建的患者DTO,失败返回错误消息
/// </returns>
Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

// 实现中:
if (exists)
{
    return ServiceResult<PatientDto>.Fail("身份证号已存在");
}
```

### 11.3 反模式3: 接口方法使用out/ref参数

#### ❌ 错误示例:

```csharp
// ❌ 使用out参数(破坏可测试性,不支持异步)
Task<bool> TryGetPatientAsync(Guid id, out PatientDto patient);
```

**问题**: out/ref参数不支持异步,且难以Mock测试

#### ✅ 正确示例:

```csharp
// ✅ 使用返回值
Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

// 调用方:
var result = await _patientService.GetByIdAsync(id);
if (result.IsSuccess)
{
    var patient = result.Data;
    // 使用patient...
}
```

### 11.4 反模式4: 接口方法参数过多

#### ❌ 错误示例:

```csharp
// ❌ 参数过多(>5个),难以维护
Task<ServiceResult<PatientDto>> CreatePatientAsync(
    string name,
    string phone,
    string idCard,
    DateTime birthDate,
    string gender,
    string address,
    string notes);
```

#### ✅ 正确示例:

```csharp
// ✅ 使用DTO封装
Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

// PatientCreateDto封装所有参数
public class PatientCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdCard { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Notes { get; set; }
}
```

### 11.5 反模式5: 接口方法返回类型不一致

#### ❌ 错误示例:

```csharp
// ❌ 返回类型不一致
public interface IPatientService
{
    Task<PatientDto?> GetByIdAsync(Guid id); // 返回null表示失败
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto); // 返回ServiceResult
    Task<bool> DeleteAsync(Guid id); // 返回bool表示成功/失败
}
```

**问题**: 错误处理方式不统一,Controller需要多种处理逻辑

#### ✅ 正确示例:

```csharp
// ✅ 返回类型一致(所有方法返回ServiceResult)
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

### 11.6 反模式6: Service层直接返回Entity

#### ❌ 错误示例:

```csharp
public class PatientService : IPatientService
{
    public async Task<ServiceResult<Patient>> GetByIdAsync(Guid id)
    {
        var patient = await _repository.GetByIdAsync(id);
        return ServiceResult<Patient>.Success(patient); // ❌ 返回Entity
    }
}
```

**问题**: 暴露了数据库实体结构,违反DTO设计原则

#### ✅ 正确示例:

```csharp
public class PatientService : IPatientService
{
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        var patient = await _repository.GetByIdAsync(id);
        if (patient == null)
        {
            return ServiceResult<PatientDto>.Fail("患者不存在");
        }

        var dto = _mapper.Map<PatientDto>(patient); // ✅ 映射为DTO
        return ServiceResult<PatientDto>.Success(dto);
    }
}
```

### 11.7 反模式7: Controller层不解包ServiceResult

#### ❌ 错误示例:

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _patientService.GetByIdAsync(id);
    return Ok(result); // ❌ 直接返回ServiceResult(暴露内部结构)
}
```

**问题**: 返回的JSON包含`IsSuccess`, `ErrorMessage`等内部字段,不符合RESTful API规范

**返回示例**(错误):
```json
{
  "isSuccess": true,
  "data": { "id": "...", "name": "张三" },
  "errorMessage": null
}
```

#### ✅ 正确示例:

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _patientService.GetByIdAsync(id);

    if (!result.IsSuccess)
    {
        if (result.ErrorMessage?.Contains("不存在") == true)
        {
            return NotFound(new { error = result.ErrorMessage });
        }
        return BadRequest(new { error = result.ErrorMessage });
    }

    return Ok(result.Data); // ✅ 仅返回数据
}
```

**返回示例**(正确):
```json
{
  "id": "...",
  "name": "张三",
  "phone": "13800138000"
}
```

---

## 12. 检查清单

### 12.1 接口定义检查清单

- [ ] **命名规范**: 接口名称遵循`I{Entity}Service`模式
- [ ] **返回类型**: 所有方法返回`Task<ServiceResult<T>>`
- [ ] **参数命名**: 遵循camelCase,参数顺序合理
- [ ] **XML注释**: 提供完整的summary和param说明
- [ ] **依赖约束**: 仅依赖`LYBT.Shared.Models`(DTO模型)
- [ ] **方法命名**: 遵循`GetByIdAsync`, `CreateAsync`等标准模式
- [ ] **异步约定**: 所有涉及I/O的方法使用Async结尾
- [ ] **CancellationToken**: 长时间操作支持CancellationToken

### 12.2 Service实现检查清单

- [ ] **依赖注入**: 构造函数注入Repository、AutoMapper、Logger
- [ ] **业务验证**: 实现完整的业务规则验证(重复检查、必填项等)
- [ ] **Entity映射**: 使用AutoMapper映射Entity ↔ DTO
- [ ] **ServiceResult封装**: 所有方法返回ServiceResult封装
- [ ] **异常处理**: try-catch捕获系统级异常,业务错误用Fail()
- [ ] **日志记录**: 记录关键操作(创建、更新、删除、失败)
- [ ] **审计字段**: 设置CreatedAt、UpdatedAt等审计字段
- [ ] **软删除**: 删除操作设置IsDeleted=true

### 12.3 依赖注入注册检查清单

- [ ] **模块扩展方法**: 提供`AddXxxModule`扩展方法
- [ ] **Service注册**: 使用`AddScoped<IService, Service>()`
- [ ] **Repository注册**: 使用`AddScoped<IRepository, Repository>()`
- [ ] **验证器注册**: 使用`AddValidatorsFromAssemblyContaining<>`
- [ ] **AutoMapper注册**: 使用`AddAutoMapper(typeof(MappingProfile))`
- [ ] **生命周期**: Service和Repository注册为Scoped

### 12.4 Controller集成检查清单

- [ ] **依赖注入**: 构造函数注入IService接口
- [ ] **路由配置**: 使用`[Route("api/v1/[controller]")]`
- [ ] **HTTP方法**: 正确使用`[HttpGet]`, `[HttpPost]`等
- [ ] **ServiceResult解包**: 判断`IsSuccess`返回正确HTTP状态码
- [ ] **验证错误处理**: 返回详细的验证错误列表
- [ ] **404处理**: "不存在"错误返回NotFound
- [ ] **201响应**: 创建成功返回CreatedAtAction + Location Header
- [ ] **日志记录**: 记录关键API调用和错误

### 12.5 测试检查清单

- [ ] **单元测试**: Mock IService接口测试Controller
- [ ] **集成测试**: 使用InMemory数据库测试Service
- [ ] **正常流程**: 测试成功场景
- [ ] **异常流程**: 测试失败场景(不存在、验证失败、重复数据)
- [ ] **边界条件**: 测试空输入、null参数等
- [ ] **Mock验证**: 验证Service方法被调用次数
- [ ] **覆盖率**: Service接口方法覆盖率>80%

---

## 13. 参考资料

### 13.1 内部文档

- **接口层架构设计**: `docs/explanation/architecture/server/interfaces-layer-design.md`
- **DTO设计标准**: `docs/explanation/architecture/shared/dto-design-standard.md`
- **Server端三层架构**: `docs/explanation/architecture/server/README.md`
- **ServiceResult封装**: `src/Shared/LYBT.Shared.Models/Common/ServiceResult.cs`
- **代码规范文档**: `docs/standards/code-standards.md`

### 13.2 外部参考

- **依赖倒置原则(DIP)**: [Microsoft Docs - Dependency Inversion Principle](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles#dependency-inversion)
- **接口隔离原则(ISP)**: [Microsoft Docs - Interface Segregation Principle](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles#interface-segregation)
- **Repository模式**: [Microsoft Docs - Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design#the-repository-pattern)
- **NSubstitute文档**: https://nsubstitute.github.io/
- **AutoMapper文档**: https://docs.automapper.org/
- **FluentValidation文档**: https://docs.fluentvalidation.net/

### 13.3 相关源文件

| 文件路径 | 说明 |
|---------|------|
| `src/Server/Core/LYBT.Server.Interfaces/Services/IPatientService.cs` | 患者服务接口(8方法) |
| `src/Server/Core/LYBT.Server.Interfaces/Services/IHerbService.cs` | 中药材服务接口(10方法) |
| `src/Server/Core/LYBT.Server.Interfaces/Services/IMedicalCaseService.cs` | 病案服务接口(19方法) |
| `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 患者服务实现 |
| `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs` | 患者模块DI注册 |
| `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` | 患者Controller |
| `src/Shared/LYBT.Shared.Models/Common/ServiceResult.cs` | ServiceResult封装类 |

---

## 14. 更新历史

| 版本 | 日期 | 修改内容 | 负责人 |
|-----|------|---------|--------|
| v1.0 | 2025-10-30 | 初始版本,完整使用指南 | Server端开发组 |

---

**文档所有权**: Server端开发组
**审阅人**: 架构组
**批准人**: 技术负责人
