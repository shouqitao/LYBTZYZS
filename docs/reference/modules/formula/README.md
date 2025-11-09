# Formula - 验方管理模块

## 📦 模块定位

**Server端**:
- **层级**: Server端业务模块
- **类型**: 验方管理（经典方剂和经验方）
- **职责**: 提供验方的完整生命周期管理，包括验方创建、药材组成配置、验方克隆、共享验方、从处方创建验方、Excel导入/导出、智能药材匹配、验方验证等功能。作为处方系统的模板支撑，旨在提高医生开方效率，积累诊疗经验。

**Client端**:
- **层级**: Client端业务模块
- **类型**: 验方管理UI界面
- **职责**: 为医生提供管理经典方剂和个人验方的用户界面，支持方剂的创建、编辑、克隆、查询、验证和组方配置。采用**MVVM架构 + Repository模式 + Components辅助类**，通过IFormulaRepository与Server端交互，实现验方模板化管理。

---

## 🎯 功能概述

### Server端核心功能（8个）
1. **验方档案管理** - 创建、编辑、删除验方，配置药材组成
2. **验方克隆功能** - 复制现有验方及药材配置，生成新验方
3. **智能药材匹配** - Excel导入时支持药材名称精确匹配和别名模糊匹配
4. **验方验证机制** - 检查验方中的药材是否存在/被删除，维护数据完整性
5. **共享验方管理** - 验方标记为"共享"后，其他医生可见
6. **分类搜索** - 按验方分类筛选（补益方、清热方、解表方等）
7. **Excel批量导入导出** - 批量导入验方、导出验方、下载导入模板
8. **批量删除** - 批量删除验方档案

### Client端核心功能（8个）
1. **验方列表管理** - 分页查询、搜索、刷新、批量操作
2. **验方详情编辑** - 新建/编辑/克隆验方，药材配置，总价计算
3. **验方克隆功能** - 复制验方及药材配置，支持个性化调整
4. **待验证验方管理** - 查看包含无效药材的验方，批量验证
5. **共享验方管理** - 标记验方为"共享"，查看其他医生的共享验方
6. **使用历史查询** - 查询验方在哪些处方中使用
7. **Excel批量操作** - 导入验方、导出验方、下载模板
8. **分类搜索** - 按验方分类筛选

---

## 🏗️ 模块架构

### Server端架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                    LYBT.Module.Formula                          │
│                    (验方管理模块)                                │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┼───────────────┐
                │               │               │
        ┌───────▼──────┐ ┌─────▼──────┐ ┌─────▼──────┐
        │  Interfaces  │ │  Services  │ │ Validators │
        │  (1接口)     │ │  (19方法)  │ │  (2验证器) │
        └───────┬──────┘ └─────┬──────┘ └─────┬──────┘
                │               │               │
        ┌───────▼───────────────▼───────────────▼──────┐
        │             Service Layer                     │
        │  ┌──────────────────────────────────────┐   │
        │  │  FormulaService (19个方法)           │   │
        │  │  - GetPagedAsync (分页查询)          │   │
        │  │  - CloneFormulaAsync (克隆验方)      │   │
        │  │  - ValidateFormulaHerbAsync (验证)   │   │
        │  │  - ImportFromExcelAsync (导入)       │   │
        │  │  - TryMatchHerbAsync (智能匹配)      │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ 依赖注入
        ┌───────▼──────────────────────────────────────┐
        │          Repository Layer                     │
        │  ┌──────────────────────────────────────┐   │
        │  │  IFormulaRepository (8个方法)        │   │
        │  │  - GetByIdWithHerbsAsync (含药材)    │   │
        │  │  - GetSharedFormulasAsync (共享)     │   │
        │  │  - GetByCategoryAsync (分类查询)     │   │
        │  └──────────────────────────────────────┘   │
        └───────┬──────────────────────────────────────┘
                │ Entity Framework Core
        ┌───────▼──────────────────────────────────────┐
        │           LYBT.Infrastructure                 │
        │  Formulas表 + FormulaHerbItems表 (1:N)       │
        └──────────────────────────────────────────────┘

