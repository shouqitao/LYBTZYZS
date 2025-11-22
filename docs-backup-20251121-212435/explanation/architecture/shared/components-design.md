# LYBT.Shared.Components - 跨端共享组件架构设计

## 📋 文档信息

- **文档类型**：架构设计文档
- **目标受众**：架构师、高级开发工程师
- **最后更新**：2025-10-30
- **相关文档**：
  - 使用指南：[shared/components-usage.md](../../how-to-guides/shared/components-usage.md)
  - 项目README：[LYBT.Shared.Components/README.md](../../../src/Shared/LYBT.Shared.Components/README.md)
  - 跨端架构指南：[architecture/shared/README.md](./README.md)

---

## 1. 架构概述

### 1.1 设计定位

LYBT.Shared.Components是一个**跨端业务组件库**，专注于提供Server端和Client端共用的**中药处方计算与验证逻辑**，确保：

- **业务规则一致性**：Client端计算的价格与Server端保存的价格完全一致
- **验证规则统一**：Desktop/Avalonia/WebAPI使用相同的验证逻辑
- **代码复用最大化**：避免在Client和Server重复实现相同的业务逻辑

### 1.2 技术定位

```
层级：Shared层（跨端共享）
类型：业务组件库（非基础设施）
依赖：仅依赖 LYBT.Shared.Models（共享DTO）
被依赖：LYBT.Module.Prescriptions（Server）、LYBT.Desktop.Prescriptions（Client）
技术栈：.NET 8 + 泛型编程 + 接口隔离原则
```

### 1.3 设计动机

**问题**：Server端和Client端都需要处方计算和验证，如何避免代码重复？

**反模式**：
```
❌ 在Client和Server分别实现相同逻辑
  → PrescriptionService.cs（Server）计算总价 = ∑(单价 × 剂量 × 数量)
  → PrescriptionViewModel.cs（Client）计算总价 = ∑(单价 × 剂量 × 数量)
  → 问题：逻辑分散，维护困难，可能不一致

❌ 将Server端逻辑复制到Client端
  → PrescriptionService.cs → 复制到 → PrescriptionViewModel.cs
  → 问题：违反DRY原则，逻辑变更需要同步修改两处
```

**正确模式**（当前架构）：
```
✅ 抽象公共接口 + 泛型基类，跨端复用
  → IHerbItem接口：定义药材项的最小契约（6个属性）
  → HerbCalculatorBase<T>：泛型计算器基类（8个计算方法）
  → HerbValidatorBase<T>：泛型验证器基类（7个验证方法）
  → Server端：HerbCalculatorBase<PrescriptionItemDto>
  → Client端：HerbCalculatorBase<PrescriptionItemViewModel>
  → 效果：业务逻辑集中在Shared层，Client/Server只需继承扩展
```

---

## 2. 设计目标与原则

### 2.1 核心设计目标

| 目标 | 描述 | 实现方式 |
|------|------|---------|
| **跨端一致性** | Client和Server计算/验证结果完全一致 | 泛型基类统一实现核心逻辑 |
| **类型安全** | 编译期保证类型正确性 | 泛型约束 `where T : IHerbItem` |
| **高复用性** | 核心逻辑只写一次，多处使用 | Template Method模式 + 继承扩展 |
| **低耦合性** | Shared层不依赖Client/Server具体类型 | 接口隔离原则（IHerbItem） |
| **可扩展性** | 支持模块特定的计算/验证逻辑 | 提供protected虚方法供子类重写 |

### 2.2 设计原则遵循

**SOLID原则**：
- **S（单一职责）**：IHerbItem只定义药材项属性，Calculator负责计算，Validator负责验证
- **O（开闭原则）**：基类提供默认实现，子类可扩展但不修改基类
- **L（里氏替换）**：任何使用`HerbCalculatorBase<T>`的地方都可以替换为其子类
- **I（接口隔离）**：IHerbItem只包含6个必需属性，不包含无关方法
- **D（依赖倒置）**：基类依赖IHerbItem接口，而非具体DTO或ViewModel

**DRY原则**：
- 剂量合理性验证逻辑只在`HerbValidatorBase.IsValidDosage()`实现一次
- 总价计算逻辑只在`HerbCalculatorBase.CalculateTotalPrice()`实现一次

**KISS原则**：
- IHerbItem只定义6个属性，避免过度抽象
- 不使用复杂的设计模式（如工厂、策略），直接继承即可

---

## 3. 泛型约束设计模式

### 3.1 核心设计模式

**模式名称**：泛型约束 + 模板方法（Generic Constraint + Template Method）

**结构图**：
```
┌─────────────────────────────────────┐
│          IHerbItem                  │  ← 接口（契约）
│  - HerbId: Guid                     │
│  - HerbName: string                 │
│  - Dosage: decimal                  │
│  - Unit: string                     │
│  - Quantity: decimal                │
│  - UnitPrice: decimal               │
└─────────────────────────────────────┘
               ▲
               │ implements
               │
     ┌─────────┴─────────┐
     │                   │
┌────────────┐    ┌─────────────┐
│DTO (Server)│    │ViewModel    │
│            │    │  (Client)   │
└────────────┘    └─────────────┘
     ▲                   ▲
     │                   │
     │ used by           │ used by
     │                   │
┌────────────────────────────────────┐
│  HerbCalculatorBase<T>             │  ← 泛型基类
│    where T : IHerbItem             │
│                                    │
│  + CalculateTotalDosage()          │
│  + CalculateTotalPrice()           │
│  + ValidateDosageReasonableness()  │
│  + ... (8个方法)                   │
└────────────────────────────────────┘
```

