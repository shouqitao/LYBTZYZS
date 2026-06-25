# 设计模式速查

## 架构模式

### Server: 三层架构

```
Controller (HTTP 入口)
  ├── 参数验证、权限检查
  ├── 调用 Service
  └── 构建 ApiResponse

Service (业务逻辑)
  ├── 业务规则验证
  ├── 调用 Repository
  └── 返回 Result<T>

Repository (数据访问)
  ├── EF Core 查询
  ├── 软删除处理
  └── 分页查询
```

### Desktop: MVVM + Repository

```
View (XAML)
  ├── 数据绑定 (Binding)
  └── 命令绑定 (Command)

ViewModel (CommunityToolkit.Mvvm)
  ├── 属性 ([ObservableProperty])
  ├── 命令 ([RelayCommand])
  └── 调用 Repository

Repository
   ├── 远程模式 → HttpClient → LYBT.WebAPI (IIS/Kestrel)
   └── 本地模式 → HttpClient → LocalWebAPI (嵌入式 Kestrel) → Service → LocalDB
```

---

## Repository 模式

### 标准 Repository 接口

```csharp
public interface IBaseRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, ...);
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task<bool> DeleteAsync(Guid id);        // 软删除
    Task<TEntity?> RestoreAsync(Guid id);   // 恢复
}
```

### 分页查询模板

```csharp
public async Task<PagedResult<TEntity>> GetPagedAsync(
    int page, int pageSize, string? keyword)
{
    var query = _dbContext.Set<TEntity>().AsQueryable();

    if (!string.IsNullOrEmpty(keyword))
    {
        query = query.Where(e => e.Name.Contains(keyword));
    }

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(e => e.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<TEntity>(items, totalCount, page, pageSize);
}
```

---

## Service 模式

### Result 返回模式

```csharp
public async Task<Result<PatientDetailDto>> CreateAsync(PatientInputDto dto)
{
    // 1. 业务验证
    if (await _repository.ExistsByNameAsync(dto.Name))
        return Result<PatientDetailDto>.Fail("患者已存在");

    // 2. 创建实体
    var entity = MapToEntity(dto);
    var created = await _repository.CreateAsync(entity);

    // 3. 映射 DTO 返回
    var detail = MapToDetailDto(created);
    return Result<PatientDetailDto>.Ok(detail);
}
```

### CQRS 分离 (MedicalCase)

```csharp
// Command Service: 写操作
public interface IMedicalCaseCommandService
{
    Task<MedicalCase?> SaveAsync(MedicalCaseInputDto dto, Guid doctorId, bool isAdmin);
    Task<bool> DeleteAsync(Guid id);
}

// Query Service: 读操作
public interface IMedicalCaseQueryService
{
    Task<MedicalCase?> GetByIdAsync(Guid id);
    Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync(...);
}

// State Service: 状态流转
public interface IMedicalCaseStateService
{
    Task<MedicalCase?> UpdateStatusAsync(Guid id, MedicalCaseStatus status);
    Task<MedicalCase?> CloseCaseAsync(Guid id);
    Task<MedicalCase?> CancelAsync(Guid id, Guid operatorId, bool isAdmin, string? reason);
}
```

---

## ViewModel 模式 (WPF/CommunityToolkit.Mvvm)

### 标准 ViewModel 结构

```csharp
// 正确：使用 CoreViewModelBase（Infrastructure 层提供）
public partial class PatientListViewModel : NavigableViewModelBase
{
    private readonly IPatientRepository _patientRepository;

    [ObservableProperty]
    private ObservableCollection<PatientDto> _patients = [];

    [RelayCommand]
    private async Task LoadPatientsAsync()
    {
        var result = await _patientRepository.GetPagedAsync(1, 20);
        Patients = new ObservableCollection<PatientDto>(result.Items);
    }
}
```

> 注意：不使用 Prism 的 `BindableBase`/`DelegateCommand`。ViewModel 基类定义在 `LYBT.Desktop.Infrastructure` 的 `CoreViewModelBase` 和 `NavigableViewModelBase`。

### 数据绑定模式

```xml
<!-- XAML 绑定 -->
<DataGrid ItemsSource="{Binding Patients}"
          SelectedItem="{Binding SelectedPatient}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="姓名" Binding="{Binding Name}" />
        <DataGridTextColumn Header="年龄" Binding="{Binding Age}" />
    </DataGrid.Columns>
</DataGrid>
<Button Command="{Binding RefreshCommand}" Content="刷新" />
```

---

## DI 注册模式

### Server (ASP.NET Core)

```csharp
// Module 注册
services.AddScoped<IPatientService, PatientService>();
services.AddScoped<IPatientRepository, PatientRepository>();
```

### Desktop (Prism/DryIoc)

```csharp
// Module 注册 (IModule.RegisterTypes)
containerRegistry.Register<IPatientRepository, PatientRepository>();
containerRegistry.RegisterForNavigation<PatientListView, PatientListViewModel>();
```

---

## 常用工具类

---

## Desktop 模式详解

### DelegatingHandler 模式 (BaseUrlDelegatingHandler)

每次请求时根据 `ConnectionSettings.CurrentUrl` 重写 URI，线程安全：