特性:
1. 验方克隆: CloneFormulaAsync复制验方及药材配置
2. 智能药材匹配: 精确匹配 + 别名模糊匹配（减少导入失败）
3. 验方验证: ValidateFormulaHerbAsync检查药材有效性
4. 共享验方: IsShared标志，支持团队知识共享
5. 分类管理: 补益方、清热方、解表方等分类
```

### Client端架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                    LYBT.Desktop.Formula                         │
│                      (验方管理模块)                              │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
┌───────▼──────┐    ┌─────────▼──────┐    ┌───────▼──────┐
│ ViewModels   │    │     Views      │    │ Repository   │
│  (6个VM)     │    │   (5个View)    │    │   (1个Repo)  │
└───────┬──────┘    └────────────────┘    └───────┬──────┘
        │                                           │
┌───────▼──────┐                          ┌────────▼──────┐
│ Components   │                          │  ApiService   │
│  (4辅助类)   │                          │   (HTTP)      │
└──────────────┘                          └───────┬───────┘
                                                  │
                                          ┌───────▼───────┐
                                          │ Server端API   │
                                          │ /formulas     │
                                          └───────────────┘

ViewModels (6个):
- FormulaManagementViewModel: 458行, 20命令+20方法 (列表管理)
- FormulaDetailViewModel: 675行, 25属性+11命令+22方法 (详情编辑)
- EditFormulaDialogViewModel: 快速编辑对话框
- ViewFormulaDialogViewModel: 只读查看对话框
- FormulaValidationViewModel: 待验证列表管理
- FormulaHerbItemViewModel: 单个药材管理

Components辅助类 (4个):
- FormulaCalculator: 验方总价计算器（单例）
- FormulaCommandHandler: 命令处理器（批量删除、导入导出）
- FormulaDataManager: 数据管理器（分页加载、搜索）
- FormulaValidator: 验证器（必填项、药材数量验证）

特性:
1. MVVM架构 + Repository模式 + Components辅助类
2. 验方克隆 + 药材配置 + 总价计算
3. 待验证列表管理（检查无效药材）
4. 共享验方管理（IsShared标志）
5. Excel批量导入导出 + 智能药材匹配
6. 使用历史查询（在哪些处方中使用）
```

---

## 🔧 核心功能

### 1. 验方克隆功能（Server端 + Client端）

**Server端 - FormulaService.CloneFormulaAsync**:
```csharp
public class FormulaService : IFormulaService
{
    public async Task<FormulaDto> CloneFormulaAsync(Guid sourceId, string newName)
    {
        // 查询原验方（含药材）
        var source = await _repository.GetByIdWithHerbsAsync(sourceId);
        if (source == null) throw new NotFoundException("验方不存在");

        // 复制验方及药材
        var clone = new FormulaModel
        {
            Name = newName,
            Category = source.Category,
            Description = source.Description,
            UsageInstructions = source.UsageInstructions,
            IsShared = false, // 克隆的验方默认不共享
            HerbItems = source.HerbItems.Select(item => new FormulaHerbItem
            {
                HerbId = item.HerbId,
                Dosage = item.Dosage,
                Unit = item.Unit,
                Notes = item.Notes
            }).ToList()
        };

        await _repository.AddAsync(clone);
        return _mapper.Map<FormulaDto>(clone);
    }
}
```

**Client端 - FormulaDetailViewModel.CopyFormulaAsync**:
```csharp
public class FormulaDetailViewModel : UnifiedViewModelBase
{
    private async Task CopyFormulaAsync()
    {
        if (Formula == null) return;

        // 调用Repository克隆验方
        var newFormula = await _formulaRepository.CloneFormulaAsync(
            Formula.Id,
            $"{Formula.Name}_副本"
        );

        _logger.LogInformation($"验方克隆成功: {Formula.Name} → {newFormula.Name}");

        // 导航到新验方编辑页
        var parameters = new NavigationParameters
        {
            { "FormulaId", newFormula.Id },
            { "IsEditMode", true }
        };
        _regionManager.RequestNavigate("MainRegion", "FormulaDetailView", parameters);
    }
}
```

