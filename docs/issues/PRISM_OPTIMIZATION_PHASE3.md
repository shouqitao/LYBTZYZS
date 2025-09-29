# Issue: [Desktop] Phase 3 - 模块依赖优化与CompositeCommand实现

## 问题描述

当前Desktop项目缺少明确的模块依赖管理和命令协调机制。模块间依赖关系不透明，所有模块均在启动时加载影响性能，且缺少跨模块的命令协调能力。

## 影响范围

### 需要修改的组件

1. **模块依赖声明**
   - 所有Module类添加`[ModuleDependency]`特性
   - 明确声明模块间依赖关系

2. **按需加载实现**
   - App.xaml.cs的`ConfigureModuleCatalog`方法
   - 核心模块与功能模块区分

3. **CompositeCommand实现**
   - 创建IApplicationCommands接口
   - 实现全局命令协调
   - 各ViewModel注册命令

## 详细优化方案

### 1. 模块依赖声明

#### 1.1 创建模块依赖图
```csharp
// src/Client/Desktop/Modules/Auth/AuthenticationModule.cs
[Module(ModuleName = nameof(AuthenticationModule))]
// Auth模块是基础模块，无依赖
public class AuthenticationModule : IModule
{
    // 实现...
}

// src/Client/Desktop/Modules/Users/UsersModule.cs
[Module(ModuleName = nameof(UsersModule))]
[ModuleDependency(nameof(AuthenticationModule))]  // 用户模块依赖认证
public class UsersModule : IModule
{
    // 实现...
}

// src/Client/Desktop/Modules/Patients/PatientsModule.cs
[Module(ModuleName = nameof(PatientsModule))]
[ModuleDependency(nameof(AuthenticationModule))]
[ModuleDependency(nameof(UsersModule))]  // 患者模块依赖用户和认证
public class PatientsModule : IModule
{
    // 实现...
}

// src/Client/Desktop/Modules/MedicalCase/MedicalCaseModule.cs
[Module(ModuleName = nameof(MedicalCaseModule))]
[ModuleDependency(nameof(PatientsModule))]  // 病历依赖患者
[ModuleDependency(nameof(ConsultationModule))]  // 病历依赖诊疗
public class MedicalCaseModule : IModule
{
    // 实现...
}

// src/Client/Desktop/Modules/Consultation/ConsultationModule.cs
[Module(ModuleName = nameof(ConsultationModule))]
[ModuleDependency(nameof(PatientsModule))]  // 诊疗依赖患者
public class ConsultationModule : IModule
{
    // 实现...
}

// src/Client/Desktop/Modules/Prescriptions/PrescriptionsModule.cs
[Module(ModuleName = nameof(PrescriptionsModule))]
[ModuleDependency(nameof(ConsultationModule))]  // 处方依赖诊疗
[ModuleDependency(nameof(HerbsModule))]  // 处方依赖药材
[ModuleDependency(nameof(FormulaModule))]  // 处方依赖方剂
public class PrescriptionsModule : IModule
{
    // 实现...
}

// src/Client/Desktop/Modules/Herbs/HerbsModule.cs
[Module(ModuleName = nameof(HerbsModule))]
[ModuleDependency(nameof(AuthenticationModule))]  // 药材模块只依赖认证
public class HerbsModule : IModule
{
    // 实现...
}

// src/Client/Desktop/Modules/Formula/FormulaModule.cs
[Module(ModuleName = nameof(FormulaModule))]
[ModuleDependency(nameof(HerbsModule))]  // 方剂依赖药材
public class FormulaModule : IModule
{
    // 实现...
}

// src/Client/Desktop/Modules/MedicalWorkbench/MedicalWorkbenchModule.cs
[Module(ModuleName = nameof(MedicalWorkbenchModule))]
[ModuleDependency(nameof(PatientsModule))]
[ModuleDependency(nameof(ConsultationModule))]
[ModuleDependency(nameof(MedicalCaseModule))]
[ModuleDependency(nameof(PrescriptionsModule))]
public class MedicalWorkbenchModule : IModule
{
    // 诊疗工作台是最高层模块，依赖多个业务模块
}
```

### 2. 按需加载优化