### 3.2 为什么使用泛型约束？

**对比分析**：

**方案A：不使用泛型，直接使用接口**
```csharp
// ❌ 方案A：每次传参都需要转换
public class HerbCalculator
{
    public decimal CalculateTotalPrice(IEnumerable<IHerbItem> items)
    {
        return items.Sum(i => i.Dosage * i.Quantity * i.UnitPrice);
    }
}

// 使用时需要转换
var dtoList = GetPrescriptionItemsFromDatabase();
var calculator = new HerbCalculator();
var total = calculator.CalculateTotalPrice(dtoList.Cast<IHerbItem>());
                                                   ^^^^^^^^^ 需要转换
```

**方案B：使用泛型约束（当前方案）**
```csharp
// ✅ 方案B：类型安全，无需转换
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    public decimal CalculateTotalPrice(IEnumerable<T> items)
    {
        return items.Sum(i => i.Dosage * i.Quantity * i.UnitPrice);
    }
}

// 使用时无需转换
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemDto> { }
var calculator = new PrescriptionCalculator();
var total = calculator.CalculateTotalPrice(dtoList);  // 直接使用，无需Cast
```

**优势**：
1. **类型安全**：编译期检查，避免运行时类型转换错误
2. **性能优化**：无需运行时类型检查和转换
3. **代码清晰**：明确表达"这个计算器处理的是PrescriptionItemDto"
4. **扩展性**：子类可以添加`T`特定的方法（如访问DTO的额外属性）

### 3.3 泛型约束的限制

**约束**：`where T : IHerbItem`

**含义**：
- ✅ `T`必须实现`IHerbItem`接口
- ✅ 可以访问`T`的所有`IHerbItem`属性（HerbId, HerbName, Dosage等）
- ❌ 不能访问`T`的其他属性（如ViewModel的UI属性）

**示例**：
```csharp
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    public decimal CalculateTotalPrice(IEnumerable<T> items)
    {
        // ✅ 可以访问IHerbItem的属性
        var total = items.Sum(i => i.Dosage * i.Quantity * i.UnitPrice);

        // ❌ 不能访问T的其他属性
        // var name = items.First().FullDescription;  // 编译错误（FullDescription不在IHerbItem中）

        return total;
    }
}
```

**设计权衡**：
- ✅ 优点：基类保持简洁，不耦合到具体类型
- ⚠️ 缺点：如果需要访问`T`的特定属性，需要在子类中实现

---

## 4. 接口定义架构（IHerbItem）

### 4.1 接口设计

**代码定义**：
```csharp
/// <summary>
/// 中药项接口，定义处方项的最小契约
/// </summary>
public interface IHerbItem
{
    /// <summary>
    /// 中药ID（数据库主键）
    /// </summary>
    Guid HerbId { get; }

    /// <summary>
    /// 中药名称（如"黄芪"、"党参"）
    /// </summary>
    string HerbName { get; }

    /// <summary>
    /// 单剂剂量（如15g、30g）
    /// </summary>
    decimal Dosage { get; }

    /// <summary>
    /// 剂量单位（g、kg、mg、钱、两）
    /// </summary>
    string Unit { get; }

    /// <summary>
    /// 数量（剂数，如7剂、14剂）
    /// </summary>
    decimal Quantity { get; }

    /// <summary>
    /// 单价（元/单位）
    /// </summary>
    decimal UnitPrice { get; }
}
```

### 4.2 设计决策

**为什么选择6个属性？**

| 属性 | 必要性理由 | 排除的替代方案 |
|------|-----------|--------------|
| `HerbId` | ✅ 必需：唯一标识药材，用于去重检查 | ❌ 不能用HerbName（可能重名） |
| `HerbName` | ✅ 必需：验证逻辑和UI显示需要名称 | ❌ 不能只用HerbId（需要可读性） |
| `Dosage` | ✅ 必需：核心计算依赖（总重量、总价） | - |
| `Unit` | ✅ 必需：单位转换需要（g→kg、钱→g） | ❌ 不能假设统一单位（业务需求） |
| `Quantity` | ✅ 必需：剂数计算（7剂、14剂） | - |
| `UnitPrice` | ✅ 必需：价格计算 | - |

**排除的属性**：
- ❌ `TotalPrice`：可计算得出，不应在接口中
- ❌ `Category`：不影响计算和验证，属于业务扩展
- ❌ `IsSelected`：UI状态，不应在Shared层

**接口隔离原则验证**：
- ✅ 所有属性都被Calculator或Validator使用
- ✅ 没有"胖接口"问题（只有6个必需属性）
- ✅ Client和Server都需要这6个属性

### 4.3 实现示例对比

**Server端实现（DTO）**：
```csharp
public class PrescriptionItemDto : IHerbItem
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public decimal Dosage { get; set; }
    public string Unit { get; set; } = "g";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // DTO特有属性（不在IHerbItem中）
    public Guid PrescriptionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Client端实现（ViewModel）**：
```csharp
public class PrescriptionItemViewModel : ViewModelBase, IHerbItem
{
    // IHerbItem属性（带通知）
    private Guid _herbId;
    public Guid HerbId
    {
        get => _herbId;
        set => SetProperty(ref _herbId, value);
    }

