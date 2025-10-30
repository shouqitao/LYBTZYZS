# Shared层组件使用指南

## 📋 概述

本文档提供LYBT系统**共享组件库（LYBT.Shared.Components）**的完整使用指南，涵盖跨端业务组件的集成、使用和扩展方法。

**适用场景**：
- ✅ 开发Server端业务服务（Prescriptions/Formula模块）
- ✅ 开发Client端业务模块（Desktop/Avalonia Prescriptions/Formula模块）
- ✅ 实现跨端复用的计算和验证逻辑
- ✅ 确保Client端和Server端使用相同的业务规则

**核心优势**：
- **跨端复用**：一次编写，Server端和Client端同时使用
- **类型安全**：泛型约束确保编译时类型检查
- **易于扩展**：基于继承扩展特定模块的计算和验证逻辑
- **零依赖**：纯.NET 8标准库，无第三方依赖

**参考文档**：
- 架构设计：`docs/explanation/architecture/shared/components-design.md`
- 项目README：`src/Shared/LYBT.Shared.Components/README.md`
- Server端对齐：`docs/how-to-guides/server/prescriptions-development.md`
- Client端对齐：`docs/how-to-guides/client/prescriptions-development.md`

---

## 🔧 前置条件

### 1. 开发环境

**必需工具**：
```
- Visual Studio 2022（17.8+）
- .NET 8.0 SDK
```

### 2. 核心依赖

**NuGet包引用**：
```xml
<!-- 在Server端或Client端项目中添加引用 -->
<ProjectReference Include="..\..\Shared\LYBT.Shared.Components\LYBT.Shared.Components.csproj" />
```

**无外部包依赖**：LYBT.Shared.Components项目是纯.NET 8标准库，无第三方NuGet包依赖。

### 3. 核心组件理解

**LYBT.Shared.Components提供的核心组件**：

| 组件 | 类型 | 用途 |
|-----|------|------|
| **IHerbItem** | 接口 | 定义药材项目的6个核心属性 |
| **HerbCalculatorBase<T>** | 泛型基类 | 提供剂量计算、价格计算、用量分析 |
| **HerbValidatorBase<T>** | 泛型基类 | 提供重复检测、剂量验证、必填项验证 |
| **ValidationResult** | 结果类 | 封装验证错误和警告信息 |

---

## 📦 IHerbItem接口实现

### 2.1 接口定义

**IHerbItem接口（6个属性）**：
```csharp
namespace LYBT.Shared.Components
{
    /// <summary>
    /// 药材项目接口 - 用于共享组件的泛型约束
    /// </summary>
    public interface IHerbItem
    {
        Guid HerbId { get; }        // 药材ID
        string HerbName { get; }    // 药材名称
        decimal Dosage { get; }     // 剂量
        string Unit { get; }        // 单位
        decimal Quantity { get; }   // 数量（克重）
        decimal UnitPrice { get; }  // 单价
    }
}
```

### 2.2 Server端实现（DTO）

**PrescriptionItemDto示例**：
```csharp
using LYBT.Shared.Components;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    public class PrescriptionItemDto : IHerbItem
    {
        public Guid Id { get; set; }

        // ✅ 实现IHerbItem接口
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Dosage { get; set; }
        public string Unit { get; set; } = "g";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // DTO特有的其他属性
        public Guid PrescriptionId { get; set; }
        public int SortOrder { get; set; }
        public string? Usage { get; set; }
        public string? Notes { get; set; }
    }
}
```

### 2.3 Client端实现（ViewModel）

**PrescriptionItemViewModel示例**：
```csharp
using LYBT.Shared.Components;
using LYBT.Desktop.Foundation.ViewModels;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    public class PrescriptionItemViewModel : ViewModelBase, IHerbItem
    {
        private Guid _herbId;
        private string _herbName = string.Empty;
        private decimal _dosage;
        private string _unit = "g";
        private decimal _quantity;
        private decimal _unitPrice;

        // ✅ 实现IHerbItem接口（带数据绑定）
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        public decimal Dosage
        {
            get => _dosage;
            set
            {
                if (SetProperty(ref _dosage, value))
                {
                    // ✅ 剂量变化时自动触发计算
                    RaisePropertyChanged(nameof(SubtotalPrice));
                }
            }
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    RaisePropertyChanged(nameof(SubtotalPrice));
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    RaisePropertyChanged(nameof(SubtotalPrice));
                }
            }
        }

        // ViewModel特有的计算属性
        public decimal SubtotalPrice => Quantity * UnitPrice;
    }
}
```

