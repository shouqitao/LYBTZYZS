# Design: print-prescription-slip

## Overview

实现处方笺打印功能的完整数据集成，包括诊所配置、患者信息获取和诊断数据映射。

## Architecture

### 组件关系图

```
┌─────────────────────────────────────────────────────────────────────┐
│                        PrescriptionPanelViewModel                    │
│                              (调用方)                                │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │ PrintPrescriptionAsync(patientId, consultationId)
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       PrescriptionPrintService                       │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────────┐ │
│  │PopulateClinicInfo│ │PopulatePatientInfo│ │PopulateDiagnosisInfo│ │
│  └────────┬─────────┘ └────────┬─────────┘ └──────────┬───────────┘ │
└───────────┼────────────────────┼──────────────────────┼─────────────┘
            │                    │                      │
            ▼                    ▼                      ▼
┌───────────────────┐ ┌───────────────────┐ ┌─────────────────────────┐
│IClinicSettingsService│ │IPatientDataManager│ │ ConsultationDto (传入) │
│ (从appsettings读取) │ │  (患者数据访问)   │ │ TCMDiagnosis           │
│                   │ │                   │ │ TreatmentPrinciple      │
└───────────────────┘ └───────────────────┘ └─────────────────────────┘
```

## Data Models

### ClinicSettings (新增)

```csharp
// 位置: src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Models/ClinicSettings.cs
namespace LYBT.Desktop.Infrastructure.Models;

/// <summary>
/// 诊所配置信息
/// OpenSpec: print-prescription-slip
/// </summary>
public class ClinicSettings
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "ClinicSettings";

    /// <summary>
    /// 诊所名称
    /// </summary>
    public string Name { get; set; } = "中医诊所";

    /// <summary>
    /// 诊所地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 诊所电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 科别
    /// </summary>
    public string Department { get; set; } = "中医科";
}
```

### appsettings.json 配置结构

```json
{
  "ClinicSettings": {
    "Name": "凌隐宝堂中医诊所",
    "Address": "",
    "Phone": "",
    "Department": "中医科"
  }
}
```

## Interfaces

### IClinicSettingsService (新增)

```csharp
// 位置: src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Interfaces/IClinicSettingsService.cs
namespace LYBT.Desktop.Infrastructure.Interfaces;

/// <summary>
/// 诊所配置服务接口
/// OpenSpec: print-prescription-slip
/// </summary>
public interface IClinicSettingsService
{
    /// <summary>
    /// 获取当前诊所配置
    /// 支持热更新，配置文件修改后自动生效
    /// </summary>
    ClinicSettings GetSettings();
}
```

### IPrescriptionPrintService 扩展

```csharp
// 位置: src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Interfaces/IPrescriptionPrintService.cs
// 新增方法签名（保持原有方法向后兼容）

/// <summary>
/// 打印处方（带完整上下文）
/// </summary>
/// <param name="prescription">处方数据</param>
/// <param name="patientId">患者ID</param>
/// <param name="consultation">诊断信息</param>
Task PrintPrescriptionAsync(
    PrescriptionDto prescription,
    Guid patientId,
    ConsultationDto? consultation);

/// <summary>
/// 预览处方（带完整上下文）
/// </summary>
Task<FlowDocument> PreviewPrescriptionAsync(
    PrescriptionDto prescription,
    Guid patientId,
    ConsultationDto? consultation);
```

## Implementation Details

### ClinicSettingsService 实现

```csharp
// 位置: src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Services/ClinicSettingsService.cs
namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 诊所配置服务实现
/// OpenSpec: print-prescription-slip
///
/// 职责:
/// - 从本地配置文件读取诊所信息
/// - 支持配置热更新（基于IConfigurationService的reloadOnChange）
/// </summary>
public class ClinicSettingsService : IClinicSettingsService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ClinicSettingsService> _logger;

    public ClinicSettingsService(
        IConfigurationService configurationService,
        ILogger<ClinicSettingsService> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    public ClinicSettings GetSettings()
    {
        try
        {
            var section = _configurationService.GetSection(ClinicSettings.SectionName);
            var settings = section.Get<ClinicSettings>() ?? new ClinicSettings();

            _logger.LogDebug("读取诊所配置: {ClinicName}", settings.Name);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取诊所配置失败，使用默认值");
            return new ClinicSettings();
        }
    }
}
```

### PrescriptionPrintService 修改

#### 构造函数注入