    private string _herbName = string.Empty;
    public string HerbName
    {
        get => _herbName;
        set => SetProperty(ref _herbName, value);
    }

    // ... 其他IHerbItem属性 ...

    // ViewModel特有属性（不在IHerbItem中）
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
```

**关键观察**：
- ✅ DTO和ViewModel都实现了IHerbItem的6个属性
- ✅ 各自有特定的额外属性（PrescriptionId、IsSelected等）
- ✅ Shared层的Calculator/Validator只访问IHerbItem属性，不耦合到具体类型

---

## 5. 计算器基类架构（HerbCalculatorBase）

### 5.1 类设计

**完整定义**：
```csharp
/// <summary>
/// 中药计算器基类，提供8个核心计算方法
/// </summary>
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    // ========== 基础计算方法 ==========

    /// <summary>
    /// 计算总剂量（单位：克）
    /// </summary>
    protected decimal CalculateTotalDosage(IEnumerable<T> items);

    /// <summary>
    /// 计算总重量（单位：克，考虑剂数）
    /// </summary>
    protected decimal CalculateTotalWeight(IEnumerable<T> items);

    /// <summary>
    /// 计算单个药材占比（百分比）
    /// </summary>
    protected decimal CalculateItemRatio(T item, decimal totalDosage);

    /// <summary>
    /// 计算总价（单剂）
    /// </summary>
    protected decimal CalculateTotalPrice(IEnumerable<T> items);

    /// <summary>
    /// 计算估算总价（考虑剂数）
    /// </summary>
    protected decimal CalculateEstimatedTotalPrice(IEnumerable<T> items);

    // ========== 验证辅助方法 ==========

    /// <summary>
    /// 验证剂量合理性（阈值：3-100g）
    /// </summary>
    protected bool ValidateDosageReasonableness(decimal dosageInGrams);

    /// <summary>
    /// 计算标准差（剂量均衡性检查）
    /// </summary>
    protected decimal CalculateStandardDeviation(IEnumerable<T> items);

    // ========== 单位转换方法 ==========

    /// <summary>
    /// 将任意单位转换为克（g/kg/mg/钱/两）
    /// </summary>
    protected virtual decimal ConvertToGrams(decimal value, string unit);
}
```

### 5.2 设计亮点

**亮点1：Template Method模式**

```csharp
// 基类提供核心逻辑
protected decimal CalculateTotalPrice(IEnumerable<T> items)
{
    return items.Sum(item =>
        ConvertToGrams(item.Dosage, item.Unit) * item.UnitPrice);
                         ^^^^^^^^^^^^^^^^^^^^^^^^
                         调用虚方法，子类可重写
}

// 子类可扩展单位转换逻辑
public class ExtendedCalculator : HerbCalculatorBase<PrescriptionItemDto>
{
    protected override decimal ConvertToGrams(decimal value, string unit)
    {
        // 扩展：支持新单位"斤"
        if (unit == "斤") return value * 500;
        return base.ConvertToGrams(value, unit);
    }
}
```

**亮点2：方法可见性设计**

```csharp
// ✅ 所有方法都是 protected
// 原因：
// 1. 基类不对外暴露API（由子类决定暴露哪些方法）
// 2. 子类可以组合多个基类方法实现复杂逻辑
// 3. 避免外部直接调用基类（强制通过继承使用）

public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemDto>
{
    // 子类决定暴露哪些方法为public
    public decimal GetTotalPrice(List<PrescriptionItemDto> items)
    {
        return CalculateTotalPrice(items);  // 调用基类protected方法
    }

    // 子类可组合多个基类方法
    public PriceBreakdown GetPriceBreakdown(List<PrescriptionItemDto> items, int dosageCount)
    {
        return new PriceBreakdown
        {
            SingleDosagePrice = CalculateTotalPrice(items),
            TotalWeight = CalculateTotalWeight(items),
            TotalPrice = CalculateEstimatedTotalPrice(items) * dosageCount
        };
    }
}
```

**亮点3：单位转换扩展点**

```csharp
protected virtual decimal ConvertToGrams(decimal value, string unit)
{
    return unit switch
    {
        "g" or "克" => value,
        "kg" or "千克" or "公斤" => value * 1000,
        "mg" or "毫克" => value / 1000,
        "钱" => value * 3.125m,   // 1钱 = 3.125克
        "两" => value * 31.25m,   // 1两 = 31.25克（10钱）
        _ => throw new ArgumentException($"不支持的单位: {unit}")
    };
}
```

### 5.3 方法职责矩阵

| 方法 | 输入 | 输出 | 业务含义 | 使用场景 |
|------|------|------|---------|---------|
| `CalculateTotalDosage` | `IEnumerable<T>` | decimal | 单剂总克数 | 处方配比检查 |
| `CalculateTotalWeight` | `IEnumerable<T>` | decimal | 总重量（×剂数） | 药材准备、库存扣减 |
| `CalculateItemRatio` | `T`, totalDosage | decimal | 单药占比% | 配比警告（某药占比>30%） |
| `CalculateTotalPrice` | `IEnumerable<T>` | decimal | 单剂总价 | 价格显示 |
| `CalculateEstimatedTotalPrice` | `IEnumerable<T>` | decimal | 总价（×剂数） | 最终结算 |
| `ValidateDosageReasonableness` | decimal | bool | 剂量是否合理 | 输入验证 |
| `CalculateStandardDeviation` | `IEnumerable<T>` | decimal | 剂量标准差 | 配比均衡性检查 |
| `ConvertToGrams` | decimal, string | decimal | 单位→克 | 所有计算的基础 |

---

## 6. 验证器基类架构（HerbValidatorBase）

### 6.1 类设计

**完整定义**：
```csharp
/// <summary>
/// 中药验证器基类，提供7个核心验证方法
/// </summary>
public abstract class HerbValidatorBase<T> where T : IHerbItem
{
    // ========== 重复检查方法 ==========