**关键点**：
- ✅ DTO实现IHerbItem（自动属性）
- ✅ ViewModel实现IHerbItem（属性通知）
- ✅ 两者都可以传递给HerbCalculatorBase和HerbValidatorBase

---

## 🧮 HerbCalculatorBase使用

### 3.1 基类能力概览

**HerbCalculatorBase<T>提供的方法**：

| 方法 | 返回类型 | 用途 |
|-----|---------|------|
| `CalculateTotalDosage()` | decimal | 计算总剂量 |
| `CalculateTotalWeight()` | decimal | 计算总重量（转换为克） |
| `CalculateItemRatio()` | decimal | 计算单味药在配方中的比例 |
| `CalculateTotalPrice()` | decimal | 计算总价 |
| `CalculateEstimatedTotalPrice()` | decimal | 计算估算总价（基于价格字典） |
| `ValidateDosageReasonableness()` | List<string> | 验证剂量合理性（返回警告列表） |
| `CalculateStandardDeviation()` | decimal | 计算标准差（用量均衡性） |
| `ConvertToGrams()` | decimal | 单位转换为克（支持kg/g/mg/钱/两） |

### 3.2 Client端使用（Desktop端）

**PrescriptionCalculator示例**：
```csharp
using LYBT.Shared.Components;

namespace LYBT.Desktop.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方计算器 - 继承共享基类
    /// </summary>
    public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemViewModel>
    {
        /// <summary>
        /// 计算处方价格详情
        /// </summary>
        public CalculationResult CalculatePrescriptionPrice(
            IEnumerable<PrescriptionItemViewModel> items,
            int dosageCount = 1,
            decimal discount = 1.0m)
        {
            if (items == null || !items.Any())
            {
                return new CalculationResult { IsValid = false, ErrorMessage = "处方项目为空" };
            }

            var itemList = items.ToList();

            // ✅ 调用基类方法计算总价
            var singleDosagePrice = CalculateTotalPrice(itemList);
            var totalPrice = singleDosagePrice * dosageCount;
            var discountedPrice = totalPrice * discount;

            return new CalculationResult
            {
                SingleDosagePrice = singleDosagePrice,
                TotalPrice = totalPrice,
                DiscountedPrice = discountedPrice,
                TotalSaved = totalPrice - discountedPrice,
                ItemCount = itemList.Count,
                IsValid = true
            };
        }

        /// <summary>
        /// 分析处方用量分布
        /// </summary>
        public PrescriptionDosageAnalysis AnalyzeDosageDistribution(IEnumerable<PrescriptionItemViewModel> items)
        {
            if (items == null || !items.Any())
            {
                return new PrescriptionDosageAnalysis();
            }

            var dosages = items.Select(i => i.Dosage).ToList();

            return new PrescriptionDosageAnalysis
            {
                TotalItems = dosages.Count,
                MinDosage = dosages.Min(),
                MaxDosage = dosages.Max(),
                AverageDosage = dosages.Average(),
                TotalDosage = dosages.Sum(),
                // ✅ 调用基类的protected方法
                StandardDeviation = CalculateStandardDeviation(dosages)
            };
        }
    }
}
```

