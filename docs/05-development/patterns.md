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

ViewModel (Prism BindableBase)
  ├── 属性 (SetProperty)
  ├── 命令 (DelegateCommand)
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

## ViewModel 模式 (WPF/Prism)

### 标准 ViewModel 结构

```csharp
public class PatientListViewModel : BindableBase, INavigationAware
{
    private readonly IPatientRepository _repository;

    // 属性
    private ObservableCollection<PatientDto> _patients;
    public ObservableCollection<PatientDto> Patients
    {
        get => _patients;
        set => SetProperty(ref _patients, value);
    }

    // 命令
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand<PatientDto> EditCommand { get; }

    // 构造函数 (DI)
    public PatientListViewModel(IPatientRepository repository)
    {
        _repository = repository;
        RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
    }

    // 导航回调
    public void OnNavigatedTo(NavigationContext context)
    {
        LoadDataAsync().FireAndForget();
    }
}
```

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
| Desktop 直接绑定 Entity/DTO | 绑定 Observable Model (BindableBase) | Entity/DTO 无 INotifyPropertyChanged |

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-22 | v1.1 | 新增常见反模式表 |