### 2. 智能药材匹配（Server端Excel导入）

**FormulaService.TryMatchHerbAsync**:
```csharp
public class FormulaService : IFormulaService
{
    /// <summary>
    /// 智能匹配药材（精确匹配 + 别名模糊匹配）
    /// </summary>
    private async Task<HerbItemData?> TryMatchHerbAsync(string herbName)
    {
        // 1. 精确匹配
        var herb = await _herbRepository.GetByNameAsync(herbName);
        if (herb != null) return new HerbItemData { HerbId = herb.Id, Name = herb.Name };

        // 2. 别名模糊匹配（中医药材别名）
        herb = await _herbRepository.SearchByAliasAsync(herbName);
        if (herb != null)
        {
            _logger.LogWarning($"使用别名匹配: {herbName} → {herb.Name}");
            return new HerbItemData { HerbId = herb.Id, Name = herb.Name };
        }

        return null; // 匹配失败
    }

    /// <summary>
    /// Excel导入验方（智能药材匹配）
    /// </summary>
    private async Task<ImportResult> ImportFromExcelAsync(Stream stream)
    {
        var result = new ImportResult();
        var formulas = ParseExcelData(stream);

        foreach (var (rowNumber, formula) in formulas)
        {
            try
            {
                // 验证必填项
                if (string.IsNullOrWhiteSpace(formula.Name))
                {
                    result.Failed.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = "验方名称不能为空",
                        Data = formula
                    });
                    continue;
                }

                // 智能匹配药材
                var herbItems = new List<FormulaHerbItem>();
                foreach (var herbName in formula.HerbNames)
                {
                    var herb = await TryMatchHerbAsync(herbName);
                    if (herb == null)
                    {
                        result.Failed.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = $"找不到药材: {herbName}",
                            Data = formula
                        });
                        continue;
                    }

                    herbItems.Add(new FormulaHerbItem
                    {
                        HerbId = herb.HerbId,
                        Dosage = herb.Dosage,
                        Unit = herb.Unit
                    });
                }

                // 保存验方
                formula.HerbItems = herbItems;
                await _repository.AddAsync(formula);
                result.Succeeded.Add(formula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导入验方失败: 行{rowNumber}");
                result.Failed.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    ErrorMessage = ex.Message,
                    Data = formula
                });
            }
        }

        return result;
    }
}
```

### 3. 验方验证机制（Server端 + Client端）

**Server端 - FormulaService.ValidateFormulaHerbAsync**:
```csharp
public async Task ValidateFormulaHerbAsync(Guid formulaId)
{
    var formula = await _repository.GetByIdWithHerbsAsync(formulaId);
    if (formula == null) throw new NotFoundException("验方不存在");

    var invalidHerbs = new List<string>();
    foreach (var item in formula.HerbItems)
    {
        var herb = await _herbRepository.GetByIdAsync(item.HerbId);
        if (herb == null || herb.IsDeleted)
        {
            invalidHerbs.Add(item.Notes ?? item.HerbId.ToString());
        }
    }

    if (invalidHerbs.Any())
    {
        throw new ValidationException(
            $"验方包含无效药材: {string.Join(", ", invalidHerbs)}"
        );
    }
}
```

