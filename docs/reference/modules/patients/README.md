# Patients 模块 - 患者档案管理

## 📦 模块定位

**Server端定位**:
- **层级**:Server端业务模块
- **职责**:患者档案完整生命周期管理,提供基本信息维护、就诊历史追踪、健康信息记录、手机号唯一性验证、Excel导入导出等功能
- **架构模式**:标准三层架构（Controller → Service → Repository）
- **数据源地位**:作为医疗案例系统的基础数据源,患者档案是所有诊疗流程的起点

**Client端定位**:
- **层级**:Client端业务模块
- **职责**:患者档案UI管理,支持患者选择、快速创建、导入向导、未完成病案处理、待诊队列管理
- **架构模式**:MVVM架构 + Prism模块化
- **流程入口**:作为诊疗流程的入口模块,医生日常工作的起点,承载复杂的患者选择逻辑和待诊队列管理

---

## 🎯 功能概述

### Server端核心能力

1. **患者档案管理**:CRUD操作,支持姓名/手机号搜索,分页查询
2. **就诊历史追踪**:关联MedicalCase/Consultation/Prescriptions,完整就诊记录
3. **手机号唯一性验证**:创建/更新时防重复,支持排除自身
4. **Excel批量导入**:支持批量导入患者档案,逐行验证,错误汇总
5. **患者统计分析**:总患者数、活跃患者数、平均年龄、性别分布
6. **数据完整性保障**:FluentValidation验证器（姓名必填、手机号格式、年龄范围）

### Client端核心能力

1. **患者选择与分页**:支持20条/页分页查询、搜索（防抖500ms）、双击快速选择
2. **快速创建患者**:通过QuickCreatePatientDialog快速创建并立即开始看诊
3. **未完成病案检查**:自动检查未完成病案,提供三种处理方式（继续看诊/关闭旧案新建/仅关闭）
4. **待诊队列管理**:展示所有待诊患者（InProgress状态病案）,支持快速切换
5. **批量导入向导**:通过PatientImportWizardView支持Excel批量导入
6. **Prism事件驱动**:发布PatientSelectedEvent通知其他模块（MedicalCase/Consultation/Prescriptions）

---

## 🏗️ 模块架构

### Server端架构图

```
┌─────────────────────────────────────────────────────────────┐
│                  LYBT.Module.Patients                        │
│                   (患者档案管理模块)                          │
└─────────────────────────────────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
  ┌──────▼──────┐    ┌──────▼──────┐    ┌──────▼──────┐
  │  Service层   │    │ Repository层│    │ Validators  │
  │  (9个方法)   │    │  (9+2辅助)  │    │  (2个)      │
  └──────┬──────┘    └──────┬──────┘    └─────────────┘
         │                   │
         └───────────────────┤
                             │
            ┌────────────────▼────────────────┐
            │      LYBT.Infrastructure        │
            │  - AppDbContext                 │
            │  - BaseRepository<T>            │
            └────────────────┬────────────────┘
                             │
            ┌────────────────▼────────────────┐
            │         数据库层                 │
            │  - Patients表                   │
            │  - MedicalCases表(关联)         │
            └─────────────────────────────────┘
```

**核心组件**:
- **PatientService** (9方法):GetPagedAsync、GetByIdAsync、CreateAsync、UpdateAsync、SearchAsync、DeleteAsync、ImportFromExcelAsync、GenerateImportTemplate
- **PatientRepository** (9方法 + 2辅助类):GetByNameAsync、GetPatientWithVisitsAsync、GetPatientSummariesAsync、SearchPatientsAsync、GetPatientsByIdsAsync、PhoneNumberExistsAsync、GetStatisticsAsync、UpdateLastVisitDateAsync
- **Validators** (2个):PatientCreateDtoValidator、PatientUpdateDtoValidator

### Client端架构图

