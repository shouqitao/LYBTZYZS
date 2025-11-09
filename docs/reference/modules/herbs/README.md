# Herbs - 中药材管理模块

## 📦 模块定位

**Server端**:
- **层级**: Server端业务模块
- **类型**: 基础数据管理（中药材档案）
- **职责**: 提供中药材信息的完整生命周期管理，包括药材档案创建、价格维护、拼音检索、批量导入导出等功能。采用**Record-Only模式**（只管理药材档案信息，不涉及库存），以简化流程，特别适合小型诊所的需求。

**Client端**:
- **层级**: Client端业务模块
- **类型**: 药材管理UI界面
- **职责**: 提供中药材信息的完整UI管理界面，支持药材档案的增删改查、拼音快速检索、批量导入导出、双价格体系（售价+成本价）、状态管理、使用历史查询等功能。采用**MVVM架构 + Repository模式**，通过IHerbRepository与Server端交互。

---

## 🎯 功能概述

### Server端核心功能（6个）
1. **药材档案管理** - 创建、编辑、删除药材档案（名称、功效、价格、剂量等）
2. **拼音快速检索** - 支持拼音首字母快速查询（如"dg"匹配"当归"）
3. **分页查询与搜索** - 按名称/拼音/功效搜索，支持分页加载
4. **Excel批量导入导出** - 批量导入药材、导出药材、下载导入模板
5. **价格维护** - Record-Only模式下的单价管理（不涉及库存）
6. **批量删除** - 批量删除药材档案

### Client端核心功能（7个）
1. **药材列表管理** - 分页查询、搜索、刷新、双价格显示（售价+成本价）
2. **药材详情编辑** - 新建/编辑/复制药材，双价格体系，利润率计算
3. **拼音快速检索** - SearchText支持名称/拼音即时搜索
4. **批量操作** - Excel导入/导出、批量删除、导出模板
5. **状态管理** - Active/Inactive状态切换，禁用药材不可添加到新处方
6. **使用历史查询** - 查询药材在处方中的使用情况（处方编号、患者、用量、日期）
7. **分类搜索** - 按药材分类筛选（补益药、清热药、解表药等）

---

## 🏗️ 模块架构

### Server端架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                    LYBT.Module.Herbs                            │
│                    (中药材管理模块)                              │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┼───────────────┐
                │               │               │
        ┌───────▼──────┐ ┌─────▼──────┐ ┌─────▼──────┐
        │  Interfaces  │ │  Services  │ │ Validators │
        │  (2个接口)   │ │  (13方法)  │ │  (2验证器) │
        └───────┬──────┘ └─────┬──────┘ └─────┬──────┘
                │               │               │
        ┌───────▼───────────────▼───────────────▼──────┐
        │             Service Layer                     │
        │  ┌──────────────────────────────────────┐   │
        │  │  HerbService (13个方法)              │   │
        │  │  - GetPagedAsync (分页查询)          │   │
        │  │  - GetByIdAsync (按ID查询)           │   │
        │  │  - CreateAsync (创建药材)            │   │
        │  │  - UpdateAsync (更新药材)            │   │
        │  │  - DeleteAsync (删除药材)            │   │
        │  │  - SearchAsync (搜索药材)            │   │
        │  │  - BatchDeleteAsync (批量删除)       │   │
        │  │  - ImportFromExcelAsync (导入)       │   │
        │  │  - ExportAsync (导出)                │   │
        │  │  - GenerateImportTemplate (模板)     │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ 依赖注入
        ┌───────▼──────────────────────────────────────┐
        │          Repository Layer                     │
        │  ┌──────────────────────────────────────┐   │
        │  │  IHerbRepository (2个方法)           │   │
        │  │  - GetByNameAsync (精确查询)         │   │
        │  │  - GetByNameOrPinyinAsync (拼音查询) │   │
        │  └──────────────────────────────────────┘   │
        │  ┌──────────────────────────────────────┐   │
        │  │  HerbRepository (Repository实现)     │   │
        │  │  - BaseRepository (继承)             │   │
        │  │  - AppDbContext (依赖)               │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ Entity Framework Core
        ┌───────▼──────────────────────────────────────┐
        │           LYBT.Infrastructure                 │
        │  ┌──────────────────────────────────────┐   │
        │  │  AppDbContext (数据库上下文)         │   │
        │  │  - DbSet<HerbModel> Herbs            │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ SQL Server
        ┌───────▼──────────────────────────────────────┐
        │        SQL Server Database                    │
        │  Herbs表 (药材档案)                          │
        │  - Id (Guid, PK)                             │
        │  - Name (nvarchar(100), 药材名称)            │
        │  - PinyinAbbreviation (nvarchar(50), 拼音)   │
        │  - Category (nvarchar(50), 分类)             │
        │  - Effects (nvarchar(500), 功效)             │
        │  - UnitPrice (decimal(18,2), 单价)           │
        │  - DefaultUnit (nvarchar(20), 默认单位)      │
        │  - DefaultDosage (nvarchar(50), 常用剂量)    │
        │  - Notes (nvarchar(1000), 备注)              │
        └──────────────────────────────────────────────┘