**Client端 - FormulaValidationViewModel**:
```csharp
public class FormulaValidationViewModel : UnifiedViewModelBase
{
    // 待验证验方列表
    public ObservableCollection<FormulaDto> PendingFormulas { get; set; }

    /// <summary>
    /// 加载待验证验方列表（包含无效药材的验方）
    /// </summary>
    public async Task LoadPendingFormulasAsync()
    {
        IsBusy = true;
        try
        {
            var formulas = await _formulaRepository.GetPendingValidationFormulasAsync();

            PendingFormulas.Clear();
            foreach (var formula in formulas)
            {
                PendingFormulas.Add(formula);
            }

            _logger.LogInformation($"加载待验证验方: {formulas.Count}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载待验证验方失败");
            await _dialogService.ShowAlertAsync("错误", $"加载失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 验证单个验方药材有效性
    /// </summary>
    private async Task ValidateFormulaAsync(FormulaDto formula)
    {
        if (formula == null) return;

        try
        {
            IsBusy = true;
            await _formulaRepository.ValidateFormulaHerbAsync(formula.Id);
            await _dialogService.ShowAlertAsync("成功", "验方药材验证通过");
            PendingFormulas.Remove(formula);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"验证验方失败: {formula.Name}");
            await _dialogService.ShowAlertAsync("验证失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 4. 验方总价计算器（Client端Components）

**FormulaCalculator.cs**:
```csharp
/// <summary>
/// 验方总价计算器（单例，无状态）
/// </summary>
public class FormulaCalculator
{
    /// <summary>
    /// 计算验方总价（所有药材价格之和）
    /// </summary>
    public decimal CalculateTotalPrice(IEnumerable<FormulaHerbItemDto> herbItems)
    {
        if (herbItems == null || !herbItems.Any())
            return 0;

        return herbItems.Sum(item =>
        {
            // 计算单味药材价格: 单价 × 用量
            var price = item.HerbPrice ?? 0;        // 药材单价（元/克）
            var dosage = item.Dosage ?? 0;          // 用量（克）
            return price * dosage;
        });
    }

    /// <summary>
    /// 计算单味药材价格
    /// </summary>
    public decimal CalculateHerbPrice(FormulaHerbItemDto item)
    {
        return (item.HerbPrice ?? 0) * (item.Dosage ?? 0);
    }
}
```

**FormulaDetailViewModel使用计算器**:
```csharp
public class FormulaDetailViewModel : UnifiedViewModelBase
{
    private readonly FormulaCalculator _calculator;

    // 药材列表与总价
    public ObservableCollection<FormulaHerbItemDto> HerbItems { get; set; }
    public int HerbCount => HerbItems?.Count ?? 0;
    public decimal TotalPrice => _calculator?.CalculateTotalPrice(HerbItems) ?? 0;
}
```

### 5. 共享验方管理（Server端 + Client端）

**Server端 - 共享验方查询**:
```csharp
public class FormulaRepository : BaseRepository<FormulaModel>, IFormulaRepository
{
    /// <summary>
    /// 获取共享验方列表
    /// </summary>
    public async Task<List<FormulaModel>> GetSharedFormulasAsync()
    {
        return await _dbSet
            .Where(f => f.IsShared == true)
            .Include(f => f.HerbItems)
            .ToListAsync();
    }
}
```

**Client端 - 共享验方筛选**:
```csharp
public class FormulaManagementViewModel : UnifiedViewModelBase
{
    // 验方来源筛选
    public bool ShowSharedFormulas { get; set; } = false;  // false=我的验方，true=共享验方

    /// <summary>
    /// 切换验方来源（我的验方 / 共享验方）
    /// </summary>
    private async Task ToggleFormulaSourceAsync()
    {
        ShowSharedFormulas = !ShowSharedFormulas;

        // 重新加载列表
        CurrentPage = 1;
        await LoadPageAsync(CurrentPage);
    }

