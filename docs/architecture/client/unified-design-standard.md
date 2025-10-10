# Client 端业务模块统一设计标准

> **版本**: 2.1
> **制定日期**: 2025-01-11
> **适用范围**: Desktop WPF 客户端所有业务模块
> **关联 Issue**: #1114, #1119, #1118, #1013

---

## 一、架构概览

### 1.1 分层架构（模块化架构 v2.0）

```
┌─────────────────────────────────────────┐
│           View (XAML)                   │
│     用户界面、数据绑定、样式             │
└───────────────┬─────────────────────────┘
                │ Binding
┌───────────────▼─────────────────────────┐
│         ViewModel                       │
│   UI逻辑、命令、属性、状态管理           │
│   异常处理（ViewModelBase）              │
└───────────────┬─────────────────────────┘
                │ 直接调用（无Service层）
┌───────────────▼─────────────────────────┐
│        Repository                       │
│   数据访问、HTTP调用、ServiceResult封装   │
└───────────────┬─────────────────────────┘
                │ HTTP
┌───────────────▼─────────────────────────┐
│         WebAPI (Server)                 │
│   业务逻辑、数据持久化                   │
└─────────────────────────────────────────┘
```

**架构变更说明（v2.1）**：
- ❌ **移除Service层**：Desktop端不应重复Server端业务逻辑
- ✅ **ViewModel直调Repository**：简化调用链，提升性能
- ✅ **Repository返回裸类型**（v2.1修订）：直接返回DTO或PagedResult，异常通过抛出处理
- ✅ **异常处理在UnifiedViewModelBase**：基类统一捕获Repository异常

### 1.2 模块组织原则（模块化架构）

- **模块 = 垂直切片**：每个模块包含 Models、ViewModels、Views、Repositories
- **职责独立**：每个模块拥有独立的数据访问层（Repositories）
- **水平分层**：技术基础设施（Foundation）、UI基础设施（Presentation）集中管理
- **接口统一**：使用 `Shared.Interfaces.Repositories`

---