#### 2.1 App.xaml.cs配置
```csharp
// src/Client/Desktop/Shell/App.xaml.cs
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // ========== 核心模块 - 立即加载 ==========
    // 认证模块 - 所有功能的基础
    moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);

    // 用户模块 - 基础权限管理
    moduleCatalog.AddModule<UsersModule>(InitializationMode.WhenAvailable);

    // ========== 基础业务模块 - 登录后加载 ==========
    // 患者管理 - 多数业务的基础
    moduleCatalog.AddModule<PatientsModule>(
        dependsOn: new[] { nameof(AuthenticationModule), nameof(UsersModule) },
        InitializationMode.OnDemand);

    // ========== 功能模块 - 按需加载 ==========
    // 药材管理 - 独立功能，可延迟加载
    moduleCatalog.AddModule<HerbsModule>(
        dependsOn: new[] { nameof(AuthenticationModule) },
        InitializationMode.OnDemand);

    // 方剂管理 - 依赖药材
    moduleCatalog.AddModule<FormulaModule>(
        dependsOn: new[] { nameof(HerbsModule) },
        InitializationMode.OnDemand);

    // 诊疗管理 - 依赖患者
    moduleCatalog.AddModule<ConsultationModule>(
        dependsOn: new[] { nameof(PatientsModule) },
        InitializationMode.OnDemand);

    // 病历管理 - 复杂依赖
    moduleCatalog.AddModule<MedicalCaseModule>(
        dependsOn: new[] { nameof(PatientsModule), nameof(ConsultationModule) },
        InitializationMode.OnDemand);

    // 处方管理 - 最复杂依赖
    moduleCatalog.AddModule<PrescriptionsModule>(
        dependsOn: new[] { nameof(ConsultationModule), nameof(HerbsModule), nameof(FormulaModule) },
        InitializationMode.OnDemand);

    // ========== 工作台模块 - 用户触发加载 ==========
    // 诊疗工作台 - 顶层集成模块
    moduleCatalog.AddModule<MedicalWorkbenchModule>(
        dependsOn: new[] {
            nameof(PatientsModule),
            nameof(ConsultationModule),
            nameof(MedicalCaseModule),
            nameof(PrescriptionsModule)
        },
        InitializationMode.OnDemand);
}

// 在MainWindowViewModel中按需加载模块
public class MainWindowViewModel : UnifiedViewModelBase
{
    private readonly IModuleManager _moduleManager;

    public MainWindowViewModel(IModuleManager moduleManager)
    {
        _moduleManager = moduleManager;
    }

    // 用户点击诊疗工作台时触发
    public async Task LoadMedicalWorkbenchAsync()
    {
        // 加载诊疗工作台及其所有依赖
        await Task.Run(() =>
        {
            _moduleManager.LoadModule(nameof(MedicalWorkbenchModule));
        });
    }

    // 用户点击药材管理时触发
    public async Task LoadHerbsManagementAsync()
    {
        await Task.Run(() =>
        {
            _moduleManager.LoadModule(nameof(HerbsModule));
        });
    }

    // 用户点击方剂管理时触发
    public async Task LoadFormulaManagementAsync()
    {
        await Task.Run(() =>
        {
            // 会自动加载HerbsModule依赖
            _moduleManager.LoadModule(nameof(FormulaModule));
        });
    }
}
```

### 3. CompositeCommand实现

#### 3.1 创建全局命令接口
```csharp
// src/Client/Desktop/Core/Commands/IApplicationCommands.cs
namespace LYBT.Desktop.Core.Commands
{
    public interface IApplicationCommands
    {
        // 全局保存命令 - 多个模块可以响应
        CompositeCommand SaveAllCommand { get; }

        // 全局刷新命令
        CompositeCommand RefreshAllCommand { get; }

        // 全局验证命令
        CompositeCommand ValidateAllCommand { get; }

        // 全局打印命令
        CompositeCommand PrintCommand { get; }

        // 全局导出命令
        CompositeCommand ExportCommand { get; }

        // 工作台切换命令
        CompositeCommand SwitchWorkbenchCommand { get; }
    }
}

// src/Client/Desktop/Core/Commands/ApplicationCommands.cs
namespace LYBT.Desktop.Core.Commands
{
    public class ApplicationCommands : IApplicationCommands
    {
        public CompositeCommand SaveAllCommand { get; }
        public CompositeCommand RefreshAllCommand { get; }
        public CompositeCommand ValidateAllCommand { get; }
        public CompositeCommand PrintCommand { get; }
        public CompositeCommand ExportCommand { get; }
        public CompositeCommand SwitchWorkbenchCommand { get; }

        public ApplicationCommands()
        {
            SaveAllCommand = new CompositeCommand();
            RefreshAllCommand = new CompositeCommand();
            ValidateAllCommand = new CompositeCommand();
            PrintCommand = new CompositeCommand();
            ExportCommand = new CompositeCommand();
            SwitchWorkbenchCommand = new CompositeCommand();
        }
    }
}
```