    /// <summary>
    /// 分页加载验方（支持共享验方筛选）
    /// </summary>
    public async Task<PagedResult<FormulaDto>> GetItemsAsync(int pageIndex, int pageSize)
    {
        var queryString = $"?pageIndex={pageIndex}&pageSize={pageSize}";
        queryString += $"&isShared={ShowSharedFormulas}";

        return await _apiService.GetAsync<PagedResult<FormulaDto>>($"formulas{queryString}");
    }
}
```

---

## 📋 业务规则

### Server端业务规则

| 规则ID | 规则描述 | 验证位置 | 错误处理 |
|-------|---------|---------|---------|
| **FR-001** | 验方名称必填，最大100字符 | FormulaCreateDtoValidator | 返回ValidationException |
| **FR-002** | 验方必须包含至少1味药材 | FormulaService.CreateAsync | 返回ValidationException |
| **FR-003** | 克隆验方名称自动添加"_副本"后缀 | FormulaService.CloneFormulaAsync | 无 |
| **FR-004** | 克隆的验方默认不共享（IsShared=false） | FormulaService.CloneFormulaAsync | 无 |
| **FR-005** | Excel导入时，智能匹配药材（精确+别名） | FormulaService.TryMatchHerbAsync | 记录失败行号 |
| **FR-006** | 验方验证时，检查药材是否存在/被删除 | FormulaService.ValidateFormulaHerbAsync | 返回ValidationException |
| **FR-007** | 共享验方（IsShared=true）对其他医生可见 | FormulaRepository.GetSharedFormulasAsync | 无 |

### Client端业务规则

| 规则ID | 规则描述 | 验证位置 | 错误处理 |
|-------|---------|---------|---------|
| **FR-C-001** | 验方名称不能为空 | FormulaDetailViewModel.SaveAsync | ShowAlertAsync |
| **FR-C-002** | 验方必须包含至少1味药材 | FormulaDetailViewModel.SaveAsync | ShowAlertAsync |
| **FR-C-003** | 克隆验方时名称自动添加"_副本" | FormulaManagementViewModel.CopyFormulaAsync | 无 |
| **FR-C-004** | 批量删除前必须确认 | FormulaManagementViewModel.OnExecuteBatchDeleteAsync | ShowConfirmationAsync |
| **FR-C-005** | Excel导入失败时显示错误详情（行号+错误信息） | FormulaManagementViewModel.ExecuteImportFormulasAsync | ShowAlertAsync |
| **FR-C-006** | 验方总价自动计算（Σ(单价 × 用量)） | FormulaCalculator.CalculateTotalPrice | 无 |

---

## 🔌 API 端点

### Server端API端点（11个）

| 方法 | 端点 | 说明 | 请求DTO | 响应DTO |
|------|-----|------|---------|---------|
| **GET** | `/api/v1/formulas` | 分页查询验方 | pageIndex, pageSize, category, isShared | PagedResult<FormulaDto> |
| **GET** | `/api/v1/formulas/{id}` | 按ID查询验方详情 | id (Guid) | FormulaDto |
| **POST** | `/api/v1/formulas` | 创建验方 | CreateFormulaDto | FormulaDto |
| **PUT** | `/api/v1/formulas/{id}` | 更新验方 | id (Guid), UpdateFormulaDto | FormulaDto |
| **DELETE** | `/api/v1/formulas/{id}` | 删除验方 | id (Guid) | 204 No Content |
| **POST** | `/api/v1/formulas/{id}/clone` | 克隆验方 | id (Guid), newName (string) | FormulaDto |
| **GET** | `/api/v1/formulas/search` | 搜索验方（名称/分类） | keyword (string) | List<FormulaDto> |
| **POST** | `/api/v1/formulas/import` | Excel导入验方 | IFormFile | ImportResult |
| **GET** | `/api/v1/formulas/export` | 导出验方到Excel | - | FileContentResult |
| **GET** | `/api/v1/formulas/pending-validation` | 获取待验证验方 | - | List<FormulaDto> |
| **POST** | `/api/v1/formulas/{id}/validate-herbs` | 验证验方药材 | id (Guid) | 200 OK / ValidationException |

---

## 🎯 设计原则

### Server端设计原则（5条）

#### 1. 验方克隆功能（数据复用）
- **核心思想**: 复制验方及药材配置，生成新验方，支持个性化调整
- **优势**: 减少重复录入工作（复制验方比重新创建快10倍），支持验方库积累
- **实现**: CloneFormulaAsync复制验方及HerbItems，名称添加"_副本"后缀

#### 2. 智能药材匹配（容错机制）
- **核心思想**: Excel导入时支持精确匹配和别名模糊匹配（如"当归"可匹配"当归头"、"当归尾"）
- **优势**: 降低导入失败率，支持中医药材别名（提升用户体验）
- **实现**: TryMatchHerbAsync先精确匹配，失败后尝试别名模糊匹配

#### 3. 验方验证机制（数据完整性）
- **核心思想**: 定期检查验方中的药材是否被删除或禁用，维护数据完整性
- **优势**: 及时发现数据问题，防止使用无效药材的验方创建处方
- **实现**: ValidateFormulaHerbAsync检查HerbItems中的药材是否存在且未被删除

#### 4. 共享验方管理（知识共享）
- **核心思想**: 验方标记为"共享"后，其他医生可见，支持团队知识共享
- **优势**: 积累团队验方库，新医生可学习前辈经验方
- **实现**: IsShared标志 + GetSharedFormulasAsync查询

#### 5. 验方分类管理（组织结构）
- **核心思想**: 按验方分类管理（补益方、清热方、解表方等），提高查询效率
- **优势**: 快速定位常用验方，减少搜索时间
- **实现**: Category字段 + GetByCategoryAsync查询

### Client端设计原则（7条）

#### 1. MVVM架构与数据绑定
- **核心原则**: ViewModel封装业务逻辑，View通过DataBinding绑定属性，Repository封装数据访问
- **优势**: UI与业务逻辑分离，易于单元测试，ObservableCollection自动触发UI更新
- **反模式**: ViewModel直接操作UI控件、View CodeBehind包含业务逻辑

#### 2. Components辅助类与职责分离
- **核心原则**: 将复杂逻辑拆分为4个辅助类（计算器、命令处理器、数据管理器、验证器）
- **优势**: 降低ViewModel复杂度（FormulaDetailViewModel从1000+行降至675行），提高代码复用性
- **示例**: FormulaCalculator（总价计算）、FormulaValidator（必填项验证）

#### 3. Repository模式与三层架构
- **核心原则**: ViewModel → IFormulaRepository → FormulaRepository → ApiService → HTTP → Server
- **优势**: ViewModel不关心数据来源，Repository返回裸类型（无Result<T>包装），易于切换数据源
- **反模式**: ViewModel直接调用HttpClient、Repository返回Result<T>

#### 4. 验方克隆功能与数据复用
- **核心原则**: 复制验方及药材配置，生成新验方，支持个性化调整
- **优势**: 减少重复录入工作，支持验方库积累
- **实现**: CloneFormulaAsync复制验方后导航到编辑页

#### 5. 待验证验方管理与数据完整性
- **核心原则**: 定期检查验方中的药材是否被删除或禁用，维护数据完整性
- **优势**: 及时发现数据问题，提高处方质量
- **实现**: FormulaValidationViewModel管理待验证列表

#### 6. Excel导入导出与智能药材匹配
- **核心原则**: 支持批量导入导出验方，Server端智能匹配药材（精确+别名）
- **优势**: 快速批量录入（从Excel导入比手动录入快100倍），降低学习成本
- **实现**: ExecuteImportFormulasAsync上传文件 + Server端TryMatchHerbAsync智能匹配

#### 7. 异步优先与UI响应性
- **核心原则**: 所有数据操作使用async/await，IsBusy模式管理Loading状态
- **优势**: UI始终保持响应，防止重复提交，用户体验更好
- **反模式**: 同步阻塞方法（Task.Wait）、未设置IsBusy导致重复提交

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

### Client端技术栈

| 类别 | 技术 | 版本 | 用途 |
|------|------|------|------|
| **核心框架** | .NET & WPF | 8.0 | Windows桌面应用 |
| **MVVM框架** | Prism.DryIoc | 9.0.x | 模块化、区域导航、命令、事件 |
| **UI组件库** | MaterialDesignThemes | 5.1.x | Material Design风格UI |
| **数据绑定** | ObservableCollection | .NET 8.0 | 集合变更通知 |
| **命令模式** | AsyncDelegateCommand | Prism 8.x | 异步命令 |
| **HTTP通信** | IApiService | 自定义 | HTTP通信封装（返回裸类型） |

---

## 🚀 快速开始

### Server端集成

```csharp
// 1. 注册验方模块（Startup.cs）
services.AddFormulaModule();