**在ViewModel中使用**：
```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase
{
    private readonly PrescriptionCalculator _calculator = new();

    private decimal _totalPrice;
    public decimal TotalPrice
    {
        get => _totalPrice;
        set => SetProperty(ref _totalPrice, value);
    }

    public ObservableCollection<PrescriptionItemViewModel> Items { get; }

    public DelegateCommand CalculateCommand { get; }

    public PrescriptionEditorViewModel()
    {
        Items = new ObservableCollection<PrescriptionItemViewModel>();
        Items.CollectionChanged += (s, e) => RecalculatePrice();

        CalculateCommand = new DelegateCommand(RecalculatePrice);
    }

    private void RecalculatePrice()
    {
        var result = _calculator.CalculatePrescriptionPrice(Items, dosageCount: 7);
        TotalPrice = result.DiscountedPrice;
    }
}
```

### 3.3 Server端使用（Service层）

**PrescriptionService示例**：
```csharp
using LYBT.Shared.Components;

namespace LYBT.Module.Prescriptions.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly PrescriptionCalculator _calculator = new();

        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(CreatePrescriptionRequest request)
        {
            // 计算处方总价
            var calculationResult = _calculator.CalculatePrescriptionPrice(
                request.Items,
                request.DosageCount,
                request.Discount);

            if (!calculationResult.IsValid)
            {
                return ServiceResult<PrescriptionDto>.Fail(calculationResult.ErrorMessage);
            }

            var prescription = new Prescription
            {
                TotalPrice = calculationResult.TotalPrice,
                DiscountedPrice = calculationResult.DiscountedPrice
            };

            await _repository.AddAsync(prescription);
            return ServiceResult<PrescriptionDto>.Success(prescription.ToDto());
        }
    }
}
```

### 3.4 单位转换示例

**ConvertToGrams()支持的单位**：
```csharp
// ✅ 基类自动处理单位转换
var calculator = new PrescriptionCalculator();

// kg → g
var grams1 = calculator.ConvertToGrams(1, "kg");      // 1000g

// g → g
var grams2 = calculator.ConvertToGrams(10, "g");      // 10g

// mg → g
var grams3 = calculator.ConvertToGrams(500, "mg");    // 0.5g

// 钱 → g（1钱 = 3.125克）
var grams4 = calculator.ConvertToGrams(3, "钱");      // 9.375g

// 两 → g（1两 = 31.25克）
var grams5 = calculator.ConvertToGrams(1, "两");      // 31.25g
```

---

## ✅ HerbValidatorBase使用

### 4.1 基类能力概览

**HerbValidatorBase<T>提供的方法**：

| 方法 | 返回类型 | 用途 |
|-----|---------|------|
| `GetDuplicateHerbs()` | List<string> | 获取重复药材名称列表 |
| `HasDuplicateHerbs()` | bool | 检查是否存在重复药材 |
| `IsValidDosage()` | bool | 验证剂量是否在合理范围内 |
| `GetDosageWarning()` | string? | 获取剂量警告信息 |
| `ValidateRequiredFields()` | ValidationResult | 验证单个项目的必填字段 |
| `ValidateHerbListNotEmpty()` | ValidationResult | 验证药材列表非空 |
| `ValidateHerbList()` | ValidationResult | 综合验证药材列表 |

**ValidationResult类**：
```csharp
public class ValidationResult
{
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();

    public void AddError(string error);
    public void AddWarning(string warning);
    public void Merge(ValidationResult other);
    public string GetErrorSummary();
    public string GetWarningSummary();
}
```

### 4.2 Client端使用（Desktop端）