特性:
1. Record-Only模式: 只管理药材档案，不涉及库存管理
2. 拼音检索: PinyinAbbreviation字段支持快速输入（"dg"→"当归"）
3. Excel导入导出: 批量导入药材 + 导出药材 + 下载模板
4. FluentValidation: DTO验证（必填项、格式验证）
5. AutoMapper: Entity ↔ DTO自动映射
```

### Client端架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                      LYBT.Desktop.Herbs                         │
│                      (药材管理模块)                              │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┼───────────────┐
                │               │               │
        ┌───────▼──────┐ ┌─────▼──────┐ ┌─────▼──────┐
        │ HerbManage   │ │  HerbDetail │ │  Herbs     │
        │ mentView     │ │  View       │ │  Module    │
        │ (XAML)       │ │  (XAML)     │ │  (Prism)   │
        └───────┬──────┘ └─────┬──────┘ └─────┬──────┘
                │               │               │
        ┌───────▼──────────────▼───────────────▼──────┐
        │         MVVM ViewModel Layer                 │
        │  ┌──────────────────────────────────────┐   │
        │  │  HerbManagementViewModel             │   │
        │  │  - 19 Commands (Add/Delete/Edit...)  │   │
        │  │  - 17 Methods (CRUD/Search/Import)   │   │
        │  │  - ObservableCollection<HerbDto>     │   │
        │  └──────────────────────────────────────┘   │
        │  ┌──────────────────────────────────────┐   │
        │  │  HerbDetailViewModel                 │   │
        │  │  - 16 Properties (Name/Price/...)    │   │
        │  │  - 15 Methods (Save/Load/Print)      │   │
        │  │  - 双价格体系 (Price + CostPrice)   │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ 依赖注入
        ┌───────▼──────────────────────────────────────┐
        │       Repository Layer                       │
        │  ┌──────────────────────────────────────┐   │
        │  │  IHerbRepository (Interface)         │   │
        │  │  - 6 Methods (GetPaged/CRUD/Search)  │   │
        │  └──────────────────────────────────────┘   │
        │  ┌──────────────────────────────────────┐   │
        │  │  HerbRepository (Implementation)     │   │
        │  │  - BaseApiRepository (继承)          │   │
        │  │  - ApiService (依赖)                 │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ HTTP通信
        ┌───────▼──────────────────────────────────────┐
        │     LYBT.Desktop.Foundation                  │
        │  ┌──────────────────────────────────────┐   │
        │  │  ApiService (IApiService)            │   │
        │  │  - HttpClient封装                    │   │
        │  │  - 返回裸类型（非Result<T>）         │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ REST API
        ┌───────▼──────────────────────────────────────┐
        │        LYBT.WebAPI (Server端)                │
        │  /api/v1/herbs/*                             │
        │  - GET /herbs (分页查询)                     │
        │  - POST /herbs (创建药材)                    │
        │  - PUT /herbs/{id} (更新药材)                │
        │  - DELETE /herbs/{id} (删除药材)             │
        │  - GET /herbs/search (搜索药材)              │
        │  - POST /herbs/import (Excel导入)            │
        │  - GET /herbs/export (Excel导出)             │
        └──────────────────────────────────────────────┘

特性:
1. MVVM架构 + Repository模式 + 三层分离
2. 拼音快速检索（PinyinAbbreviation字段）
3. 双价格体系（Price售价 + CostPrice成本价 + 利润率计算）
4. 批量操作（导入/导出/批量删除）
5. 状态管理（Active/Inactive切换）
6. 使用历史查询（ViewUsageHistoryCommand）
7. 分类搜索（按药材分类筛选）
```

### Record-Only模式说明

**核心理念**: 药材模块仅管理药材档案信息（名称、功效、价格、剂量），**不涉及库存管理**，以简化流程，符合MVP原则。

**HerbDto结构**:
```csharp
public class HerbDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }           // 药材名称
    public string? Category { get; set; }      // 分类（如:补益药、清热药）
    public string? Effects { get; set; }       // 功效（如:补气养血）
    public decimal? UnitPrice { get; set; }    // 单价（元/克）
    public string? DefaultUnit { get; set; }   // 默认计量单位（如:克、两）
    public string? DefaultDosage { get; set; } // 常用剂量（如:3-9g）
    public string? PinyinAbbreviation { get; set; } // 拼音首字母（快速检索）
    public string? Notes { get; set; }         // 备注

    // ⚠️ 不包含库存字段（库存管理超出MVP范围）
}
```

**Client端双价格体系**:
```csharp
public class HerbDetailViewModel
{
    public decimal Price { get; set; }      // 售价（元/单位）
    public decimal CostPrice { get; set; }  // 成本价（元/单位）

    // 利润率计算
    private async Task SaveHerbAsync()
    {
        decimal profitMargin = Price > 0 ? (Price - CostPrice) / Price * 100 : 0;
        if (profitMargin < 0)
        {
            // 警告但允许保存（赠送药材或促销）
            await _dialogService.ShowConfirmationAsync(
                "价格警告",
                $"售价低于成本价，利润率为 {profitMargin:F2}%，是否继续保存？"
            );
        }
    }
}
```

