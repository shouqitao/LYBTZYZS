# Desktop 层重构 Phase 1 实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** 修复 P0 级问题，解决启动阻塞和关键 ViewModel 臃肿，冷启动时间从 >5s 优化到 <3s

**Architecture:** 通过延迟初始化消除启动阻塞，通过 Child ViewModel 模式拆分臃肿 ViewModel，保持向后兼容

**Tech Stack:** .NET 8, WPF, Prism.DryIoc, CommunityToolkit.Mvvm, xUnit, NSubstitute

---

## 前置检查

**依赖项确认**:
- `DataSourceRegistrationExtensions.cs` 存在且第 201-207 行包含 DatabaseInitializer 注册
- `ApiHealthCheckStartupStep.cs` 第 44 行调用 CheckApiHealthAsync
- `PatientMasterDetailViewModel.cs` 注入 9 个服务（第 80-89 行）

**测试验证命令**:
```bash
dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
dotnet test tests/LYBT.Tests.Desktop --list-tests 2>/dev/null | wc -l
```

---

## Task 1: 延迟数据库初始化

**目标**: 将 DatabaseInitializer 从立即初始化改为延迟初始化

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/DataSourceRegistrationExtensions.cs:200-207`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/DatabaseInitializer.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/Startup/DatabaseInitializerTests.cs` (新建)

**Step 1: 修改 DatabaseInitializer 构造函数**

```csharp
// File: src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/DatabaseInitializer.cs

public class DatabaseInitializer
{
    private readonly Func<LocalDbContext> _contextFactory;
    private readonly ILogger<DatabaseInitializer> _logger;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // 修改构造函数，接受工厂而非实例
    public DatabaseInitializer(
        Func<LocalDbContext> contextFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // 添加延迟初始化方法
    public async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            _logger.LogInformation("开始延迟初始化本地数据库...");
            await using var context = _contextFactory();
            await context.Database.EnsureCreatedAsync();
            _isInitialized = true;
            _logger.LogInformation("本地数据库初始化完成");
        }
        finally
        {
            _initLock.Release();
        }
    }
}
```

**Step 2: 修改 DI 注册**

```csharp
// File: src/Client/Desktop/Shell/Extensions/DataSourceRegistrationExtensions.cs
// 替换第 200-207 行

// 数据库初始化器 (Singleton) - 延迟初始化
containerRegistry.RegisterSingleton<DatabaseInitializer>(resolver =>
{
    var loggerFactory = resolver.Resolve<ILoggerFactory>();
    // 使用工厂模式延迟创建 LocalDbContext
    return new DatabaseInitializer(
        () => resolver.Resolve<LocalDbContext>(),
        loggerFactory.CreateLogger<DatabaseInitializer>());
});
```

**Step 3: 在首次使用本地模式时触发初始化**

```csharp
// File: src/Client/Desktop/Shell/Services/ConnectionModeProvider.cs
// 在 SwitchModeAsync 方法中，切换到 Local 模式时调用 EnsureInitializedAsync

public async Task<ModeSwitchResult> SwitchModeAsync(ConnectionMode newMode)
{
    // ... 现有验证逻辑 ...

    if (newMode == ConnectionMode.Local)
    {
        // 延迟初始化数据库
        var dbInitializer = _resolver.Resolve<DatabaseInitializer>();
        await dbInitializer.EnsureInitializedAsync();
    }

    // ... 其余切换逻辑 ...
}
```

**Step 4: 编写测试**

```bash
# 创建测试文件目录
mkdir -p tests/LYBT.Tests.Desktop/PureLogic/Startup
```