```csharp
// 新增依赖注入
private readonly IClinicSettingsService _clinicSettingsService;
private readonly IPatientDataManager _patientDataManager;

public PrescriptionPrintService(
    IClinicSettingsService clinicSettingsService,
    IPatientDataManager patientDataManager,
    ILogger<PrescriptionPrintService> logger)
{
    _clinicSettingsService = clinicSettingsService;
    _patientDataManager = patientDataManager;
    _logger = logger;
}
```

#### PopulateClinicInfo 实现

```csharp
/// <summary>
/// 填充诊所信息
/// </summary>
private void PopulateClinicInfo(PrescriptionPrintDto printDto)
{
    var settings = _clinicSettingsService.GetSettings();

    printDto.ClinicName = settings.Name;
    printDto.ClinicAddress = settings.Address;
    printDto.ClinicPhone = settings.Phone;
    // Department 可用于科别字段
}
```

#### PopulatePatientInfo 实现

```csharp
/// <summary>
/// 填充患者信息
/// </summary>
private async Task PopulatePatientInfoAsync(PrescriptionPrintDto printDto, Guid patientId)
{
    var patient = await _patientDataManager.GetPatientByIdAsync(patientId);

    if (patient == null)
    {
        _logger.LogWarning("未找到患者信息: {PatientId}", patientId);
        return;
    }

    printDto.PatientName = patient.Name ?? string.Empty;
    printDto.Gender = patient.Gender ?? string.Empty;
    printDto.Age = CalculateAge(patient.BirthDate);
    printDto.Address = patient.Address;
    printDto.Phone = patient.Phone;
}

private int CalculateAge(DateTime? birthDate)
{
    if (!birthDate.HasValue) return 0;

    var today = DateTime.Today;
    var age = today.Year - birthDate.Value.Year;
    if (birthDate.Value.Date > today.AddYears(-age)) age--;
    return age;
}
```

#### PopulateDiagnosisInfo 实现

```csharp
/// <summary>
/// 填充诊断信息
/// 映射: TCMDiagnosis → 诊断, TreatmentPrinciple → 诊见
/// </summary>
private void PopulateDiagnosisInfo(PrescriptionPrintDto printDto, ConsultationDto? consultation)
{
    if (consultation == null)
    {
        _logger.LogDebug("无诊断信息传入");
        return;
    }

    // "诊断"字段使用中医诊断
    printDto.TCMDiagnosis = consultation.TCMDiagnosis;

    // "诊见"字段使用治疗原则
    printDto.TreatmentPrinciple = consultation.TreatmentPrinciple;
}
```

### 调用方修改 (PrescriptionPanelViewModel)

```csharp
// 位置: src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionPanelViewModel.cs

private async Task PrintPrescriptionAsync()
{
    if (CurrentPrescription == null) return;

    // 获取当前医案的诊断信息
    var consultation = _medicalCaseDataManager.CurrentConsultation;
    var patientId = _medicalCaseDataManager.CurrentPatientId;

    if (!patientId.HasValue)
    {
        _logger.LogWarning("无法打印处方：未选择患者");
        return;
    }

    await _prescriptionPrintService.PrintPrescriptionAsync(
        CurrentPrescription,
        patientId.Value,
        consultation);
}
```

## Sequence Diagram

### 打印流程时序图

```
┌─────────────────┐     ┌─────────────────────┐     ┌───────────────────┐
│PrescriptionPanel│     │PrescriptionPrintSvc │     │ClinicSettingsSvc  │
│   ViewModel     │     │                     │     │                   │
└────────┬────────┘     └──────────┬──────────┘     └─────────┬─────────┘
         │                         │                          │
         │ PrintPrescriptionAsync  │                          │
         │ (prescription,patientId,│                          │
         │  consultation)          │                          │
         │────────────────────────>│                          │
         │                         │                          │
         │                         │ GetSettings()            │
         │                         │─────────────────────────>│
         │                         │                          │
         │                         │<─────────────────────────│
         │                         │   ClinicSettings         │
         │                         │                          │
         │                         │                          │
┌────────┴────────┐     ┌──────────┴──────────┐     ┌─────────┴─────────┐
│                 │     │                     │     │IPatientDataManager│
│                 │     │                     │     │                   │
└────────┬────────┘     └──────────┬──────────┘     └─────────┬─────────┘
         │                         │                          │
         │                         │ GetPatientByIdAsync      │
         │                         │─────────────────────────>│
         │                         │                          │
         │                         │<─────────────────────────│
         │                         │   PatientDto             │
         │                         │                          │
         │                         │ MapToPrintDto()          │
         │                         │ (fill all fields)        │
         │                         │                          │
         │                         │ BuildFlowDocument()      │
         │                         │                          │
         │                         │ PrintDialog.PrintDocument│
         │                         │                          │
         │<────────────────────────│                          │
         │   完成                   │                          │
         │                         │                          │
```