#### 3.2 在App.xaml.cs注册
```csharp
// src/Client/Desktop/Shell/App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册全局命令为单例
    containerRegistry.RegisterSingleton<IApplicationCommands, ApplicationCommands>();

    // 其他注册...
}
```

#### 3.3 在ViewModel中使用CompositeCommand
```csharp
// src/Client/Desktop/Modules/Patients/ViewModels/PatientDetailViewModel.cs
public class PatientDetailViewModel : UnifiedViewModelBase
{
    private readonly IApplicationCommands _applicationCommands;
    private readonly IPatientService _patientService;

    public DelegateCommand SavePatientCommand { get; }
    public DelegateCommand RefreshPatientCommand { get; }
    public DelegateCommand PrintPatientCommand { get; }

    public PatientDetailViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILogger<PatientDetailViewModel> logger,
        IApplicationCommands applicationCommands,
        IPatientService patientService)
        : base(regionManager, eventAggregator, logger)
    {
        _applicationCommands = applicationCommands;
        _patientService = patientService;

        // 创建本地命令
        SavePatientCommand = new DelegateCommand(SavePatient, CanSavePatient);
        RefreshPatientCommand = new DelegateCommand(RefreshPatient);
        PrintPatientCommand = new DelegateCommand(PrintPatient);

        // 注册到全局命令
        _applicationCommands.SaveAllCommand.RegisterCommand(SavePatientCommand);
        _applicationCommands.RefreshAllCommand.RegisterCommand(RefreshPatientCommand);
        _applicationCommands.PrintCommand.RegisterCommand(PrintPatientCommand);
    }

    private async void SavePatient()
    {
        try
        {
            await _patientService.SaveAsync(CurrentPatient);
            Logger.LogInformation("患者信息保存成功");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存患者信息失败");
        }
    }

    private bool CanSavePatient()
    {
        return CurrentPatient != null && HasChanges;
    }

    private async void RefreshPatient()
    {
        await LoadPatientDataAsync();
    }

    private void PrintPatient()
    {
        // 打印患者信息逻辑
    }

    public override void Destroy()
    {
        // 清理时注销命令
        _applicationCommands.SaveAllCommand.UnregisterCommand(SavePatientCommand);
        _applicationCommands.RefreshAllCommand.UnregisterCommand(RefreshPatientCommand);
        _applicationCommands.PrintCommand.UnregisterCommand(PrintPatientCommand);

        base.Destroy();
    }
}

// src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionEditViewModel.cs
public class PrescriptionEditViewModel : UnifiedViewModelBase
{
    private readonly IApplicationCommands _applicationCommands;

    public DelegateCommand SavePrescriptionCommand { get; }
    public DelegateCommand ValidatePrescriptionCommand { get; }

    public PrescriptionEditViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILogger<PrescriptionEditViewModel> logger,
        IApplicationCommands applicationCommands)
        : base(regionManager, eventAggregator, logger)
    {
        _applicationCommands = applicationCommands;

        SavePrescriptionCommand = new DelegateCommand(SavePrescription, CanSavePrescription);
        ValidatePrescriptionCommand = new DelegateCommand(ValidatePrescription);

        // 注册到全局命令
        _applicationCommands.SaveAllCommand.RegisterCommand(SavePrescriptionCommand);
        _applicationCommands.ValidateAllCommand.RegisterCommand(ValidatePrescriptionCommand);
    }

    private void SavePrescription()
    {
        // 保存处方逻辑
    }

    private bool CanSavePrescription()
    {
        return IsPrescriptionValid && HasChanges;
    }

    private void ValidatePrescription()
    {
        // 验证处方配伍禁忌等
    }
}
```