```
┌─────────────────────────────────────────────────────────────┐
│                 LYBT.Desktop.Patients                        │
│                    (患者管理模块)                             │
└─────────────────────────────────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
  ┌──────▼──────┐    ┌──────▼──────┐    ┌──────▼──────┐
  │ ViewModels   │    │    Views    │    │ Repository  │
  │   (5个)      │    │   (5对)     │    │   (7方法)   │
  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘
         │                   │                   │
         └───────────────────┼───────────────────┘
                             │
            ┌────────────────▼────────────────┐
            │    LYBT.Desktop.Foundation      │
            │  - UnifiedViewModelBase         │
            │  - BaseApiRepository            │
            │  - IApiService                  │
            │  - IDialogService               │
            │  - IRegionManager               │
            └────────────────┬────────────────┘
                             │ HTTP API
            ┌────────────────▼────────────────┐
            │         LYBT.WebAPI             │
            │    GET /api/v1/patients         │
            │    POST /api/v1/patients        │
            │    GET /api/v1/patients/search  │
            └─────────────────────────────────┘
```

**核心组件**:
- **PatientSelectionViewModel** (20属性+25方法):患者列表、分页、搜索、选择、开始看诊、待诊队列、未完成病案检查
- **QuickCreatePatientDialogViewModel**:快速创建患者对话框
- **UnfinishedCaseDialogViewModel**:未完成病案处理对话框
- **PatientRepository** (7方法):GetAllAsync、GetByIdAsync、CreateAsync、UpdateAsync、DeleteAsync、SearchAsync、GetPagedAsync

**事件驱动通信**:
```
PatientSelectedEvent: Patients → MedicalCase/Consultation/Prescriptions
MedicalCaseCreatedEvent: MedicalCase → Patients(更新待诊队列)
```

---

## 🔧 核心功能

### 1. 患者档案创建与验证（Server端）

**功能描述**:创建患者档案时验证手机号唯一性,确保数据完整性和业务规则约束。

**核心代码**:
```csharp
// PatientService.cs - 创建患者档案
public class PatientService : IPatientService
{
    public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
    {
        // 验证手机号唯一性
        if (!string.IsNullOrEmpty(dto.PhoneNumber))
        {
            var phoneExists = await _repository.PhoneNumberExistsAsync(dto.PhoneNumber);
            if (phoneExists)
            {
                throw new InvalidOperationException("手机号已存在");
            }
        }

        // 创建患者实体
        var patient = _mapper.Map<PatientModel>(dto);
        patient.Id = Guid.NewGuid();
        patient.CreatedAt = DateTime.Now;

        // 保存到数据库
        await _repository.AddAsync(patient);
        return _mapper.Map<PatientDto>(patient);
    }
}

// FluentValidation验证器
public class PatientCreateDtoValidator : AbstractValidator<CreatePatientDto>
{
    public PatientCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名长度不能超过50字符");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("性别值无效");

        RuleFor(x => x.Age)
            .InclusiveBetween(0, 150).WithMessage("年龄必须在0-150之间");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("手机号码格式不正确");
    }
}
```

**验证规则**:
- 患者姓名:必填,最大50字符
- 性别:必填,有效枚举值（Male/Female）
- 年龄:0-150之间
- 手机号:可选,格式验证（1[3-9]开头11位数字）

### 2. 就诊历史查询（Server端）

**功能描述**:查询患者及其关联的就诊历史（MedicalCases、Consultation、Prescriptions）,支持完整的就诊记录追踪。