**PrescriptionValidator示例**：
```csharp
using LYBT.Shared.Components;

namespace LYBT.Desktop.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方验证器 - 继承共享基类
    /// </summary>
    public class PrescriptionValidator : HerbValidatorBase<PrescriptionItemViewModel>
    {
        /// <summary>
        /// 验证处方基本信息
        /// </summary>
        public ValidationResult ValidateBasicInfo(string prescriptionNumber, Guid patientId, string doctorName)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(prescriptionNumber))
            {
                result.AddError("处方编号不能为空");
            }

            if (patientId == Guid.Empty)
            {
                result.AddError("患者信息不能为空");
            }

            if (string.IsNullOrWhiteSpace(doctorName))
            {
                result.AddError("医生信息不能为空");
            }

            return result;
        }

        /// <summary>
        /// 验证处方项目列表
        /// </summary>
        public ValidationResult ValidatePrescriptionItems(IEnumerable<PrescriptionItemViewModel> items)
        {
            // ✅ 调用基类的ValidateHerbList方法（包含重复检测和必填项验证）
            return ValidateHerbList(items, "处方");
        }

        /// <summary>
        /// 验证药材相互作用（处方特有）
        /// </summary>
        public ValidationResult ValidateHerbInteractions(IEnumerable<PrescriptionItemViewModel> items)
        {
            var result = new ValidationResult();

            if (items == null || !items.Any())
            {
                return result;
            }

            var herbNames = items.Select(i => i.HerbName).ToList();

            // 配伍禁忌检查（简化实现）
            var knownContraindications = GetKnownContraindications();

            foreach (var contraindication in knownContraindications)
            {
                if (herbNames.Contains(contraindication.Herb1) && herbNames.Contains(contraindication.Herb2))
                {
                    result.AddWarning($"注意：{contraindication.Herb1} 与 {contraindication.Herb2} 可能存在配伍禁忌");
                }
            }

            return result;
        }

        private List<HerbContraindication> GetKnownContraindications()
        {
            // 简化实现，实际应该从数据库读取
            return new List<HerbContraindication>
            {
                new("甘草", "甘遂"),
                new("甘草", "大戟"),
                new("乌头", "半夏"),
                new("藜芦", "人参")
            };
        }
    }

    public record HerbContraindication(string Herb1, string Herb2);
}
```

**在ViewModel中实时验证**：
```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase
{
    private readonly PrescriptionValidator _validator = new();

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private string _warningMessage = string.Empty;
    public string WarningMessage
    {
        get => _warningMessage;
        set => SetProperty(ref _warningMessage, value);
    }

    public ObservableCollection<PrescriptionItemViewModel> Items { get; }

    public PrescriptionEditorViewModel()
    {
        Items = new ObservableCollection<PrescriptionItemViewModel>();
        Items.CollectionChanged += (s, e) => ValidateItems();
    }

    private void ValidateItems()
    {
        // ✅ 实时验证
        var result = _validator.ValidatePrescriptionItems(Items);

        ErrorMessage = result.IsValid ? string.Empty : result.GetErrorSummary();
        WarningMessage = result.HasWarnings ? result.GetWarningSummary() : string.Empty;

        // 更新命令的CanExecute状态
        SaveCommand.RaiseCanExecuteChanged();
    }

    public DelegateCommand SaveCommand { get; }

    private bool CanExecuteSave()
    {
        // ✅ 无错误时才允许保存
        var result = _validator.ValidatePrescriptionItems(Items);
        return result.IsValid;
    }
}
```

### 4.3 Server端使用（Service层）

**PrescriptionService验证示例**：
```csharp
namespace LYBT.Module.Prescriptions.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly PrescriptionValidator _validator = new();

        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(CreatePrescriptionRequest request)
        {
            // ✅ Server端验证
            var validationResult = _validator.ValidatePrescriptionItems(request.Items);

            if (!validationResult.IsValid)
            {
                return ServiceResult<PrescriptionDto>.Fail(validationResult.GetErrorSummary());
            }

            // ✅ 有警告时记录日志
            if (validationResult.HasWarnings)
            {
                _logger.LogWarning("处方创建警告：{Warnings}", validationResult.GetWarningSummary());
            }

            // 继续保存逻辑...
        }
    }
}
```

---

## 🖥️ Client端集成（Desktop/Avalonia）

### 5.1 Desktop端完整示例

