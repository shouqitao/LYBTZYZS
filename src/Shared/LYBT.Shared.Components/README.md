# LYBT.Shared.Components - 跨端共享组件库

## 📦 项目定位

- **层级**:Shared
- **类型**:跨端共享库(组件层)
- **职责**:提供Server端和Client端通用的业务组件,实现跨端代码复用。专注于中药处方计算和验证逻辑,确保Client端(Desktop/Avalonia)和Server端(WebAPI)使用相同的计算规则和验证规则,避免业务逻辑重复实现。

## 📂 代码结构

```
LYBT.Shared.Components/
├── IHerbItem.cs                # 中药项接口(6个属性)
│   ├── HerbId                  # 中药ID
│   ├── HerbName                # 中药名称
│   ├── Dosage                  # 剂量(单剂)
│   ├── Unit                    # 剂量单位(克/g/两等)
│   ├── Quantity                # 数量(剂数)
│   └── UnitPrice               # 单价
├── HerbCalculatorBase.cs       # 中药计算器基类(8个方法)
│   ├── CalculateTotalDosage()  # 计算总剂量
│   ├── CalculateTotalWeight()  # 计算总重量
│   ├── CalculateItemRatio()    # 计算药材比例
│   ├── CalculateTotalPrice()   # 计算总价
│   ├── CalculateEstimatedTotalPrice() # 计算估算总价
│   ├── ValidateDosageReasonableness() # 验证剂量合理性
│   ├── CalculateStandardDeviation()   # 计算标准差(剂量均衡性)
│   └── ConvertToGrams()        # 单位转换为克
└── HerbValidatorBase.cs        # 中药验证器基类(7个方法)
    ├── GetDuplicateHerbs()     # 获取重复药材列表
    ├── HasDuplicateHerbs()     # 检查是否有重复药材
    ├── IsValidDosage()         # 验证剂量是否有效
    ├── GetDosageWarning()      # 获取剂量警告信息
    ├── ValidateRequiredFields() # 验证必填字段
    ├── ValidateHerbListNotEmpty() # 验证药材列表非空
    └── ValidateHerbList()      # 综合验证药材列表
```

**说明**:
- **IHerbItem**:中药项的通用接口,DTO和ViewModel都可以实现此接口
- **HerbCalculatorBase**:泛型基类,支持任何实现IHerbItem的类型进行计算
- **HerbValidatorBase**:泛型基类,支持任何实现IHerbItem的类型进行验证

### 核心设计模式

**泛型约束设计**:
```csharp
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    // 支持任何实现IHerbItem的类型
}

public abstract class HerbValidatorBase<T> where T : IHerbItem
{
    // 支持任何实现IHerbItem的类型
}
```

**跨端复用场景**:
- **Server端**:使用`HerbCalculatorBase<PrescriptionItemDto>`计算处方总价
- **Client端**:使用`HerbCalculatorBase<PrescriptionItemViewModel>`实时验证用户输入

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Shared.Models** - 共享DTO模型(ValidationResult等类型)

### 被依赖项目
1. **LYBT.Module.Prescriptions** - Server端处方模块使用计算器和验证器
2. **LYBT.Desktop.Prescriptions** - Desktop端处方模块使用计算器和验证器
3. **LYBT.Avalonia.Prescriptions** - Avalonia端处方模块使用计算器和验证器

### NuGet包
- **无外部包依赖** - 纯.NET 8标准库,无第三方依赖

## 🛠 技术栈

- **.NET 8**:基础框架
- **泛型编程**:通过泛型约束实现类型安全的跨端复用
- **接口隔离原则(ISP)**:IHerbItem定义最小接口契约
- **策略模式**:计算器和验证器可继承扩展不同策略

## 🚀 快速开始

此项目是一个类库,无法独立运行。

```bash
# 构建此项目
dotnet build src/Shared/LYBT.Shared.Components/LYBT.Shared.Components.csproj
```

**集成说明**:

### 1. 实现IHerbItem接口(DTO或ViewModel)
```csharp
using LYBT.Shared.Components;

// Server端DTO实现
public class PrescriptionItemDto : IHerbItem
{
    public int HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public decimal Dosage { get; set; }
    public string Unit { get; set; } = "g";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// Client端ViewModel实现
public class PrescriptionItemViewModel : ViewModelBase, IHerbItem
{
    private decimal _dosage;
    public decimal Dosage
    {
        get => _dosage;
        set => SetProperty(ref _dosage, value);
    }
    // 实现其他属性...
}
```

### 2. 使用HerbCalculatorBase(计算处方总价)
```csharp
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemDto>
{
    public decimal CalculatePrescriptionTotal(List<PrescriptionItemDto> items)
    {
        // 计算总剂量
        var totalDosage = CalculateTotalDosage(items);

        // 计算总价
        var totalPrice = CalculateTotalPrice(items);

        // 计算估算总价(考虑数量)
        var estimatedTotal = CalculateEstimatedTotalPrice(items);

        return estimatedTotal;
    }
}
```

### 3. 使用HerbValidatorBase(验证处方合规性)
```csharp
public class PrescriptionValidator : HerbValidatorBase<PrescriptionItemDto>
{
    public ValidationResult ValidatePrescription(List<PrescriptionItemDto> items)
    {
        // 检查列表非空
        var emptyResult = ValidateHerbListNotEmpty(items);
        if (!emptyResult.IsValid)
            return emptyResult;

        // 检查重复药材
        if (HasDuplicateHerbs(items))
        {
            var duplicates = GetDuplicateHerbs(items);
            return ValidationResult.Fail($"发现重复药材:{string.Join(",", duplicates)}");
        }

        // 检查剂量合理性
        foreach (var item in items)
        {
            if (!IsValidDosage(item.Dosage))
            {
                var warning = GetDosageWarning(item.Dosage);
                return ValidationResult.Fail($"{item.HerbName} 剂量异常:{warning}");
            }
        }

        // 综合验证
        return ValidateHerbList(items);
    }
}
```

### 4. Client端实时验证示例(WPF + MVVM)
```csharp
public class PrescriptionViewModel : ViewModelBase
{
    private readonly PrescriptionValidator _validator = new();
    private readonly PrescriptionCalculator _calculator = new();

    private ObservableCollection<PrescriptionItemViewModel> _items;
    public ObservableCollection<PrescriptionItemViewModel> Items
    {
        get => _items;
        set
        {
            SetProperty(ref _items, value);
            ValidateAndCalculate(); // 数据变化时自动验证计算
        }
    }

    private decimal _totalPrice;
    public decimal TotalPrice
    {
        get => _totalPrice;
        set => SetProperty(ref _totalPrice, value);
    }

    private void ValidateAndCalculate()
    {
        // 实时验证
        var result = _validator.ValidatePrescription(Items.ToList());
        HasError = !result.IsValid;
        ErrorMessage = result.ErrorMessage;

        // 实时计算
        if (result.IsValid)
        {
            TotalPrice = _calculator.CalculatePrescriptionTotal(Items.ToList());
        }
    }
}
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/components/](../../../docs/reference/modules/components/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/shared/components-design.md](../../../docs/explanation/architecture/shared/components-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/shared/components-usage.md](../../../docs/how-to-guides/shared/components-usage.md) *(待创建)*
- **跨端架构**:[docs/explanation/architecture/shared/README.md](../../../docs/explanation/architecture/shared/README.md) - 参见"跨端共享原则"章节

---

**最后更新**:2025-10-29
**维护负责**:Shared层开发组
