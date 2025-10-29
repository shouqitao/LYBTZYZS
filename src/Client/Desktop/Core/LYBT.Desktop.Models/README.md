# LYBT.Desktop.Models - ViewModels基类与映射服务

## 📦 项目定位

- **层级**：Client端
- **类型**：核心库（Models层）
- **职责**：提供MVVM模式的ViewModel基类、对象映射服务、业务模型、异常处理等。为所有Desktop模块的ViewModel提供统一的基础功能（异步执行、错误处理、状态管理、验证、资源清理等）。

## 📂 代码结构

```
LYBT.Desktop.Models/
├── Exceptions/
│   └── ApiCallException.cs              # API调用异常类
│       └── 构造函数（多个重载）           # 封装HTTP错误信息
├── Http/
│   └── ProblemDetails.cs                # HTTP问题详情模型（RFC 7807）
│       └── 属性（Status、Title、Detail等）# 标准化错误响应
├── Mappers/
│   └── SimpleMapper.cs                  # 简单对象映射器
│       └── Map()                        # 对象属性映射方法
├── Mapping/
│   └── MappingService.cs                # 映射服务
│       └── Map()                        # 通用映射方法
├── Prescriptions/
│   └── PrescriptionTemplate.cs          # 处方模板模型（客户端用）
│       └── 属性（模板ID、名称、内容等）   # 处方模板数据结构
└── ViewModels/
    └── Base/
        ├── ViewModelBase.cs             # ViewModel基类（核心）
        │   ├── IsLoading                # 加载状态属性
        │   ├── IsBusy                   # 忙碌状态属性
        │   ├── HasError                 # 错误状态属性
        │   ├── ErrorMessage             # 错误消息属性
        │   ├── StatusMessage            # 状态消息属性
        │   ├── ExecuteSafelyAsync()     # 异步安全执行（2个重载）
        │   ├── ExecuteSafely()          # 同步安全执行
        │   ├── HandleError()            # 错误处理
        │   ├── AddValidationError()     # 添加验证错误
        │   ├── ClearValidationErrors()  # 清除验证错误
        │   ├── SetStatus()              # 设置状态消息
        │   ├── ClearStatus()            # 清除状态消息
        │   ├── RunOnUIThread()          # UI线程执行
        │   ├── AddDisposable()          # 添加可释放资源
        │   └── Dispose()                # 资源清理（IDisposable）
        ├── UnifiedViewModelBase.cs      # 统一ViewModel基类（扩展ViewModelBase）
        │   └── 统一的模块级功能         # 模块级通用功能封装
        └── UnifiedListViewModelBase.cs  # 统一列表ViewModel基类（扩展ViewModelBase）
            └── 列表级通用功能           # 列表页面通用功能封装
```