**PrescriptionEditorViewModel完整代码**：
```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase, INavigationAware
{
    private readonly PrescriptionCalculator _calculator = new();
    private readonly PrescriptionValidator _validator = new();
    private readonly IPrescriptionRepository _repository;

    public ObservableCollection<PrescriptionItemViewModel> Items { get; }

    private decimal _totalPrice;
    public decimal TotalPrice
    {
        get => _totalPrice;
        set => SetProperty(ref _totalPrice, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public DelegateCommand AddItemCommand { get; }
    public DelegateCommand<PrescriptionItemViewModel> RemoveItemCommand { get; }
    public DelegateCommand SaveCommand { get; }

    public PrescriptionEditorViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IPrescriptionRepository repository,
        ILogger<PrescriptionEditorViewModel> logger)
        : base(regionManager, eventAggregator, logger)
    {
        _repository = repository;
        Items = new ObservableCollection<PrescriptionItemViewModel>();
        Items.CollectionChanged += OnItemsChanged;

        AddItemCommand = new DelegateCommand(ExecuteAddItem);
        RemoveItemCommand = new DelegateCommand<PrescriptionItemViewModel>(ExecuteRemoveItem);
        SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave);
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ValidateAndCalculate();
    }

    private void ValidateAndCalculate()
    {
        // ✅ 实时验证
        var validationResult = _validator.ValidatePrescriptionItems(Items);
        ErrorMessage = validationResult.IsValid ? string.Empty : validationResult.GetErrorSummary();

        // ✅ 实时计算
        if (validationResult.IsValid)
        {
            var calcResult = _calculator.CalculatePrescriptionPrice(Items, dosageCount: 7);
            TotalPrice = calcResult.DiscountedPrice;
        }
        else
        {
            TotalPrice = 0;
        }

        SaveCommand.RaiseCanExecuteChanged();
    }

    private bool CanExecuteSave()
    {
        var result = _validator.ValidatePrescriptionItems(Items);
        return result.IsValid;
    }

    private async Task ExecuteSaveAsync()
    {
        try
        {
            // 保存逻辑...
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存处方失败");
        }
    }
}
```

### 5.2 Avalonia端使用（相同方式）

**Avalonia端与Desktop端使用方式完全相同**：
```csharp
// Avalonia.Desktop.Prescriptions模块
namespace LYBT.Avalonia.Prescriptions.ViewModels
{
    // ✅ 代码完全相同，只是命名空间不同
    public class PrescriptionEditorViewModel : UnifiedViewModelBase
    {
        private readonly PrescriptionCalculator _calculator = new();
        private readonly PrescriptionValidator _validator = new();

        // 完全相同的实现...
    }
}
```

---

## 🌐 Server端集成

### 6.1 Service层使用

**PrescriptionService完整示例**：
```csharp
using LYBT.Shared.Components;

namespace LYBT.Module.Prescriptions.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly PrescriptionCalculator _calculator = new();
        private readonly PrescriptionValidator _validator = new();
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repository,
            ILogger<PrescriptionService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(CreatePrescriptionRequest request)
        {
            // ✅ Step 1: 验证
            var validationResult = _validator.ValidatePrescriptionItems(request.Items);
            if (!validationResult.IsValid)
            {
                return ServiceResult<PrescriptionDto>.Fail(validationResult.GetErrorSummary());
            }

            // ✅ Step 2: 记录警告
            if (validationResult.HasWarnings)
            {
                _logger.LogWarning("处方创建警告：{Warnings}", validationResult.GetWarningSummary());
            }

            // ✅ Step 3: 计算价格
            var calculationResult = _calculator.CalculatePrescriptionPrice(
                request.Items,
                request.DosageCount,
                request.Discount);

            if (!calculationResult.IsValid)
            {
                return ServiceResult<PrescriptionDto>.Fail(calculationResult.ErrorMessage);
            }

            // ✅ Step 4: 创建实体
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                PrescriptionNumber = GeneratePrescriptionNumber(),
                PatientId = request.PatientId,
                DosageCount = request.DosageCount,
                TotalPrice = calculationResult.TotalPrice,
                DiscountedPrice = calculationResult.DiscountedPrice,
                CreatedAt = DateTime.UtcNow
            };

            // ✅ Step 5: 保存到数据库
            await _repository.AddAsync(prescription);

            return ServiceResult<PrescriptionDto>.Success(prescription.ToDto());
        }
    }
}
```

### 6.2 API Controller中使用

