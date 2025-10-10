# Client 端业务模块统一设计标准

> **版本**: 2.0
> **制定日期**: 2025-01-09
> **适用范围**: Desktop WPF 客户端所有业务模块
> **关联 Issue**: #1114, #1013

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

**架构变更说明（v2.0）**：
- ❌ **移除Service层**：Desktop端不应重复Server端业务逻辑
- ✅ **ViewModel直调Repository**：简化调用链，提升性能
- ✅ **Repository返回ServiceResult**：统一异常处理与结果封装
- ✅ **异常处理下沉到ViewModelBase**：通过基类或AOP统一处理

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

**v2.0 关键变更**：
- ❌ 不再注入 `IXxxService`
- ✅ 直接注入 `IXxxRepository`
- ❌ 不再注入 `IMapper`（Repository直接返回DTO，无需映射）

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
            // v2.0: 直接调用Repository（Repository返回ServiceResult）
            var result = await _{entity}Repository.GetPagedAsync(page, pageSize, searchText);

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

**v2.0 关键变更**：
- ❌ 移除 `using LYBT.Shared.Interfaces.Services`
- ✅ 新增 `using LYBT.Desktop.{Module}.Repositories`
- ❌ 移除 `I{Entity}Service` 依赖
- ✅ 新增 `I{Entity}Repository` 依赖
- ✅ Repository直接返回 `ServiceResult<T>`，无需Service层包装

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
    HttpClient httpClient,                  // 1️⃣ HTTP客户端（已配置BaseAddress）
    ILogger<PatientRepository> logger)      // 2️⃣ 日志
{
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**v2.0 关键变更**：
- ❌ 不再注入 `IMapper`（Repository直接返回DTO）
- ❌ 不再注入 `IExceptionHandler`（返回ServiceResult，由ViewModel基类处理）
- ✅ 注入 `HttpClient`（通过IHttpClientFactory配置）

### 4.3 Repository 方法模板（统一返回ServiceResult）

```csharp
/// <summary>
/// {方法功能描述}
/// </summary>
public async Task<ServiceResult<{Entity}Dto>> {Method}Async({Request}Dto dto)
{
    try
    {
        _logger.LogInformation($"{操作描述}: {dto}");

        // 1. 构建HTTP请求
        var response = await _httpClient.PostAsJsonAsync("/api/{entity}/{action}", dto);

        // 2. 处理响应
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<{Entity}Dto>();
            return ServiceResult<{Entity}Dto>.Success(result);
        }

        // 3. 处理错误
        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError($"{Method}失败: {error}");
        return ServiceResult<{Entity}Dto>.Failure(error);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"{Method}异常");
        return ServiceResult<{Entity}Dto>.Failure($"{Method}失败: {ex.Message}");
    }
}
```

**关键设计原则**：
- ✅ **Repository直接返回ServiceResult<T>**：封装成功/失败状态
- ✅ **Repository处理HTTP调用**：包含重试、超时等逻辑
- ✅ **Repository直接返回DTO**：无需映射，Server API已返回DTO
- ❌ **Repository不处理业务验证**：业务逻辑在Server端

### 4.4 Repository 返回类型标准

| 场景 | 返回类型 | 说明 |
|------|---------|------|
| 查询单条 | `Task<ServiceResult<{Entity}Dto>>` | 返回单个实体 |
| 查询列表 | `Task<ServiceResult<PagedResult<{Entity}Dto>>>` | 分页结果 |
| 创建 | `Task<ServiceResult<{Entity}Dto>>` | 返回创建的实体 |
| 更新 | `Task<ServiceResult<{Entity}Dto>>` | 返回更新的实体 |
| 删除 | `Task<ServiceResult>` | 无返回数据 |

**v2.0 废弃规则**：
- ❌ **不再使用AutoMapper**：Repository直接从HTTP响应反序列化DTO
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

### 4.6 Repository 示例模板（v2.0）

```csharp
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace LYBT.Desktop.{Module}.Repositories
{
    /// <summary>
    /// {Entity}Repository - 数据访问层（v2.0 模块化架构）
    /// </summary>
    public interface I{Entity}Repository
    {
        Task<ServiceResult<PagedResult<{Entity}Dto>>> GetPagedAsync(int page, int pageSize, string? keyword);
        Task<ServiceResult<{Entity}Dto>> GetByIdAsync(Guid id);
        Task<ServiceResult<{Entity}Dto>> CreateAsync({Entity}CreateDto dto);
        Task<ServiceResult<{Entity}Dto>> UpdateAsync(Guid id, {Entity}UpdateDto dto);
        Task<ServiceResult> DeleteAsync(Guid id);
    }

    /// <summary>
    /// {Entity}Repository 实现
    /// </summary>
    public class {Entity}Repository : I{Entity}Repository
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<{Entity}Repository> _logger;
        private const string ApiBase = "/api/{entity}";

        public {Entity}Repository(
            HttpClient httpClient,
            ILogger<{Entity}Repository> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResult<PagedResult<{Entity}Dto>>> GetPagedAsync(
            int page, int pageSize, string? keyword)
        {
            try
            {
                // ✅ 服务端分页：关键参数通过查询字符串传递
                var url = $"{ApiBase}?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrEmpty(keyword))
                    url += $"&keyword={Uri.EscapeDataString(keyword)}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PagedResult<{Entity}Dto>>();
                    return ServiceResult<PagedResult<{Entity}Dto>>.Success(result);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"GetPagedAsync失败: {error}");
                return ServiceResult<PagedResult<{Entity}Dto>>.Failure(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPagedAsync异常");
                return ServiceResult<PagedResult<{Entity}Dto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<{Entity}Dto>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBase}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<{Entity}Dto>();
                    return ServiceResult<{Entity}Dto>.Success(result);
                }

                var error = await response.Content.ReadAsStringAsync();
                return ServiceResult<{Entity}Dto>.Failure(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetByIdAsync({id})异常");
                return ServiceResult<{Entity}Dto>.Failure($"查询失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<{Entity}Dto>> CreateAsync({Entity}CreateDto dto)
        {
            try
            {
                _logger.LogInformation($"创建{Entity}: {dto}");

                var response = await _httpClient.PostAsJsonAsync(ApiBase, dto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<{Entity}Dto>();
                    return ServiceResult<{Entity}Dto>.Success(result);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"CreateAsync失败: {error}");
                return ServiceResult<{Entity}Dto>.Failure(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAsync异常");
                return ServiceResult<{Entity}Dto>.Failure($"创建失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<{Entity}Dto>> UpdateAsync(Guid id, {Entity}UpdateDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{ApiBase}/{id}", dto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<{Entity}Dto>();
                    return ServiceResult<{Entity}Dto>.Success(result);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"UpdateAsync({id})失败: {error}");
                return ServiceResult<{Entity}Dto>.Failure(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UpdateAsync({id})异常");
                return ServiceResult<{Entity}Dto>.Failure($"更新失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{ApiBase}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult.Success();
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"DeleteAsync({id})失败: {error}");
                return ServiceResult.Failure(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DeleteAsync({id})异常");
                return ServiceResult.Failure($"删除失败: {ex.Message}");
            }
        }
    }
}
```

**v2.0 关键改进**：
- ✅ **服务端分页**：GetPagedAsync使用查询字符串传递参数，由Server端分页
- ❌ **不再使用AutoMapper**：HTTP响应直接反序列化为DTO
- ✅ **统一异常处理**：所有方法返回ServiceResult，由ViewModel基类统一处理
- ✅ **接口与实现在同一模块**：模块化架构，职责清晰

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

### 7.2 Repository 检查清单（v2.0）

- [ ] 接口定义在模块的 `Repositories/I{Entity}Repository.cs`
- [ ] 实现类在模块的 `Repositories/{Entity}Repository.cs`
- [ ] 构造函数依赖顺序符合标准（HttpClient, Logger）
- [ ] 所有方法返回 `ServiceResult<T>` 或 `ServiceResult`
- [ ] **GetPagedAsync使用服务端分页**（通过查询字符串传递page/pageSize/keyword）
- [ ] 使用 `_logger` 记录关键操作和错误
- [ ] HTTP响应直接反序列化为DTO（使用ReadFromJsonAsync）
- [ ] ❌ 不使用AutoMapper
- [ ] ❌ 不使用IExceptionHandler（由ServiceResult封装）

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
public PatientManagementViewModel(
    IPatientService patientService,  // 删除Service依赖
    ...)
{
    _patientService = patientService;
}

protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(...)
{
    var result = await _patientService.GetPagedAsync(...);
    // ...
}

// ✅ 新代码（v2.0）
public PatientManagementViewModel(
    IPatientRepository patientRepository,  // 直接注入Repository
    ...)
{
    _patientRepository = patientRepository;
}

protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(...)
{
    var result = await _patientRepository.GetPagedAsync(...);
    // ...
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

**Q2: ViewModel如何处理Repository返回的ServiceResult？**
```csharp
// ✅ ViewModelBase已内置异常处理
var result = await _repository.GetPagedAsync(...);

if (result.IsSuccess && result.Data != null)
{
    TotalCount = result.Data.TotalCount;
    return result.Data.Items;
}

// 失败时ViewModelBase会自动显示错误消息
return Enumerable.Empty<PatientDto>();
```

**Q3: 如何确保使用服务端分页？**
```csharp
// ✅ 通过查询字符串传递分页参数
var url = $"/api/patients?page={page}&pageSize={pageSize}&keyword={keyword}";
var response = await _httpClient.GetAsync(url);

// ❌ 不要调用GetAllAsync()再在客户端过滤
var allPatients = await _repository.GetAllAsync();  // 禁止
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
| 2.0 | 2025-01-09 | **重大架构变更** - 移除Service层，实现模块化架构 (Issue #1114)<br/>- ❌ 删除Desktop.Services项目<br/>- ✅ Repository下沉到各模块<br/>- ✅ 新增Desktop.Foundation/Presentation<br/>- ✅ 修复P0性能问题（服务端分页）<br/>- ❌ 废弃AutoMapper<br/>- 更新所有代码模板与检查清单 | Claude Code |
| 1.1 | 2025-01-09 | 添加 DTO 使用规范章节,引用 DTO 设计原则文档 (Issue #1094) | Claude Code |
| 1.0 | 2025-10-07 | 初始版本，制定统一设计标准 | Claude Code |

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