## 二、目录结构标准（模块化架构 v2.0）

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
├── Repositories/                🆕 模块独立数据访问层
│   ├── I{Entity}Repository.cs  (Repository接口)
│   └── {Entity}Repository.cs   (Repository实现)
│
├── {ModuleName}Module.cs        ✅ Prism模块注册
└── README.md                    ✅ 模块说明文档
```

**v2.0 关键变更**：
- 🆕 **Repositories/** 目录：每个模块拥有独立的数据访问层
- ❌ **Services/** 目录：已废弃，不再使用Service层

### 2.2 禁止的目录（已废弃）

- ❌ **Interfaces/** - 接口统一在模块的 `Repositories/` 目录
- ❌ **Mappings/** - AutoMapper配置已废弃（Repository直接返回DTO）
- ❌ **Services/** - Service层已移除

### 2.3 Core 层目录结构

```
Desktop/Core/
├── Desktop.Foundation/          🆕 技术基础设施
│   ├── Caching/
│   ├── Configuration/
│   ├── Diagnostics/
│   ├── ErrorHandling/
│   ├── Http/
│   ├── Performance/
│   ├── Security/
│   ├── Session/
│   ├── Settings/
│   ├── HealthCheck/
│   ├── Modules/
│   ├── Handlers/
│   └── Extensions/
│
├── Desktop.Presentation/        🆕 UI基础设施
│   ├── Navigation/
│   ├── Notifications/
│   ├── Theming/
│   ├── UserExperience/
│   └── Print/
│
├── Desktop.Infrastructure/      ✅ 保留（通用接口与基类）
└── Desktop.Models/              ✅ 保留（共享模型）
```

**说明**：
- `Desktop.Services` 项目已删除
- 技术基础设施迁移至 `Desktop.Foundation`
- UI基础设施迁移至 `Desktop.Presentation`

---

## 三、ViewModel 设计标准

### 3.1 基类选择规则

| 场景 | 基类 | 示例 |
|------|------|------|
| 列表管理 | `UnifiedListViewModelBase<TDto>` | PatientManagementViewModel |
| 详情/单项 | `UnifiedViewModelBase` | PatientDetailViewModel |
| 对话框 | `UnifiedViewModelBase` | ConfirmDialogViewModel |

### 3.2 构造函数依赖注入（强制标准，v2.0）

```csharp
/// <summary>
/// {Entity}{ViewType}ViewModel - {简要描述}
/// </summary>
public XxxViewModel(
    // 1️⃣ Repository依赖（必需，非null）
    IXxxRepository xxxRepository,

    // 2️⃣ 基类必需依赖
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,

    // 3️⃣ 可选依赖（末尾，使用 = null）
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
    : base(eventAggregator, loggerFactory, regionManager,
           sessionManager, userNotificationService)
{
    _xxxRepository = xxxRepository ?? throw new ArgumentNullException(nameof(xxxRepository));
}
```

**依赖顺序规则（v2.0）**：
1. Repository依赖优先（如 IPatientRepository）
2. 基类必需依赖（EventAggregator, LoggerFactory, RegionManager）
3. 可选依赖最后（SessionManager, NotificationService）

**v2.1 关键变更**：
- ❌ 不再注入 `IXxxService`（已废弃Server Service依赖）
- ✅ 直接注入 `IXxxRepository`（模块内数据访问层）
- ❌ 不再注入 `IMapper`（Repository直接返回DTO，无需映射）
- ⚠️ **重要**：禁止使用 `LYBT.Shared.Interfaces.Services.*` 命名空间（会导致DI容器解析失败）

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

### 3.5 ViewModel 示例模板（v2.0）

```csharp
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.{Module}.Repositories;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.{Module}.ViewModels
{
    /// <summary>
    /// {Entity}管理视图模型 - 列表管理功能（v2.0 无Service层）
    /// </summary>
    public class {Entity}ManagementViewModel : UnifiedListViewModelBase<{Entity}Dto>
    {
        #region 私有字段

        private readonly I{Entity}Repository _{entity}Repository;

        #endregion

        #region 构造函数

        public {Entity}ManagementViewModel(
            I{Entity}Repository {entity}Repository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager,
                   sessionManager, userNotificationService)
        {
            _{entity}Repository = {entity}Repository ?? throw new ArgumentNullException(nameof({entity}Repository));

            PageTitle = "{Entity}管理";
            InitializeCustomCommands();
        }

        #endregion

        #region 实现基类抽象方法

        protected override async Task<IEnumerable<{Entity}Dto>> GetItemsAsync(
            int page, int pageSize, string? searchText)
        {
            // v2.1: Repository返回裸类型，异常由UnifiedViewModelBase捕获
            var result = await _{entity}Repository.GetPagedAsync(page, pageSize, searchText);

            if (result != null && result.Items != null)
            {
                TotalCount = result.TotalCount;
                return result.Items;
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

**v2.1 关键变更**：
- ❌ 移除 `using LYBT.Shared.Interfaces.Services`（会导致DI解析失败）
- ✅ 新增 `using LYBT.Desktop.{Module}.Repositories`（模块内Repository）
- ❌ 移除 `I{Entity}Service` 依赖（已废弃Server Service层）
- ✅ 新增 `I{Entity}Repository` 依赖（模块数据访问层）
- ✅ Repository返回裸类型（`PagedResult<T>`、`T`），异常通过抛出处理

---

## 四、Repository 层设计标准（v2.0）

### 4.1 Repository 实现位置

- **位置**: `Desktop.{Module}/Repositories/{Entity}Repository.cs`
- **接口**: `Desktop.{Module}/Repositories/I{Entity}Repository.cs`
- **命名**: `{Entity}Repository` (如 PatientRepository, UserRepository)
- **原则**: 每个模块拥有独立的Repository，不再集中管理

### 4.2 构造函数依赖（强制顺序）

```csharp
public PatientRepository(
    IApiClientManager apiClientManager,     // 1️⃣ Foundation层的统一API客户端管理器
    ILogger<PatientRepository> logger)      // 2️⃣ 日志
{
    _apiClient = apiClientManager ?? throw new ArgumentNullException(nameof(apiClientManager));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**v2.1 关键变更**：
- ❌ 不再注入 `IMapper`（Repository直接返回DTO）
- ❌ 不再注入 `IExceptionHandler`（异常直接抛出，由ViewModel基类捕获）
- ✅ 注入 `IApiClientManager`（Foundation层统一HTTP客户端，替代直接注入HttpClient）

### 4.3 Repository 方法模板（v2.1修订：返回裸类型）

```csharp
/// <summary>
/// {方法功能描述}
/// </summary>
public async Task<{Entity}Dto> {Method}Async({Request}Dto dto)
{
    _logger.LogInformation($"{操作描述}: {dto}");

    // 1. 调用 Foundation 层的 ApiClient
    var result = await _apiClient.PostAsync<{Entity}Dto>("/api/{entity}/{action}", dto);

    // 2. 直接返回结果（异常由ApiClient抛出，UnifiedViewModelBase捕获）
    return result;
}
```

**v2.1 关键设计原则**：
- ✅ **Repository返回裸类型**：直接返回 `T` 或 `PagedResult<T>`，不封装 `ServiceResult`
- ✅ **异常向上抛出**：不捕获异常，由 UnifiedViewModelBase 统一处理
- ✅ **调用 Foundation 层 ApiClient**：统一的HTTP封装，包含重试、超时等逻辑
- ✅ **Repository直接返回DTO**：无需映射，Server API已返回DTO
- ❌ **Repository不处理业务验证**：业务逻辑在Server端

### 4.4 Repository 返回类型标准（v2.1）

| 场景 | 返回类型 | 说明 |
|------|---------|------|
| 查询单条 | `Task<{Entity}Dto>` | 返回单个实体（裸类型） |
| 查询列表 | `Task<PagedResult<{Entity}Dto>>` | 分页结果（裸类型） |
| 创建 | `Task<{Entity}Dto>` | 返回创建的实体（裸类型） |
| 更新 | `Task<{Entity}Dto>` | 返回更新的实体（裸类型） |
| 删除 | `Task` | 无返回数据（删除成功或抛异常） |

**v2.1 关键变更**：
- ✅ **Repository返回裸类型**：不再封装 `ServiceResult<T>`，直接返回 DTO
- ✅ **错误处理**：异常向上抛出，由 UnifiedViewModelBase 统一捕获
- ❌ **不再使用AutoMapper**：Repository直接从ApiClient获取DTO
- ❌ **不再返回Entity**：Desktop端不使用Entity类型
- ❌ **不再手动映射字段**：Server API已返回标准DTO

### 4.5 DTO 使用规范

**📚 权威参考**: 请参阅 [DTO 设计原则](../dto-design-principles.md) 获取完整的 DTO 设计规范。

**Desktop 端 DTO 使用要点（v2.0）**:

1. **DTO 来源**:
   - ✅ 使用 `Shared.Models.Contracts.*` 中的标准 DTO
   - ❌ 禁止在 Desktop 项目中重复定义 DTO

2. **场景选择**:
   ```csharp
   // ViewModel → Repository (创建场景)
   var createDto = new PatientCreateDto { Name = "张三", ... };
   var result = await _patientRepository.CreateAsync(createDto);

   // ViewModel → Repository (更新场景)
   var updateDto = new PatientUpdateDto { Name = "李四", ... };
   var result = await _patientRepository.UpdateAsync(id, updateDto);

   // Repository → ViewModel (展示场景)
   var patient = result.Data; // PatientDto
   ```

3. **Repository 层数据传输**:
   - Desktop Repository 通过 HTTP 调用 Server API
   - Repository 方法直接返回 DTO（从HTTP响应反序列化）
   - **无需DTO映射**：Server API已返回标准DTO格式

4. **常见错误**:
   - ❌ 在 Desktop 端使用 Entity 类型
   - ❌ 使用 `Guid.Empty` 作为默认值
   - ❌ 混用 CreateDto/UpdateDto/Dto 场景
   - ❌ 在Repository中使用AutoMapper（已废弃）

### 4.6 Repository 示例模板（v2.1）

```csharp
using LYBT.Desktop.Foundation.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.{Module}.Repositories
{
    /// <summary>
    /// {Entity}Repository - 数据访问层（v2.1 模块化架构，返回裸类型）
    /// </summary>
    public interface I{Entity}Repository
    {
        Task<PagedResult<{Entity}Dto>> GetPagedAsync(int pageIndex, int pageSize, string? keyword = null);
        Task<{Entity}Dto> GetByIdAsync(Guid id);
        Task<{Entity}Dto> CreateAsync({Entity}CreateDto dto);
        Task<{Entity}Dto> UpdateAsync({Entity}UpdateDto dto);  // ⚠️ dto.Id 在内部赋值
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// {Entity}Repository 实现（v2.1 返回裸类型）
    /// </summary>
    public class {Entity}Repository : I{Entity}Repository
    {
        private readonly IApiClientManager _apiClient;
        private readonly ILogger<{Entity}Repository> _logger;
        private const string ApiBase = "/api/{entity}";

        public {Entity}Repository(
            IApiClientManager apiClientManager,
            ILogger<{Entity}Repository> logger)
        {
            _apiClient = apiClientManager ?? throw new ArgumentNullException(nameof(apiClientManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<{Entity}Dto>> GetPagedAsync(
            int pageIndex, int pageSize, string? keyword = null)
        {
            _logger.LogInformation("查询{Entity}列表: pageIndex={PageIndex}, pageSize={PageSize}, keyword={Keyword}",
                pageIndex, pageSize, keyword);

            // ✅ 服务端分页：参数通过URL查询字符串传递给Server API
            var query = new PagedQueryBaseDto
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Keyword = keyword
            };

            // ApiClient 统一处理HTTP请求，异常向上抛出
            return await _apiClient.GetPagedAsync<{Entity}Dto>(ApiBase, query);
        }

        public async Task<{Entity}Dto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("查询{Entity}详情: id={Id}", id);

            // ApiClient 统一处理HTTP GET请求
            return await _apiClient.GetAsync<{Entity}Dto>($"{ApiBase}/{id}");
        }

        public async Task<{Entity}Dto> CreateAsync({Entity}CreateDto dto)
        {
            _logger.LogInformation("创建{Entity}: {@Dto}", dto);

            // ApiClient 统一处理HTTP POST请求
            return await _apiClient.PostAsync<{Entity}Dto>(ApiBase, dto);
        }

        public async Task<{Entity}Dto> UpdateAsync({Entity}UpdateDto dto)
        {
            _logger.LogInformation("更新{Entity}: {@Dto}", dto);

            // ⚠️ 注意：UpdateDto 需要包含 Id 属性
            // ApiClient 统一处理HTTP PUT请求
            return await _apiClient.PutAsync<{Entity}Dto>($"{ApiBase}/{dto.Id}", dto);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("删除{Entity}: id={Id}", id);

            // ApiClient 统一处理HTTP DELETE请求（无返回值）
            await _apiClient.DeleteAsync($"{ApiBase}/{id}");
        }
    }
}
```

**v2.1 关键改进**：
- ✅ **服务端分页**：GetPagedAsync 通过 ApiClient 传递查询参数，由Server端分页
- ✅ **统一API客户端**：使用 Foundation 层的 `IApiClientManager`，统一HTTP调用
- ✅ **返回裸类型**：直接返回 DTO，异常向上抛出
- ✅ **简化错误处理**：不再使用 try-catch 和 ServiceResult，由 UnifiedViewModelBase 统一捕获异常
- ✅ **模块化架构**：接口与实现在同一模块，职责清晰
- ❌ **不再使用AutoMapper**：ApiClient 直接返回DTO

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

### 7.2 Repository 检查清单（v2.1）

- [ ] 接口定义在模块的 `Repositories/I{Entity}Repository.cs`
- [ ] 实现类在模块的 `Repositories/{Entity}Repository.cs`
- [ ] 构造函数依赖顺序符合标准（`IApiClientManager`, `ILogger`）
- [ ] ✅ **所有方法返回裸类型**（如 `Task<T>`, `Task<PagedResult<T>>`, `Task`）
- [ ] ✅ **GetPagedAsync使用服务端分页**（通过ApiClient传递PagedQueryBaseDto）
- [ ] ✅ **UpdateAsync方法签名**：`Task<{Entity}Dto> UpdateAsync({Entity}UpdateDto dto)`，dto包含Id
- [ ] 使用 `_logger` 记录关键操作（使用结构化日志）
- [ ] 调用 Foundation 层的 `IApiClientManager`（GetAsync, PostAsync, PutAsync, DeleteAsync）
- [ ] ❌ 不使用AutoMapper
- [ ] ❌ 不使用 try-catch 封装（异常向上抛出，由ViewModel基类捕获）

### 7.3 View 检查清单

- [ ] 使用 `prism:ViewModelLocator.AutoWireViewModel="True"`
- [ ] 标题栏 + 内容区 + 加载遮罩 三段式结构
- [ ] 命令绑定使用 `{Binding XxxCommand}`
- [ ] 数据绑定指定 `Mode` 和 `UpdateSourceTrigger`
- [ ] 使用 `{StaticResource}` 引用样式
- [ ] 使用 `{DynamicResource}` 引用主题资源
- [ ] 代码后置仅包含 `InitializeComponent()`

### 7.4 目录结构检查清单（v2.0）

- [ ] ✅ 有 `Models/`、`ViewModels/`、`Views/`
- [ ] ✅ 有 `Repositories/`（包含接口和实现）
- [ ] ✅ 有 `{Module}Module.cs` 和 `README.md`
- [ ] ❌ 无 `Interfaces/` 目录（接口在Repositories/内）
- [ ] ❌ 无 `Mappings/` 目录（已废弃）
- [ ] ❌ 无 `Services/` 目录（已废弃）

---

## 八、迁移指南（v1.0 → v2.0）

### 8.1 从Service层迁移到Repository层

**旧架构（v1.0）**：
```
ViewModel → Service → Repository → WebAPI
```

**新架构（v2.0）**：
```
ViewModel → Repository → WebAPI
```

**迁移步骤**：

#### Step 1：创建模块Repository目录
```bash
# 在模块内创建Repositories目录
mkdir src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories
```

#### Step 2：迁移Repository接口和实现
```csharp
// 旧位置: Desktop.Services/Repositories/Interfaces/IPatientRepository.cs
// 新位置: Desktop.Patients/Repositories/IPatientRepository.cs

namespace LYBT.Desktop.Patients.Repositories
{
    public interface IPatientRepository
    {
        // ✅ 返回ServiceResult（而非原始DTO）
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page, int pageSize, string? keyword);
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
        Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}
```

#### Step 3：更新ViewModel依赖
```csharp
// ❌ 旧代码（v1.0）
using LYBT.Shared.Interfaces.Services;  // ❌ 会导致DI解析失败

public PatientManagementViewModel(
    IPatientService patientService,  // 删除Service依赖
    ...)
{
    _patientService = patientService;
}

protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(...)
{
    var result = await _patientService.GetPagedAsync(...);
    if (result.IsSuccess && result.Data != null)  // ❌ 旧的ServiceResult模式
    {
        return result.Data.Items;
    }
}

// ✅ 新代码（v2.1）
using LYBT.Desktop.Patients.Repositories;  // ✅ 模块内Repository

public PatientManagementViewModel(
    IPatientRepository patientRepository,  // 直接注入Repository
    ...)
{
    _patientRepository = patientRepository;
}

protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(...)
{
    var result = await _patientRepository.GetPagedAsync(...);
    if (result != null && result.Items != null)  // ✅ 直接检查裸类型
    {
        return result.Items;
    }
}
```

#### Step 4：修复P0性能问题（客户端分页→服务端分页）
```csharp
// ❌ 旧代码（PatientService.GetPagedAsync - 客户端分页）
public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(...)
{
    var allPatients = await _repository.GetAllAsync();  // ❌ 获取全部10,000条
    allPatients = allPatients.Where(...).ToList();      // 客户端过滤
    var items = allPatients.Skip(...).Take(...);        // 客户端分页
    // ...
}

// ✅ 新代码（Repository.GetPagedAsync - 服务端分页）
public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
    int page, int pageSize, string? keyword)
{
    // ✅ 参数通过查询字符串传递给Server API
    var url = $"/api/patients?page={page}&pageSize={pageSize}";
    if (!string.IsNullOrEmpty(keyword))
        url += $"&keyword={Uri.EscapeDataString(keyword)}";

    var response = await _httpClient.GetAsync(url);
    // Server端分页，仅返回20条
}
```

#### Step 5：删除废弃代码
- 删除 `Desktop.Services/Business/{Entity}Service.cs`
- 删除 `Desktop.Services/Repositories/` 目录
- 删除 `Desktop.Services/Mapping/` 目录
- 最终删除整个 `Desktop.Services` 项目

### 8.2 迁移清单（按模块）

| 模块 | 旧Service位置 | 新Repository位置 | P0修复 |
|------|-------------|----------------|--------|
| Patients | Desktop.Services/Business/PatientService.cs | Desktop.Patients/Repositories/PatientRepository.cs | ✅ 修复GetPagedAsync客户端分页 |
| Users | Desktop.Services/Business/UserService.cs | Desktop.Users/Repositories/UserRepository.cs | ✅ 已正确（参考实现） |
| MedicalCase | Desktop.Services/Business/MedicalCaseService.cs | Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs | - |
| Consultation | Desktop.Services/Business/ConsultationService.cs | Desktop.Consultation/Repositories/ConsultationRepository.cs | - |
| Prescriptions | Desktop.Services/Business/PrescriptionService.cs | Desktop.Prescriptions/Repositories/PrescriptionRepository.cs | - |
| Herbs | Desktop.Services/Business/HerbService.cs | Desktop.Herbs/Repositories/HerbRepository.cs | - |
| Formula | Desktop.Services/Business/FormulaService.cs | Desktop.Formula/Repositories/FormulaRepository.cs | - |
| Auth | Desktop.Services/Business/AuthService.cs | Desktop.Auth/Repositories/AuthRepository.cs | - |

### 8.3 常见问题与解决方案

**Q1: Repository如何处理异常？**
```csharp
// ✅ 使用ServiceResult封装异常
try
{
    var response = await _httpClient.GetAsync(...);
    // ...
    return ServiceResult<T>.Success(result);
}
catch (Exception ex)
{
    _logger.LogError(ex, "操作失败");
    return ServiceResult<T>.Failure($"操作失败: {ex.Message}");
}
```

**Q2: ViewModel如何处理Repository返回的裸类型？（v2.1修订）**
```csharp
// ✅ Repository 返回裸类型，异常由 UnifiedViewModelBase 自动捕获
var result = await _repository.GetPagedAsync(...);

if (result != null && result.Items != null)  // ✅ 直接检查null
{
    TotalCount = result.TotalCount;
    return result.Items;
}

// null 时返回空集合（UnifiedViewModelBase会自动记录警告）
return Enumerable.Empty<PatientDto>();
```

**Q3: 如何确保使用服务端分页？（v2.1修订）**
```csharp
// ✅ 使用 Foundation 层的 ApiClient，传递PagedQueryBaseDto
var query = new PagedQueryBaseDto
{
    PageIndex = pageIndex,
    PageSize = pageSize,
    Keyword = keyword
};
var result = await _apiClient.GetPagedAsync<PatientDto>("/api/patients", query);

// ❌ 不要调用GetAllAsync()再在客户端过滤
var allPatients = await _repository.GetAllAsync();  // 禁止！会导致性能问题
```

**Q4: UpdateAsync 方法为何不再传递 id 参数？（v2.1新增）**
```csharp
// ❌ 旧代码（v2.0）
Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);

// 调用时：
var result = await _repository.UpdateAsync(patient.Id, updateDto);

// ✅ 新代码（v2.1）
Task<PatientDto> UpdateAsync(PatientUpdateDto dto);  // dto.Id 已包含

// 调用时：
updateDto.Id = patient.Id;  // ViewModel 中赋值
var updated = await _repository.UpdateAsync(updateDto);

// 原因：统一模式，避免参数冗余（UpdateDto 本身就应该包含 Id）
```

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
| 2.1 | 2025-01-11 | **架构实现修订** - 基于 Issue #1119 Phase 1-4 实际迁移经验修订（Epic #1119）<br/>- ✅ **Repository 返回裸类型**（非 ServiceResult）<br/>- ✅ **UpdateAsync 方法签名调整**（dto 包含 Id，无需额外参数）<br/>- ✅ **IApiClientManager 替代 HttpClient**（Foundation 层统一API客户端）<br/>- ✅ **异常处理模式**：Repository 抛出异常，UnifiedViewModelBase 捕获<br/>- ✅ 更新所有代码示例、检查清单、迁移指南<br/>- ⚠️ 强调禁止使用 `LYBT.Shared.Interfaces.Services.*`（DI 解析失败） | Claude Code |
| 2.0 | 2025-01-09 | **重大架构变更** - 移除Service层，实现模块化架构 (Issue #1114)<br/>- ❌ 删除Desktop.Services项目<br/>- ✅ Repository下沉到各模块<br/>- ✅ 新增Desktop.Foundation/Presentation<br/>- ✅ 修复P0性能问题（服务端分页）<br/>- ❌ 废弃AutoMapper<br/>- 更新所有代码模板与检查清单 | Claude Code |
| 1.1 | 2025-01-09 | 添加 DTO 使用规范章节,引用 DTO 设计原则文档 (Issue #1094) | Claude Code |
| 1.0 | 2025-10-07 | 初始版本，制定统一设计标准 | Claude Code |

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