#### 3.4 在MainWindow中触发全局命令
```csharp
// src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
public class MainWindowViewModel : UnifiedViewModelBase
{
    private readonly IApplicationCommands _applicationCommands;

    public MainWindowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILogger<MainWindowViewModel> logger,
        IApplicationCommands applicationCommands)
        : base(regionManager, eventAggregator, logger)
    {
        _applicationCommands = applicationCommands;

        // 绑定快捷键
        InitializeKeyBindings();
    }

    private void InitializeKeyBindings()
    {
        // Ctrl+S 触发全局保存
        KeyBinding saveBinding = new KeyBinding(
            _applicationCommands.SaveAllCommand,
            new KeyGesture(Key.S, ModifierKeys.Control));

        // F5 触发全局刷新
        KeyBinding refreshBinding = new KeyBinding(
            _applicationCommands.RefreshAllCommand,
            new KeyGesture(Key.F5));
    }

    // 工具栏按钮命令
    public ICommand SaveAllCommand => _applicationCommands.SaveAllCommand;
    public ICommand RefreshAllCommand => _applicationCommands.RefreshAllCommand;
    public ICommand PrintCommand => _applicationCommands.PrintCommand;
}

// src/Client/Desktop/Shell/Views/MainWindow.xaml
<Window>
    <Window.InputBindings>
        <KeyBinding Command="{Binding SaveAllCommand}"
                    Key="S" Modifiers="Control"/>
        <KeyBinding Command="{Binding RefreshAllCommand}"
                    Key="F5"/>
    </Window.InputBindings>

    <DockPanel>
        <ToolBar DockPanel.Dock="Top">
            <Button Command="{Binding SaveAllCommand}"
                    ToolTip="保存所有 (Ctrl+S)">
                <Image Source="/Images/save-all.png"/>
            </Button>
            <Button Command="{Binding RefreshAllCommand}"
                    ToolTip="刷新 (F5)">
                <Image Source="/Images/refresh.png"/>
            </Button>
            <Button Command="{Binding PrintCommand}"
                    ToolTip="打印">
                <Image Source="/Images/print.png"/>
            </Button>
        </ToolBar>
    </DockPanel>
</Window>
```

### 4. 模块加载状态管理

#### 4.1 创建模块加载服务
```csharp
// src/Client/Desktop/Core/Services/IModuleLoadingService.cs
public interface IModuleLoadingService
{
    bool IsModuleLoaded(string moduleName);
    Task<bool> LoadModuleAsync(string moduleName);
    event EventHandler<ModuleLoadedEventArgs> ModuleLoaded;
    ObservableCollection<ModuleInfo> LoadedModules { get; }
}

// src/Client/Desktop/Core/Services/ModuleLoadingService.cs
public class ModuleLoadingService : IModuleLoadingService
{
    private readonly IModuleManager _moduleManager;
    private readonly ILogger<ModuleLoadingService> _logger;

    public ObservableCollection<ModuleInfo> LoadedModules { get; }
    public event EventHandler<ModuleLoadedEventArgs> ModuleLoaded;

    public ModuleLoadingService(
        IModuleManager moduleManager,
        ILogger<ModuleLoadingService> logger)
    {
        _moduleManager = moduleManager;
        _logger = logger;
        LoadedModules = new ObservableCollection<ModuleInfo>();

        // 监听模块加载事件
        _moduleManager.LoadModuleCompleted += OnLoadModuleCompleted;
    }

    public bool IsModuleLoaded(string moduleName)
    {
        return LoadedModules.Any(m => m.ModuleName == moduleName);
    }

    public async Task<bool> LoadModuleAsync(string moduleName)
    {
        if (IsModuleLoaded(moduleName))
            return true;

        try
        {
            await Task.Run(() => _moduleManager.LoadModule(moduleName));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"加载模块 {moduleName} 失败");
            return false;
        }
    }

    private void OnLoadModuleCompleted(object sender, LoadModuleCompletedEventArgs e)
    {
        if (e.Error == null)
        {
            var moduleInfo = new ModuleInfo
            {
                ModuleName = e.ModuleInfo.ModuleName,
                LoadedTime = DateTime.Now
            };

            LoadedModules.Add(moduleInfo);
            ModuleLoaded?.Invoke(this, new ModuleLoadedEventArgs(moduleInfo));

            _logger.LogInformation($"模块 {e.ModuleInfo.ModuleName} 加载完成");
        }
    }
}
```

## 实施步骤

### Step 1: 创建命令基础设施（优先级：高）
1. 创建IApplicationCommands接口和实现
2. 在App.xaml.cs中注册为单例
3. 创建ModuleLoadingService