    /// <summary>
    /// 获取重复药材列表（按HerbId分组）
    /// </summary>
    protected List<string> GetDuplicateHerbs(IEnumerable<T> items);

    /// <summary>
    /// 检查是否有重复药材（快捷方法）
    /// </summary>
    protected bool HasDuplicateHerbs(IEnumerable<T> items);

    // ========== 剂量验证方法 ==========

    /// <summary>
    /// 验证剂量是否有效（范围：3-100g）
    /// </summary>
    protected bool IsValidDosage(decimal dosageInGrams);

    /// <summary>
    /// 获取剂量警告信息（过低/过高/合理）
    /// </summary>
    protected string GetDosageWarning(decimal dosageInGrams);

    // ========== 字段验证方法 ==========

    /// <summary>
    /// 验证必填字段（HerbId、HerbName、Dosage等）
    /// </summary>
    protected ValidationResult ValidateRequiredFields(T item);

    /// <summary>
    /// 验证药材列表非空
    /// </summary>
    protected ValidationResult ValidateHerbListNotEmpty(IEnumerable<T> items);

    // ========== 综合验证方法 ==========

    /// <summary>
    /// 综合验证药材列表（调用所有验证方法）
    /// </summary>
    protected ValidationResult ValidateHerbList(IEnumerable<T> items, string context = "药材列表");
}
```

### 6.2 ValidationResult设计

**类定义**：
```csharp
/// <summary>
/// 验证结果类，支持错误和警告
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 错误列表（导致验证失败）
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告列表（不影响验证通过）
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 是否验证通过（无错误即通过）
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// 添加错误
    /// </summary>
    public void AddError(string error);

    /// <summary>
    /// 添加警告
    /// </summary>
    public void AddWarning(string warning);

    /// <summary>
    /// 合并多个验证结果
    /// </summary>
    public static ValidationResult Merge(params ValidationResult[] results);

    /// <summary>
    /// 获取错误摘要（用于UI显示）
    /// </summary>
    public string GetErrorSummary();

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ValidationResult Success();

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ValidationResult Fail(string error);
}
```

**设计亮点**：

**亮点1：Errors vs Warnings分离**

```csharp
// 场景：剂量过高（警告但不阻止）
var result = new ValidationResult();
if (dosage > 80)
    result.AddWarning("剂量较高（>80g），请复核");  // ⚠️ 警告（允许继续）
if (dosage > 200)
    result.AddError("剂量过高（>200g），禁止使用");  // ❌ 错误（阻止操作）

// 验证结果
result.IsValid;  // true（只有警告，无错误）
```

**亮点2：Merge方法支持链式验证**

```csharp
public ValidationResult ValidatePrescription(List<PrescriptionItemViewModel> items)
{
    var emptyCheck = ValidateHerbListNotEmpty(items);
    var duplicateCheck = CheckDuplicates(items);
    var dosageCheck = CheckDosages(items);

    // 合并所有验证结果
    return ValidationResult.Merge(emptyCheck, duplicateCheck, dosageCheck);
}
```

### 6.3 验证方法分层设计

**三层验证架构**：

```
Layer 1: 原子验证（单字段）
  - IsValidDosage(decimal)
  - ValidateRequiredFields(T)
  → 输出：bool或ValidationResult

Layer 2: 组合验证（单对象）
  - GetDosageWarning(decimal)
  - ValidateHerbItem(T)
  → 输出：ValidationResult

Layer 3: 集合验证（列表）
  - HasDuplicateHerbs(IEnumerable<T>)
  - ValidateHerbList(IEnumerable<T>)
  → 输出：ValidationResult
```

**示例**：
```csharp
// Layer 1: 原子验证
protected bool IsValidDosage(decimal dosageInGrams)
{
    return dosageInGrams >= 3 && dosageInGrams <= 100;
}

// Layer 2: 组合验证
protected ValidationResult ValidateRequiredFields(T item)
{
    var result = new ValidationResult();
    if (item.HerbId == Guid.Empty)
        result.AddError("药材ID不能为空");
    if (string.IsNullOrWhiteSpace(item.HerbName))
        result.AddError("药材名称不能为空");
    if (!IsValidDosage(ConvertToGrams(item.Dosage, item.Unit)))
        result.AddError($"{item.HerbName} 剂量不合理");
    return result;
}

