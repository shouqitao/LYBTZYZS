# Epic #1822 新增服务文档

**文档类型**: 补充文档（Explanation）
**创建时间**: 2025-11-05
**Epic来源**: #1822 - 启动到工作台流程端到端重构优化
**相关Issue**: #1823（API健康检查），#1825（连接模式）

---

## 📋 目录

- [概述](#概述)
- [Foundation层新增服务](#foundation层新增服务)
  - [ApplicationStateService](#applicationstateservice)
- [Auth模块新增服务](#auth模块新增服务)
  - [ConnectionSettingsService](#connectionsettingsservice)
  - [ConnectionMode枚举](#connectionmode枚举)
- [MedicalCase模块组件化重构](#medicalcase模块组件化重构)
- [服务注册](#服务注册)

---

## 概述

本文档记录Epic #1822实施过程中新增的8个服务类：
- **Foundation层**: 1个（ApplicationStateService）
- **Auth模块**: 1个（ConnectionSettingsService） + 1个枚举（ConnectionMode）
- **MedicalCase模块**: 6个（组件化重构服务）

这些服务均遵循**依赖注入**原则，通过接口定义行为，支持单元测试和模块解耦。

---

## Foundation层新增服务

### ApplicationStateService

**文件路径**:
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Application/IApplicationStateService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Application/ApplicationStateService.cs`

**Issue来源**: #1823 - API健康检查前置优化

#### 职责

管理Desktop应用的全局状态，包括：
- API健康状态（IsApiHealthy）
- API基础URL（从配置文件读取）
- 连接状态描述（ConnectionStatus）
- 最后一次健康检查时间（LastHealthCheckTime）

#### 接口定义

```csharp
public interface IApplicationStateService
{
    /// <summary>
    /// API是否健康（可访问）
    /// </summary>
    bool IsApiHealthy { get; set; }

    /// <summary>
    /// API基础URL
    /// </summary>
    string ApiBaseUrl { get; set; }

    /// <summary>
    /// 连接状态描述
    /// 例如："已连接"、"连接失败"、"连接超时"
    /// </summary>
    string ConnectionStatus { get; set; }

    /// <summary>
    /// 最后一次健康检查时间
    /// </summary>
    DateTime? LastHealthCheckTime { get; set; }

    /// <summary>
    /// 执行API健康检查
    /// </summary>
    /// <param name="timeoutSeconds">超时时间（秒），默认10秒</param>
    /// <returns>健康检查是否成功</returns>
    Task<bool> CheckApiHealthAsync(int timeoutSeconds = 10);
}
```

#### 核心功能

**1. API健康检查**

```csharp
public async Task<bool> CheckApiHealthAsync(int timeoutSeconds = 10)
{
    // 委托给IApiHealthCheckService执行实际健康检查
    var status = await _apiHealthCheckService.CheckHealthAsync(timeoutMs);

    // 根据结果更新状态
    switch (status)
    {
        case ApiHealthStatus.Healthy:
            IsApiHealthy = true;
            ConnectionStatus = "已连接";
            return true;

        case ApiHealthStatus.Unhealthy:
            IsApiHealthy = false;
            ConnectionStatus = $"连接失败: {error}";
            return false;
    }
}
```

**2. 配置驱动的API基础URL**

```csharp
// 从配置文件读取：appsettings.json → ApiSettings:BaseUrl
_apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
```

#### 依赖项

- `IApiHealthCheckService?` - API健康检查服务（可选）
- `IConfiguration` - 配置服务（读取API基础URL）
- `ILogger<ApplicationStateService>` - 日志服务

#### 使用场景

**场景1：启动时健康检查**

```csharp
// 在App.xaml.cs或ShellViewModel中
var appState = Container.Resolve<IApplicationStateService>();
bool isHealthy = await appState.CheckApiHealthAsync(timeoutSeconds: 10);

if (!isHealthy)
{
    // 显示连接失败提示
    MessageBox.Show($"连接失败: {appState.ConnectionStatus}");
}
```

**场景2：状态栏显示连接状态**

```csharp
public class StatusBarViewModel
{
    private readonly IApplicationStateService _appState;

    public string StatusText => _appState.ConnectionStatus;
    public DateTime? LastCheckTime => _appState.LastHealthCheckTime;
}
```

#### 注册方式

```csharp
// Foundation层服务注册
services.AddSingleton<IApplicationStateService, ApplicationStateService>();
```

---

## Auth模块新增服务

### ConnectionSettingsService

**文件路径**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Services/IConnectionSettingsService.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Services/ConnectionSettingsService.cs`

**Issue来源**: #1825 - 连接模式选择（远程API vs 本地数据库）

#### 职责

管理Desktop客户端的**连接模式配置**，支持：
- 远程模式（连接WebAPI）
- 本地模式（使用本地数据库，v2.0实现）
- 持久化用户选择（JSON文件）
- "记住上次选择"功能

#### 接口定义

```csharp
public interface IConnectionSettingsService
{
    /// <summary>
    /// 获取当前连接模式
    /// </summary>
    ConnectionMode GetConnectionMode();

    /// <summary>
    /// 保存连接模式
    /// </summary>
    void SaveConnectionMode(ConnectionMode mode);

    /// <summary>
    /// 是否记住上次选择
    /// </summary>
    bool RememberLastChoice { get; set; }
}
```

#### 核心功能

**1. 持久化存储**

```csharp
// 配置文件路径：%LOCALAPPDATA%\LYBT\Desktop\connection-settings.json
var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var settingsFilePath = Path.Combine(localAppData, "LYBT", "Desktop", "connection-settings.json");
```

**配置文件格式**:
```json
{
  "DefaultMode": "Remote",
  "RememberLastChoice": true
}
```

**2. 加载/保存逻辑**

```csharp
// 加载配置（启动时）
private ConnectionSettings LoadSettings()
{
    if (File.Exists(_settingsFilePath))
    {
        var json = File.ReadAllText(_settingsFilePath);
        return JsonSerializer.Deserialize<ConnectionSettings>(json);
    }

    // 默认配置
    return new ConnectionSettings
    {
        DefaultMode = ConnectionMode.Remote,
        RememberLastChoice = true
    };
}

// 保存配置（用户选择后）
private void SaveSettings()
{
    var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
    {
        WriteIndented = true
    });
    File.WriteAllText(_settingsFilePath, json);
}
```

#### 依赖项

- `ILogger<ConnectionSettingsService>` - 日志服务

#### 使用场景

**场景1：启动时读取连接模式**

```csharp
// 在LoginViewModel中
var connectionSettings = Container.Resolve<IConnectionSettingsService>();
var mode = connectionSettings.GetConnectionMode();

if (mode == ConnectionMode.Remote)
{
    // 使用WebAPI连接
    InitializeApiConnection();
}
else
{
    // 使用本地数据库（v2.0）
    InitializeLocalDatabase();
}
```

**场景2：用户切换连接模式**

```csharp
// 在ConnectionModeDialog中
public void OnRemoteModeSelected()
{
    _connectionSettings.SaveConnectionMode(ConnectionMode.Remote);
    _connectionSettings.RememberLastChoice = true;
}
```

#### 注册方式

```csharp
// Auth模块服务注册
services.AddSingleton<IConnectionSettingsService, ConnectionSettingsService>();
```

---

### ConnectionMode枚举

**文件路径**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Models/ConnectionMode.cs`

**定义**:

```csharp
/// <summary>
/// 连接模式枚举 - Issue #1825
/// </summary>
public enum ConnectionMode
{
    /// <summary>
    /// 远程模式 - 连接到远程WebAPI服务
    /// </summary>
    Remote = 0,

    /// <summary>
    /// 本地模式 - 使用本地数据库（v2.0实现）
    /// </summary>
    Local = 1
}
```

**使用说明**:
- **Remote（0）**: MVP阶段默认模式，所有数据通过WebAPI交互
- **Local（1）**: v2.0规划功能，支持离线工作（使用本地SQLite/SQL Server LocalDB）

---

## MedicalCase模块组件化重构

**Issue来源**: #1806-#1807 - MedicalCaseFlowViewModel组件化重构

### 重构背景

原`MedicalCaseFlowViewModel`代码行数：**992行**
重构后代码行数：**629行**
减少代码量：**36%**

**重构目标**：
- 遵循**单一职责原则**（SRP）
- ViewModel专注UI逻辑，业务逻辑委托给服务类
- 提升代码可测试性（6个服务类可独立单元测试）

### 新增服务类（6个）

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/`

#### 1. FormulaImportHandler

**职责**: 处理验方导入到处方的业务逻辑

```csharp
public class FormulaImportHandler
{
    private readonly IFormulaRepository _formulaRepository;

    /// <summary>
    /// 导入验方到处方
    /// </summary>
    public async Task<PrescriptionDto> ImportFormulaAsync(Guid formulaId)
    {
        var formula = await _formulaRepository.GetByIdAsync(formulaId);

        // 转换验方到处方格式
        return new PrescriptionDto
        {
            Items = formula.Items.Select(item => new PrescriptionItemDto
            {
                HerbId = item.HerbId,
                Dosage = item.Dosage,
                Unit = item.Unit
            }).ToList()
        };
    }
}
```

#### 2. HerbSelectionManager

**职责**: 管理药材选择与剂量计算

```csharp
public class HerbSelectionManager
{
    /// <summary>
    /// 添加药材到处方
    /// </summary>
    public void AddHerb(HerbDto herb, decimal dosage)

    /// <summary>
    /// 更新药材剂量
    /// </summary>
    public void UpdateDosage(Guid herbId, decimal newDosage)

    /// <summary>
    /// 移除药材
    /// </summary>
    public void RemoveHerb(Guid herbId)
}
```

#### 3. MedicalCaseDataLoader

**职责**: 加载病历相关数据（患者、历史病历）

```csharp
public class MedicalCaseDataLoader
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IPatientRepository _patientRepository;

    /// <summary>
    /// 加载患者数据
    /// </summary>
    public async Task<PatientDto> LoadPatientDataAsync(Guid patientId)

    /// <summary>
    /// 加载历史病历
    /// </summary>
    public async Task<List<MedicalCaseDto>> LoadHistoryAsync(Guid patientId)
}
```

#### 4. MedicalCaseFlowManager

**职责**: 管理三步诊疗流程状态（辨证→开方→处方）

```csharp
public class MedicalCaseFlowManager
{
    private int _currentStep = 0; // 0=辨证, 1=开方标记, 2=处方

    /// <summary>
    /// 进入下一步
    /// </summary>
    public void MoveToNextStep()

    /// <summary>
    /// 验证当前步骤是否可进入下一步
    /// </summary>
    public bool ValidateCurrentStep()
}
```

#### 5. MedicalCaseLifecycleHandler

**职责**: 处理病历生命周期事件（创建、保存、完成）

```csharp
public class MedicalCaseLifecycleHandler
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    /// <summary>
    /// 创建新病历
    /// </summary>
    public async Task<MedicalCaseDto> CreateCaseAsync(CreateMedicalCaseDto dto)

    /// <summary>
    /// 保存草稿
    /// </summary>
    public async Task SaveDraftAsync(MedicalCaseDto dto)

    /// <summary>
    /// 完成病历
    /// </summary>
    public async Task CompleteCaseAsync(Guid medicalCaseId)
}
```

#### 6. PrescriptionCalculator

**职责**: 处方计算逻辑（总价、剂量校验）

```csharp
public class PrescriptionCalculator
{
    /// <summary>
    /// 计算处方总价
    /// </summary>
    public decimal CalculateTotal(List<PrescriptionItemDto> items)
    {
        return items.Sum(item => item.Dosage * item.UnitPrice);
    }

    /// <summary>
    /// 验证剂量是否合理
    /// </summary>
    public bool ValidateDosage(Guid herbId, decimal dosage)
    {
        // 验证剂量是否在安全范围内
        return dosage >= 3 && dosage <= 30;
    }
}
```

### 重构效果

**Before（992行）**:
```csharp
public class MedicalCaseFlowViewModel : ViewModelBase
{
    // 所有业务逻辑混在ViewModel中
    private void ImportFormula() { /* 60行代码 */ }
    private void AddHerb() { /* 40行代码 */ }
    private void LoadPatientData() { /* 80行代码 */ }
    private void SaveCase() { /* 100行代码 */ }
    private decimal CalculateTotal() { /* 30行代码 */ }
    // ... 大量业务逻辑
}
```

**After（629行）**:
```csharp
public class MedicalCaseFlowViewModel : ViewModelBase
{
    private readonly FormulaImportHandler _formulaImporter;
    private readonly HerbSelectionManager _herbManager;
    private readonly MedicalCaseDataLoader _dataLoader;
    private readonly MedicalCaseFlowManager _flowManager;
    private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
    private readonly PrescriptionCalculator _calculator;

    // ViewModel只负责UI逻辑
    private async void ImportFormula()
    {
        var prescription = await _formulaImporter.ImportFormulaAsync(selectedFormulaId);
        UpdateUI(prescription);
    }

    private void AddHerb()
    {
        _herbManager.AddHerb(selectedHerb, dosage);
        RefreshPrescriptionList();
    }
}
```

### 服务注册

```csharp
// MedicalCase模块服务注册（Transient）
services.AddTransient<FormulaImportHandler>();
services.AddTransient<HerbSelectionManager>();
services.AddTransient<MedicalCaseDataLoader>();
services.AddTransient<MedicalCaseFlowManager>();
services.AddTransient<MedicalCaseLifecycleHandler>();
services.AddTransient<PrescriptionCalculator>();
```

---

## 服务注册

### Foundation层服务（Singleton）

```csharp
// LYBT.Desktop.Foundation\DependencyInjection\FoundationServiceCollectionExtensions.cs
public static IServiceCollection AddFoundationServices(this IServiceCollection services)
{
    services.AddSingleton<IApplicationStateService, ApplicationStateService>();
    // ... 其他Foundation服务
    return services;
}
```

### Auth模块服务（Singleton）

```csharp
// LYBT.Desktop.Auth\AuthModule.cs
public class AuthModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IConnectionSettingsService, ConnectionSettingsService>();
        // ... 其他Auth服务
    }
}
```

### MedicalCase模块服务（Transient）

```csharp
// LYBT.Desktop.MedicalCase\MedicalCaseModule.cs
public class MedicalCaseModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<FormulaImportHandler>();
        containerRegistry.Register<HerbSelectionManager>();
        containerRegistry.Register<MedicalCaseDataLoader>();
        containerRegistry.Register<MedicalCaseFlowManager>();
        containerRegistry.Register<MedicalCaseLifecycleHandler>();
        containerRegistry.Register<PrescriptionCalculator>();
    }
}
```

---

## 相关文档

### 内部文档

- **Foundation层架构**: `docs/explanation/architecture/client/foundation-design.md`
- **Auth模块设计**: `docs/explanation/architecture/client/auth-design.md`
- **MedicalCase模块设计**: `docs/explanation/architecture/client/medical-case-design.md`

### Issue追踪

- **#1822**: Epic - 启动到工作台流程端到端重构优化
- **#1823**: API健康检查前置优化
- **#1825**: 连接模式选择（远程/本地）
- **#1806**: MedicalCaseManagementView注册修复
- **#1807**: MedicalCaseFlowViewModel组件化重构

---

**文档创建时间**: 2025-11-05
**最后更新时间**: 2025-11-05
**维护责任人**: Architecture Team
