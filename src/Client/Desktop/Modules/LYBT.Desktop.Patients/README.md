# LYBT.Desktop.Patients - 患者管理模块

## 📦 项目定位

- **层级**:Client端
- **类型**:业务模块(患者管理)
- **职责**:提供患者档案的完整生命周期管理UI,包括患者选择、快速创建、导入向导、未完成病案处理、待诊队列管理等功能。作为诊疗流程的入口模块,采用MVVM架构,通过PatientSelectionViewModel支持分页查询、搜索、选择患者并启动看诊流程。本模块是医生日常工作的起点,承载了复杂的患者选择逻辑、待诊队列管理和病案状态检查功能。

## 📂 代码结构

```
LYBT.Desktop.Patients/
├── Interfaces/                                    # 接口定义(1个)
│   └── IPatientRepository.cs                      # 患者仓储接口(7个方法)
├── Models/                                        # 数据模型(3个)
│   ├── ImportWizardStep.cs                        # 导入向导步骤枚举
│   ├── PatientItem.cs                             # 患者列表项模型
│   └── PatientViewState.cs                        # 视图状态模型
├── Repositories/                                  # 数据仓储实现(1个)
│   └── PatientRepository.cs                       # 患者仓储实现(7个方法)
├── ViewModels/                                    # MVVM视图模型(5个)
│   ├── PatientDetailViewModel.cs                  # 患者详情视图模型
│   ├── PatientImportWizardViewModel.cs            # 批量导入向导视图模型
│   ├── PatientSelectionViewModel.cs               # 患者选择视图模型(主列表,1065行)
│   │   ├── 构造函数(31行)                         # 依赖注入+命令初始化+事件订阅
│   │   ├── 20个属性                               # Patients/SelectedPatient/SearchKeyword/CurrentPage/TotalPages等
│   │   └── 25个方法                               # 搜索/分页/选择/开始看诊/待诊队列/未完成病案检查等
│   ├── QuickCreatePatientDialogViewModel.cs      # 快速创建患者对话框视图模型
│   └── UnfinishedCaseDialogViewModel.cs           # 未完成病案对话框视图模型
├── Views/                                         # WPF视图(5对10个文件)
│   ├── PatientDetailView.xaml/xaml.cs             # 患者详情视图
│   ├── PatientImportWizardView.xaml/xaml.cs       # 批量导入向导视图
│   ├── PatientSelectionView.xaml/xaml.cs          # 患者选择视图(主列表)
│   ├── QuickCreatePatientDialog.xaml/xaml.cs      # 快速创建患者对话框
│   └── UnfinishedCaseDialog.xaml/xaml.cs          # 未完成病案对话框
└── PatientsModule.cs                              # Prism模块注册(2个方法)
    ├── OnInitialized()                            # 模块初始化
    └── RegisterTypes()                            # 依赖注入注册(仓储+5个ViewModel+5个View+2个Dialog)
```

**说明**:
- **PatientSelectionViewModel**:核心ViewModel(1065行),包含20个属性+25个方法,支持患者列表分页、搜索、选择、快速创建、开始看诊、待诊队列管理、未完成病案检查等复杂功能
- **未完成病案检查**:在选择患者后自动检查是否有未完成的病案,提供三个选择(继续看诊/关闭旧病案再新建/仅关闭旧病案)
- **待诊队列**:支持待诊患者队列管理,医生可以快速切换到待诊患者
- **快速创建**:通过QuickCreatePatientDialog快速创建患者并立即开始看诊
- **批量导入**:通过PatientImportWizardView支持Excel批量导入患者档案
- **Prism导航**:实现INavigationAware接口,支持参数传递和生命周期管理

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Foundation** - 基础设施(ViewModel基类、IApiService、导航服务)
2. **LYBT.Desktop.Presentation** - 共享UI组件和样式
3. **LYBT.Shared.Models** - 共享DTO模型(PatientDto、CreatePatientDto、UpdatePatientDto、PagedResultDto等)
4. **Prism.Core** (8.x) - MVVM框架核心
5. **Prism.DryIoc** (8.x) - 依赖注入容器
6. **MaterialDesignThemes** (5.1.x) - Material Design UI组件库

### 被依赖项目
1. **LYBT.Desktop.Shell** - Shell在启动时加载Patients模块
2. **LYBT.Desktop.MedicalCase** - 病案模块依赖患者选择结果(PatientSelectedEvent)
3. **LYBT.Desktop.Consultation** - 诊断模块依赖患者信息展示

### NuGet包
- **Prism.Core** (8.x) - MVVM核心框架
- **Prism.DryIoc** (8.x) - 依赖注入与模块化支持
- **MaterialDesignThemes** (5.1.x) - Material Design UI组件
- **Microsoft.Extensions.Logging** (8.0.x) - 结构化日志
- **System.Collections.ObjectModel** (8.0.x) - ObservableCollection支持

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF (Windows Presentation Foundation)**: Windows桌面UI框架
- **Prism.DryIoc 8.x**: MVVM框架,模块化,依赖注入,区域导航
- **MaterialDesignThemes 5.1.x**: Material Design风格UI组件库
- **MVVM架构**: Model-View-ViewModel架构模式
- **Prism EventAggregator**: 事件聚合器(PatientSelectedEvent、MedicalCaseCreatedEvent等)
- **Prism Dialog Service**: 模态对话框服务(QuickCreatePatientDialog、UnfinishedCaseDialog)
- **Prism Navigation**: 区域导航(PatientSelectionView → ConsultationView)
- **Repository Pattern**: 三层架构(ViewModel → Repository → Foundation.BaseApiRepository → IApiService)
- **异步编程**: async/await异步模式,避免阻塞UI线程