**核心代码**:
```csharp
// PatientRepository.cs - 查询患者及就诊历史
public class PatientRepository : BaseRepository<PatientModel>, IPatientRepository
{
    public async Task<PatientModel?> GetPatientWithVisitsAsync(Guid patientId)
    {
        return await _dbSet
            .Include(p => p.MedicalCases)           // 包含医疗案例
                .ThenInclude(mc => mc.Consultation) // 包含诊断记录
            .Include(p => p.MedicalCases)
                .ThenInclude(mc => mc.Prescriptions) // 包含处方
            .FirstOrDefaultAsync(p => p.Id == patientId);
    }

    // 获取患者摘要列表（优化的轻量级查询）
    public async Task<List<PatientSummary>> GetPatientSummariesAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        var query = _dbSet.AsQueryable();

        // 搜索过滤
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(searchTerm) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(searchTerm))
            );
        }

        // 投影到PatientSummary（减少数据传输）
        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientSummary
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Age = p.Age,
                PhoneNumber = p.PhoneNumber,
                LastVisitDate = p.LastVisitDate,
                TotalVisits = p.MedicalCases.Count // 计算就诊次数
            })
            .ToListAsync();
    }
}
```

**性能优化**:
- 使用`Include/ThenInclude`预加载关联数据,避免N+1查询
- `PatientSummary`投影减少数据传输量（只返回必要字段）
- 分页查询避免一次性加载大量数据

### 3. Excel批量导入患者（Server端）

**功能描述**:支持批量导入患者档案,逐行验证数据,汇总成功/失败记录。

**核心代码**:
```csharp
// PatientService.cs - Excel批量导入
private async Task<ImportResult> ImportFromExcelAsync(Stream stream)
{
    var result = new ImportResult();
    var patients = ParseExcelData(stream); // 解析Excel数据

    foreach (var (rowNumber, patient) in patients)
    {
        try
        {
            // 验证必填项
            if (string.IsNullOrWhiteSpace(patient.Name))
            {
                result.Failed.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    ErrorMessage = "患者姓名不能为空",
                    Data = patient
                });
                continue;
            }

            // 检查手机号重复
            if (!string.IsNullOrEmpty(patient.PhoneNumber))
            {
                var phoneExists = await _repository.PhoneNumberExistsAsync(patient.PhoneNumber);
                if (phoneExists)
                {
                    result.Failed.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = $"手机号已存在:{patient.PhoneNumber}",
                        Data = patient
                    });
                    continue;
                }
            }

            // 保存患者
            await _repository.AddAsync(patient);
            result.Succeeded.Add(patient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"导入患者失败:行{rowNumber}");
            result.Failed.Add(new ImportError
            {
                RowNumber = rowNumber,
                ErrorMessage = ex.Message,
                Data = patient
            });
        }
    }

    return result;
}
```

**错误处理**:
- 逐行验证,单行失败不影响其他行
- 记录失败行号和错误原因
- 返回`ImportResult`（成功列表 + 失败列表）

### 4. 患者选择与分页查询（Client端）

**功能描述**:支持20条/页分页查询、搜索（防抖500ms）、双击快速选择患者。

**核心代码**:
```csharp
// PatientSelectionViewModel.cs - 分页查询
public class PatientSelectionViewModel : BindableBase, INavigationAware
{
    private const int PageSize = 20; // 每页20条
    private int _currentPage = 1;
    private int _totalPages = 1;

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
                    Patients.Add(new PatientItem { ... });
                }

                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / PageSize);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 搜索防抖（500ms）
    private string? _searchKeyword;
    public string? SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
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
}
```

**性能优化**:
- **Server端分页**:只加载当前页数据,减少网络传输
- **防抖搜索**:500ms延迟,避免频繁API请求
- **DataGrid虚拟化**:启用`VirtualizingPanel.IsVirtualizing="True"`

### 5. 未完成病案检查与处理（Client端）

**功能描述**:开始看诊前自动检查患者是否有未完成病案,提供三种处理方式（继续看诊/关闭旧案新建/仅关闭）。