// Layer 3: 集合验证
protected ValidationResult ValidateHerbList(IEnumerable<T> items, string context)
{
    var result = new ValidationResult();

    // 调用Layer 2验证
    foreach (var item in items)
    {
        var itemResult = ValidateRequiredFields(item);
        result.Errors.AddRange(itemResult.Errors);
    }

    // 集合级验证
    if (HasDuplicateHerbs(items))
    {
        var duplicates = GetDuplicateHerbs(items);
        result.AddError($"{context}中发现重复药材: {string.Join(", ", duplicates)}");
    }

    return result;
}
```

---

## 7. 跨平台复用策略

### 7.1 复用场景对比

| 场景 | Server端 | Client端（Desktop） | Client端（Avalonia） |
|------|---------|-------------------|---------------------|
| **数据类型** | `PrescriptionItemDto` | `PrescriptionItemViewModel` | `PrescriptionItemViewModel` |
| **计算器** | `HerbCalculatorBase<PrescriptionItemDto>` | `HerbCalculatorBase<PrescriptionItemViewModel>` | `HerbCalculatorBase<PrescriptionItemViewModel>` |
| **验证器** | `HerbValidatorBase<PrescriptionItemDto>` | `HerbValidatorBase<PrescriptionItemViewModel>` | `HerbValidatorBase<PrescriptionItemViewModel>` |
| **使用时机** | API保存处方时 | 用户输入实时验证 | 用户输入实时验证 |
| **计算频率** | 1次/保存 | N次/输入变化 | N次/输入变化 |

### 7.2 Server端集成架构

**位置**：`LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

```csharp
public class PrescriptionService
{
    private readonly PrescriptionCalculator _calculator = new();
    private readonly PrescriptionValidator _validator = new();

    public async Task<OperationResult<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionCommand command)
    {
        // Step 1: 验证处方项
        var validationResult = _validator.ValidatePrescriptionItems(command.Items);
        if (!validationResult.IsValid)
            return OperationResult<PrescriptionDto>.Fail(validationResult.GetErrorSummary());

        // Step 2: 计算总价（使用Shared层逻辑）
        var totalPrice = _calculator.CalculateTotalPrice(command.Items);

        // Step 3: 保存到数据库
        var prescription = new Prescription
        {
            PatientId = command.PatientId,
            TotalPrice = totalPrice,  // ✅ 与Client计算结果一致
            Items = command.Items.Select(i => new PrescriptionItem
            {
                HerbId = i.HerbId,
                Dosage = i.Dosage,
                // ...
            }).ToList()
        };

        await _context.Prescriptions.AddAsync(prescription);
        await _context.SaveChangesAsync();

        return OperationResult<PrescriptionDto>.Success(...);
    }
}
```

**关键点**：
- ✅ Server端使用相同的计算逻辑（CalculateTotalPrice）
- ✅ 保存前验证（ValidatePrescriptionItems）
- ✅ 计算结果与Client端一致（避免"前端显示100元，后端保存98元"的BUG）

### 7.3 Client端集成架构

**位置**：`LYBT.Desktop.Prescriptions/ViewModels/PrescriptionEditorViewModel.cs`

```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase
{
    private readonly PrescriptionCalculator _calculator = new();
    private readonly PrescriptionValidator _validator = new();

    private ObservableCollection<PrescriptionItemViewModel> _items;
    public ObservableCollection<PrescriptionItemViewModel> Items
    {
        get => _items;
        set
        {
            SetProperty(ref _items, value);
            ValidateAndCalculate();  // 数据变化时自动验证计算
        }
    }

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

    private void ValidateAndCalculate()
    {
        // Step 1: 实时验证
        var validationResult = _validator.ValidatePrescriptionItems(Items);
        ErrorMessage = validationResult.IsValid ? string.Empty : validationResult.GetErrorSummary();

        // Step 2: 实时计算
        if (validationResult.IsValid)
        {
            TotalPrice = _calculator.CalculateTotalPrice(Items);
            // ✅ 用户看到的价格 = Server保存的价格
        }
    }

    private async Task ExecuteSaveAsync()
    {
        // 最终验证
        var finalValidation = _validator.ValidatePrescriptionItems(Items);
        if (!finalValidation.IsValid)
        {
            _dialogService.ShowError(finalValidation.GetErrorSummary());
            return;
        }

        // 提交到API（价格由Server重新计算，但应与Client一致）
        await _apiService.CreatePrescriptionAsync(new CreatePrescriptionCommand
        {
            PatientId = CurrentPatient.Id,
            Items = Items.Select(i => new PrescriptionItemDto
            {
                HerbId = i.HerbId,
                Dosage = i.Dosage,
                // ...
            }).ToList()
        });
    }
}
```

**关键点**：
- ✅ 实时验证：用户输入后立即反馈错误
- ✅ 实时计算：价格实时更新
- ✅ 数据一致性：Client显示的价格 = Server保存的价格

### 7.4 复用效益分析

**代码复用率**：
```
总计算逻辑代码：152行（HerbCalculatorBase）
总验证逻辑代码：211行（HerbValidatorBase）
Server端继承扩展：~30行
Client端继承扩展：~50行

复用率 = (152 + 211) / (152 + 211 + 30 + 50) = 81.9%
```

**维护成本对比**：

| 维护场景 | 无Shared层 | 有Shared层 |
|---------|-----------|-----------|
| 修改剂量合理性阈值 | 修改Server+Client（2处） | 修改基类（1处） |
| 新增单位"斤" | 修改Server+Client（2处） | 重写ConvertToGrams（1处） |
| 修改总价计算公式 | 修改Server+Client（2处） | 修改基类（1处） |
| BUG修复 | 修复Server+Client（2处，可能不一致） | 修复基类（1处，自动同步） |