##  快速开始

此项目是一个类库,作为Prism模块被 `LYBT.Desktop.Shell` 加载和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Patients/LYBT.Desktop.Patients.csproj
```

**集成说明**:

### 1. Shell加载Patients模块(在App.xaml.cs中)

```csharp
// App.xaml.cs - Prism应用程序入口
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // Patients模块按需加载(InitializationMode.WhenAvailable)
    // 医生登录后,Shell会自动加载Patients模块
    moduleCatalog.AddModule<PatientsModule>(InitializationMode.WhenAvailable);
}
```

### 2. IPatientRepository核心接口

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `GetAllAsync()` | `Task<List<PatientDto>>` | 查询所有患者(无分页) |
| `GetByIdAsync(Guid id)` | `Task<PatientDto?>` | 按ID查询患者详情 |
| `CreateAsync(CreatePatientDto dto)` | `Task<PatientDto?>` | 创建新患者 |
| `UpdateAsync(UpdatePatientDto dto)` | `Task<PatientDto?>` | 更新患者信息 |
| `DeleteAsync(Guid id)` | `Task<bool>` | 删除患者 |
| `SearchAsync(string keyword)` | `Task<List<PatientDto>>` | 搜索患者(按姓名/电话) |
| `GetPagedAsync(int pageIndex, int pageSize, Dictionary<string, string>? queryParams)` | `Task<PagedResultDto<PatientDto>>` | 分页查询患者 |

### 3. PatientSelectionViewModel核心属性与方法

**核心属性(20个)**:

| 属性 | 类型 | 说明 |
|------|------|------|
| `Patients` | `ObservableCollection<PatientItem>` | 患者列表(当前页) |
| `SelectedPatient` | `PatientItem?` | 当前选中的患者 |
| `SearchKeyword` | `string?` | 搜索关键字(防抖500ms) |
| `CurrentPage` | `int` | 当前页码(从1开始) |
| `TotalPages` | `int` | 总页数 |
| `TotalCount` | `int` | 总记录数 |
| `PageSize` | `const int` | 每页大小(常量20) |
| `PendingQueue` | `ObservableCollection<PendingPatientItem>` | 待诊队列 |
| `SelectedPendingPatient` | `PendingPatientItem?` | 选中的待诊患者 |
| `CurrentPatient` | `PatientDto?` | 当前诊疗的患者(发布事件后) |
| `MedicalCaseFlowId` | `Guid?` | 当前病案流程ID |
| `SearchCommand` | `DelegateCommand` | 搜索命令 |
| `NewPatientCommand` | `DelegateCommand` | 快速创建患者命令 |
| `SelectPatientCommand` | `DelegateCommand` | 选择患者命令 |
| `DoubleClickPatientCommand` | `DelegateCommand<PatientItem>` | 双击患者命令 |
| `PreviousPageCommand` | `DelegateCommand` | 上一页命令 |
| `NextPageCommand` | `DelegateCommand` | 下一页命令 |
| `BackToHomeCommand` | `DelegateCommand` | 返回首页命令 |
| `StartConsultationCommand` | `DelegateCommand` | 开始看诊命令 |

**核心方法(25个)**:

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `PatientSelectionViewModel(...)` | 构造函数 | 依赖注入7个服务,初始化命令,订阅事件 |
| `ExecuteSearchAsync()` | `Task` | 执行搜索(重置到第1页,调用API) |
| `CanExecuteSearch()` | `bool` | 判断搜索按钮是否可用(非空+非忙碌) |
| `ExecuteNewPatient()` | `void` | 打开快速创建患者对话框 |
| `ExecuteSelectPatient()` | `void` | 选择患者(更新SelectedPatient) |
| `CanExecuteSelectPatient()` | `bool` | 判断选择按钮是否可用(必须选中患者) |
| `ExecuteDoubleClickPatient(PatientItem)` | `void` | 双击患者(快速选择+开始看诊) |
| `ExecutePreviousPageAsync()` | `Task` | 上一页(CurrentPage-1,重新加载) |
| `CanExecutePreviousPage()` | `bool` | 判断上一页按钮是否可用(CurrentPage>1) |
| `ExecuteNextPageAsync()` | `Task` | 下一页(CurrentPage+1,重新加载) |
| `CanExecuteNextPage()` | `bool` | 判断下一页按钮是否可用(CurrentPage<TotalPages) |
| `ExecuteBackToHome()` | `void` | 返回首页(导航到HomeView) |
| `ExecuteStartConsultation()` | `Task` | 开始看诊(检查未完成病案→创建/继续病案→导航到ConsultationView) |
| `CanExecuteStartConsultation()` | `bool` | 判断开始看诊按钮是否可用(必须选中患者+非忙碌) |
| `CheckUnfinishedMedicalCaseAsync(Guid)` | `Task<MedicalCaseDto?>` | 检查患者是否有未完成病案 |
| `ShowUnfinishedCaseDialogAsync(MedicalCaseDto)` | `Task<IDialogResult>` | 显示未完成病案对话框(3个选项) |
| `ContinueConsultationAsync(Guid)` | `Task` | 继续看诊(加载旧病案→导航) |
| `CreateNewCaseAfterClosingOldAsync(Guid, Guid)` | `Task` | 关闭旧病案后创建新病案 |
| `CloseOldCaseOnlyAsync(Guid)` | `Task` | 仅关闭旧病案(不创建新病案) |
| `CloseOldMedicalCaseAsync(Guid)` | `Task` | 关闭旧病案(调用API更新状态) |
| `PublishPatientSelectedEvent(PatientDto, Guid)` | `void` | 发布患者选择事件(通知其他模块) |
| `LoadCurrentPageAsync()` | `Task` | 加载当前页(调用API,更新Patients) |
| `LoadInitialPatientsAsync()` | `Task` | 加载初始患者列表(第1页) |
| `LoadPendingCasesAsync()` | `Task` | 加载待诊队列(未完成病案患者列表) |
| `LoadPatientForPendingCaseAsync(Guid)` | `Task` | 加载待诊患者详情 |
| `OnNavigatedTo(NavigationContext)` | `void` | 导航进入时(加载初始数据) |
| `IsNavigationTarget(NavigationContext)` | `bool` | 判断是否可以重用(返回true) |
| `OnNavigatedFrom(NavigationContext)` | `void` | 导航离开时(清理资源) |

### 4. 患者列表加载与分页

```csharp
// PatientSelectionViewModel.cs
public class PatientSelectionViewModel : BindableBase, INavigationAware
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IDialogService _dialogService;
    private readonly IMedicalCaseApi _medicalCaseApi;
    private const int PageSize = 20; // 每页20条

    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalCount = 0;

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            SetProperty(ref _currentPage, value);
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    public ObservableCollection<PatientItem> Patients { get; } = new();

    // 加载当前页数据
    private async Task LoadCurrentPageAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessage();

            var queryParams = new Dictionary<string, string>
            {
                ["pageIndex"] = CurrentPage.ToString(),
                ["pageSize"] = PageSize.ToString()
            };

            // 如果有搜索关键字,添加到查询参数
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                queryParams["keyword"] = SearchKeyword;
            }

            // 调用Repository分页查询
            var result = await _patientRepository.GetPagedAsync(CurrentPage, PageSize, queryParams);

            if (result != null)
            {
                Patients.Clear();
                foreach (var patient in result.Items)
                {
                    Patients.Add(new PatientItem
                    {
                        Id = patient.Id,
                        Name = patient.Name,
                        Gender = patient.Gender,
                        Age = patient.Age,
                        PhoneNumber = patient.PhoneNumber,
                        Address = patient.Address,
                        CreatedAt = patient.CreatedAt
                    });
                }

                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / PageSize);

                _logger.LogInformation("成功加载患者列表:当前页{CurrentPage},共{TotalPages}页,总计{TotalCount}条",
                    CurrentPage, TotalPages, TotalCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载患者列表失败");
            SetErrorMessage($"加载患者列表失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 上一页
    private async Task ExecutePreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadCurrentPageAsync();
        }
    }

    private bool CanExecutePreviousPage()
    {
        return CurrentPage > 1;
    }

    // 下一页
    private async Task ExecuteNextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadCurrentPageAsync();
        }
    }

    private bool CanExecuteNextPage()
    {
        return CurrentPage < TotalPages;
    }
}
```

### 5. 快速创建患者对话框

```csharp
// PatientSelectionViewModel.cs - 打开快速创建对话框
private void ExecuteNewPatient()
{
    try
    {
        _logger.LogInformation("打开快速创建患者对话框");

        // 使用Prism Dialog Service打开对话框
        _dialogService.ShowDialog(
            "QuickCreatePatientDialog",
            new DialogParameters(),
            result =>
            {
                // 对话框关闭后的回调
                if (result.Result == ButtonResult.OK)
                {
                    // 获取对话框返回的患者DTO
                    var newPatient = result.Parameters.GetValue<PatientDto>("Patient");

                    if (newPatient != null)
                    {
                        _logger.LogInformation("快速创建患者成功:{Name},ID:{Id}",
                            newPatient.Name, newPatient.Id);

                        // 刷新患者列表
                        _ = LoadCurrentPageAsync();

                        SetSuccessMessage($"患者 {newPatient.Name} 创建成功");
                    }
                }
            });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "打开快速创建患者对话框失败");
        SetErrorMessage($"打开对话框失败:{ex.Message}");
    }
}