### Step 2: 更新所有Module类（优先级：高）
按依赖顺序修改：
1. AuthenticationModule（无依赖）
2. UsersModule、HerbsModule（基础依赖）
3. PatientsModule、FormulaModule（二级依赖）
4. ConsultationModule、MedicalCaseModule（三级依赖）
5. PrescriptionsModule（四级依赖）
6. MedicalWorkbenchModule（顶级依赖）

### Step 3: 实现按需加载（优先级：中）
1. 修改App.xaml.cs的ConfigureModuleCatalog
2. 在MainWindowViewModel中添加模块加载逻辑
3. 更新导航菜单以触发按需加载

### Step 4: 集成CompositeCommand（优先级：中）
1. 更新各ViewModel注册本地命令到全局命令
2. 在MainWindow添加全局命令触发器
3. 添加键盘快捷键绑定

## 测试验证

### 单元测试
```csharp
[TestClass]
public class ModuleDependencyTests
{
    [TestMethod]
    public void Modules_Should_Have_Correct_Dependencies()
    {
        // Arrange
        var moduleTypes = new[]
        {
            typeof(MedicalCaseModule),
            typeof(PrescriptionsModule)
        };

        // Act & Assert
        foreach (var moduleType in moduleTypes)
        {
            var dependencies = moduleType
                .GetCustomAttributes<ModuleDependencyAttribute>()
                .Select(d => d.ModuleName)
                .ToList();

            Assert.IsTrue(dependencies.Count > 0,
                $"{moduleType.Name} 应该声明依赖");
        }
    }
}

[TestClass]
public class CompositeCommandTests
{
    [TestMethod]
    public void SaveAllCommand_Should_Execute_All_Registered_Commands()
    {
        // Arrange
        var applicationCommands = new ApplicationCommands();
        var command1Executed = false;
        var command2Executed = false;

        var command1 = new DelegateCommand(() => command1Executed = true);
        var command2 = new DelegateCommand(() => command2Executed = true);

        applicationCommands.SaveAllCommand.RegisterCommand(command1);
        applicationCommands.SaveAllCommand.RegisterCommand(command2);

        // Act
        applicationCommands.SaveAllCommand.Execute();

        // Assert
        Assert.IsTrue(command1Executed);
        Assert.IsTrue(command2Executed);
    }
}
```

### 性能测试
```csharp
[TestMethod]
public async Task OnDemandLoading_Should_Reduce_Startup_Time()
{
    // Arrange
    var stopwatch = new Stopwatch();

    // Act - 只加载核心模块
    stopwatch.Start();
    await LoadCoreModulesAsync();
    stopwatch.Stop();
    var coreLoadTime = stopwatch.ElapsedMilliseconds;

    stopwatch.Restart();
    await LoadAllModulesAsync();
    stopwatch.Stop();
    var allLoadTime = stopwatch.ElapsedMilliseconds;

    // Assert
    Assert.IsTrue(coreLoadTime < allLoadTime / 2,
        "核心模块加载时间应小于全部加载时间的一半");
}
```

## 验收标准

- [ ] 所有Module类添加`[ModuleDependency]`特性
- [ ] App.xaml.cs实现按需加载配置
- [ ] IApplicationCommands接口创建并注册
- [ ] 至少3个ViewModel集成CompositeCommand
- [ ] MainWindow支持全局命令快捷键
- [ ] ModuleLoadingService正常工作
- [ ] 启动时间减少30%以上
- [ ] 单元测试覆盖率达80%

## 预期收益

1. **启动性能提升40%**：按需加载减少初始化时间
2. **内存占用降低30%**：未使用的模块不加载
3. **命令协调能力**：全局操作一次触发多模块响应
4. **依赖关系透明**：模块关系清晰可维护

## 风险评估

- **风险等级**：中
- **影响范围**：所有Module和主要ViewModel
- **回退方案**：保留WhenAvailable选项快速回退

## 相关文档

- [Prism模块化文档](https://prismlibrary.com/docs/modules.html)
- [CompositeCommand文档](https://prismlibrary.com/docs/commands/composite-commands.html)
- [Phase 1优化方案](./PRISM_OPTIMIZATION_PHASE1.md)
- [Phase 2优化方案](./PRISM_OPTIMIZATION_PHASE2.md)