# Client 端业务模块统一设计标准

> **版本**: 1.0
> **制定日期**: 2025-10-07
> **适用范围**: Desktop WPF 客户端所有业务模块
> **关联 Issue**: #1013

---

## 一、架构概览

### 1.1 分层架构

```
┌─────────────────────────────────────────┐
│           View (XAML)                   │
│     用户界面、数据绑定、样式             │
└───────────────┬─────────────────────────┘
                │ Binding
┌───────────────▼─────────────────────────┐
│         ViewModel                       │
│   UI逻辑、命令、属性、状态管理           │
└───────────────┬─────────────────────────┘
                │ 调用
┌───────────────▼─────────────────────────┐
│          Service                        │
│   业务逻辑、DTO转换、异常处理            │
└───────────────┬─────────────────────────┘
                │ 调用
┌───────────────▼─────────────────────────┐
│        Repository                       │
│     数据访问、HTTP调用                   │
└───────────────┬─────────────────────────┘
                │ HTTP
┌───────────────▼─────────────────────────┐
│         WebAPI (Server)                 │
└─────────────────────────────────────────┘
```

### 1.2 模块组织原则

- **模块 = UI层**：仅包含 ViewModels、Views、UI专用Models
- **业务逻辑集中**：统一在 `Desktop.Services/Business/`
- **数据访问集中**：统一在 `Desktop.Services/Repositories/`
- **接口统一**：使用 `Shared.Interfaces.Services`

---

## 二、目录结构标准

### 2.1 模块目录结构（强制）

```
LYBT.Desktop.{ModuleName}/
├── Models/                      ✅ UI专用模型
│   ├── {Entity}Item.cs         (列表项模型)
│   ├── {Entity}ViewState.cs    (视图状态)
│   └── {Wizard}Step.cs         (向导步骤枚举)
│
├── ViewModels/                  ✅ 视图模型
│   ├── {Entity}ManagementViewModel.cs  (列表管理)
│   ├── {Entity}DetailViewModel.cs      (详情查看)
│   ├── {Entity}CreateViewModel.cs      (创建)
│   ├── {Entity}EditViewModel.cs        (编辑)
│   └── {Action}DialogViewModel.cs      (对话框)
│
├── Views/                       ✅ XAML视图
│   ├── {Entity}ManagementView.xaml     (+ .xaml.cs)
│   ├── {Entity}DetailView.xaml         (+ .xaml.cs)
│   └── {Action}Dialog.xaml             (+ .xaml.cs)
│
├── {ModuleName}Module.cs        ✅ Prism模块注册
└── README.md                    ✅ 模块说明文档
```

### 2.2 禁止的目录（已废弃）