```csharp
// File: tests/LYBT.Tests.Desktop/PureLogic/Startup/DatabaseInitializerTests.cs

[Fact]
public async Task EnsureInitializedAsync_CreatesDatabase_OnlyOnFirstCall()
{
    // Arrange
    var mockContext = new Mock<LocalDbContext>();
    var mockLogger = new Mock<ILogger<DatabaseInitializer>>();
    var factory = () => mockContext.Object;

    var initializer = new DatabaseInitializer(factory, mockLogger.Object);

    // Act - 第一次调用
    await initializer.EnsureInitializedAsync();

    // Assert
    mockContext.Verify(c => c.Database.EnsureCreatedAsync(), Times.Once);

    // Act - 第二次调用
    await initializer.EnsureInitializedAsync();

    // Assert - 不应再次创建
    mockContext.Verify(c => c.Database.EnsureCreatedAsync(), Times.Once);
}
```

**Step 5: 验证启动时间**

```bash
# 运行应用，检查启动日志
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
# 预期: 启动时间 < 2s（首次，无数据库创建阻塞）
```

**Step 6: Commit**

```bash
git add -A
git commit -m "perf(startup): 延迟数据库初始化，避免启动阻塞

- DatabaseInitializer 改为接受工厂函数
- 数据库创建延迟到首次切换到本地模式时
- 添加线程安全的 EnsureInitializedAsync 方法
- 添加单元测试验证延迟行为

Fixes P0-2"
```

---

## Task 2: 异步 API 健康检查

**目标**: 将 API 健康检查改为后台异步执行，不阻塞启动

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Startup/Steps/ApiHealthCheckStartupStep.cs`
- Modify: `src/Client/Desktop/Shell/Services/HealthCheck/HealthCheckCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/App.xaml.cs:325-335`

**Step 1: 修改 ApiHealthCheckStartupStep**

```csharp
// File: src/Client/Desktop/Shell/Services/Startup/Steps/ApiHealthCheckStartupStep.cs

public class ApiHealthCheckStartupStep : IStartupStep
{
    // ... 现有字段 ...

    /// <inheritdoc />
    public bool IsRequired => false;

    /// <inheritdoc />
    public async Task<StartupStepResult> ExecuteAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 立即返回成功，健康检查在后台执行
        progress?.Report("API健康检查将在后台进行...");

        // 触发后台健康检查（不 await）
        _ = Task.Run(async () =>
        {
            try
            {
                var isHealthy = await _applicationStateService.CheckApiHealthAsync(_timeoutSeconds);
                _logger.LogInformation(isHealthy
                    ? "API健康检查通过"
                    : "API健康检查未通过");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "后台API健康检查失败");
            }
        }, cancellationToken);

        return StartupStepResult.Succeeded(TimeSpan.Zero);
    }
}
```

**Step 2: 修改 HealthCheckCoordinator 立即触发检查**

```csharp
// File: src/Client/Desktop/Shell/Services/HealthCheck/HealthCheckCoordinator.cs
// 在构造函数中立即触发一次健康检查

public HealthCheckCoordinator(...)
{
    // ... 现有初始化 ...

    // 启动时立即在后台执行一次健康检查
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000); // 延迟1秒，等待应用完全启动
        await CheckNowAsync();
    });
}
```

**Step 3: 更新 App.xaml.cs 启动步骤**

```csharp
// File: src/Client/Desktop/Shell/App.xaml.cs
// 在 RegisterStartupSteps 方法中，确保 API 健康检查是可选步骤

private void RegisterStartupSteps(IStartupPipeline pipeline)
{
    pipeline.RegisterStep(new ErrorHandlingStartupStep(...));
    pipeline.RegisterStep(new ModuleCoordinatorStartupStep(...));
    pipeline.RegisterStep(new CoreServicesStartupStep(...));
    // API健康检查改为非阻塞，快速完成
    pipeline.RegisterStep(new ApiHealthCheckStartupStep(..., timeoutSeconds: 5)); // 减少超时到5秒
    pipeline.RegisterStep(new WarmupStartupStep(...));
}
```

**Step 4: 验证启动时间**

```bash
# 关闭 WebAPI，测试启动时间
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
# 预期: 即使 WebAPI 未启动，启动时间 < 3s
```

**Step 5: Commit**

```bash
git commit -m "perf(startup): API健康检查改为后台异步执行