**PrescriptionsController示例**：
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class PrescriptionsController : BaseApiController
{
    private readonly IPrescriptionService _service;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Create([FromBody] CreatePrescriptionRequest request)
    {
        try
        {
            // ✅ Service层已经使用共享组件进行验证和计算
            var result = await _service.CreateAsync(request);
            return HandleServiceResult(result);
        }
        catch (Exception ex)
        {
            return HandleException<PrescriptionDto>(ex, "创建处方");
        }
    }
}
```

---

## ✅ 最佳实践

### 7.1 跨端一致性

**确保Client端和Server端使用相同的计算逻辑**：
```csharp
// ❌ 错误：Client端和Server端各自实现
// Client端
var clientTotal = Items.Sum(i => i.Quantity * i.UnitPrice);

// Server端
var serverTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);

// ✅ 正确：使用共享的HerbCalculatorBase
// Client端和Server端都使用
var calculator = new PrescriptionCalculator();
var total = calculator.CalculateTotalPrice(items);
```

### 7.2 验证层次

**Client端和Server端的验证职责**：

| 验证类型 | Client端 | Server端 |
|---------|---------|---------|
| **实时验证** | ✅ 必需（用户体验） | ❌ 不需要 |
| **提交前验证** | ✅ 必需（防止无效请求） | ✅ 必需（安全） |
| **业务规则验证** | ✅ 使用共享组件 | ✅ 使用共享组件 |
| **权限验证** | ❌ 不需要 | ✅ 必需 |

**示例**：
```csharp
// Client端：实时验证 + 提交前验证
public class PrescriptionEditorViewModel
{
    private void OnItemsChanged()
    {
        // ✅ 实时验证（用户输入时）
        var result = _validator.ValidatePrescriptionItems(Items);
        ErrorMessage = result.GetErrorSummary();
    }

    private async Task ExecuteSaveAsync()
    {
        // ✅ 提交前验证
        var result = _validator.ValidatePrescriptionItems(Items);
        if (!result.IsValid)
        {
            MessageBox.Show(result.GetErrorSummary());
            return;
        }

        await _repository.SaveAsync(Items);
    }
}

// Server端：接收请求时验证
public class PrescriptionService
{
    public async Task<ServiceResult> CreateAsync(CreatePrescriptionRequest request)
    {
        // ✅ Server端验证（防止绕过Client端验证）
        var result = _validator.ValidatePrescriptionItems(request.Items);
        if (!result.IsValid)
        {
            return ServiceResult.Fail(result.GetErrorSummary());
        }

        // 继续保存...
    }
}
```

### 7.3 扩展自定义逻辑

**在保留基类逻辑的同时扩展特定模块的逻辑**：
```csharp
// ✅ 正确：继承基类并扩展
public class FormulaCalculator : HerbCalculatorBase<FormulaItemViewModel>
{
    // ✅ 重用基类的所有方法（CalculateTotalDosage等）

    // ✅ 扩展Formula特有的逻辑
    public FormulaComplexity AnalyzeFormulaComplexity(IEnumerable<FormulaItemViewModel> items)
    {
        var totalItems = items.Count();
        var totalDosage = CalculateTotalDosage(items); // 调用基类方法
        var stdDev = CalculateStandardDeviation(items.Select(i => i.Dosage)); // 调用基类方法

        // Formula特有的复杂度分析
        if (totalItems > 15 && stdDev > 30)
            return FormulaComplexity.VeryComplex;
        else if (totalItems > 10)
            return FormulaComplexity.Complex;
        else
            return FormulaComplexity.Simple;
    }
}
```

### 7.4 单元测试

**测试共享组件**：
```csharp
using Xunit;
using LYBT.Shared.Components;

public class PrescriptionCalculatorTests
{
    private readonly PrescriptionCalculator _calculator;

    public PrescriptionCalculatorTests()
    {
        _calculator = new PrescriptionCalculator();
    }