// 2. API Controller集成（FormulasController）
[ApiController]
[Route("api/v1/[controller]")]
public class FormulasController : ControllerBase
{
    private readonly IFormulaService _formulaService;

    // 克隆验方
    [HttpPost("{id}/clone")]
    public async Task<IActionResult> CloneFormula(Guid id, [FromQuery] string newName)
    {
        var formulaDto = await _formulaService.CloneFormulaAsync(id, newName);
        return Ok(formulaDto);
    }

    // Excel导入验方
    [HttpPost("import")]
    public async Task<IActionResult> ImportFormulas(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var result = await _formulaService.ImportFromExcelAsync(stream);

        return Ok(new
        {
            SuccessCount = result.Succeeded.Count,
            FailedCount = result.Failed.Count,
            Errors = result.Failed
        });
    }

    // 获取待验证验方
    [HttpGet("pending-validation")]
    public async Task<IActionResult> GetPendingValidationFormulas()
    {
        var formulas = await _formulaService.GetPendingValidationFormulasAsync();
        return Ok(formulas);
    }
}
```

### Client端集成

```csharp
// 1. Shell加载Formula模块（App.xaml.cs）
moduleCatalog.AddModule<FormulaModule>(InitializationMode.WhenAvailable);

// 2. FormulaModule注册（FormulaModule.cs）
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册ViewModels (6个)
    containerRegistry.Register<FormulaManagementViewModel>();
    containerRegistry.Register<FormulaDetailViewModel>();

    // 注册辅助类 (4个Components)
    containerRegistry.RegisterSingleton<FormulaCalculator>();
    containerRegistry.Register<FormulaValidator>();

    // 注册Views (5个)
    containerRegistry.RegisterForNavigation<FormulaManagementView>();
    containerRegistry.RegisterForNavigation<FormulaDetailView>();

    // 注册Repository
    containerRegistry.Register<IFormulaRepository, FormulaRepository>();
}