**核心代码**:
```csharp
// PatientSelectionViewModel.cs - 开始看诊前检查
private async Task ExecuteStartConsultation()
{
    if (SelectedPatient == null) return;

    try
    {
        IsBusy = true;

        // Step 1: 检查是否有未完成的病案
        var unfinishedCase = await CheckUnfinishedMedicalCaseAsync(SelectedPatient.Id);

        if (unfinishedCase != null)
        {
            _logger.LogWarning("患者{Name}存在未完成病案,ID:{CaseId}",
                SelectedPatient.Name, unfinishedCase.Id);

            // Step 2: 显示未完成病案对话框（三个选项）
            var dialogResult = await ShowUnfinishedCaseDialogAsync(unfinishedCase);

            if (dialogResult.Result == ButtonResult.OK)
            {
                var action = dialogResult.Parameters.GetValue<string>("Action");

                switch (action)
                {
                    case "Continue":
                        // 选项1: 继续看诊（加载旧病案）
                        await ContinueConsultationAsync(unfinishedCase.Id);
                        break;

                    case "CloseAndNew":
                        // 选项2: 关闭旧病案后创建新病案
                        await CreateNewCaseAfterClosingOldAsync(
                            unfinishedCase.Id, SelectedPatient.Id);
                        break;

                    case "CloseOnly":
                        // 选项3: 仅关闭旧病案（不创建新病案）
                        await CloseOldCaseOnlyAsync(unfinishedCase.Id);
                        break;
                }
            }
        }
        else
        {
            // Step 3: 无未完成病案,直接创建新病案
            var newCase = await _medicalCaseRepository.CreateAsync(new CreateMedicalCaseDto
            {
                PatientId = SelectedPatient.Id,
                DoctorId = CurrentUser.Id,
                Status = MedicalCaseStatus.InProgress
            });

            if (newCase != null)
            {
                // 发布患者选择事件
                PublishPatientSelectedEvent(
                    await _patientRepository.GetByIdAsync(SelectedPatient.Id), newCase.Id);

                // 导航到诊断视图
                _regionManager.RequestNavigate("ContentRegion", "ConsultationView",
                    new NavigationParameters
                    {
                        { "MedicalCaseId", newCase.Id },
                        { "PatientId", SelectedPatient.Id }
                    });
            }
        }
    }
    finally
    {
        IsBusy = false;
    }
}
```

**业务流程**:
1. 选择患者 → 点击"开始看诊"
2. 自动检查该患者是否有未完成病案（Status=InProgress）
3. 如有未完成病案,显示对话框提供3个选项
4. 如无未完成病案,直接创建新病案并导航到诊断视图

### 6. 待诊队列管理（Client端）

**功能描述**:展示所有待诊患者（InProgress状态病案）,支持快速切换到待诊患者继续看诊。