    [Fact]
    public void CalculateTotalPrice_ValidItems_ReturnsCorrectTotal()
    {
        // Arrange
        var items = new List<TestHerbItem>
        {
            new TestHerbItem { HerbId = Guid.NewGuid(), HerbName = "当归", Quantity = 10, UnitPrice = 0.5m },
            new TestHerbItem { HerbId = Guid.NewGuid(), HerbName = "黄芪", Quantity = 20, UnitPrice = 0.3m }
        };

        // Act
        var total = _calculator.CalculateTotalPrice(items);

        // Assert
        Assert.Equal(11.0m, total); // (10 * 0.5) + (20 * 0.3) = 11.0
    }

    // 测试用的IHerbItem实现
    private class TestHerbItem : IHerbItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Dosage { get; set; }
        public string Unit { get; set; } = "g";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
```

---

## ❓ 常见问题

### 8.1 如何处理不同单位？

**问题**：药材单位不统一（克/两/钱/kg）

**解决方案**：
```csharp
// ✅ 基类自动处理单位转换
var calculator = new PrescriptionCalculator();

var items = new List<PrescriptionItemViewModel>
{
    new() { Dosage = 10, Unit = "g" },
    new() { Dosage = 1, Unit = "两" },
    new() { Dosage = 3, Unit = "钱" }
};

// ✅ CalculateTotalWeight会自动转换所有单位为克
var totalGrams = calculator.CalculateTotalWeight(items); // 10 + 31.25 + 9.375 = 50.625g
```

### 8.2 如何扩展配伍禁忌检查？

**问题**：需要更复杂的配伍禁忌规则

**解决方案**：
```csharp
public class PrescriptionValidator : HerbValidatorBase<PrescriptionItemViewModel>
{
    private readonly IHerbContraindicationRepository _contraindicationRepo;

    public PrescriptionValidator(IHerbContraindicationRepository repo)
    {
        _contraindicationRepo = repo;
    }

    public async Task<ValidationResult> ValidateHerbInteractionsAsync(IEnumerable<PrescriptionItemViewModel> items)
    {
        var result = new ValidationResult();

        // ✅ 从数据库加载配伍禁忌规则
        var contraindications = await _contraindicationRepo.GetAllAsync();

        var herbIds = items.Select(i => i.HerbId).ToList();

        foreach (var contraindication in contraindications)
        {
            if (herbIds.Contains(contraindication.HerbId1) && herbIds.Contains(contraindication.HerbId2))
            {
                result.AddWarning($"配伍禁忌：{contraindication.Description}");
            }
        }

        return result;
    }
}
```

### 8.3 如何处理异步验证？

**问题**：验证需要访问数据库或API

**解决方案**：
```csharp
public class PrescriptionValidator : HerbValidatorBase<PrescriptionItemViewModel>
{
    private readonly IHerbRepository _herbRepo;

    public async Task<ValidationResult> ValidateHerbsExistAsync(IEnumerable<PrescriptionItemViewModel> items)
    {
        var result = new ValidationResult();

        // ✅ 先执行同步验证（基类方法）
        var basicResult = ValidateHerbList(items, "处方");
        result.Merge(basicResult);

        if (!result.IsValid) return result;

        // ✅ 再执行异步验证
        foreach (var item in items)
        {
            var herb = await _herbRepo.GetByIdAsync(item.HerbId);
            if (herb == null)
            {
                result.AddError($"药材不存在：{item.HerbName}");
            }
        }

        return result;
    }
}
```

---

## 🔧 扩展指南

### 9.1 添加新的计算方法

**在特定模块中扩展计算逻辑**：
```csharp
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemViewModel>
{
    /// <summary>
    /// 计算处方性价比
    /// </summary>
    public decimal CalculateCostEffectivenessRatio(IEnumerable<PrescriptionItemViewModel> items)
    {
        var totalPrice = CalculateTotalPrice(items); // 使用基类方法
        var totalWeight = CalculateTotalWeight(items); // 使用基类方法

        if (totalWeight == 0) return 0;
        return totalPrice / totalWeight; // 元/克
    }
}
```

### 9.2 添加新的验证规则

**扩展特定模块的验证逻辑**：
```csharp
public class PrescriptionValidator : HerbValidatorBase<PrescriptionItemViewModel>
{
    /// <summary>
    /// 验证处方适用人群
    /// </summary>
    public ValidationResult ValidateTargetPopulation(
        IEnumerable<PrescriptionItemViewModel> items,
        int patientAge,
        string patientGender)
    {
        var result = new ValidationResult();

        // ✅ 先执行基础验证
        var basicResult = ValidateHerbList(items, "处方");
        result.Merge(basicResult);

        // ✅ 扩展特定验证规则
        foreach (var item in items)
        {
            // 孕妇禁用药材检查
            if (patientGender == "Female" && IsContraindicatedForPregnancy(item.HerbName))
            {
                result.AddWarning($"{item.HerbName} 孕妇慎用");
            }

            // 儿童用药剂量检查
            if (patientAge < 12 && item.Dosage > 50)
            {
                result.AddWarning($"{item.HerbName} 儿童用药剂量过大");
            }
        }

        return result;
    }
}
```

---

## 🧪 测试指南

### 10.1 单元测试示例

**PrescriptionCalculatorTests**：
```csharp
using Xunit;
using LYBT.Shared.Components;

public class PrescriptionCalculatorTests
{
    private readonly PrescriptionCalculator _calculator;