```csharp
public sealed class BaseUrlDelegatingHandler : DelegatingHandler
{
    private readonly IConnectionSettingsService _connectionSettings;

    public BaseUrlDelegatingHandler(IConnectionSettingsService connectionSettings)
    {
        _connectionSettings = connectionSettings;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null)
        {
            var currentUrl = _connectionSettings.CurrentUrl;
            if (Uri.TryCreate(currentUrl, UriKind.Absolute, out var baseUri))
            {
                request.RequestUri = new Uri(baseUri, request.RequestUri.PathAndQuery);
            }
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
```

Handler 链: `HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler → BaseUrlDelegatingHandler → HttpClient`

### SwitchingApiClient 代理模式

根据 URL 自动路由到 Remote (Refit) 或 Local (HttpClient) 实现：

```csharp
public sealed class SwitchingApiClient : IApiClient, IDisposable
{
    private readonly IConnectionSettingsService _connectionSettings;
    private IApiClient? _current;
    private string? _currentUrl;
    private readonly object _lock = new();

    private IApiClient Current
    {
        get
        {
            lock (_lock)
            {
                var url = _connectionSettings.CurrentUrl;
                if (_current is null || _currentUrl != url)
                {
                    var oldClient = _current as IDisposable;
                    _current = _connectionSettings.IsLocal
                        ? new HttpClientApiClient(_localHttpClientFactory(url))
                        : new RefitApiClient(_remoteHttpClientFactory(url), _refitSettings);
                    _currentUrl = url;
                    oldClient?.Dispose();
                }
                return _current;
            }
        }
    }

    // 属性委托 — Repository 层完全无感知
    public IApiClientAuth Auth => Current.Auth;
    public IApiClientPatients Patients => Current.Patients;
    // ...
}
```

### ISP 拆分接口模式 (跨模块服务)

大接口拆分为多个职责接口，按需注入：

```csharp
// 替代单一大接口
public interface IPatientService
{
    Task<PatientDetailDto?> GetByIdAsync(Guid id);
    Task<PagedResult<PatientListDto>> GetPagedAsync(int page, int pageSize);
}

// ISP 拆分后
public interface IPatientQueryService
{
    Task<PatientDetailDto?> GetByIdAsync(Guid id);
    Task<PagedResult<PatientListDto>> GetPagedAsync(int page, int pageSize);
}

public interface IPatientCommandService
{
    Task<PatientDetailDto> CreateAsync(PatientInputDto dto);
    Task<bool> UpdateAsync(Guid id, PatientInputDto dto);
    Task<bool> DeleteAsync(Guid id);
}
```

### MasterDetailControlBase 模式

Desktop 列表-详情控件的基础 ViewModel：

```csharp
public abstract partial class MasterDetailViewModelBase<TListItem, TDetail>
    : NavigableViewModelBase where TListItem : class where TDetail : class
{
    [ObservableProperty]
    private ObservableCollection<TListItem> _items = [];

    [ObservableProperty]
    private TListItem? _selectedItem;

    [ObservableProperty]
    private TDetail? _detailItem;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    protected virtual async Task LoadItemsAsync() { /* 分页加载 */ }

    [RelayCommand]
    protected virtual async Task SaveAsync() { /* 保存详情 */ }

    [RelayCommand]
    protected virtual async Task DeleteAsync() { /* 删除选中项 */ }
}
```

---

## 常用工具类

| 类 | 用途 | 位置 |
|----|------|------|
| `ApiResponse<T>` | 统一 API 响应格式 | Shared.Models |
| `Result<T>` | Service 层操作结果 | Shared.Models |
| `PagedResult<T>` | 分页查询结果 | Shared.Models |
| `BaseEntity` | 实体基类 (Id, CreatedAt, IsDeleted) | Entities |
| `SensitiveDataMasker` | 日志脱敏 | Shared.Logging |
| `ChecksumHelper` | 同步校验和计算 | Module.Sync |

---

## 常见反模式

| 反模式 | 正确做法 | 原因 |
|--------|----------|------|
| ViewModel 直接注入 DbContext | ViewModel → Repository → API/DataSource | 违反分层架构，绕过业务逻辑 |
| Controller 注入 Repository | Controller → Service → Repository | Controller 只协调，不访问数据层 |
| 子实体独立 Repository | 通过聚合根 MedicalCaseRepository 操作 | DDD 聚合根边界约束 |
| Service 返回 null 表示未找到 | throw `NotFoundException` | 统一异常处理，避免调用方遗漏 null 检查 |
| 在 Service 中使用 `HttpContext` | 通过方法参数传递 userId/isAdmin | Service 层不应依赖 HTTP 上下文 |
| Desktop 直接绑定 Entity/DTO | 绑定 Observable Model ([ObservableProperty]) | Entity/DTO 无 INotifyPropertyChanged |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-22 | v1.1 | 新增常见反模式表 |
| 2026-06-25 | v1.2 | 修正 ViewModel 示例: Prism BindableBase → CommunityToolkit.Mvvm CoreViewModelBase |
| 2026-06-25 | v1.3 | 补充 BaseUrlDelegatingHandler、SwitchingApiClient、ISP 拆分、MasterDetailControlBase 模式 |