**核心代码**:
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
```

**UI展示**:
- 右侧边栏展示待诊队列（ListBox）
- 显示患者姓名、年龄、登记时间
- 点击待诊患者自动加载病案并导航到诊断视图

---

## 📋 业务规则

### 患者档案规则

| 规则类别 | 规则描述 | 约束条件 | 实施位置 |
|---------|---------|---------|---------|
| **创建规则** | 患者姓名必填 | 非空,最大50字符 | FluentValidation |
| **创建规则** | 性别必填 | Male/Female枚举 | FluentValidation |
| **创建规则** | 年龄范围 | 0-150之间 | FluentValidation |
| **创建规则** | 手机号格式 | 1[3-9]开头11位数字（可选） | FluentValidation |
| **手机号唯一性** | 同一手机号只能属于一个患者 | 创建/更新时验证 | PatientService |
| **手机号更新** | 更新时排除自身手机号 | excludePatientId参数 | PhoneNumberExistsAsync |
| **软删除** | 删除患者使用IsDeleted标记 | 不物理删除 | PatientService |

### 就诊历史规则

| 规则类别 | 规则描述 | 约束条件 | 实施位置 |
|---------|---------|---------|---------|
| **就诊记录关联** | 患者关联多个MedicalCase | 1:N关系 | PatientModel |
| **就诊次数计算** | TotalVisits = MedicalCases.Count | 聚合查询 | PatientSummary |
| **最后就诊日期** | LastVisitDate自动更新 | 病案创建/更新时 | UpdateLastVisitDateAsync |
| **活跃患者定义** | 3个月内就诊过的患者 | LastVisitDate >= Now.AddMonths(-3) | GetStatisticsAsync |

### 患者统计规则

| 规则类别 | 规则描述 | 约束条件 | 实施位置 |
|---------|---------|---------|---------|
| **总患者数** | 统计未删除的患者 | IsDeleted=false | GetStatisticsAsync |
| **活跃患者数** | 3个月内就诊过的患者 | LastVisitDate >= Now.AddMonths(-3) | GetStatisticsAsync |
| **平均年龄** | 排除年龄为0的患者 | Age > 0 | GetStatisticsAsync |
| **性别分布** | 按Gender分组统计 | GroupBy(Gender) | GetStatisticsAsync |

### 未完成病案规则（Client端）

| 规则类别 | 规则描述 | 约束条件 | 实施位置 |
|---------|---------|---------|---------|
| **未完成病案检查** | 开始看诊前自动检查 | Status=InProgress | CheckUnfinishedMedicalCaseAsync |
| **处理选项** | 提供3种处理方式 | 继续看诊/关闭旧案新建/仅关闭 | UnfinishedCaseDialog |
| **自动加载** | 继续看诊时加载旧病案数据 | MedicalCaseId传递 | ContinueConsultationAsync |

---

## 🔌 API 端点

### RESTful API端点

| 方法 | 端点 | 说明 | 请求体 | 响应体 |
|------|------|------|--------|--------|
| GET | `/api/v1/patients` | 分页查询患者 | 无（Query参数） | `PagedResult<PatientDto>` |
| GET | `/api/v1/patients/{id}` | 按ID查询患者详情 | 无 | `PatientDto` |
| POST | `/api/v1/patients` | 创建患者档案 | `CreatePatientDto` | `PatientDto` |
| PUT | `/api/v1/patients/{id}` | 更新患者档案 | `UpdatePatientDto` | `PatientDto` |
| DELETE | `/api/v1/patients/{id}` | 删除患者（软删除） | 无 | `bool` |
| GET | `/api/v1/patients/search` | 搜索患者（按姓名/手机） | 无（Query参数:keyword） | `List<PatientDto>` |
| POST | `/api/v1/patients/import` | Excel导入患者 | `IFormFile` | `ImportResult` |
| GET | `/api/v1/patients/export` | 导出患者到Excel | 无（Query参数） | `FileResult` |
| GET | `/api/v1/patients/template` | 下载Excel导入模板 | 无 | `FileResult` |
| GET | `/api/v1/patients/statistics` | 获取患者统计信息 | 无 | `PatientStatistics` |

### DTO定义

**CreatePatientDto**:
```csharp
public class CreatePatientDto
{
    public string Name { get; set; }           // 患者姓名（必填）
    public Gender Gender { get; set; }         // 性别（必填,Male/Female）
    public int Age { get; set; }               // 年龄（0-150）
    public string? PhoneNumber { get; set; }   // 手机号（可选,格式验证）
    public string? Address { get; set; }       // 地址（可选）
    public string? Allergies { get; set; }     // 过敏史（可选）
    public string? MedicalHistory { get; set; }// 病史（可选）
}
```

**UpdatePatientDto**:
```csharp
public class UpdatePatientDto
{
    public Guid Id { get; set; }               // 患者ID（必填）
    public string Name { get; set; }           // 患者姓名（必填）
    public Gender Gender { get; set; }         // 性别（必填）
    public int Age { get; set; }               // 年龄
    public string? PhoneNumber { get; set; }   // 手机号（可选）
    public string? Address { get; set; }       // 地址（可选）
    public string? Allergies { get; set; }     // 过敏史（可选）
    public string? MedicalHistory { get; set; }// 病史（可选）
}
```

**PatientDto**:
```csharp
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public DateTime? LastVisitDate { get; set; } // 最后就诊日期
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**PatientStatistics**:
```csharp
public class PatientStatistics
{
    public int TotalPatients { get; set; }          // 总患者数
    public int ActivePatients { get; set; }         // 活跃患者数（3个月内就诊）
    public double AverageAge { get; set; }          // 平均年龄
    public Dictionary<string, int> GenderDistribution { get; set; } // 性别分布
}
```