---

## 8. 扩展点设计

### 8.1 扩展点分类

**Level 1：基类虚方法重写**

```csharp
// 扩展场景：支持新单位
public class ExtendedCalculator : HerbCalculatorBase<PrescriptionItemDto>
{
    protected override decimal ConvertToGrams(decimal value, string unit)
    {
        // 扩展：支持台制单位
        if (unit == "台钱") return value * 3.75m;
        if (unit == "台两") return value * 37.5m;

        // 调用基类逻辑处理标准单位
        return base.ConvertToGrams(value, unit);
    }
}
```

**Level 2：添加模块特定方法**

```csharp
// 扩展场景：处方模块需要禁忌检查
public class PrescriptionValidator : HerbValidatorBase<PrescriptionItemDto>
{
    // 继承基类所有验证方法，添加模块特定验证
    public ValidationResult ValidateHerbInteractions(IEnumerable<PrescriptionItemDto> items)
    {
        var result = new ValidationResult();
        var herbNames = items.Select(i => i.HerbName).ToList();

        // 检查配伍禁忌（十八反、十九畏）
        foreach (var contraindication in GetKnownContraindications())
        {
            if (herbNames.Contains(contraindication.Herb1) &&
                herbNames.Contains(contraindication.Herb2))
            {
                result.AddError($"配伍禁忌：{contraindication.Herb1} 与 {contraindication.Herb2} 不能同用");
            }
        }

        return result;
    }

    private List<Contraindication> GetKnownContraindications()
    {
        // 从配置或数据库加载禁忌列表
        return _contraIndicationRepository.GetAll();
    }
}
```

**Level 3：组合多个基类方法**

```csharp
// 扩展场景：复杂业务逻辑
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemDto>
{
    public PriceBreakdown CalculatePriceBreakdown(
        List<PrescriptionItemDto> items,
        int dosageCount,
        decimal memberDiscount)
    {
        // 组合多个基类方法
        var singleDosagePrice = CalculateTotalPrice(items);
        var totalWeight = CalculateTotalWeight(items);
        var totalPrice = singleDosagePrice * dosageCount;
        var discountedPrice = totalPrice * memberDiscount;

        return new PriceBreakdown
        {
            SingleDosagePrice = singleDosagePrice,
            TotalWeight = totalWeight,
            TotalPrice = totalPrice,
            DiscountedPrice = discountedPrice,
            MemberDiscount = memberDiscount,
            ItemDetails = items.Select(item => new ItemDetail
            {
                HerbName = item.HerbName,
                Ratio = CalculateItemRatio(item, CalculateTotalDosage(items)),
                Subtotal = ConvertToGrams(item.Dosage, item.Unit) * item.UnitPrice
            }).ToList()
        };
    }
}
```

### 8.2 扩展模式总结

| 扩展类型 | 使用场景 | 修改范围 | 复杂度 |
|---------|---------|---------|--------|
| **虚方法重写** | 修改核心逻辑（如单位转换） | 子类单个方法 | 低 |
| **添加新方法** | 模块特定逻辑（如禁忌检查） | 子类新增方法 | 中 |
| **组合基类方法** | 复杂业务场景（如价格明细） | 子类组合调用 | 中 |

**推荐扩展原则**：
- ✅ 优先使用"添加新方法"而非"重写虚方法"（保持基类逻辑稳定）
- ✅ 新方法应调用基类方法而非重复实现（复用基类逻辑）
- ❌ 避免在子类中修改`CalculateTotalPrice`等核心方法（保持跨端一致性）

---

## 9. 设计决策与权衡

### 9.1 为什么使用抽象基类而非接口？

**对比分析**：

**方案A：使用接口**
```csharp
// ❌ 方案A：接口（无共享实现）
public interface IHerbCalculator<T> where T : IHerbItem
{
    decimal CalculateTotalPrice(IEnumerable<T> items);
    decimal CalculateTotalDosage(IEnumerable<T> items);
    // ... 8个方法
}

// 问题：每个实现类都需要重复实现8个方法
public class PrescriptionCalculatorServer : IHerbCalculator<PrescriptionItemDto>
{
    public decimal CalculateTotalPrice(IEnumerable<PrescriptionItemDto> items)
    {
        return items.Sum(i => i.Dosage * i.UnitPrice);  // 重复实现
    }
    // ... 其他7个方法也需要重复实现
}

public class PrescriptionCalculatorClient : IHerbCalculator<PrescriptionItemViewModel>
{
    public decimal CalculateTotalPrice(IEnumerable<PrescriptionItemViewModel> items)
    {
        return items.Sum(i => i.Dosage * i.UnitPrice);  // 完全相同的逻辑，重复了！
    }
    // ... 其他7个方法也需要重复实现
}
```

**方案B：使用抽象基类（当前方案）**
```csharp
// ✅ 方案B：抽象基类（共享实现）
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    protected decimal CalculateTotalPrice(IEnumerable<T> items)
    {
        return items.Sum(i => i.Dosage * i.UnitPrice);  // 实现一次
    }
    // ... 其他7个方法在基类实现
}

// Server端：直接继承，无需重复实现
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemDto> { }

// Client端：直接继承，无需重复实现
public class PrescriptionCalculatorViewModel : HerbCalculatorBase<PrescriptionItemViewModel> { }
```