**说明**：
- **Exceptions/**：自定义异常类，封装API调用错误
- **Http/**：HTTP标准模型（ProblemDetails符合RFC 7807规范）
- **Mappers/**：对象映射器，用于DTO与ViewModel之间的转换
- **Mapping/**：映射服务，提供通用映射能力
- **Prescriptions/**：客户端处方相关模型（与Shared.Models不同）
- **ViewModels/Base/**：ViewModel基类，提供MVVM核心功能

### ViewModelBase核心功能

**状态管理**：
- `IsLoading`：加载状态（用于显示加载指示器）
- `IsBusy`：忙碌状态（用于禁用操作按钮）
- `HasError`：错误状态（用于显示错误UI）
- `ErrorMessage`：错误消息（用于错误提示）
- `StatusMessage`：状态消息（用于状态栏显示）

**异步执行**：
- `ExecuteSafelyAsync<T>(Func<Task<T>> action)`：执行异步操作，自动处理异常和状态
- `ExecuteSafelyAsync(Func<Task> action)`：执行无返回值的异步操作
- `ExecuteSafely(Action action)`：执行同步操作

**错误处理**：
- `HandleError(Exception ex)`：统一错误处理，转换为用户友好消息
- `AddValidationError(string propertyName, string errorMessage)`：添加验证错误
- `ClearValidationErrors()`：清除所有验证错误

**资源管理**：
- `AddDisposable(IDisposable disposable)`：注册需要清理的资源
- `Dispose()`：自动清理所有注册的资源（实现IDisposable）

**事件系统**：
- `EventAggregator`：Prism事件聚合器（依赖注入）
- `SubscribeToEvents()`：虚方法，子类重写订阅事件

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Infrastructure** - 基础设施库（事件、接口、服务等）
2. **LYBT.Desktop.Contracts** - 契约定义（接口、常量等）
3. **LYBT.Shared.Models** - 共享DTO模型（API请求/响应模型）
4. **LYBT.Shared.Utilities** - 共享工具类库（扩展方法、辅助类）

### 被依赖项目
1. **LYBT.Desktop.Modules.*** - 所有业务模块的ViewModel继承自ViewModelBase
2. **LYBT.Desktop.Workstations.*** - 所有工作站的ViewModel继承自ViewModelBase

### NuGet包
- **Prism.Core** (9.0.x) - Prism核心库（BindableBase、DelegateCommand等）
- **Prism.Wpf** (9.0.x) - Prism WPF扩展（事件聚合器、依赖注入等）
- **System.ComponentModel.Annotations** - 数据注解支持（验证特性）
- **System.Reactive** - 响应式扩展库（Rx.NET）
- **Microsoft.Extensions.Logging** (8.0.x) - 日志框架（ViewModelBase内置日志支持）
- **System.Text.Json** (8.0.x) - JSON序列化/反序列化

## 🛠 技术栈

- **.NET 8**: 基础框架
- **Prism 9.x**: MVVM框架（BindableBase、EventAggregator、DelegateCommand）
- **INotifyPropertyChanged**: WPF数据绑定接口（ViewModelBase实现）
- **INotifyDataErrorInfo**: 数据验证接口（ViewModelBase实现）
- **IDisposable**: 资源清理接口（ViewModelBase实现）
- **Reactive Extensions (Rx.NET)**: 响应式编程支持

## 🚀 快速开始

此项目是一个类库，无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Models/LYBT.Desktop.Models.csproj
```

**集成说明**：

### 1. 创建ViewModel（继承ViewModelBase）
```csharp
using LYBT.Desktop.Models.ViewModels.Base;
using Prism.Events;
using Microsoft.Extensions.Logging;

public class PatientListViewModel : ViewModelBase
{
    private readonly IPatientService _patientService;
    private ObservableCollection<PatientDto> _patients;

    public PatientListViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IPatientService patientService)
        : base(eventAggregator, loggerFactory)
    {
        _patientService = patientService;
        LoadPatientsCommand = new DelegateCommand(async () => await LoadPatientsAsync());
    }

    public ObservableCollection<PatientDto> Patients
    {
        get => _patients;
        set => SetProperty(ref _patients, value);
    }

    public DelegateCommand LoadPatientsCommand { get; }

    private async Task LoadPatientsAsync()
    {
        // ExecuteSafelyAsync 自动处理 IsLoading、HasError、ErrorMessage
        await ExecuteSafelyAsync(async () =>
        {
            var result = await _patientService.GetAllAsync();
            Patients = new ObservableCollection<PatientDto>(result);
            SetStatus($"加载了 {result.Count} 个患者");
        });
    }

    protected override void InitializeCommands()
    {
        base.InitializeCommands();
        // 初始化其他命令
    }

    protected override void SubscribeToEvents()
    {
        base.SubscribeToEvents();
        // 订阅事件
        EventAggregator.GetEvent<PatientSelectedEvent>().Subscribe(OnPatientSelected);
    }

    private void OnPatientSelected(PatientSelectedPayload payload)
    {
        // 处理患者选中事件
    }

    protected override void OnDisposing()
    {
        base.OnDisposing();
        // 清理资源（ViewModelBase会自动调用）
    }
}
```

### 2. XAML数据绑定（使用ViewModelBase属性）
```xml
<UserControl>
    <Grid>
        <!-- 加载指示器 -->
        <ProgressBar IsIndeterminate="{Binding IsLoading}"
                     Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />

        <!-- 错误提示 -->
        <TextBlock Text="{Binding ErrorMessage}" Foreground="Red"
                   Visibility="{Binding HasError, Converter={StaticResource BoolToVis}}" />

        <!-- 状态栏 -->
        <TextBlock Text="{Binding StatusMessage}" />

        <!-- 数据列表 -->
        <ListBox ItemsSource="{Binding Patients}"
                 IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}" />

        <!-- 操作按钮 -->
        <Button Content="加载患者" Command="{Binding LoadPatientsCommand}"
                IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}" />
    </Grid>
</UserControl>
```

### 3. 使用SimpleMapper（对象映射）
```csharp
using LYBT.Desktop.Models.Mappers;

// DTO → ViewModel映射
var patientDto = await _patientService.GetByIdAsync(patientId);
var patientViewModel = SimpleMapper.Map<PatientDto, PatientViewModel>(patientDto);

// ViewModel → DTO映射
var consultationDto = SimpleMapper.Map<ConsultationViewModel, ConsultationCreateRequest>(viewModel);
await _consultationService.CreateAsync(consultationDto);
```

### 4. 处理API异常
```csharp
try
{
    var result = await _apiService.CallAsync();
}
catch (ApiCallException ex)
{
    // ApiCallException封装了HTTP错误信息
    Logger.LogError(ex, "API调用失败: {StatusCode} - {Message}", ex.StatusCode, ex.Message);

    // 访问ProblemDetails（如果API返回RFC 7807格式）
    if (ex.ProblemDetails != null)
    {
        Console.WriteLine($"详细错误: {ex.ProblemDetails.Detail}");
    }
}
```

## 📚 详细文档

- **完整模块文档**: [docs/reference/modules/models/](../../../../../docs/reference/modules/models/) *(待创建)*
- **架构设计**: [docs/explanation/architecture/client/models-layer-design.md](../../../../../docs/explanation/architecture/client/models-layer-design.md) *(待创建)*
- **开发指南**: [docs/how-to-guides/client/models-usage.md](../../../../../docs/how-to-guides/client/models-usage.md) *(待创建)*
- **MVVM模式**: [docs/reference/quick-reference/code-patterns.md](../../../../../docs/reference/quick-reference/code-patterns.md) - 参见"MVVM模式"章节

---

**最后更新**：2025-10-29
**维护负责**：Client端开发组