## DI Registration

```csharp
// 位置: src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/InfrastructureModule.cs

public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 新增诊所配置服务注册
    containerRegistry.RegisterSingleton<IClinicSettingsService, ClinicSettingsService>();
}
```

## File Changes Summary

| 文件 | 操作 | 说明 |
|------|------|------|
| `appsettings.json` | 修改 | 添加 ClinicSettings 配置节 |
| `ClinicSettings.cs` | 新增 | 诊所配置模型类 |
| `IClinicSettingsService.cs` | 新增 | 诊所配置服务接口 |
| `ClinicSettingsService.cs` | 新增 | 诊所配置服务实现 |
| `InfrastructureModule.cs` | 修改 | 注册诊所配置服务 |
| `IPrescriptionPrintService.cs` | 修改 | 添加带上下文的打印方法 |
| `PrescriptionPrintService.cs` | 修改 | 实现数据填充逻辑 |
| `PrescriptionPanelViewModel.cs` | 修改 | 调用时传递完整上下文 |
| `PrescriptionFlowDocumentBuilder.cs` | 修改 | 调整布局匹配模板 |

## Testing Strategy

### 单元测试

```csharp
// 位置: tests/UnitTests/Client/Desktop/LYBT.Desktop.Prescriptions.Tests/Services/PrescriptionPrintServiceTests.cs

[Fact]
public async Task MapToPrintDto_ShouldPopulateClinicInfo_FromSettings()
{
    // Arrange
    var mockClinicService = Substitute.For<IClinicSettingsService>();
    mockClinicService.GetSettings().Returns(new ClinicSettings
    {
        Name = "测试诊所",
        Address = "测试地址"
    });

    var service = new PrescriptionPrintService(mockClinicService, ...);

    // Act
    var result = await service.MapToPrintDtoAsync(prescription, patientId, consultation);

    // Assert
    result.ClinicName.Should().Be("测试诊所");
    result.ClinicAddress.Should().Be("测试地址");
}

[Fact]
public async Task MapToPrintDto_ShouldMapDiagnosisFields_Correctly()
{
    // Arrange
    var consultation = new ConsultationDto
    {
        TCMDiagnosis = "肝郁脾虚",
        TreatmentPrinciple = "疏肝健脾"
    };

    // Act
    var result = await service.MapToPrintDtoAsync(prescription, patientId, consultation);

    // Assert
    result.TCMDiagnosis.Should().Be("肝郁脾虚");
    result.TreatmentPrinciple.Should().Be("疏肝健脾");
}
```

## Backward Compatibility

- 保留原有 `PrintPrescriptionAsync(PrescriptionDto)` 方法签名
- 新方法作为重载添加，不影响现有调用
- 原方法内部调用新方法，使用默认值填充缺失参数

```csharp
// 向后兼容：原方法委托到新方法
public Task PrintPrescriptionAsync(PrescriptionDto prescription)
{
    return PrintPrescriptionAsync(prescription, Guid.Empty, null);
}
```

## Configuration Hot Reload

配置热更新通过 `IConfigurationService` 的 `reloadOnChange: true` 实现：

1. 用户修改 `appsettings.json` 中的 ClinicSettings
2. 配置系统自动检测文件变化
3. 下次调用 `GetSettings()` 返回新值
4. 无需重启应用

## Error Handling

```csharp
// 配置读取失败时使用默认值
public ClinicSettings GetSettings()
{
    try
    {
        return _configurationService.GetSection(ClinicSettings.SectionName)
            .Get<ClinicSettings>() ?? new ClinicSettings();
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "读取诊所配置失败，使用默认值");
        return new ClinicSettings();
    }
}

// 患者信息获取失败时继续打印，字段留空
private async Task PopulatePatientInfoAsync(...)
{
    var patient = await _patientDataManager.GetPatientByIdAsync(patientId);
    if (patient == null)
    {
        _logger.LogWarning("未找到患者信息: {PatientId}", patientId);
        return; // 不抛异常，字段保持默认值
    }
    // ...填充数据
}
```