---

## 🔧 核心功能

### 1. 药材档案管理（Server端 + Client端）

**Server端 - HerbService.CreateAsync**:
```csharp
public class HerbService : IHerbService
{
    private readonly IHerbRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateHerbDto> _validator;

    public async Task<HerbDto> CreateAsync(CreateHerbDto dto)
    {
        // 1. FluentValidation验证
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. 检查名称重复
        var existing = await _repository.GetByNameAsync(dto.Name);
        if (existing != null)
            throw new BusinessException($"药材已存在: {dto.Name}");

        // 3. 映射并保存
        var herb = _mapper.Map<HerbModel>(dto);
        herb.Id = Guid.NewGuid();
        herb.CreatedAt = DateTime.UtcNow;

        await _repository.AddAsync(herb);
        return _mapper.Map<HerbDto>(herb);
    }
}
```

**Client端 - HerbDetailViewModel.SaveHerbAsync**:
```csharp
public class HerbDetailViewModel : UnifiedViewModelBase
{
    private readonly IHerbRepository _herbRepository;
    private readonly IDialogService _dialogService;

    // 双价格体系
    public decimal Price { get; set; }      // 售价
    public decimal CostPrice { get; set; }  // 成本价

    private async Task SaveHerbAsync()
    {
        // 1. 基础验证
        if (string.IsNullOrWhiteSpace(Name))
        {
            await _dialogService.ShowAlertAsync("验证错误", "药材名称不能为空");
            return;
        }

        // 2. 利润率计算与警告
        decimal profitMargin = Price > 0 ? (Price - CostPrice) / Price * 100 : 0;
        if (profitMargin < 0)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "价格警告",
                $"售价低于成本价，利润率为 {profitMargin:F2}%，是否继续保存？"
            );
            if (result != ButtonResult.OK) return;
        }

        // 3. 创建或更新
        if (Herb == null || Herb.Id == Guid.Empty)
        {
            var createDto = new CreateHerbDto
            {
                Name = Name,
                PinYinCode = PinYinCode,
                Price = Price,
                CostPrice = CostPrice,
                // ...其他字段
            };
            await _herbRepository.CreateAsync(createDto);
        }
        else
        {
            var updateDto = new UpdateHerbDto
            {
                Name = Name,
                Price = Price,
                CostPrice = CostPrice,
                // ...其他字段
            };
            await _herbRepository.UpdateAsync(Herb.Id, updateDto);
        }

        // 4. 返回列表
        _regionManager.RequestNavigate("MainRegion", "HerbManagementView");
    }
}
```

### 2. 拼音快速检索（中医快速输入）

**Server端 - HerbRepository.GetByNameOrPinyinAsync**:
```csharp
public class HerbRepository : BaseRepository<HerbModel>, IHerbRepository
{
    public async Task<HerbModel?> GetByNameOrPinyinAsync(string keyword)
    {
        return await _dbSet
            .Where(h =>
                h.Name.Contains(keyword) ||            // 名称匹配
                h.PinyinAbbreviation.Contains(keyword) // 拼音首字母匹配
            )
            .FirstOrDefaultAsync();
    }
}
```

**Client端 - HerbManagementViewModel.ExecuteSearchAsync**:
```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    public string SearchText { get; set; }  // 搜索关键字（支持名称/拼音）

    // 搜索命令（TextBox回车触发）
    public AsyncDelegateCommand SearchCommand { get; }

    private async Task ExecuteSearchAsync()
    {
        CurrentPage = 1; // 重置到第一页
        await LoadPageAsync(CurrentPage);
    }

    // 分页加载（传递搜索参数）
    public async Task<PagedResult<HerbDto>> GetItemsAsync(int pageIndex, int pageSize)
    {
        return await _herbRepository.GetPagedAsync(
            pageIndex,
            pageSize,
            SearchText // Server端会匹配 Name 或 PinyinAbbreviation
        );
    }
}
```

**拼音检索示例**:

| 输入 | 匹配药材 | 说明 |
|------|---------|------|
| `dg` | 当归 | PinyinAbbreviation = "DG" |
| `hq` | 黄芪 | PinyinAbbreviation = "HQ" |
| `rsh` | 人参 | PinyinAbbreviation = "RSH" |
| `当` | 当归 | Name.Contains("当") |

### 3. Excel批量导入药材（Server端验证 + Client端上传）

**Server端 - HerbService.ImportFromExcelAsync**:
```csharp
public class HerbService : IHerbService
{
    private async Task<ImportResult> ImportFromExcelAsync(Stream stream)
    {
        var result = new ImportResult();
        var herbs = ParseExcelData(stream);

        foreach (var (rowNumber, herb) in herbs)
        {
            try
            {
                // 验证必填项
                if (string.IsNullOrWhiteSpace(herb.Name))
                {
                    result.Failed.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = "药材名称不能为空",
                        Data = herb
                    });
                    continue;
                }

                // 检查重复
                var existing = await _repository.GetByNameAsync(herb.Name);
                if (existing != null)
                {
                    result.Failed.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = $"药材已存在: {herb.Name}",
                        Data = herb
                    });
                    continue;
                }

                // 保存药材
                await _repository.AddAsync(herb);
                result.Succeeded.Add(herb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导入药材失败: 行{rowNumber}");
                result.Failed.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    ErrorMessage = ex.Message,
                    Data = herb
                });
            }
        }

        return result;
    }
}
```