// 3. FormulaManagementView.xaml数据绑定
<DataGrid ItemsSource="{Binding Formulas}"
          SelectedItem="{Binding SelectedFormula}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="验方名称" Binding="{Binding Name}" />
        <DataGridTextColumn Header="分类" Binding="{Binding Category}" />
        <DataGridTextColumn Header="功效" Binding="{Binding Effect}" />
        <DataGridTextColumn Header="药材数量" Binding="{Binding HerbCount}" />
        <DataGridTextColumn Header="总价" Binding="{Binding TotalPrice, StringFormat=¥{0:F2}}" />
        <DataGridCheckBoxColumn Header="共享" Binding="{Binding IsShared}" />
    </DataGrid.Columns>
</DataGrid>
```

---

## 📚 相关文档

- **完整模块文档**: [docs/reference/modules/formula/](../../../../docs/reference/modules/formula/)
- **Server端架构设计**: [docs/architecture/server/README.md](../../../../docs/architecture/server/README.md)
- **Client端架构设计**: [docs/architecture/client/README.md](../../../../docs/architecture/client/README.md)
- **三层对齐架构**: [docs/architecture/README.md](../../../../docs/architecture/README.md)

---

**最后更新**: 2025-10-29
**维护负责**: Server端开发组 + Client端开发组