---

## 🎯 设计原则

### Server端设计原则

#### 1. 标准三层架构
- **Controller层**:接收HTTP请求,参数验证,返回统一响应格式
- **Service层**:业务逻辑实现,事务管理,FluentValidation验证
- **Repository层**:数据访问,LINQ查询,EF Core操作

#### 2. 手机号唯一性验证
- **创建时**:调用`PhoneNumberExistsAsync(phoneNumber)`验证
- **更新时**:调用`PhoneNumberExistsAsync(phoneNumber, excludePatientId:id)`排除自身
- **业务异常**:手机号重复时抛出`InvalidOperationException`

#### 3. 软删除与数据完整性
- **软删除标记**:使用`IsDeleted`字段标记删除
- **查询过滤**:所有查询自动排除`IsDeleted=true`的记录
- **数据恢复**:支持通过修改`IsDeleted=false`恢复数据

#### 4. 分页与性能优化
- **Server端分页**:只返回当前页数据,减少网络传输
- **投影查询**:使用`Select`投影到`PatientSummary`,减少数据传输量
- **Include预加载**:使用`Include/ThenInclude`避免N+1查询

#### 5. Excel批量导入
- **逐行验证**:单行失败不影响其他行
- **错误汇总**:记录失败行号和错误原因
- **事务管理**:批量导入使用事务,确保数据一致性

#### 6. 患者统计分析
- **活跃患者**:3个月内就诊过的患者（`LastVisitDate >= Now.AddMonths(-3)`）
- **平均年龄**:排除年龄为0的患者（`Age > 0`）
- **性别分布**:按Gender分组统计

### Client端设计原则

#### 1. MVVM架构与Prism导航
- **INavigationAware**:实现导航生命周期管理（OnNavigatedTo/OnNavigatedFrom）
- **NavigationParameters**:跨视图参数传递（PatientId、MedicalCaseId）
- **区域导航**:使用`IRegionManager.RequestNavigate`实现视图切换

#### 2. Repository模式与三层架构
- **ViewModel → Repository → BaseApiRepository → IApiService**
- **异常隔离**:Repository内部捕获异常,向ViewModel返回null或空列表
- **复用基类**:继承`BaseApiRepository<PatientDto>`复用CRUD方法

#### 3. 事件驱动通信与模块解耦
- **Prism EventAggregator**:发布/订阅模式实现模块间通信
- **PatientSelectedEvent**:Patients发布,MedicalCase/Consultation/Prescriptions订阅
- **ThreadOption.UIThread**:确保事件处理在UI线程执行

#### 4. 对话框服务与用户交互
- **Prism Dialog Service**:统一的模态对话框管理
- **DialogParameters**:向对话框传递参数（输入）
- **DialogResult**:对话框返回结果（输出）
- **IDialogAware**:对话框ViewModel实现的接口

#### 5. 分页优化与虚拟化
- **Server端分页**:只加载当前页数据（pageIndex、pageSize）
- **DataGrid虚拟化**:启用`VirtualizingPanel.IsVirtualizing="True"`
- **防抖搜索**:SearchKeyword属性设置500ms防抖定时器

#### 6. 异步优先与用户体验
- **IsBusy标志**:显示加载动画,禁用按钮,防止重复操作
- **ClearMessage**:清除旧的错误/成功消息
- **SetErrorMessage/SetSuccessMessage**:统一的消息提示

---

## 🛠 技术栈