    public PrescriptionCalculatorTests()
    {
        _calculator = new PrescriptionCalculator();
    }

    [Theory]
    [InlineData("g", 10, 10)]
    [InlineData("kg", 1, 1000)]
    [InlineData("mg", 1000, 1)]
    [InlineData("钱", 1, 3.125)]
    [InlineData("两", 1, 31.25)]
    public void ConvertToGrams_DifferentUnits_ReturnsCorrectGrams(string unit, decimal dosage, decimal expectedGrams)
    {
        // Act
        var grams = _calculator.ConvertToGrams(dosage, unit);

        // Assert
        Assert.Equal(expectedGrams, grams);
    }
}
```

**PrescriptionValidatorTests**：
```csharp
public class PrescriptionValidatorTests
{
    private readonly PrescriptionValidator _validator;

    [Fact]
    public void ValidatePrescriptionItems_DuplicateHerbs_ReturnsError()
    {
        // Arrange
        var items = new List<TestHerbItem>
        {
            new() { HerbId = Guid.NewGuid(), HerbName = "当归" },
            new() { HerbId = Guid.NewGuid(), HerbName = "当归" } // 重复
        };

        // Act
        var result = _validator.ValidatePrescriptionItems(items);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("重复药材", result.GetErrorSummary());
    }
}
```

### 10.2 集成测试示例

**PrescriptionServiceTests**：
```csharp
public class PrescriptionServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var service = new PrescriptionService(mockRepo.Object, mockLogger.Object);
        var request = new CreatePrescriptionRequest
        {
            Items = new List<PrescriptionItemDto>
            {
                new() { HerbId = Guid.NewGuid(), HerbName = "当归", Dosage = 10, Unit = "g", Quantity = 10, UnitPrice = 0.5m }
            }
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }
}
```

---

## 📚 参考资料

### 官方文档
- **.NET 8 Documentation**: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8
- **泛型编程指南**: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics

### 内部文档
- 架构设计：`docs/explanation/architecture/shared/components-design.md`
- Server端集成：`docs/how-to-guides/server/prescriptions-development.md`
- Client端集成：`docs/how-to-guides/client/prescriptions-development.md`
- 项目README：`src/Shared/LYBT.Shared.Components/README.md`

### 代码示例
- IHerbItem接口：`src/Shared/LYBT.Shared.Components/IHerbItem.cs`
- HerbCalculatorBase：`src/Shared/LYBT.Shared.Components/HerbCalculatorBase.cs`
- HerbValidatorBase：`src/Shared/LYBT.Shared.Components/HerbValidatorBase.cs`
- Prescription实现：`src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/Components/`
- Formula实现：`src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/`

---

**最后更新**: 2025-10-30
**维护负责**: Shared层开发组
**文档版本**: v1.0.0