**结论**：
- ✅ 抽象基类避免代码重复（8个方法只实现一次）
- ✅ 虚方法提供扩展点（ConvertToGrams可重写）
- ⚠️ 缺点：C#不支持多继承（但本场景不需要）

### 9.2 为什么方法可见性是protected？

**对比分析**：

**方案A：public方法**
```csharp
// ❌ 方案A：基类方法public
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    public decimal CalculateTotalPrice(IEnumerable<T> items) { ... }
}

// 问题：外部可直接调用基类
var calculator = new HerbCalculatorBase<PrescriptionItemDto>();  // ❌ 抽象类不能实例化
calculator.CalculateTotalPrice(items);  // ❌ 强制通过子类使用
```

**方案B：protected方法（当前方案）**
```csharp
// ✅ 方案B：基类方法protected
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    protected decimal CalculateTotalPrice(IEnumerable<T> items) { ... }
}

// 子类决定暴露哪些方法
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemDto>
{
    public decimal GetTotalPrice(List<PrescriptionItemDto> items)
    {
        return CalculateTotalPrice(items);  // 调用基类方法
    }
}
```

**优势**：
- ✅ 子类控制API（可以重命名、组合、添加参数）
- ✅ 避免外部直接使用基类（强制继承扩展）
- ✅ 更灵活（子类可选择暴露部分方法）

### 9.3 为什么不使用领域事件？

**问题场景**：处方保存后需要通知其他模块（如库存扣减）

**方案A：在Shared层使用领域事件**
```csharp
// ❌ 方案A：Shared层依赖事件总线
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    private readonly IEventBus _eventBus;  // ❌ 引入基础设施依赖

    protected decimal CalculateTotalPrice(IEnumerable<T> items)
    {
        var total = items.Sum(i => i.Dosage * i.UnitPrice);
        _eventBus.Publish(new PrescriptionCalculatedEvent(total));  // ❌ Shared层不应有副作用
        return total;
    }
}
```

**方案B：在Service层使用领域事件（当前方案）**
```csharp
// ✅ 方案B：Shared层保持纯函数
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    protected decimal CalculateTotalPrice(IEnumerable<T> items)
    {
        return items.Sum(i => i.Dosage * i.UnitPrice);  // 纯计算，无副作用
    }
}

// ✅ Service层负责发布事件
public class PrescriptionService
{
    private readonly PrescriptionCalculator _calculator;
    private readonly IEventBus _eventBus;

    public async Task CreatePrescriptionAsync(CreatePrescriptionCommand command)
    {
        var totalPrice = _calculator.CalculateTotalPrice(command.Items);  // 纯计算

        // 保存处方
        await SavePrescriptionAsync(...);

        // Service层发布事件
        await _eventBus.PublishAsync(new PrescriptionCreatedEvent
        {
            PrescriptionId = prescription.Id,
            TotalPrice = totalPrice
        });
    }
}
```

**原因**：
- ✅ Shared层保持纯函数（无副作用、易测试）
- ✅ 避免Shared层依赖基础设施（EventBus、Logger等）
- ✅ 符合Single Responsibility（计算只负责计算，事件由Service管理）

### 9.4 为什么不使用FluentValidation？

**对比分析**：

**方案A：使用FluentValidation**
```csharp
// ❌ 方案A：引入第三方库
public class PrescriptionItemValidator : AbstractValidator<PrescriptionItemDto>
{
    public PrescriptionItemValidator()
    {
        RuleFor(x => x.HerbId).NotEmpty();
        RuleFor(x => x.Dosage).InclusiveBetween(3, 100);
        // ...
    }
}

// 问题：
// 1. Shared层增加外部依赖（FluentValidation NuGet包）
// 2. Client端（WPF/Avalonia）需要额外引用FluentValidation
// 3. 验证规则与UI绑定困难（FluentValidation主要面向Server端）
```

**方案B：自定义ValidationResult（当前方案）**
```csharp
// ✅ 方案B：轻量级自定义验证
public class ValidationResult
{
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool IsValid => Errors.Count == 0;
}

public abstract class HerbValidatorBase<T> where T : IHerbItem
{
    protected ValidationResult ValidateRequiredFields(T item)
    {
        var result = new ValidationResult();
        if (item.HerbId == Guid.Empty)
            result.AddError("药材ID不能为空");
        // ...
        return result;
    }
}
```

**优势**：
- ✅ 无外部依赖（纯.NET 8）
- ✅ 轻量级（ValidationResult只有100行代码）
- ✅ 易于与WPF绑定（直接绑定ErrorMessage属性）
- ✅ 支持Warnings（FluentValidation主要针对Errors）

---

## 10. 性能与测试

### 10.1 性能考量

**计算频率分析**：

| 场景 | 计算频率 | 性能要求 |
|------|---------|---------|
| Server端保存 | 1次/保存 | 低（<10ms） |
| Client端实时计算 | N次/输入 | 中（<100ms） |
| Client端验证 | N次/输入 | 中（<100ms） |

**性能优化策略**：