- ApiHealthCheckStartupStep 立即返回，不阻塞启动
- 健康检查在后台 Task.Run 中执行
- HealthCheckCoordinator 启动后延迟1秒自动检查
- 超时时间从 10s 减少到 5s

Fixes P0-3"
```

---

## Task 3: PatientMasterDetailViewModel 拆分

**目标**: 将注入服务从 9 个减少到 5 个，拆分读卡器和导入导出功能到 Child ViewModel

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientCardReaderViewModel.cs`
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientImportExportViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/Patients/PatientCardReaderViewModelTests.cs` (新建)

**Step 1: 创建 PatientCardReaderViewModel**

```csharp
// File: src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientCardReaderViewModel.cs

using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 患者读卡器功能 ViewModel (Child VM)
/// 从 PatientMasterDetailViewModel 拆分出来的读卡器相关功能
/// </summary>
public partial class PatientCardReaderViewModel : CoreViewModelBase
{
    private readonly ICardReaderService _cardReaderService;
    private readonly IPatientCardReaderIntegration _patientCardReaderIntegration;
    private readonly ICommonDialogService _dialogService;
    private readonly ILogger<PatientCardReaderViewModel> _logger;

    public PatientCardReaderViewModel(
        IViewModelServices viewModelServices,
        ICardReaderService cardReaderService,
        IPatientCardReaderIntegration patientCardReaderIntegration,
        ILogger<PatientCardReaderViewModel> logger) : base(viewModelServices)
    {
        _cardReaderService = cardReaderService ?? throw new ArgumentNullException(nameof(cardReaderService));
        _patientCardReaderIntegration = patientCardReaderIntegration ?? throw new ArgumentNullException(nameof(patientCardReaderIntegration));
        _dialogService = viewModelServices.Dialog;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>是否已连接读卡器</summary>
    public bool IsCardReaderConnected => _cardReaderService.IsConnected;

    /// <summary>是否正在读卡</summary>
    private bool _isReadingCard;
    public bool IsReadingCard
    {
        get => _isReadingCard;
        private set => SetProperty(ref _isReadingCard, value);
    }

    /// <summary>刷卡录入命令</summary>
    [RelayCommand(CanExecute = nameof(CanReadCard))]
    public async Task<CardReadResult?> ReadCardAsync()
    {
        if (!_cardReaderService.IsConnected)
        {
            var initialized = await _cardReaderService.InitializeAsync();
            if (!initialized)
            {
                await _dialogService.ShowErrorAsync("读卡器未连接，请检查设备", "读卡器未连接");
                return null;
            }
        }

        try
        {
            IsReadingCard = true;
            var result = await _cardReaderService.ReadCardAsync();

            if (!result.IsSuccess)
            {
                await _dialogService.ShowErrorAsync($"读卡失败：{result.ErrorMessage}", "读卡失败");
                return null;
            }

            _logger.LogInformation("读卡成功：{Name}", result.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读卡时发生异常");
            await _dialogService.ShowErrorAsync("读卡失败，请重试", "读卡失败");
            return null;
        }
        finally
        {
            IsReadingCard = false;
        }
    }

    private bool CanReadCard() => !IsReadingCard;

    /// <summary>根据身份证号查找患者</summary>
    public async Task<PatientCardReaderResult?> FindPatientByIdNumberAsync(string idNumber)
    {
        return await _patientCardReaderIntegration.FindPatientByIdNumberAsync(idNumber);
    }

    /// <summary>根据读卡结果查找或创建患者</summary>
    public async Task<PatientCardReaderResult> FindOrCreatePatientAsync(CardReadResult cardResult)
    {
        return await _patientCardReaderIntegration.FindOrCreatePatientAsync(cardResult);
    }
}
```

**Step 2: 创建 PatientImportExportViewModel**

```csharp
// File: src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientImportExportViewModel.cs

using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.Patients.ViewModels.Handlers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 患者导入导出功能 ViewModel (Child VM)
/// 从 PatientMasterDetailViewModel 拆分出来的导入导出功能
/// </summary>
public partial class PatientImportExportViewModel : CoreViewModelBase
{
    private readonly IPatientImportExportHandler _importExportHandler;
    private readonly ILogger<PatientImportExportViewModel> _logger;

    public PatientImportExportViewModel(
        IViewModelServices viewModelServices,
        IPatientImportExportHandler importExportHandler,
        ILogger<PatientImportExportViewModel> logger) : base(viewModelServices)
    {
        _importExportHandler = importExportHandler ?? throw new ArgumentNullException(nameof(importExportHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 导入患者命令
    /// 返回是否成功导入
    /// </summary>
    [RelayCommand]
    public async Task<bool> ImportAsync()
    {
        var result = await _importExportHandler.ImportAsync();
        if (result)
        {
            _logger.LogInformation("患者导入成功");
        }
        return result;
    }

    /// <summary>
    /// 导出患者命令
    /// </summary>
    [RelayCommand]
    public async Task ExportAsync(string? searchText = null)
    {
        await _importExportHandler.ExportAsync(searchText);
    }

    /// <summary>
    /// 下载模板命令
    /// </summary>
    [RelayCommand]
    public async Task DownloadTemplateAsync()
    {
        await _importExportHandler.DownloadTemplateAsync();
    }
}
```

**Step 3: 修改 PatientMasterDetailViewModel**

```csharp
// File: src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs

public partial class PatientMasterDetailViewModel : MasterDetailViewModelBase<PatientListDto, PatientDetailModel>
{
    private readonly PatientService _commandHandler;
    private readonly IPatientRepository _patientRepository;
    private readonly IPatientStatusHandler _statusHandler;
    private readonly IDesktopCacheManager _cacheManager;

    // Child ViewModels - 通过构造函数注入
    private readonly PatientCardReaderViewModel _cardReaderViewModel;
    private readonly PatientImportExportViewModel _importExportViewModel;

    public PatientMasterDetailViewModel(
        IViewModelServices viewModelServices,
        IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServices,
        PatientService commandHandler,
        IPatientRepository patientRepository,
        IPatientStatusHandler statusHandler,
        IDesktopCacheManager cacheManager,
        // Child ViewModels
        PatientCardReaderViewModel cardReaderViewModel,
        PatientImportExportViewModel importExportViewModel)
        : base(viewModelServices, masterDetailServices)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));

        // Child ViewModels
        _cardReaderViewModel = cardReaderViewModel ?? throw new ArgumentNullException(nameof(cardReaderViewModel));
        _importExportViewModel = importExportViewModel ?? throw new ArgumentNullException(nameof(importExportViewModel));

        PageTitle = "患者管理";
    }

    // 对外暴露 Child ViewModels 属性
    public PatientCardReaderViewModel CardReaderViewModel => _cardReaderViewModel;
    public PatientImportExportViewModel ImportExportViewModel => _importExportViewModel;

    // 读卡器相关属性代理到 Child VM
    public bool IsCardReaderConnected => _cardReaderViewModel.IsCardReaderConnected;
    public bool IsReadingCard => _cardReaderViewModel.IsReadingCard;

    // 修改导入导出命令
    [RelayCommand]
    private async Task ImportAsync()
    {
        if (await _importExportViewModel.ImportAsync())
        {
            _cacheManager.InvalidatePatientCaches();
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        await _importExportViewModel.ExportAsync(SearchText);
    }

    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        await _importExportViewModel.DownloadTemplateAsync();
    }

    // 修改读卡命令
    [RelayCommand(CanExecute = nameof(CanReadCard))]
    private async Task ReadCardAsync()
    {
        var cardResult = await _cardReaderViewModel.ReadCardAsync();
        if (cardResult == null) return;

        // 查找患者
        var existingPatient = await _cardReaderViewModel.FindPatientByIdNumberAsync(cardResult.IdNumber);
        if (existingPatient != null)
        {
            await MasterDetailServices.Dialog.ShowSuccessAsync($"找到患者：{existingPatient.Name}", "查找成功");
            await SearchAndSelectPatientAsync(existingPatient.PatientId);
        }
        else
        {
            await HandleNewPatientFromCardAsync(cardResult);
        }
    }

    private bool CanReadCard() => !IsReadingCard;

    // ... 其余代码保持不变 ...
}
```

**Step 4: 注册新的 ViewModels**

```csharp
// File: src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs

public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ... 现有注册 ...

    // 注册新的 Child ViewModels
    containerRegistry.Register<PatientCardReaderViewModel>();
    containerRegistry.Register<PatientImportExportViewModel>();
}
```

**Step 5: Commit**

```bash
git add -A
git commit -m "refactor(patients): 拆分 PatientMasterDetailViewModel

- 创建 PatientCardReaderViewModel 处理读卡器功能
- 创建 PatientImportExportViewModel 处理导入导出功能
- 主 ViewModel 注入服务从 9 个减少到 5 个
- 保持向后兼容，通过属性代理访问 Child VM

Fixes P0-1"
```

---

## Task 4: 验证和文档

**目标**: 验证所有 P0 修复完成，更新进度文档

**Files:**
- Modify: `progress.md`
- Modify: `task_plan.md`

**Step 1: 运行全量测试**

```bash
dotnet test tests/LYBT.Tests.Desktop --verbosity normal
# 预期: 所有现有测试通过 + 新添加的测试通过
```

**Step 2: 验证启动时间**

```bash
# 清除构建，测试冷启动
git clean -xdf
dotnet build LYBT.All.sln
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
# 检查日志中的启动时间，预期 < 3s
```

**Step 3: 更新 progress.md**

```markdown
### Phase 1 完成

**Date**: 2026-03-14

**Completed Tasks**:
- [✓] Task 1: 延迟数据库初始化
- [✓] Task 2: 异步 API 健康检查
- [✓] Task 3: PatientMasterDetailViewModel 拆分

**Metrics**:
- 冷启动时间: Xs (目标 < 3s)
- PatientMasterDetailViewModel 注入服务: 5 (目标 < 6)
- 测试通过率: XX%

**Next**: Phase 2 - 测试覆盖
```

**Step 4: Commit 进度更新**

```bash
git commit -m "docs: update Phase 1 progress

- 所有 P0 问题已修复
- 启动时间优化完成
- PatientMasterDetailViewModel 拆分完成

Closes Phase 1"
```

---

## 执行检查清单

**Before Starting**:
- [ ] 确认当前分支干净
- [ ] 确认 WebAPI 可以正常启动（用于测试）
- [ ] 备份当前代码（可选）

**During Implementation**:
- [ ] 每个 Task 完成后立即运行相关测试
- [ ] 确保没有破坏现有功能
- [ ] 保持提交信息清晰描述

**After Completion**:
- [ ] 全量测试通过
- [ ] 手动验证核心功能正常
- [ ] 更新相关文档

---

## 风险与回滚

**Risk 1: 数据库初始化失败**
- 回滚: 恢复 `DataSourceRegistrationExtensions.cs` 原始代码
- 检测: 切换到本地模式时报错

**Risk 2: 读卡器功能不可用**
- 回滚: 检查 PatientCardReaderViewModel 注册和注入
- 检测: 患者管理界面读卡按钮状态

**Risk 3: 导入导出失败**
- 回滚: 检查 PatientImportExportViewModel 依赖注册
- 检测: 导入/导出/下载模板功能