### Server端技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 基础框架 |
| Entity Framework Core | 8.0.x | ORM框架,数据持久化 |
| AutoMapper | 13.x | Entity ↔ DTO自动映射 |
| FluentValidation | 11.x | DTO数据验证框架 |
| LINQ | - | 复杂查询表达式（分页、搜索、统计） |
| 异步编程 | async/await | 全异步方法,提升性能 |

### Client端技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 基础框架 |
| WPF | .NET 8.0 | Windows桌面UI框架 |
| Prism.DryIoc | 9.0.x | MVVM框架,模块化,依赖注入 |
| MaterialDesignThemes | 5.1.x | Material Design风格UI组件库 |
| Prism EventAggregator | 9.0.x | 事件聚合器（模块间通信） |
| Prism Dialog Service | 9.0.x | 模态对话框服务 |
| Prism Navigation | 9.0.x | 区域导航（视图切换） |
| Repository Pattern | - | 三层架构数据访问 |
| 异步编程 | async/await | 避免阻塞UI线程 |

---

## 🚀 快速开始

### Server端集成

#### 1. 注册Patients模块（在Startup.cs中）

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册Patients模块（自动注册仓储+服务+验证器）
        services.AddPatientsModule();
    }
}
```

#### 2. API Controller集成（在LYBT.WebAPI中）

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    // 分页查询患者
    [HttpGet]
    public async Task<IActionResult> GetPatients(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _patientService.GetPagedAsync(pageIndex, pageSize, searchTerm);
        return Ok(result);
    }

    // 创建患者
    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto)
    {
        var patientDto = await _patientService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetPatientById), new { id = patientDto.Id }, patientDto);
    }
}
```

### Client端集成

#### 1. Shell加载Patients模块（在App.xaml.cs中）

```csharp
// App.xaml.cs - Prism应用程序入口
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // Patients模块按需加载（InitializationMode.WhenAvailable）
    // 医生登录后,Shell会自动加载Patients模块
    moduleCatalog.AddModule<PatientsModule>(InitializationMode.WhenAvailable);
}
```

#### 2. 导航到患者选择视图

```csharp
// 从任意ViewModel导航到患者选择视图
_regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
```

#### 3. 订阅患者选择事件

```csharp
// 在其他模块（如MedicalCase、Consultation）中订阅患者选择事件
public MyViewModel(IEventAggregator eventAggregator)
{
    _eventAggregator = eventAggregator;

    // 订阅患者选择事件
    _eventAggregator.GetEvent<PatientSelectedEvent>()
        .Subscribe(OnPatientSelected, ThreadOption.UIThread);
}

private void OnPatientSelected(PatientSelectedPayload payload)
{
    // 处理患者选择事件
    _logger.LogInformation("接收到患者选择事件,患者:{Name},病案ID:{CaseId}",
        payload.Patient.Name, payload.MedicalCaseId);

    // 加载患者相关数据
    CurrentPatient = payload.Patient;
    MedicalCaseId = payload.MedicalCaseId;
}
```

---

## 📚 相关文档

- **Server端模块README**:[src/Server/Modules/LYBT.Module.Patients/README.md](../../../../src/Server/Modules/LYBT.Module.Patients/README.md)
- **Client端模块README**:[src/Client/Desktop/Modules/LYBT.Desktop.Patients/README.md](../../../../src/Client/Desktop/Modules/LYBT.Desktop.Patients/README.md)
- **架构设计**:[docs/architecture/server/README.md](../../../architecture/server/README.md)（Server端架构指南）
- **架构设计**:[docs/architecture/client/README.md](../../../architecture/client/README.md)（Client端架构指南）
- **API文档**:[docs/api/patients-api.md](../../../api/patients-api.md) *(待创建)*
- **开发指南**:[docs/development/server/patients-development.md](../../../development/server/patients-development.md) *(待创建)*
- **开发指南**:[docs/development/client/patients-development.md](../../../development/client/patients-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组 + Client端开发组
