# 设计模式速查

> Server 端通用模式（三层架构、Repository、Service、CQRS）见 [03-server.md](../03-architecture/03-server.md)。本文档仅记录 Desktop 独有和跨端补充模式。

---

## Desktop: MVVM + Repository

```
View (XAML) → ViewModel (CommunityToolkit.Mvvm) → Repository
  ├── 远程模式 → HttpClient → LYBT.WebAPI
  └── 本地模式 → HttpClient → LocalWebAPI → Service → LocalDB
```

---

## ViewModel 模式

```csharp
// 使用 CoreViewModelBase（Infrastructure 层），不用 Prism BindableBase
public partial class PatientListViewModel : NavigableViewModelBase
{
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

```xml
<DataGrid ItemsSource="{Binding Patients}" SelectedItem="{Binding SelectedPatient}" />
<Button Command="{Binding RefreshCommand}" Content="刷新" />
```

---

## DI 注册模式

**Server** (ASP.NET Core): `services.AddScoped<IPatientService, PatientService>();`

**Desktop** (Prism/DryIoc): `containerRegistry.RegisterForNavigation<PatientListView, PatientListViewModel>();`

---

## Desktop 模式详解

### DelegatingHandler (BaseUrlDelegatingHandler)

每次请求根据 `ConnectionSettings.CurrentUrl` 重写 URI，线程安全：

```csharp
public sealed class BaseUrlDelegatingHandler : DelegatingHandler
{
    private readonly IConnectionSettingsService _connectionSettings;
    public BaseUrlDelegatingHandler(IConnectionSettingsService cs) => _connectionSettings = cs;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (request.RequestUri != null &&
            Uri.TryCreate(_connectionSettings.CurrentUrl, UriKind.Absolute, out var baseUri))
            request.RequestUri = new Uri(baseUri, request.RequestUri.PathAndQuery);
        return await base.SendAsync(request, ct);
    }
}
```

Handler 链: `HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler → BaseUrlDelegatingHandler → HttpClient`

### SwitchingApiClient

根据 URL 自动路由到 Remote (Refit) 或 Local (HttpClient)，Repository 层无感知：

```csharp
public sealed class SwitchingApiClient : IApiClient
{
    private IApiClient? _current;
    private string? _currentUrl;
    private readonly object _lock = new();

    private IApiClient Current { get { lock (_lock) {
        var url = _connectionSettings.CurrentUrl;
        if (_current is null || _currentUrl != url) { /* 切换实现 */ }
        return _current;
    }}}
}
```

### ISP 拆分接口

大接口拆分为 `I{Entity}QueryService` + `I{Entity}CommandService`，按需注入。

### MasterDetailControlBase

Desktop 列表-详情控件基类：`MasterDetailViewModelBase<TListItem, TDetail> : NavigableViewModelBase`，提供 Items/SelectedItem/DetailItem/IsLoading 属性 + LoadItems/Save/Delete 命令。

---

## 常用工具类

| 类 | 用途 | 位置 |
|----|------|------|
| `ApiResponse<T>` | 统一 API 响应格式 | Shared.Models |
| `ServiceResult<T>` | Service 层操作结果 | Shared.Models |
| `PagedResult<T>` | 分页查询结果 | Shared.Models |
| `BaseEntity` | 实体基类 (Id, CreatedAt, IsDeleted) | Entities |
| `SensitiveDataMasker` | 日志脱敏 | Shared.Logging |
| `ChecksumHelper` | 同步校验和计算 | Module.Sync |

---

## 常见反模式

| 反模式 | 正确做法 | 原因 |
|--------|----------|------|
| ViewModel 直接注入 DbContext | ViewModel → Repository → API/DataSource | 违反分层架构 |
| Controller 注入 Repository | Controller → Service → Repository | Controller 只协调 |
| 子实体独立 Repository | 通过聚合根操作 | DDD 聚合根边界 |
| Service 返回 null | throw `NotFoundException` | 统一异常处理 |
| Service 使用 `HttpContext` | 通过方法参数传递 userId | 不依赖 HTTP 上下文 |
| Desktop 绑定 Entity/DTO | 绑定 [ObservableProperty] Model | Entity 无 INotifyPropertyChanged |