```csharp
// ✅ 优化1：避免重复计算
public class PrescriptionEditorViewModel : UnifiedViewModelBase
{
    private decimal? _cachedTotalPrice;
    private int _itemsHash;

    private void ValidateAndCalculate()
    {
        var currentHash = Items.GetHashCode();
        if (_itemsHash == currentHash && _cachedTotalPrice.HasValue)
            return;  // 数据未变化，跳过计算

        _itemsHash = currentHash;
        _cachedTotalPrice = _calculator.CalculateTotalPrice(Items);
        TotalPrice = _cachedTotalPrice.Value;
    }
}

// ✅ 优化2：延迟计算（防抖）
private void OnItemsChanged()
{
    _debounceTimer?.Stop();
    _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
    _debounceTimer.Tick += (s, e) =>
    {
        ValidateAndCalculate();
        _debounceTimer.Stop();
    };
    _debounceTimer.Start();
}
```

### 10.2 单元测试策略

**测试金字塔**：

```
Layer 3: 集成测试（5%）
  - Server端API测试（保存处方）
  - Client端E2E测试（用户输入流程）

Layer 2: 组件测试（20%）
  - PrescriptionCalculator测试（组合基类方法）
  - PrescriptionValidator测试（组合验证）

Layer 1: 单元测试（75%） ← 重点
  - HerbCalculatorBase方法测试
  - HerbValidatorBase方法测试
  - ValidationResult类测试
```

**单元测试示例**：

```csharp
[TestFixture]
public class HerbCalculatorBaseTests
{
    private class TestCalculator : HerbCalculatorBase<TestHerbItem> { }
    private class TestHerbItem : IHerbItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; }
        public decimal Dosage { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    [Test]
    public void CalculateTotalPrice_ShouldReturnCorrectSum()
    {
        // Arrange
        var calculator = new TestCalculator();
        var items = new List<TestHerbItem>
        {
            new() { Dosage = 10, Unit = "g", UnitPrice = 0.5m },  // 10 * 0.5 = 5
            new() { Dosage = 20, Unit = "g", UnitPrice = 1.0m }   // 20 * 1.0 = 20
        };

        // Act
        var total = calculator.CalculateTotalPrice(items);

        // Assert
        Assert.That(total, Is.EqualTo(25m));
    }

    [TestCase("g", 10, 10)]
    [TestCase("kg", 1, 1000)]
    [TestCase("钱", 10, 31.25)]
    public void ConvertToGrams_ShouldHandleVariousUnits(string unit, decimal value, decimal expected)
    {
        // Arrange
        var calculator = new TestCalculator();

        // Act
        var result = calculator.ConvertToGrams(value, unit);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
}
```

---

## 11. 演进路线图

### 11.1 当前版本（v1.0）

**功能范围**：
- ✅ IHerbItem接口（6个属性）
- ✅ HerbCalculatorBase（8个方法）
- ✅ HerbValidatorBase（7个方法）
- ✅ ValidationResult类
- ✅ 支持5种单位（g/kg/mg/钱/两）

**限制**：
- ❌ 不支持配伍禁忌检查（需要模块扩展）
- ❌ 不支持异步验证（如检查库存）
- ❌ 不支持批量计算优化

### 11.2 未来演进方向

**Phase 1：扩展验证能力（v1.1）**
```csharp
// 新增：异步验证基类
public abstract class AsyncHerbValidatorBase<T> : HerbValidatorBase<T>
    where T : IHerbItem
{
    protected abstract Task<ValidationResult> ValidateHerbInteractionsAsync(IEnumerable<T> items);
    protected abstract Task<ValidationResult> CheckStockAvailabilityAsync(IEnumerable<T> items);
}
```

**Phase 2：支持自定义单位（v1.2）**
```csharp
// 新增：单位配置接口
public interface IUnitConverter
{
    decimal ConvertToGrams(decimal value, string unit);
    List<string> GetSupportedUnits();
}

// 基类支持注入自定义转换器
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    protected IUnitConverter UnitConverter { get; set; } = new DefaultUnitConverter();
}
```

**Phase 3：性能优化（v1.3）**
```csharp
// 新增：批量计算优化
public abstract class HerbCalculatorBase<T> where T : IHerbItem
{
    protected virtual PriceSummary CalculateBatch(IEnumerable<IEnumerable<T>> prescriptionBatch)
    {
        // 批量计算优化（SIMD、并行计算）
    }
}
```

---

## 12. 参考资料

### 12.1 内部文档

- **使用指南**：[docs/how-to-guides/shared/components-usage.md](../../how-to-guides/shared/components-usage.md)
- **项目README**：[src/Shared/LYBT.Shared.Components/README.md](../../../src/Shared/LYBT.Shared.Components/README.md)
- **跨端架构指南**：[docs/architecture/shared/README.md](./README.md)
- **Quick Reference**：[docs/quick-reference/code-patterns.md](../../quick-reference/code-patterns.md)

### 12.2 设计模式参考

- **Template Method模式**：Gang of Four《设计模式》第5章
- **泛型约束**：Microsoft C# Programming Guide - Generic Constraints
- **接口隔离原则**：Robert C. Martin《敏捷软件开发：原则、模式与实践》

### 12.3 .NET官方文档

- **泛型约束**：https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters
- **接口设计**：https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/interface
- **抽象类vs接口**：https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/abstract-and-sealed-classes-and-class-members

---

**文档版本**：v1.0
**最后更新**：2025-10-30
**维护负责**：Shared层架构组