- ❌ **Interfaces/** - 接口统一在 `Shared.Interfaces.Services`
- ❌ **Mappings/** - AutoMapper配置集中在 `Desktop.Services/Mapping/`
- ❌ **Services/** - 业务服务统一在 `Desktop.Services/Business/`

### 2.3 Service 层目录结构

```
Desktop.Services/
├── Business/                    ✅ 业务服务实现
│   ├── AuthService.cs
│   ├── PatientService.cs
│   ├── UserService.cs
│   └── ...
│
├── Repositories/                ✅ 数据访问层
│   ├── Interfaces/
│   │   ├── IPatientRepository.cs
│   │   └── ...
│   ├── BaseApiRepository.cs
│   ├── PatientRepository.cs
│   └── ...
│
└── Mapping/                     ✅ AutoMapper配置
    ├── PatientMappingProfile.cs
    ├── UserMappingProfile.cs
    └── ...
```

---

## 三、ViewModel 设计标准

### 3.1 基类选择规则

| 场景 | 基类 | 示例 |
|------|------|------|
| 列表管理 | `UnifiedListViewModelBase<TDto>` | PatientManagementViewModel |
| 详情/单项 | `UnifiedViewModelBase` | PatientDetailViewModel |
| 对话框 | `UnifiedViewModelBase` | ConfirmDialogViewModel |

### 3.2 构造函数依赖注入（强制标准）

```csharp
/// <summary>
/// {Entity}{ViewType}ViewModel - {简要描述}
/// </summary>
public XxxViewModel(
    // 1️⃣ 核心业务服务（必需，非null）
    IXxxService xxxService,

    // 2️⃣ 基类必需依赖
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,

    // 3️⃣ 可选依赖（末尾，使用 = null）
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null,
    IMapper? mapper = null)
    : base(eventAggregator, loggerFactory, regionManager,
           sessionManager, userNotificationService)
{
    _xxxService = xxxService ?? throw new ArgumentNullException(nameof(xxxService));
    _mapper = mapper;
}
```

**依赖顺序规则**：
1. 业务服务优先（如 IPatientService）
2. 基类必需依赖（EventAggregator, LoggerFactory, RegionManager）
3. 可选依赖最后（SessionManager, NotificationService, Mapper）

### 3.3 命令命名标准

| 命令类型 | 命名规则 | 示例 |
|---------|---------|------|
| CRUD | `{Action}Command` | `AddCommand`, `EditCommand`, `DeleteCommand`, `SaveCommand` |
| 导航 | `{Direction/Target}Command` | `BackCommand`, `NextCommand`, `GotoPatientCommand` |
| 刷新 | `RefreshCommand` / `LoadDataCommand` | `RefreshCommand` |
| 搜索 | `SearchCommand` / `ClearSearchCommand` | `SearchCommand` |
| 自定义 | `{Verb}{Noun}Command` | `ExportDataCommand`, `ImportPatientsCommand` |

### 3.4 属性命名标准

| 属性类型 | 命名规则 | 示例 |
|---------|---------|------|
| 数据集合 | `Items` | `Items` (列表项) |
| 当前选中 | `SelectedItem` / `CurrentItem` | `SelectedPatient`, `CurrentUser` |
| 状态标志 | `Is{State}` | `IsLoading`, `IsBusy`, `IsReadOnly` |
| 计数 | `{Noun}Count` / `Total{Noun}` | `ItemCount`, `TotalPages` |
| UI文本 | `{Context}Text` | `PageTitle`, `StatusText`, `ErrorMessage` |

### 3.5 ViewModel 示例模板

```csharp
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.{Module}.ViewModels
{
    /// <summary>
    /// {Entity}管理视图模型 - 列表管理功能
    /// </summary>
    public class {Entity}ManagementViewModel : UnifiedListViewModelBase<{Entity}Dto>
    {
        #region 私有字段

        private readonly I{Entity}Service _{entity}Service;

        #endregion

        #region 构造函数

        public {Entity}ManagementViewModel(
            I{Entity}Service {entity}Service,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager,
                   sessionManager, userNotificationService)
        {
            _{entity}Service = {entity}Service ?? throw new ArgumentNullException(nameof({entity}Service));

            PageTitle = "{Entity}管理";
            InitializeCustomCommands();
        }

        #endregion

        #region 实现基类抽象方法

        protected override async Task<IEnumerable<{Entity}Dto>> GetItemsAsync(
            int page, int pageSize, string? searchText)
        {
            var result = await _{entity}Service.GetPagedAsync(page, pageSize, searchText);

            if (result.IsSuccess && result.Data != null)
            {
                TotalCount = result.Data.TotalCount;
                return result.Data.Items;
            }

            return Enumerable.Empty<{Entity}Dto>();
        }

        #endregion

        #region 自定义命令

        private void InitializeCustomCommands()
        {
            // 添加模块特定命令
        }

        #endregion
    }
}
```

---

## 四、Service 层设计标准

### 4.1 Service 实现位置

- **位置**: `Desktop.Services/Business/{Entity}Service.cs`
- **接口**: 实现 `Shared.Interfaces.Services.I{Entity}Service`
- **命名**: `{Entity}Service` (如 PatientService, UserService)

### 4.2 构造函数依赖（强制顺序）

```csharp
public PatientService(
    IPatientRepository repository,          // 1️⃣ Repository依赖
    ILogger<PatientService> logger,         // 2️⃣ 日志
    IExceptionHandler exceptionHandler,     // 3️⃣ 异常处理
    IMapper mapper)                         // 4️⃣ AutoMapper（强制）
{
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
    _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
}
```

### 4.3 Service 方法模板（统一）

```csharp
/// <summary>
/// {方法功能描述}
/// </summary>
public async Task<ServiceResult<{Entity}Dto>> {Method}Async({Request}Dto dto)
{
    return await _exceptionHandler.SafeExecuteAsync(async () =>
    {
        _logger.LogInformation($"{操作描述}: {dto}");

        // 1. 业务验证（如需要）
        ValidateXxx(dto);

        // 2. DTO → Entity（使用AutoMapper）
        var entity = _mapper.Map<{Entity}>(dto);

        // 3. 调用 Repository
        var result = await _repository.{Method}Async(entity);

        // 4. Entity → DTO（使用AutoMapper）
        var resultDto = _mapper.Map<{Entity}Dto>(result);

        return ServiceResult<{Entity}Dto>.Success(resultDto);

    }, nameof({Method}Async));
}
```

### 4.4 DTO 转换标准

**强制规则**：
- ✅ **所有 DTO 转换必须使用 AutoMapper**
- ❌ **禁止手动字段赋值映射**

**Mapping Profile 示例**：
```csharp
using AutoMapper;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Entities.Patients;

namespace LYBT.Desktop.Services.Mapping
{
    public class PatientMappingProfile : Profile
    {
        public PatientMappingProfile()
        {
            // Entity → Dto
            CreateMap<Patient, PatientDto>();

            // CreateDto → Entity
            CreateMap<PatientCreateDto, Patient>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // UpdateDto → Entity (partial update)
            CreateMap<PatientUpdateDto, Patient>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}
```

### 4.5 DTO 使用规范

**📚 权威参考**: 请参阅 [DTO 设计原则](../dto-design-principles.md) 获取完整的 DTO 设计规范。

**Desktop 端 DTO 使用要点**:

1. **DTO 来源**:
   - ✅ 使用 `Shared.Models.Contracts.*` 中的标准 DTO
   - ❌ 禁止在 Desktop 项目中重复定义 DTO

2. **场景选择**:
   ```csharp
   // ViewModel → Service (创建场景)
   var createDto = new PatientCreateDto { Name = "张三", ... };
   var result = await _patientService.CreateAsync(createDto);

   // ViewModel → Service (更新场景)
   var updateDto = new PatientUpdateDto { Name = "李四", ... };
   var result = await _patientService.UpdateAsync(id, updateDto);

   // Service → ViewModel (展示场景)
   var patient = result.Data; // PatientDto
   ```

3. **Repository 层数据传输**:
   - Desktop Repository 通过 HTTP 调用 Server API
   - Repository 方法直接返回 DTO,**不返回 Entity**
   - Service 层从 Repository 获取 DTO,无需 Entity → DTO 转换

4. **常见错误**:
   - ❌ 在 Desktop 端使用 Entity 类型
   - ❌ 使用 `Guid.Empty` 作为默认值
   - ❌ 混用 CreateDto/UpdateDto/Dto 场景

### 4.6 Service 示例模板

```csharp
using AutoMapper;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// {Entity}服务实现 - 统一架构标准
    /// </summary>
    public class {Entity}Service : I{Entity}Service
    {
        private readonly ILogger<{Entity}Service> _logger;
        private readonly I{Entity}Repository _repository;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly IMapper _mapper;

        public {Entity}Service(
            I{Entity}Repository repository,
            ILogger<{Entity}Service> logger,
            IExceptionHandler exceptionHandler,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ServiceResult<PagedResult<{Entity}Dto>>> GetPagedAsync(
            int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var result = await _repository.GetPagedAsync(page, pageSize, keyword);
                var dto = _mapper.Map<PagedResult<{Entity}Dto>>(result);
                return ServiceResult<PagedResult<{Entity}Dto>>.Success(dto);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<{Entity}Dto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var entity = await _repository.GetByIdAsync(id);
                var dto = _mapper.Map<{Entity}Dto>(entity);
                return ServiceResult<{Entity}Dto>.Success(dto);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<{Entity}Dto>> CreateAsync({Entity}CreateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建{Entity}: {dto.Name}");

                var entity = _mapper.Map<{Entity}>(dto);
                var created = await _repository.CreateAsync(entity);
                var resultDto = _mapper.Map<{Entity}Dto>(created);

                return ServiceResult<{Entity}Dto>.Success(resultDto);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<{Entity}Dto>> UpdateAsync(Guid id, {Entity}UpdateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var existing = await _repository.GetByIdAsync(id);
                _mapper.Map(dto, existing);  // 将 dto 映射到 existing

                var updated = await _repository.UpdateAsync(existing);
                var resultDto = _mapper.Map<{Entity}Dto>(updated);

                return ServiceResult<{Entity}Dto>.Success(resultDto);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }
    }
}
```

---

## 五、View 层设计标准

### 5.1 XAML 基础结构（强制模板）

```xml
<UserControl x:Class="LYBT.Desktop.{Module}.Views.{Entity}View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True"
             mc:Ignorable="d"
             d:DesignHeight="700" d:DesignWidth="1200">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- 标题栏 -->
            <RowDefinition Height="*" />     <!-- 内容区 -->
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <Border Grid.Row="0" Style="{StaticResource TitleBarStyle}" Padding="16">
            <Grid>
                <TextBlock Text="{Binding PageTitle}"
                           FontSize="20" FontWeight="Bold"
                           Foreground="White" />
            </Grid>
        </Border>

        <!-- 内容区 -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
            <Grid Margin="16">
                <!-- 具体内容 -->
            </Grid>
        </ScrollViewer>

        <!-- 加载遮罩（统一模式） -->
        <Grid Grid.RowSpan="2"
              Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}"
              Background="#80000000">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar Width="50" Height="50"
                             IsIndeterminate="True"
                             Margin="0,0,0,16" />
                <TextBlock Text="正在加载..."
                           Foreground="White"
                           HorizontalAlignment="Center" />
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

### 5.2 数据绑定标准

| 绑定类型 | 语法 | 示例 |
|---------|------|------|
| 命令绑定 | `Command="{Binding XxxCommand}"` | `Command="{Binding SaveCommand}"` |
| 双向绑定 | `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged` | `Text="{Binding Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"` |
| 只读绑定 | `Mode=OneWay` | `Text="{Binding StatusText, Mode=OneWay}"` |
| 可见性 | `Converter={StaticResource XxxConverter}` | `Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}"` |

### 5.3 样式和资源标准

**资源引用规则**：
- ✅ **样式**: 使用 `{StaticResource XxxStyle}`（应用级样式）
- ✅ **主题资源**: 使用 `{DynamicResource XxxBrush}`（可切换主题）
- ✅ **Converter**: 定义在 `Desktop.Infrastructure/Converters/`
- ❌ **禁止内联样式**（除非确实特殊且有注释说明）

**常用 Converter**：
- `BooleanToVisibilityConverter` - bool → Visibility
- `InverseBooleanToVisibilityConverter` - !bool → Visibility
- `NullToVisibilityConverter` - null检查 → Visibility
- `EnumToStringConverter` - 枚举 → 显示文本

### 5.4 代码后置 (Code-behind) 标准

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.{Module}.Views
{
    /// <summary>
    /// {Entity}View.xaml 的交互逻辑
    /// </summary>
    public partial class {Entity}View : UserControl
    {
        public {Entity}View()
        {
            InitializeComponent();
            // 仅初始化，不包含任何业务逻辑
            // 所有逻辑必须在 ViewModel 中
        }
    }
}
```

**强制规则**：
- ✅ 代码后置仅包含 `InitializeComponent()`
- ❌ 禁止在代码后置中编写业务逻辑
- ❌ 禁止在代码后置中访问 ViewModel

---

## 六、命名约定

### 6.1 文件命名

| 文件类型 | 命名规则 | 示例 |
|---------|---------|------|
| ViewModel | `{Entity}{ViewType}ViewModel.cs` | `PatientManagementViewModel.cs` |
| View (XAML) | `{Entity}{ViewType}View.xaml` | `PatientDetailView.xaml` |
| Model | `{Entity}{Suffix}.cs` | `PatientItem.cs`, `PatientViewState.cs` |
| Service | `{Entity}Service.cs` | `PatientService.cs` |
| Repository | `{Entity}Repository.cs` | `PatientRepository.cs` |
| Interface | `I{Name}` | `IPatientService.cs` |

### 6.2 ViewType 后缀标准

| ViewType | 用途 | 示例 |
|----------|------|------|
| Management | 列表管理 | PatientManagementViewModel |
| Detail | 详情查看 | PatientDetailViewModel |
| Create | 创建表单 | PatientCreateViewModel |
| Edit | 编辑表单 | PatientEditViewModel |
| Dialog | 对话框 | ConfirmDialogViewModel |

---

## 七、质量检查清单

### 7.1 ViewModel 检查清单

- [ ] 继承正确的基类（`UnifiedViewModelBase` 或 `UnifiedListViewModelBase<TDto>`）
- [ ] 构造函数依赖顺序符合标准
- [ ] 所有必需依赖使用 `?? throw new ArgumentNullException`
- [ ] 可选依赖使用 `= null` 默认值
- [ ] 命令命名符合标准
- [ ] 属性命名符合标准
- [ ] 使用 `async`/`await` 处理异步操作
- [ ] 使用基类的 `ShowErrorMessageAsync` 等方法显示消息
- [ ] 重写 `OnNavigatedTo` 时调用 `base.OnNavigatedTo()`

### 7.2 Service 检查清单

- [ ] 实现 `Shared.Interfaces.Services.I{Entity}Service`
- [ ] 构造函数依赖顺序符合标准
- [ ] 注入 `IMapper` 并使用（不再手动映射）
- [ ] 所有方法返回 `ServiceResult<T>` 或 `ServiceResult`
- [ ] 使用 `_exceptionHandler.SafeExecuteAsync` 包装
- [ ] 使用 `_logger` 记录关键操作
- [ ] DTO 转换使用 `_mapper.Map<T>()`

### 7.3 View 检查清单

- [ ] 使用 `prism:ViewModelLocator.AutoWireViewModel="True"`
- [ ] 标题栏 + 内容区 + 加载遮罩 三段式结构
- [ ] 命令绑定使用 `{Binding XxxCommand}`
- [ ] 数据绑定指定 `Mode` 和 `UpdateSourceTrigger`
- [ ] 使用 `{StaticResource}` 引用样式
- [ ] 使用 `{DynamicResource}` 引用主题资源
- [ ] 代码后置仅包含 `InitializeComponent()`

### 7.4 目录结构检查清单

- [ ] 无 `Interfaces/` 目录
- [ ] 无 `Mappings/` 目录
- [ ] 无 `Services/` 目录
- [ ] 有 `Models/`、`ViewModels/`、`Views/`
- [ ] 有 `{Module}Module.cs` 和 `README.md`

---

## 八、迁移指南

### 8.1 从手动映射迁移到 AutoMapper

**旧代码（手动映射）**：
```csharp
var patient = new PatientDto
{
    Id = Guid.NewGuid(),
    Name = dto.Name,
    Gender = dto.Gender,
    BirthDate = dto.BirthDate,
    // ... 10+ 行字段赋值
};
```

**新代码（AutoMapper）**：
```csharp
var patient = _mapper.Map<PatientDto>(dto);
```

**步骤**：
1. 创建对应的 MappingProfile
2. 在 Service 构造函数注入 `IMapper`
3. 替换所有手动映射为 `_mapper.Map<T>()`
4. 删除手动映射代码

### 8.2 从分散的 Mappings/ 迁移到集中配置

**旧位置**：
- `LYBT.Desktop.Auth/Mappings/MappingProfile.cs`
- `LYBT.Desktop.Herbs/Mappings/MappingProfile.cs`

**新位置**：
- `Desktop.Services/Mapping/AuthMappingProfile.cs`
- `Desktop.Services/Mapping/HerbMappingProfile.cs`

**步骤**：
1. 移动文件到 `Desktop.Services/Mapping/`
2. 重命名为 `{Entity}MappingProfile.cs`
3. 更新命名空间为 `LYBT.Desktop.Services.Mapping`
4. 删除原 Mappings/ 目录
5. 更新 DI 注册（在 ServiceRegistration.cs）

---

## 九、参考资料

- [DTO 设计原则](../dto-design-principles.md) - 本项目 DTO 设计规范
- [Server Module Design Standard](../server-module-design-standard.md) - Server 端模块设计标准
- [Prism 官方文档](https://prismlibrary.com/)
- [AutoMapper 官方文档](https://docs.automapper.org/)
- [MVVM 设计模式](https://learn.microsoft.com/zh-cn/dotnet/architecture/maui/mvvm)
- [WPF 数据绑定](https://learn.microsoft.com/zh-cn/dotnet/desktop/wpf/data/)

---

## 十、版本历史

| 版本 | 日期 | 修订内容 | 作者 |
|------|------|---------|------|
| 1.1 | 2025-01-09 | 添加 DTO 使用规范章节,引用 DTO 设计原则文档 (Issue #1094) | Claude Code |
| 1.0 | 2025-10-07 | 初始版本，制定统一设计标准 | Claude Code |

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