**Client端 - HerbManagementViewModel.ImportHerbsAsync**:
```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    private async Task ImportHerbsAsync()
    {
        // 1. 打开文件选择对话框
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择药材Excel文件",
            Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() != true) return;

        try
        {
            IsBusy = true;

            // 2. 上传文件到Server
            using var fileStream = File.OpenRead(openFileDialog.FileName);
            var formData = new MultipartFormDataContent();
            formData.Add(new StreamContent(fileStream), "file", Path.GetFileName(openFileDialog.FileName));

            var response = await _apiService.PostAsync<ImportResult>("herbs/import", formData);

            // 3. 显示导入结果
            var successCount = response.Succeeded?.Count ?? 0;
            var failedCount = response.Failed?.Count ?? 0;

            if (failedCount > 0)
            {
                var errorMessage = string.Join("\n", response.Failed.Select(f =>
                    $"行{f.RowNumber}: {f.ErrorMessage}"
                ));

                await _dialogService.ShowAlertAsync(
                    "导入结果",
                    $"成功导入: {successCount}条\n失败: {failedCount}条\n\n错误详情:\n{errorMessage}"
                );
            }
            else
            {
                await _dialogService.ShowAlertAsync("导入成功", $"成功导入 {successCount} 条药材数据");
            }

            // 4. 刷新列表
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入药材失败");
            await _dialogService.ShowAlertAsync("导入失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 4. 分类搜索与筛选（Client端）

**HerbManagementViewModel.SearchByCategory**:
```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    // 药材分类列表
    public ObservableCollection<string> Categories { get; set; }
    public string SelectedCategory { get; set; }

    public AsyncDelegateCommand SearchByCategoryCommand { get; }

    private async Task SearchByCategory()
    {
        if (string.IsNullOrWhiteSpace(SelectedCategory)) return;

        // 调用Repository搜索
        var herbs = await _herbRepository.SearchAsync(SelectedCategory);

        // 更新UI列表
        Herbs.Clear();
        foreach (var herb in herbs)
        {
            Herbs.Add(herb);
        }

        TotalCount = herbs.Count;
        _logger.LogInformation($"分类搜索: {SelectedCategory}, 找到 {TotalCount} 条药材");
    }

    // 加载分类列表
    private void LoadCategories()
    {
        Categories = new ObservableCollection<string>
        {
            "全部",
            "补益药",
            "清热药",
            "解表药",
            "活血化瘀药",
            "化痰止咳平喘药",
            "安神药",
            "理气药",
            "消食药",
            "利水渗湿药"
        };
    }
}
```

### 5. 状态管理与使用历史查询（Client端）

**HerbManagementViewModel.ToggleStatusAsync**:
```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    public AsyncDelegateCommand ToggleStatusCommand { get; }

    private async Task ToggleStatusAsync()
    {
        if (SelectedHerb == null) return;

        try
        {
            IsBusy = true;

            // 切换状态
            var newStatus = SelectedHerb.Status == HerbStatus.Active
                ? HerbStatus.Inactive
                : HerbStatus.Active;

            // 更新到Server
            var updateDto = new UpdateHerbDto
            {
                Status = newStatus
            };

            await _herbRepository.UpdateAsync(SelectedHerb.Id, updateDto);

            // 更新本地状态
            SelectedHerb.Status = newStatus;

            _logger.LogInformation($"药材状态已切换: {SelectedHerb.Name} -> {newStatus}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换状态失败");
            await _dialogService.ShowAlertAsync("操作失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

**HerbDetailViewModel.ExecuteViewUsageHistory**:
```csharp
public class HerbDetailViewModel : UnifiedViewModelBase
{
    public DelegateCommand ViewUsageHistoryCommand { get; }

    private async Task ExecuteViewUsageHistory()
    {
        if (Herb == null || Herb.Id == Guid.Empty) return;

        // 导航到PrescriptionHistoryView，传递HerbId
        var parameters = new NavigationParameters
        {
            { "HerbId", Herb.Id },
            { "HerbName", Herb.Name }
        };

        _regionManager.RequestNavigate("MainRegion", "PrescriptionHistoryView", parameters);
    }
}
```

### 6. 批量删除与并发执行（Client端）

**HerbManagementViewModel.OnExecuteBatchDeleteAsync**:
```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    public ObservableCollection<HerbDto> SelectedHerbs { get; set; }
    public AsyncDelegateCommand BatchDeleteCommand { get; }

    private async Task OnExecuteBatchDeleteAsync()
    {
        if (SelectedHerbs == null || SelectedHerbs.Count == 0)
        {
            await _dialogService.ShowAlertAsync("提示", "请先选择要删除的药材");
            return;
        }

        var count = SelectedHerbs.Count;
        var result = await _dialogService.ShowConfirmationAsync(
            "确认批量删除",
            $"确定要删除选中的 {count} 条药材吗？此操作不可恢复。"
        );

        if (result != ButtonResult.OK) return;

        try
        {
            IsBusy = true;

            // Task.WhenAll并发删除
            var deleteTasks = SelectedHerbs.Select(herb =>
                _herbRepository.DeleteAsync(herb.Id)
            );

            await Task.WhenAll(deleteTasks);

            _logger.LogInformation($"批量删除成功: {count} 条药材");
            await _dialogService.ShowAlertAsync("删除成功", $"已删除 {count} 条药材");

            // 刷新列表
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除失败");
            await _dialogService.ShowAlertAsync("删除失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 7. Repository模式与三层架构（ViewModel → Repository → API）

**IHerbRepository接口定义**:
```csharp
public interface IHerbRepository
{
    Task<PagedResult<HerbDto>> GetPagedAsync(int pageIndex, int pageSize, string? searchTerm = null);
    Task<HerbDto> GetByIdAsync(Guid id);
    Task<HerbDto> CreateAsync(CreateHerbDto dto);
    Task<HerbDto> UpdateAsync(Guid id, UpdateHerbDto dto);
    Task DeleteAsync(Guid id);
    Task<List<HerbDto>> SearchAsync(string keyword);
}
```

**HerbRepository实现（继承BaseApiRepository）**:
```csharp
public class HerbRepository : BaseApiRepository<HerbDto>, IHerbRepository
{
    private readonly IApiService _apiService;
    private readonly ILogger<HerbRepository> _logger;

    public HerbRepository(
        IApiService apiService,
        ILogger<HerbRepository> logger)
        : base(apiService, logger, "herbs")
    {
        _apiService = apiService;
        _logger = logger;
    }

    // 分页查询（支持搜索）
    public async Task<PagedResult<HerbDto>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        var queryString = $"?pageIndex={pageIndex}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            queryString += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        }

        return await _apiService.GetAsync<PagedResult<HerbDto>>($"herbs{queryString}");
    }

    // 搜索药材（名称/拼音/功效）
    public async Task<List<HerbDto>> SearchAsync(string keyword)
    {
        return await _apiService.GetAsync<List<HerbDto>>($"herbs/search?keyword={Uri.EscapeDataString(keyword)}");
    }

    // 其他CRUD方法...
}
```

**调用链示例**:
```
HerbManagementViewModel
  → IHerbRepository.GetPagedAsync
    → HerbRepository.GetPagedAsync
      → ApiService.GetAsync<PagedResult<HerbDto>>
        → HttpClient.GetAsync("/api/v1/herbs?pageIndex=1&pageSize=20")
          → Server端HerbsController.GetHerbs
            → HerbService.GetPagedAsync
              → HerbRepository.GetPagedAsync
                → EF Core查询
```

---

## 📋 业务规则

### Server端业务规则

| 规则ID | 规则描述 | 验证位置 | 错误处理 |
|-------|---------|---------|---------|
| **HR-001** | 药材名称必填，最大100字符 | CreateHerbDtoValidator | 返回ValidationException |
| **HR-002** | 药材名称全局唯一，不允许重复 | HerbService.CreateAsync | 返回BusinessException |
| **HR-003** | 拼音首字母最大50字符 | CreateHerbDtoValidator | 返回ValidationException |
| **HR-004** | 单价必须≥0（允许为0表示赠送药材） | CreateHerbDtoValidator | 返回ValidationException |
| **HR-005** | Excel导入时，必填项验证（名称、拼音、单价） | HerbService.ImportFromExcelAsync | 记录失败行号+错误信息 |
| **HR-006** | Excel导入时，名称重复跳过（不中断导入） | HerbService.ImportFromExcelAsync | 记录失败行号+错误信息 |
| **HR-007** | 批量删除时，单个失败不影响其他（部分成功） | HerbService.BatchDeleteAsync | 记录失败ID+错误信息 |
| **HR-008** | 药材删除时，不检查处方引用（历史数据保留） | HerbService.DeleteAsync | 无 |

### Client端业务规则

| 规则ID | 规则描述 | 验证位置 | 错误处理 |
|-------|---------|---------|---------|
| **HR-C-001** | 药材名称不能为空 | HerbDetailViewModel.SaveHerbAsync | ShowAlertAsync |
| **HR-C-002** | 售价和成本价不能为负数 | HerbDetailViewModel.SaveHerbAsync | ShowAlertAsync |
| **HR-C-003** | 售价低于成本价时警告但允许保存 | HerbDetailViewModel.SaveHerbAsync | ShowConfirmationAsync |
| **HR-C-004** | 禁用药材不可添加到新处方（HerbSelectionDialog过滤） | HerbSelectionDialog | 从列表中过滤 |
| **HR-C-005** | 禁用药材不影响历史处方（只读） | 无 | 无 |
| **HR-C-006** | 批量删除前必须确认 | HerbManagementViewModel.OnExecuteBatchDeleteAsync | ShowConfirmationAsync |
| **HR-C-007** | Excel导入失败时显示错误详情（行号+错误信息） | HerbManagementViewModel.ImportHerbsAsync | ShowAlertAsync |

### 双价格体系规则

| 规则ID | 规则描述 | 计算公式 | 说明 |
|-------|---------|---------|------|
| **PR-001** | 利润率计算 | (Price - CostPrice) / Price × 100% | 售价>成本价时为正 |
| **PR-002** | 允许成本价为0 | CostPrice = 0 | 赠送药材或自采药材 |
| **PR-003** | 允许利润率为负 | ProfitMargin < 0 | 促销或特殊情况，需确认 |
| **PR-004** | 价格精度 | decimal(18,2) | 保留2位小数 |

---

## 🔌 API 端点

### Server端API端点（10个）

| 方法 | 端点 | 说明 | 请求DTO | 响应DTO |
|------|-----|------|---------|---------|
| **GET** | `/api/v1/herbs` | 分页查询药材 | pageIndex, pageSize, searchTerm | PagedResult<HerbDto> |
| **GET** | `/api/v1/herbs/{id}` | 按ID查询药材详情 | id (Guid) | HerbDto |
| **POST** | `/api/v1/herbs` | 创建药材 | CreateHerbDto | HerbDto |
| **PUT** | `/api/v1/herbs/{id}` | 更新药材 | id (Guid), UpdateHerbDto | HerbDto |
| **DELETE** | `/api/v1/herbs/{id}` | 删除药材 | id (Guid) | 204 No Content |
| **GET** | `/api/v1/herbs/search` | 搜索药材（名称/拼音/功效） | keyword (string) | List<HerbDto> |
| **POST** | `/api/v1/herbs/batch-delete` | 批量删除药材 | List<Guid> | BatchDeleteResult |
| **POST** | `/api/v1/herbs/import` | Excel导入药材 | IFormFile | ImportResult |
| **GET** | `/api/v1/herbs/export` | 导出药材到Excel | - | FileContentResult |
| **GET** | `/api/v1/herbs/template` | 下载Excel导入模板 | - | FileContentResult |

### DTO定义

**CreateHerbDto**:
```csharp
public class CreateHerbDto
{
    [Required(ErrorMessage = "药材名称不能为空")]
    [MaxLength(100, ErrorMessage = "药材名称最大100字符")]
    public string Name { get; set; }

    [MaxLength(50)]
    public string? PinyinAbbreviation { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Effects { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "单价不能为负数")]
    public decimal? UnitPrice { get; set; }

    [MaxLength(20)]
    public string? DefaultUnit { get; set; }

    [MaxLength(50)]
    public string? DefaultDosage { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
```

**UpdateHerbDto**:
```csharp
public class UpdateHerbDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(50)]
    public string? PinyinAbbreviation { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }

    // ...其他字段（与CreateHerbDto相同）
}
```

**HerbDto**:
```csharp
public class HerbDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Category { get; set; }
    public string? Effects { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? DefaultUnit { get; set; }
    public string? DefaultDosage { get; set; }
    public string? PinyinAbbreviation { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**ImportResult**:
```csharp
public class ImportResult
{
    public List<HerbModel> Succeeded { get; set; } = new();
    public List<ImportError> Failed { get; set; } = new();
}

public class ImportError
{
    public int RowNumber { get; set; }
    public string ErrorMessage { get; set; }
    public HerbModel Data { get; set; }
}
```

---

## 🎯 设计原则

### Server端设计原则（6条）

#### 1. Record-Only模式（库存无关）
- **核心思想**: 只管理药材档案信息（名称、功效、价格、剂量），不涉及库存管理
- **优势**: 简化流程，符合MVP原则，适合小型诊所需求
- **反模式**: 过早引入库存管理（增加复杂度，超出MVP范围）

#### 2. 拼音快速检索（中医特色）
- **核心思想**: PinyinAbbreviation字段存储拼音首字母，支持快速输入（如"dg"→"当归"）
- **实现**: `h.Name.Contains(keyword) || h.PinyinAbbreviation.Contains(keyword)`
- **优势**: 提升中医师开方效率，减少鼠标操作

#### 3. FluentValidation验证（DTO层）
- **核心思想**: 在DTO层验证数据完整性，Service层验证业务逻辑
- **示例**: CreateHerbDtoValidator验证必填项、格式、长度
- **优势**: 验证逻辑集中管理，易于单元测试

#### 4. AutoMapper映射（Entity ↔ DTO）
- **核心思想**: 统一处理Entity与DTO的转换，避免手动映射
- **示例**: `_mapper.Map<HerbDto>(herbModel)`
- **优势**: 减少代码重复，统一映射规则

#### 5. Excel导入容错（部分成功）
- **核心思想**: 导入失败行不影响其他行，部分成功数据保留
- **实现**: ImportResult记录成功/失败行号+错误信息
- **优势**: 提升导入成功率，避免全部回滚

#### 6. Repository抽象（数据访问层）
- **核心思想**: IHerbRepository抽象数据访问，Service依赖接口而非具体实现
- **优势**: Service层与数据访问解耦，易于单元测试（Mock Repository）

### Client端设计原则（7条）

#### 1. MVVM架构与数据绑定
- **核心思想**: ViewModel封装UI逻辑，通过INotifyPropertyChanged实现双向绑定
- **示例**: `ObservableCollection<HerbDto> Herbs` 自动同步到DataGrid
- **反模式**: 在ViewModel中直接操作UI控件

#### 2. Repository模式（ViewModel与Server解耦）
- **核心思想**: ViewModel依赖IHerbRepository接口，通过Repository与Server交互
- **优势**: ViewModel与Server解耦，易于单元测试（Mock IHerbRepository）
- **反模式**: ViewModel直接依赖具体Repository类

#### 3. 双价格体系与利润率计算
- **核心思想**: Price（售价）+ CostPrice（成本价），自动计算利润率
- **业务逻辑**: 售价低于成本价时警告但允许保存（促销或赠送）
- **反模式**: 只存售价，不记录成本（无法分析利润）

#### 4. 拼音快速检索（即时搜索）
- **核心思想**: SearchText支持名称/拼音即时搜索，TextBox回车触发SearchCommand
- **实现**: Server端 `h.Name.Contains(keyword) || h.PinyinAbbreviation.Contains(keyword)`
- **优势**: 提升开方效率

#### 5. 批量操作与并发执行
- **核心思想**: Task.WhenAll并发执行批量删除，提升性能
- **示例**: `await Task.WhenAll(SelectedHerbs.Select(h => _herbRepository.DeleteAsync(h.Id)))`
- **反模式**: 批量操作串行执行（性能低下）

#### 6. 状态管理与使用历史查询
- **核心思想**: HerbStatus枚举（Active/Inactive），禁用药材不可添加到新处方
- **使用历史**: ViewUsageHistoryCommand查询药材在处方中的使用情况
- **反模式**: 删除药材而非禁用（历史处方引用丢失）

#### 7. 异步优先与UI响应性
- **核心思想**: 所有I/O操作使用async/await，IsBusy标志显示Loading
- **示例**: `IsBusy = true; await _herbRepository.GetPagedAsync(); IsBusy = false;`
- **反模式**: 同步方法阻塞UI线程

---

## 🛠 技术栈

### Server端技术栈

| 类别 | 技术 | 版本 | 用途 |
|------|------|------|------|
| **核心框架** | .NET | 8.0 | 基础框架 |
| **ORM** | Entity Framework Core | 8.0.x | 数据持久化 |
| **验证框架** | FluentValidation | 11.x | DTO验证 |
| **对象映射** | AutoMapper | 13.x | Entity ↔ DTO映射 |
| **依赖注入** | Microsoft.Extensions.DependencyInjection | 8.0.x | IoC容器 |
| **Excel处理** | EPPlus | 7.x | Excel导入导出 |
| **数据库** | SQL Server | 2022 Express | 数据存储 |

### Client端技术栈

| 类别 | 技术 | 版本 | 用途 |
|------|------|------|------|
| **核心框架** | .NET & WPF | 8.0 | Windows桌面应用 |
| **MVVM框架** | Prism.DryIoc | 9.0.x | 模块化、区域导航、命令、事件 |
| **UI组件库** | MaterialDesignThemes | 5.1.x | Material Design风格UI |
| **数据绑定** | ObservableCollection | .NET 8.0 | 集合变更通知 |
| **命令模式** | AsyncDelegateCommand | Prism 8.x | 异步命令 |
| **HTTP通信** | IApiService | 自定义 | HTTP通信封装（返回裸类型） |
| **日志框架** | Microsoft.Extensions.Logging | 8.0.x | 日志记录 |
| **JSON序列化** | Newtonsoft.Json | 13.0.x | JSON序列化 |

---

## 🚀 快速开始

### Server端集成

#### 1. 注册药材模块（在Startup.cs中）

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册药材模块（自动注册仓储+服务+验证器）
        services.AddHerbsModule();
    }
}
```

#### 2. API Controller集成（在LYBT.WebAPI中）

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class HerbsController : ControllerBase
{
    private readonly IHerbService _herbService;

    public HerbsController(IHerbService herbService)
    {
        _herbService = herbService;
    }

    // 分页查询药材
    [HttpGet]
    public async Task<IActionResult> GetHerbs(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _herbService.GetPagedAsync(pageIndex, pageSize, searchTerm);
        return Ok(result);
    }

    // 创建药材
    [HttpPost]
    public async Task<IActionResult> CreateHerb([FromBody] CreateHerbDto dto)
    {
        var herbDto = await _herbService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetHerbById), new { id = herbDto.Id }, herbDto);
    }

    // 搜索药材（支持拼音）
    [HttpGet("search")]
    public async Task<IActionResult> SearchHerbs([FromQuery] string keyword)
    {
        var herbs = await _herbService.SearchAsync(keyword);
        return Ok(herbs);
    }

    // Excel导入
    [HttpPost("import")]
    public async Task<IActionResult> ImportHerbs(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var result = await _herbService.ImportFromExcelAsync(stream);

        return Ok(new
        {
            SuccessCount = result.Succeeded.Count,
            FailedCount = result.Failed.Count,
            Errors = result.Failed.Select(f => new
            {
                f.RowNumber,
                f.ErrorMessage,
                f.Data
            })
        });
    }
}
```

### Client端集成

#### 1. Shell加载Herbs模块（在App.xaml.cs中）

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 药材模块（WhenAvailable模式，Shell启动时立即加载）
    moduleCatalog.AddModule<HerbsModule>(InitializationMode.WhenAvailable);
}
```

#### 2. HerbsModule注册（Prism模块注册）

```csharp
public class HerbsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion("MainRegion", typeof(HerbManagementView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册ViewModels
        containerRegistry.Register<HerbManagementViewModel>();
        containerRegistry.Register<HerbDetailViewModel>();

        // 注册Views（导航）
        containerRegistry.RegisterForNavigation<HerbManagementView, HerbManagementViewModel>();
        containerRegistry.RegisterForNavigation<HerbDetailView, HerbDetailViewModel>();

        // 注册Repository（单例）
        containerRegistry.RegisterSingleton<IHerbRepository, HerbRepository>();
    }
}
```

#### 3. HerbManagementView.xaml数据绑定

```xml
<UserControl x:Class="LYBT.Desktop.Herbs.Views.HerbManagementView"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <!-- 搜索栏 -->
    <StackPanel Orientation="Horizontal" Margin="10">
        <TextBox Width="200"
                 Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                 md:HintAssist.Hint="搜索药材（支持拼音）"
                 KeyDown="OnSearchTextBoxKeyDown" />
        <Button Content="搜索" Command="{Binding SearchCommand}" Margin="5,0" />
        <Button Content="添加" Command="{Binding AddCommand}" Margin="5,0" />
        <Button Content="导入" Command="{Binding ImportHerbsCommand}" Margin="5,0" />
        <Button Content="导出" Command="{Binding ExportHerbsCommand}" Margin="5,0" />
    </StackPanel>

    <!-- 药材列表 -->
    <DataGrid ItemsSource="{Binding Herbs}"
              SelectedItem="{Binding SelectedHerb}"
              AutoGenerateColumns="False"
              IsReadOnly="True">
        <DataGrid.Columns>
            <DataGridTextColumn Header="药材名称" Binding="{Binding Name}" Width="150" />
            <DataGridTextColumn Header="拼音" Binding="{Binding PinYinCode}" Width="100" />
            <DataGridTextColumn Header="售价" Binding="{Binding Price, StringFormat=¥{0:F2}}" Width="100" />
            <DataGridTextColumn Header="成本价" Binding="{Binding CostPrice, StringFormat=¥{0:F2}}" Width="100" />
            <DataGridTextColumn Header="功效" Binding="{Binding Effect}" Width="200" />
            <DataGridTextColumn Header="状态" Binding="{Binding Status}" Width="80" />
            <DataGridTemplateColumn Header="操作" Width="150">
                <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal">
                            <Button Content="编辑" Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" />
                            <Button Content="删除" Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" Margin="5,0" />
                        </StackPanel>
                    </DataTemplate>
                </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>
        </DataGrid.Columns>
    </DataGrid>

    <!-- 分页控制 -->
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="10">
        <Button Content="首页" Command="{Binding FirstPageCommand}" />
        <Button Content="上一页" Command="{Binding PreviousPageCommand}" Margin="5,0" />
        <TextBlock Text="{Binding CurrentPage}" VerticalAlignment="Center" Margin="5,0" />
        <TextBlock Text="/" VerticalAlignment="Center" />
        <TextBlock Text="{Binding TotalPages}" VerticalAlignment="Center" Margin="5,0" />
        <Button Content="下一页" Command="{Binding NextPageCommand}" Margin="5,0" />
        <Button Content="末页" Command="{Binding LastPageCommand}" />
    </StackPanel>
</UserControl>
```

#### 4. 拼音检索使用示例

```csharp
// HerbManagementView.xaml.cs
private void OnSearchTextBoxKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        var viewModel = DataContext as HerbManagementViewModel;
        viewModel?.SearchCommand.Execute(null);
    }
}
```

**搜索示例**:
- 输入 `dg` → 自动匹配 `当归`（PinyinAbbreviation = "DG"）
- 输入 `当` → 匹配 `当归`（Name.Contains("当")）
- 输入 `补气` → 匹配所有补气药材（Effects.Contains("补气")）

---

## 📚 相关文档

- **完整模块文档**: [docs/reference/modules/herbs/](../../../../docs/reference/modules/herbs/)
- **Server端架构设计**: [docs/architecture/server/README.md](../../../../docs/architecture/server/README.md)
- **Client端架构设计**: [docs/architecture/client/README.md](../../../../docs/architecture/client/README.md)
- **API文档**: [docs/api/herbs-api.md](../../../../docs/api/herbs-api.md)
- **三层对齐架构**: [docs/architecture/README.md](../../../../docs/architecture/README.md)

---

**最后更新**: 2025-10-29
**维护负责**: Server端开发组 + Client端开发组