// QuickCreatePatientDialogViewModel.cs - 对话框ViewModel
public class QuickCreatePatientDialogViewModel : BindableBase, IDialogAware
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<QuickCreatePatientDialogViewModel> _logger;

    public string Title => "快速创建患者";

    // 患者信息属性
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            SetProperty(ref _name, value);
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    private Gender _gender = Gender.Male;
    public Gender Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }

    private int _age;
    public int Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }

    private string? _phoneNumber;
    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    // 命令
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public QuickCreatePatientDialogViewModel(
        IPatientRepository patientRepository,
        ILogger<QuickCreatePatientDialogViewModel> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;

        SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    private async void ExecuteSave()
    {
        try
        {
            IsBusy = true;

            // 创建患者DTO
            var createDto = new CreatePatientDto
            {
                Name = Name,
                Gender = Gender,
                Age = Age,
                PhoneNumber = PhoneNumber
            };

            // 调用Repository创建患者
            var newPatient = await _patientRepository.CreateAsync(createDto);

            if (newPatient != null)
            {
                _logger.LogInformation("快速创建患者成功:{Name}", newPatient.Name);

                // 关闭对话框并返回结果
                RequestClose?.Invoke(new DialogResult(
                    ButtonResult.OK,
                    new DialogParameters { { "Patient", newPatient } }
                ));
            }
            else
            {
                SetErrorMessage("创建患者失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败");
            SetErrorMessage($"创建失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteSave()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }

    private void ExecuteCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    public event Action<IDialogResult>? RequestClose;
}
```

### 6. 未完成病案检查与处理

```csharp
// PatientSelectionViewModel.cs - 开始看诊前检查未完成病案
private async Task ExecuteStartConsultation()
{
    if (SelectedPatient == null) return;

    try
    {
        IsBusy = true;
        ClearMessage();

        _logger.LogInformation("开始看诊,患者:{Name},ID:{Id}",
            SelectedPatient.Name, SelectedPatient.Id);

        // Step 1: 检查是否有未完成的病案
        var unfinishedCase = await CheckUnfinishedMedicalCaseAsync(SelectedPatient.Id);

        if (unfinishedCase != null)
        {
            _logger.LogWarning("患者{Name}存在未完成病案,ID:{CaseId},状态:{Status}",
                SelectedPatient.Name, unfinishedCase.Id, unfinishedCase.Status);

            // Step 2: 显示未完成病案对话框(三个选项)
            var dialogResult = await ShowUnfinishedCaseDialogAsync(unfinishedCase);

            if (dialogResult.Result == ButtonResult.OK)
            {
                var action = dialogResult.Parameters.GetValue<string>("Action");

                switch (action)
                {
                    case "Continue":
                        // 选项1: 继续看诊(加载旧病案)
                        await ContinueConsultationAsync(unfinishedCase.Id);
                        break;

                    case "CloseAndNew":
                        // 选项2: 关闭旧病案后创建新病案
                        await CreateNewCaseAfterClosingOldAsync(
                            unfinishedCase.Id, SelectedPatient.Id);
                        break;

                    case "CloseOnly":
                        // 选项3: 仅关闭旧病案(不创建新病案)
                        await CloseOldCaseOnlyAsync(unfinishedCase.Id);
                        break;
                }
            }
            else
            {
                // 用户取消
                _logger.LogInformation("用户取消处理未完成病案");
            }
        }
        else
        {
            // Step 3: 无未完成病案,直接创建新病案
            _logger.LogInformation("患者{Name}无未完成病案,创建新病案", SelectedPatient.Name);

            var createDto = new CreateMedicalCaseDto
            {
                PatientId = SelectedPatient.Id,
                DoctorId = CurrentUser.Id,
                Status = MedicalCaseStatus.InProgress
            };

            var newCase = await _medicalCaseRepository.CreateAsync(createDto);

            if (newCase != null)
            {
                MedicalCaseFlowId = newCase.Id;

                // 发布患者选择事件
                PublishPatientSelectedEvent(
                    await _patientRepository.GetByIdAsync(SelectedPatient.Id), newCase.Id);

                // 导航到诊断视图
                _regionManager.RequestNavigate(
                    "ContentRegion",
                    "ConsultationView",
                    new NavigationParameters
                    {
                        { "MedicalCaseId", newCase.Id },
                        { "PatientId", SelectedPatient.Id }
                    });
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "开始看诊失败");
        SetErrorMessage($"开始看诊失败:{ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

// 检查未完成病案
private async Task<MedicalCaseDto?> CheckUnfinishedMedicalCaseAsync(Guid patientId)
{
    try
    {
        // 调用API查询患者的未完成病案
        var result = await _medicalCaseApi.GetUnfinishedCasesByPatientIdAsync(patientId);

        if (result.IsSuccess && result.Data?.Any() == true)
        {
            // 返回第一个未完成病案
            return result.Data.First();
        }

        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "检查未完成病案失败,患者ID:{PatientId}", patientId);
        return null;
    }
}

// 显示未完成病案对话框
private async Task<IDialogResult> ShowUnfinishedCaseDialogAsync(MedicalCaseDto unfinishedCase)
{
    var tcs = new TaskCompletionSource<IDialogResult>();

    _dialogService.ShowDialog(
        "UnfinishedCaseDialog",
        new DialogParameters
        {
            { "MedicalCase", unfinishedCase }
        },
        result => tcs.SetResult(result));

    return await tcs.Task;
}

// 继续看诊(加载旧病案)
private async Task ContinueConsultationAsync(Guid medicalCaseId)
{
    _logger.LogInformation("继续看诊,病案ID:{CaseId}", medicalCaseId);

    MedicalCaseFlowId = medicalCaseId;

    // 加载病案详情
    var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
    if (medicalCase != null)
    {
        // 发布患者选择事件
        PublishPatientSelectedEvent(
            await _patientRepository.GetByIdAsync(medicalCase.PatientId), medicalCaseId);

        // 导航到诊断视图
        _regionManager.RequestNavigate(
            "ContentRegion",
            "ConsultationView",
            new NavigationParameters
            {
                { "MedicalCaseId", medicalCaseId },
                { "PatientId", medicalCase.PatientId }
            });
    }
}

// 关闭旧病案后创建新病案
private async Task CreateNewCaseAfterClosingOldAsync(Guid oldCaseId, Guid patientId)
{
    _logger.LogInformation("关闭旧病案{OldCaseId}后创建新病案", oldCaseId);

    // Step 1: 关闭旧病案
    await CloseOldMedicalCaseAsync(oldCaseId);

    // Step 2: 创建新病案
    var createDto = new CreateMedicalCaseDto
    {
        PatientId = patientId,
        DoctorId = CurrentUser.Id,
        Status = MedicalCaseStatus.InProgress
    };

    var newCase = await _medicalCaseRepository.CreateAsync(createDto);

    if (newCase != null)
    {
        MedicalCaseFlowId = newCase.Id;

        // 发布患者选择事件
        PublishPatientSelectedEvent(
            await _patientRepository.GetByIdAsync(patientId), newCase.Id);

        // 导航到诊断视图
        _regionManager.RequestNavigate(
            "ContentRegion",
            "ConsultationView",
            new NavigationParameters
            {
                { "MedicalCaseId", newCase.Id },
                { "PatientId", patientId }
            });
    }
}

// 关闭旧病案
private async Task CloseOldMedicalCaseAsync(Guid caseId)
{
    try
    {
        var updateDto = new UpdateMedicalCaseDto
        {
            Id = caseId,
            Status = MedicalCaseStatus.Cancelled
        };

        await _medicalCaseRepository.UpdateAsync(updateDto);

        _logger.LogInformation("成功关闭旧病案,ID:{CaseId}", caseId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "关闭旧病案失败,ID:{CaseId}", caseId);
        throw;
    }
}
```

### 7. 待诊队列管理

```csharp
// PatientSelectionViewModel.cs - 待诊队列
public ObservableCollection<PendingPatientItem> PendingQueue { get; } = new();

private PendingPatientItem? _selectedPendingPatient;
public PendingPatientItem? SelectedPendingPatient
{
    get => _selectedPendingPatient;
    set
    {
        if (SetProperty(ref _selectedPendingPatient, value))
        {
            // 选中待诊患者后,自动加载患者详情并启动看诊流程
            if (_selectedPendingPatient != null)
            {
                _ = LoadPatientForPendingCaseAsync(_selectedPendingPatient.PatientId);
            }
        }
    }
}

// 加载待诊队列
private async Task LoadPendingCasesAsync()
{
    try
    {
        _logger.LogInformation("开始加载待诊队列");

        // 调用API查询所有进行中的病案
        var result = await _medicalCaseApi.GetInProgressCasesAsync();

        if (result.IsSuccess && result.Data != null)
        {
            PendingQueue.Clear();

            foreach (var medicalCase in result.Data)
            {
                // 加载患者信息
                var patient = await _patientRepository.GetByIdAsync(medicalCase.PatientId);
                if (patient != null)
                {
                    PendingQueue.Add(new PendingPatientItem
                    {
                        MedicalCaseId = medicalCase.Id,
                        PatientId = patient.Id,
                        PatientName = patient.Name,
                        Gender = patient.Gender,
                        Age = patient.Age,
                        CreatedAt = medicalCase.CreatedAt
                    });
                }
            }

            _logger.LogInformation("成功加载待诊队列,共{Count}个患者", PendingQueue.Count);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载待诊队列失败");
    }
}

// 加载待诊患者详情并启动看诊
private async Task LoadPatientForPendingCaseAsync(Guid patientId)
{
    try
    {
        IsBusy = true;

        // 加载患者详情
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            _logger.LogWarning("待诊患者不存在,ID:{PatientId}", patientId);
            return;
        }

        // 查找对应的病案ID
        var pendingItem = PendingQueue.FirstOrDefault(p => p.PatientId == patientId);
        if (pendingItem == null)
        {
            _logger.LogWarning("待诊患者无对应病案,ID:{PatientId}", patientId);
            return;
        }

        MedicalCaseFlowId = pendingItem.MedicalCaseId;

        _logger.LogInformation("继续待诊患者看诊:{Name},病案ID:{CaseId}",
            patient.Name, pendingItem.MedicalCaseId);

        // 发布患者选择事件
        PublishPatientSelectedEvent(patient, pendingItem.MedicalCaseId);

        // 导航到诊断视图
        _regionManager.RequestNavigate(
            "ContentRegion",
            "ConsultationView",
            new NavigationParameters
            {
                { "MedicalCaseId", pendingItem.MedicalCaseId },
                { "PatientId", patientId }
            });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载待诊患者失败,ID:{PatientId}", patientId);
        SetErrorMessage($"加载待诊患者失败:{ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 8. PatientRepository实现

```csharp
// PatientRepository.cs
public class PatientRepository : BaseApiRepository<PatientDto>, IPatientRepository
{
    public PatientRepository(IApiService apiService, ILogger<PatientRepository> logger)
        : base(apiService, logger, "/api/v1/patients")
    {
    }

    // 基础CRUD方法继承自BaseApiRepository:
    // - GetAllAsync(): Task<List<PatientDto>>
    // - GetByIdAsync(Guid id): Task<PatientDto?>
    // - CreateAsync(CreatePatientDto dto): Task<PatientDto?>
    // - UpdateAsync(UpdatePatientDto dto): Task<PatientDto?>
    // - DeleteAsync(Guid id): Task<bool>

    public async Task<List<PatientDto>> SearchAsync(string keyword)
    {
        try
        {
            var result = await _apiService.GetAsync<List<PatientDto>>(
                $"{_endpoint}/search?keyword={Uri.EscapeDataString(keyword)}");

            return result.IsSuccess && result.Data != null
                ? result.Data
                : new List<PatientDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索患者失败,关键字:{Keyword}", keyword);
            return new List<PatientDto>();
        }
    }

    public async Task<PagedResultDto<PatientDto>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        Dictionary<string, string>? queryParams = null)
    {
        try
        {
            var url = $"{_endpoint}/paged?pageIndex={pageIndex}&pageSize={pageSize}";

            // 添加额外查询参数(如搜索关键字)
            if (queryParams != null)
            {
                foreach (var param in queryParams)
                {
                    url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
                }
            }

            var result = await _apiService.GetAsync<PagedResultDto<PatientDto>>(url);

            return result.IsSuccess && result.Data != null
                ? result.Data
                : new PagedResultDto<PatientDto> { Items = new List<PatientDto>(), TotalCount = 0 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询患者失败,页码:{PageIndex},大小:{PageSize}",
                pageIndex, pageSize);
            return new PagedResultDto<PatientDto> { Items = new List<PatientDto>(), TotalCount = 0 };
        }
    }
}
```

### 9. PatientSelectionView XAML绑定

```xml
<!-- PatientSelectionView.xaml -->
<UserControl x:Class="LYBT.Desktop.Patients.Views.PatientSelectionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- 搜索栏 -->
            <RowDefinition Height="*"/>    <!-- 患者列表 -->
            <RowDefinition Height="Auto"/> <!-- 分页控件 -->
        </Grid.RowDefinitions>

        <!-- Row 0: 搜索栏 -->
        <Grid Grid.Row="0" Margin="16">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- 搜索框 -->
            <TextBox Grid.Column="0"
                     Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                     md:HintAssist.Hint="搜索患者(姓名/电话)"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     Margin="0,0,8,0"/>

            <!-- 搜索按钮 -->
            <Button Grid.Column="1"
                    Command="{Binding SearchCommand}"
                    Content="搜索"
                    Style="{StaticResource MaterialDesignRaisedButton}"
                    Margin="0,0,8,0"/>

            <!-- 快速创建患者按钮 -->
            <Button Grid.Column="2"
                    Command="{Binding NewPatientCommand}"
                    Content="快速创建患者"
                    Style="{StaticResource MaterialDesignRaisedAccentButton}"
                    Margin="0,0,8,0"/>

            <!-- 返回首页按钮 -->
            <Button Grid.Column="3"
                    Command="{Binding BackToHomeCommand}"
                    Content="返回首页"
                    Style="{StaticResource MaterialDesignOutlinedButton}"/>
        </Grid>

        <!-- Row 1: 患者列表 -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>   <!-- 患者列表 -->
                <ColumnDefinition Width="Auto"/> <!-- 待诊队列 -->
            </Grid.ColumnDefinitions>

            <!-- 患者列表DataGrid -->
            <DataGrid Grid.Column="0"
                      ItemsSource="{Binding Patients}"
                      SelectedItem="{Binding SelectedPatient}"
                      AutoGenerateColumns="False"
                      IsReadOnly="True"
                      SelectionMode="Single"
                      CanUserSortColumns="True"
                      CanUserResizeColumns="True"
                      VirtualizingPanel.IsVirtualizing="True"
                      VirtualizingPanel.VirtualizationMode="Recycling"
                      Margin="16,0,8,0">

                <!-- 双击患者行快速开始看诊 -->
                <DataGrid.InputBindings>
                    <MouseBinding Gesture="LeftDoubleClick"
                                  Command="{Binding DoubleClickPatientCommand}"
                                  CommandParameter="{Binding SelectedItem, RelativeSource={RelativeSource AncestorType=DataGrid}}"/>
                </DataGrid.InputBindings>

                <DataGrid.Columns>
                    <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="120"/>
                    <DataGridTextColumn Header="性别" Binding="{Binding Gender}" Width="60"/>
                    <DataGridTextColumn Header="年龄" Binding="{Binding Age}" Width="60"/>
                    <DataGridTextColumn Header="电话" Binding="{Binding PhoneNumber}" Width="150"/>
                    <DataGridTextColumn Header="地址" Binding="{Binding Address}" Width="*"/>
                    <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd}}" Width="120"/>
                </DataGrid.Columns>
            </DataGrid>

            <!-- 待诊队列 -->
            <Border Grid.Column="1"
                    Width="250"
                    Background="{DynamicResource MaterialDesignPaper}"
                    BorderBrush="{DynamicResource MaterialDesignDivider}"
                    BorderThickness="1,0,0,0"
                    Margin="0,0,16,0">

                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>

                    <!-- 待诊队列标题 -->
                    <TextBlock Grid.Row="0"
                               Text="待诊队列"
                               Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                               Margin="16,16,16,8"/>

                    <!-- 待诊患者列表 -->
                    <ListBox Grid.Row="1"
                             ItemsSource="{Binding PendingQueue}"
                             SelectedItem="{Binding SelectedPendingPatient}"
                             Margin="8,0,8,8">
                        <ListBox.ItemTemplate>
                            <DataTemplate>
                                <StackPanel Margin="8">
                                    <TextBlock Text="{Binding PatientName}"
                                               Style="{StaticResource MaterialDesignBody1TextBlock}"/>
                                    <TextBlock Text="{Binding Age, StringFormat='{}{0}岁'}"
                                               Style="{StaticResource MaterialDesignCaptionTextBlock}"/>
                                    <TextBlock Text="{Binding CreatedAt, StringFormat='登记时间:{0:HH:mm}'}"
                                               Style="{StaticResource MaterialDesignCaptionTextBlock}"/>
                                </StackPanel>
                            </DataTemplate>
                        </ListBox.ItemTemplate>
                    </ListBox>
                </Grid>
            </Border>
        </Grid>

        <!-- Row 2: 分页控件 -->
        <Grid Grid.Row="2" Margin="16">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- 上一页 -->
            <Button Grid.Column="0"
                    Command="{Binding PreviousPageCommand}"
                    Content="上一页"
                    Style="{StaticResource MaterialDesignOutlinedButton}"
                    Margin="0,0,8,0"/>

            <!-- 页码信息 -->
            <StackPanel Grid.Column="1"
                        Orientation="Horizontal"
                        HorizontalAlignment="Center"
                        VerticalAlignment="Center">
                <TextBlock Text="第"/>
                <TextBlock Text="{Binding CurrentPage}" Margin="4,0"/>
                <TextBlock Text="/"/>
                <TextBlock Text="{Binding TotalPages}" Margin="4,0"/>
                <TextBlock Text="页,共"/>
                <TextBlock Text="{Binding TotalCount}" Margin="4,0"/>
                <TextBlock Text="条记录"/>
            </StackPanel>

            <!-- 下一页 -->
            <Button Grid.Column="2"
                    Command="{Binding NextPageCommand}"
                    Content="下一页"
                    Style="{StaticResource MaterialDesignOutlinedButton}"
                    Margin="8,0,0,0"/>
        </Grid>

        <!-- 选择患者后的操作按钮(浮动) -->
        <Button Grid.Row="1"
                Command="{Binding StartConsultationCommand}"
                Content="开始看诊"
                Style="{StaticResource MaterialDesignFloatingActionButton}"
                HorizontalAlignment="Right"
                VerticalAlignment="Bottom"
                Margin="0,0,32,32"
                Visibility="{Binding SelectedPatient, Converter={StaticResource NullToVisibilityConverter}}"/>
    </Grid>
</UserControl>
```

### 10. PatientsModule注册

```csharp
// PatientsModule.cs
public class PatientsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化(可选)
        // 用于模块加载后的初始化逻辑,如订阅全局事件
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 1. 注册Repository
        containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();

        // 2. 注册ViewModels(Transient生命周期)
        containerRegistry.Register<PatientSelectionViewModel>();
        containerRegistry.Register<PatientDetailViewModel>();
        containerRegistry.Register<PatientImportWizardViewModel>();
        containerRegistry.Register<QuickCreatePatientDialogViewModel>();
        containerRegistry.Register<UnfinishedCaseDialogViewModel>();

        // 3. 注册Views(导航视图)
        containerRegistry.RegisterForNavigation<PatientSelectionView>();
        containerRegistry.RegisterForNavigation<PatientDetailView>();
        containerRegistry.RegisterForNavigation<PatientImportWizardView>();

        // 4. 注册Dialogs(对话框)
        containerRegistry.RegisterDialog<QuickCreatePatientDialog, QuickCreatePatientDialogViewModel>();
        containerRegistry.RegisterDialog<UnfinishedCaseDialog, UnfinishedCaseDialogViewModel>();
    }
}
```

## 🎨 模块架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                      LYBT.Desktop.Patients                           │
│                        (患者管理模块)                                 │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                 ┌───────────────┼───────────────┐
                 │               │               │
         ┌───────▼───────┐ ┌────▼─────┐ ┌──────▼──────┐
         │   ViewModels   │ │  Views   │ │ Repositories│
         │   (5个)        │ │  (5对)   │ │   (1个)     │
         └───────┬───────┘ └────┬─────┘ └──────┬──────┘
                 │               │               │
                 └───────────────┼───────────────┘
                                 │
                ┌────────────────▼────────────────┐
                │     LYBT.Desktop.Foundation     │
                │         (基础设施层)             │
                │  - UnifiedViewModelBase         │
                │  - BaseApiRepository            │
                │  - IApiService                  │
                │  - IDialogService               │
                │  - IRegionManager               │
                └────────────────┬────────────────┘
                                 │
                ┌────────────────▼────────────────┐
                │       LYBT.Shared.Models        │
                │         (共享DTO层)              │
                │  - PatientDto                   │
                │  - CreatePatientDto             │
                │  - UpdatePatientDto             │
                │  - PagedResultDto<T>            │
                │  - MedicalCaseDto               │
                └────────────────┬────────────────┘
                                 │
                                 │ HTTP API
                                 ▼
                ┌─────────────────────────────────┐
                │        LYBT.WebAPI              │
                │    (Server端 RESTful API)       │
                │  GET    /api/v1/patients        │
                │  GET    /api/v1/patients/{id}   │
                │  POST   /api/v1/patients        │
                │  PUT    /api/v1/patients/{id}   │
                │  DELETE /api/v1/patients/{id}   │
                │  GET    /api/v1/patients/search │
                │  GET    /api/v1/patients/paged  │
                └─────────────────────────────────┘

事件驱动通信(Prism EventAggregator):
  PatientSelectedEvent: Patients → MedicalCase/Consultation/Prescriptions
  MedicalCaseCreatedEvent: MedicalCase → Patients(更新待诊队列)

三层架构数据流:
  ViewModel → Repository → BaseApiRepository → IApiService → HTTP → WebAPI
```

##  设计原则

### 1. MVVM架构与Prism导航

**核心实现**:
- **PatientSelectionViewModel**实现**INavigationAware**接口,支持导航生命周期管理
- **OnNavigatedTo**:加载初始患者列表和待诊队列
- **IsNavigationTarget**:判断是否可以重用ViewModel实例(返回true)
- **OnNavigatedFrom**:清理资源(取消订阅、停止定时器)
- **NavigationParameters**:跨视图参数传递(PatientId、MedicalCaseId)

**示例**:
```csharp
// 导航到诊断视图,传递病案ID和患者ID
_regionManager.RequestNavigate(
    "ContentRegion",
    "ConsultationView",
    new NavigationParameters
    {
        { "MedicalCaseId", newCase.Id },
        { "PatientId", SelectedPatient.Id }
    });
```

**反模式**:
-  直接创建视图实例并手动替换(破坏MVVM)
-  使用静态变量传递数据(线程不安全)
-  ViewModel耦合其他模块的ViewModel(违反单一职责)

### 2. Repository模式与三层架构

**核心实现**:
- **PatientRepository**继承**BaseApiRepository<PatientDto>**,复用基础CRUD方法
- **三层数据流**:ViewModel → Repository → BaseApiRepository → IApiService → HTTP
- **异常隔离**:Repository内部捕获异常并记录日志,向ViewModel返回null或空列表
- **分页查询**:GetPagedAsync支持动态查询参数(searchKeyword、filters)

**示例**:
```csharp
// Repository层处理异常,向上返回安全的默认值
public async Task<List<PatientDto>> SearchAsync(string keyword)
{
    try
    {
        var result = await _apiService.GetAsync<List<PatientDto>>(
            $"{_endpoint}/search?keyword={Uri.EscapeDataString(keyword)}");
        return result.IsSuccess && result.Data != null
            ? result.Data
            : new List<PatientDto>(); // 默认返回空列表
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "搜索患者失败");
        return new List<PatientDto>(); // 异常时返回空列表
    }
}
```

**反模式**:
-  ViewModel直接调用IApiService(跳过Repository层)
-  Repository抛出异常到ViewModel(未处理异常传播)
-  在ViewModel中构造HTTP请求URL(职责混乱)

### 3. 事件驱动通信与模块解耦

**核心实现**:
- **Prism EventAggregator**:发布/订阅模式实现模块间通信
- **PatientSelectedEvent**:Patients模块发布,MedicalCase/Consultation/Prescriptions模块订阅
- **MedicalCaseCreatedEvent**:MedicalCase模块发布,Patients模块订阅(更新待诊队列)
- **ThreadOption.UIThread**:确保事件处理在UI线程执行

**示例**:
```csharp
// 发布患者选择事件
private void PublishPatientSelectedEvent(PatientDto patient, Guid medicalCaseId)
{
    _eventAggregator.GetEvent<PatientSelectedEvent>().Publish(new PatientSelectedPayload
    {
        Patient = patient,
        MedicalCaseId = medicalCaseId
    });

    _logger.LogInformation("已发布患者选择事件,患者:{Name},病案ID:{CaseId}",
        patient.Name, medicalCaseId);
}

// 订阅病案创建事件(更新待诊队列)
public PatientSelectionViewModel(...)
{
    _eventAggregator.GetEvent<MedicalCaseCreatedEvent>()
        .Subscribe(OnMedicalCaseCreated, ThreadOption.UIThread);
}

private void OnMedicalCaseCreated(MedicalCaseCreatedPayload payload)
{
    // 刷新待诊队列
    _ = LoadPendingCasesAsync();
}
```

**反模式**:
-  直接引用其他模块的ViewModel(强耦合)
-  使用静态事件(内存泄漏风险)
-  在后台线程修改ObservableCollection(跨线程访问异常)

### 4. 对话框服务与用户交互

**核心实现**:
- **Prism Dialog Service**:统一的模态对话框管理
- **DialogParameters**:向对话框传递参数(输入)
- **DialogResult**:对话框返回结果(输出)
- **ButtonResult**:OK/Cancel/Yes/No等标准按钮结果
- **IDialogAware**:对话框ViewModel实现的接口

**示例**:
```csharp
// 打开快速创建患者对话框
_dialogService.ShowDialog(
    "QuickCreatePatientDialog",
    new DialogParameters(), // 无输入参数
    result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            // 获取对话框返回的患者DTO
            var newPatient = result.Parameters.GetValue<PatientDto>("Patient");
            if (newPatient != null)
            {
                // 刷新患者列表
                _ = LoadCurrentPageAsync();
            }
        }
    });

// 对话框关闭并返回结果
RequestClose?.Invoke(new DialogResult(
    ButtonResult.OK,
    new DialogParameters { { "Patient", newPatient } }
));
```

**反模式**:
-  直接创建Window实例(破坏MVVM,难以测试)
-  使用MessageBox(不符合Material Design,难以自定义)
-  对话框直接修改主ViewModel数据(违反单向数据流)

### 5. 分页优化与虚拟化

**核心实现**:
- **Server端分页**:只加载当前页数据(pageIndex、pageSize)
- **WPF DataGrid虚拟化**:VirtualizingPanel.IsVirtualizing="True"
- **ObservableCollection刷新**:Clear() + AddRange()避免多次触发通知
- **防抖搜索**:SearchKeyword属性设置500ms防抖定时器

**示例**:
```csharp
// SearchKeyword属性防抖
private string? _searchKeyword;
public string? SearchKeyword
{
    get => _searchKeyword;
    set
    {
        if (SetProperty(ref _searchKeyword, value))
        {
            // 重置防抖定时器
            _searchDebounceTimer?.Stop();
            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                _ = ExecuteSearchAsync(); // 500ms后触发搜索
            };
            _searchDebounceTimer.Start();
        }
    }
}

// DataGrid虚拟化配置
<DataGrid VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          EnableRowVirtualization="True"
          EnableColumnVirtualization="True"/>
```

**反模式**:
-  一次性加载所有患者(数据量大时性能崩溃)
-  每次输入字符立即搜索(频繁API请求)
-  未启用DataGrid虚拟化(大量UI元素渲染阻塞)

### 6. 异步优先与用户体验

**核心实现**:
- **IsBusy标志**:显示加载动画,禁用按钮,防止重复操作
- **ClearMessage**:清除旧的错误/成功消息
- **SetErrorMessage/SetSuccessMessage**:统一的消息提示
- **Task.Run避免**:所有I/O操作已经异步,无需Task.Run包装
- **ConfigureAwait(false)避免**:WPF需要UI线程上下文,不使用ConfigureAwait(false)

**示例**:
```csharp
private async Task LoadCurrentPageAsync()
{
    try
    {
        IsBusy = true; // 显示加载动画,禁用按钮
        ClearMessage(); // 清除旧消息

        var result = await _patientRepository.GetPagedAsync(...);

        if (result != null)
        {
            Patients.Clear();
            foreach (var patient in result.Items)
            {
                Patients.Add(...);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载患者列表失败");
        SetErrorMessage($"加载失败:{ex.Message}");
    }
    finally
    {
        IsBusy = false; // 隐藏加载动画,启用按钮
    }
}
```

**反模式**:
-  同步阻塞UI线程(Thread.Sleep、GetAwaiter().GetResult())
-  未设置IsBusy标志(用户可能重复点击按钮)
-  异常未捕获向上传播(导致应用崩溃)

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/patients/](../../../../docs/reference/modules/patients/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/patients-design.md](../../../../docs/explanation/architecture/client/patients-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/patients-development.md](../../../../docs/how-to-guides/client/patients-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Client端开发组
