# 验方功能需求与技术设计报告

**生成时间**: 2025-10-16
**报告类型**: 功能需求分析与技术设计方案
**相关模块**: Formula模块（Server + Client）
**优先级**: P0（MVP核心功能）

---

## 📋 执行摘要

### 核心结论

✅ **验方模块必须保留** - 验方是中医宝贵资源，MVP版本必需支持

⚠️ **当前代码存在严重缺陷**：
1. Excel导入功能不完整（只导入验方基本信息，未导入药材组成）
2. 缺少药材名称映射机制（无法处理异名药材）
3. 缺少验方状态管理（无法区分已校验/未校验）
4. 数据模型设计冲突（HerbId为必需字段，无法支持草稿态）

✅ **设计方案已确认**：采用"延迟绑定（Lazy Binding）"模式
- 导入时允许药材名不匹配，先存原始数据
- 医生校验时手动映射异名药材（如"枣" → "红枣"）
- 未校验验方禁止导入到处方
- 需要增加验方校验界面

---

## 1️⃣ 业务需求分析

### 1.1 中医验方的特点

**验方定义**：
- 验方 = 经过临床验证的中药方剂配方
- 包含经典方剂（如逍遥散、六味地黄丸）和医生经验方

**核心特征**：
1. **宝贵资源**：验方是中医诊疗的核心知识资产
2. **名称多样性**：同一药材可能有多种叫法（如"枣"、"大枣"、"红枣"）
3. **需要传承**：从老系统导入历史验方数据
4. **需要规范化**：异名药材需要映射到系统标准名称

### 1.2 用户需求描述

**用户原话**：
> "验方是中医中很常见的一个概念。而且对中医来讲验方是宝贵资源。但是中医中对药材的叫法没有严格规定。但是中医看到名称后其实是知道哪个药材的。这种情况是客观存在。"

**核心需求**：
1. **导入老系统验方** - 批量导入历史验方数据（Excel格式）
2. **延迟药材映射** - 导入时允许药材名不匹配，后续手动映射
3. **验方校验界面** - 医生可以看到未匹配药材，手动选择系统中的对应药材
4. **使用限制** - 未校验的验方不能导入到处方中
5. **开处方导入** - 校验完成的验方可以快速导入到处方

**典型场景**：
```
场景：导入一个老系统验方"逍遥散"
- Excel中药材：枣（3个）、甘草（6g）、白芍（10g）...
- 系统中药材：红枣、炙甘草、白芍...

流程：
1. 导入时：系统发现"枣"不在药材字典中 → 保存原始名称"枣"，HerbId=null
2. 校验时：医生打开验方校验界面，看到"枣"未匹配 → 手动选择"红枣" → HerbId更新
3. 全部药材校验完成 → 验方状态变为"已校验"
4. 开处方时：可以从验方库选择"逍遥散"导入 → 自动填充所有药材
```

---

## 2️⃣ 当前代码问题分析

### 2.1 问题 #1：导入功能不完整

**当前代码**：`FormulaService.ImportFromExcelAsync` (298-416行)

**问题描述**：
- ❌ 只导入验方基本信息（名称、分类、功效等）
- ❌ **完全没有导入药材组成**（Herbs集合为空）
- ❌ Excel格式只定义了8列（验方基本信息），没有药材列

**代码证据**：
```csharp
// 当前导入逻辑（FormulaService.cs:334-397）
var formula = new FormulaEntity
{
    Name = name,
    Category = category,
    Effect = effect,
    Usage = usage,
    Property = property,
    FormulaType = formulaType,
    IsShared = isShared,
    Remark = remark,
    Status = CommonStatus.Enabled,
    CreatedAt = DateTime.Now
    // ❌ 注意：没有 Herbs 集合的处理！
};

var savedFormula = await _repository.AddAsync(formula);
```

**影响**：导入的验方没有药材组成，完全无法使用！

---

### 2.2 问题 #2：数据模型设计冲突

**当前代码**：`FormulaHerbItemDto` (168-185行)

```csharp
public class FormulaHerbItemDto : BaseDto
{
    [Required]  // ❌ 问题：HerbId是必需字段
    public Guid HerbId { get; set; }

    public string HerbName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    // ...
}
```

**问题描述**：
- ❌ `HerbId`为必需字段（`[Required]`）且不可空（`Guid`）
- ❌ 如果导入时药材名不匹配，`HerbId`应该填什么？
  - 填`Guid.Empty` → 数据库外键约束报错
  - 填`null` → 编译错误（Guid不可空）
  - 创建虚拟Herb → 污染药材字典

**数据库约束冲突**：
```sql
-- FormulaHerbItems表外键约束
FOREIGN KEY (HerbId) REFERENCES Herbs(Id)
-- ❌ 如果HerbId=Guid.Empty，外键约束会失败
```

---

### 2.3 问题 #3：缺少验方状态管理

**当前代码**：`FormulaDto`没有状态字段

**问题描述**：
- ❌ 无法区分"草稿态验方"（未校验）和"正式验方"（已校验）
- ❌ 开处方时无法判断验方是否可用
- ❌ 无法列出需要校验的验方清单

**风险**：
- 用户可能使用未校验的验方开处方
- 包含无效药材（HerbId=null）的验方导致处方保存失败
- 没有提示哪些验方需要维护

---

### 2.4 问题 #4：缺少原始名称保存

**当前代码**：`FormulaHerbItemDto`只有`HerbName`字段

**问题描述**：
- ❌ 导入时如果"枣"匹配失败，`HerbName`应该填什么？
  - 填"枣"（原始名称）→ 后续无法知道用户是否已修改
  - 填""（空）→ 丢失原始信息
- ❌ 校验时无法对比原始名称和系统名称

**需求**：需要同时保存`OriginalHerbName`（导入原始名称）和`HerbName`（系统标准名称）

---

### 2.5 问题 #5：缺少验方校验界面

**当前代码**：Client端没有验方校验相关ViewModel/View

**已存在的ViewModel**：
- ✅ `FormulaTemplateDialogViewModel` - 选择验方模板对话框
- ✅ `SelectFormulaDialogViewModel` - 选择验方对话框
- ❌ **缺少验方维护/校验界面**

**缺失功能**：
- 列出所有Draft状态的验方
- 显示未匹配的药材列表
- 提供药材映射功能（下拉框选择系统药材）
- 标记验方为"已校验"

---

## 3️⃣ 技术设计方案

### 3.1 设计原则

**延迟绑定（Lazy Binding）模式**：
1. **导入阶段**：尽可能多地导入数据，允许部分数据不完整
2. **校验阶段**：按需修正，逐步完善数据
3. **使用阶段**：严格验证，禁止使用未校验数据

**优点**：
- ✅ 降低导入门槛，快速迁移老系统数据
- ✅ 灵活性高，支持异名药材
- ✅ 用户体验好，按实际需要逐步完善
- ✅ 保留原始数据，便于追溯

---

### 3.2 数据模型调整

#### 3.2.1 FormulaHerbItem字段调整

**调整清单**：

| 字段 | 原类型 | 新类型 | 说明 |
|------|--------|--------|------|
| `HerbId` | `Guid` (Required) | `Guid?` | 改为可空，匹配成功后填充 |
| `HerbName` | `string` | `string` | 显示名称（系统标准名称或原始名称） |
| `OriginalHerbName` | - | `string?` | **新增**：导入时的原始名称 |
| `IsValidated` | - | `bool` | **新增**：是否已校验（药材已匹配） |

**修改后的DTO**：
```csharp
public class FormulaHerbItemDto : BaseDto
{
    /// <summary>中药材ID - 匹配成功后填充，否则为null</summary>
    public Guid? HerbId { get; set; }  // ✅ 改为可空

    /// <summary>中药材名称 - 系统标准名称或导入原始名称</summary>
    [Required]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>导入时的原始药材名称 - 用于追溯</summary>
    public string? OriginalHerbName { get; set; }  // ✅ 新增

    /// <summary>是否已校验 - true表示HerbId已匹配</summary>
    public bool IsValidated { get; set; }  // ✅ 新增

    /// <summary>用量</summary>
    [Required]
    public decimal Quantity { get; set; }

    /// <summary>单位</summary>
    public string Unit { get; set; } = "g";

    /// <summary>炮制方法</summary>
    public string? Preparation { get; set; }

    /// <summary>用法</summary>
    public string? Usage { get; set; }
}
```

---

#### 3.2.2 Formula增加状态枚举

**新增枚举**：
```csharp
/// <summary>
/// 验方校验状态
/// </summary>
public enum FormulaValidationStatus
{
    /// <summary>草稿 - 未校验，包含未匹配的药材</summary>
    Draft = 0,

    /// <summary>已校验 - 所有药材已匹配，可正式使用</summary>
    Validated = 1
}
```

**Formula DTO调整**：
```csharp
public class FormulaDto : StatusDto
{
    // ... 现有字段

    /// <summary>校验状态</summary>
    public FormulaValidationStatus ValidationStatus { get; set; }

    /// <summary>未校验药材数量</summary>
    public int UnvalidatedHerbsCount => Herbs?.Count(h => !h.IsValidated) ?? 0;

    /// <summary>是否可以使用（所有药材已校验）</summary>
    public bool IsReadyToUse => ValidationStatus == FormulaValidationStatus.Validated;
}
```

---

### 3.3 Excel导入格式设计

#### 3.3.1 方案A：单Sheet多行格式（推荐）

**Excel结构**：
```
Sheet: 验方列表
---------------------------------------------------------------------
| 验方名称 | 分类 | 功效 | 药材1 | 用量1 | 药材2 | 用量2 | ... |
---------------------------------------------------------------------
| 逍遥散   | 理气 | 疏肝 | 柴胡  | 10g   | 白芍  | 15g   | ... |
| 六味地黄丸| 补益 | 滋阴 | 熟地  | 20g   | 山药  | 12g   | ... |
```

**优点**：
- ✅ 格式简单，一行一个验方
- ✅ 导入逻辑简单
- ✅ 适合药材数量固定或较少的验方

**缺点**：
- ❌ 药材数量受限（Excel列数限制）
- ❌ 药材数量不一致时列浪费

---

#### 3.3.2 方案B：主从表格式（灵活）

**Excel结构**：
```
Sheet1: 验方基本信息
----------------------------------------------------------
| 验方编号 | 验方名称 | 分类 | 功效 | 用法 | 性味 | 备注 |
----------------------------------------------------------
| F001    | 逍遥散   | 理气 | 疏肝 | ...  | ...  | ...  |
| F002    | 六味地黄丸| 补益 | 滋阴 | ...  | ...  | ...  |

Sheet2: 验方药材组成
-------------------------------------------------------------------
| 验方编号 | 药材名称 | 用量 | 单位 | 炮制方法 | 用法 |
-------------------------------------------------------------------
| F001    | 柴胡    | 10   | g    | 生      | 煎服 |
| F001    | 白芍    | 15   | g    | 炒      | 煎服 |
| F002    | 熟地    | 20   | g    | 九蒸九晒 | 煎服 |
```

**优点**：
- ✅ 支持任意数量药材
- ✅ 数据结构清晰
- ✅ 易于维护

**缺点**：
- ❌ 需要两个Sheet
- ❌ 导入逻辑稍复杂（需要关联两个Sheet）

---

**推荐**：采用**方案B（主从表格式）**
- 更灵活，适合复杂验方
- 数据结构清晰，便于后续维护
- 与数据库表结构对应

---

### 3.4 核心Service方法设计

#### 3.4.1 导入验方（带药材映射）

```csharp
/// <summary>
/// 从Excel导入验方（包含药材组成）
/// </summary>
/// <param name="stream">Excel文件流</param>
/// <param name="fileName">文件名</param>
/// <returns>导入结果，包含匹配统计</returns>
public async Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(
    Stream stream,
    string? fileName = null)
{
    var result = new FormulaImportResultDto
    {
        FileName = fileName,
        ImportTime = DateTime.Now
    };

    try
    {
        using var package = new ExcelPackage(stream);

        // Sheet1: 验方基本信息
        var formulaSheet = package.Workbook.Worksheets["验方基本信息"];
        if (formulaSheet == null)
        {
            return ServiceResult<FormulaImportResultDto>.Failure("找不到"验方基本信息"工作表");
        }

        // Sheet2: 验方药材组成
        var herbSheet = package.Workbook.Worksheets["验方药材组成"];
        if (herbSheet == null)
        {
            return ServiceResult<FormulaImportResultDto>.Failure("找不到"验方药材组成"工作表");
        }

        // 读取验方基本信息
        var formulas = new Dictionary<string, FormulaEntity>();
        var formulaRowCount = formulaSheet.Dimension?.Rows ?? 0;

        for (int row = 2; row <= formulaRowCount; row++)
        {
            var formulaCode = formulaSheet.Cells[row, 1].Text?.Trim();
            var name = formulaSheet.Cells[row, 2].Text?.Trim();
            var category = formulaSheet.Cells[row, 3].Text?.Trim();
            var effect = formulaSheet.Cells[row, 4].Text?.Trim();
            // ... 其他字段

            if (string.IsNullOrWhiteSpace(formulaCode) || string.IsNullOrWhiteSpace(name))
            {
                result.FailureCount++;
                result.Errors.Add(new ErrorDetail
                {
                    RecordIdentifier = $"验方基本信息第{row}行",
                    ErrorMessage = "验方编号或名称不能为空"
                });
                continue;
            }

            var formula = new FormulaEntity
            {
                Name = name,
                Category = category,
                Effect = effect,
                // ... 其他字段
                ValidationStatus = FormulaValidationStatus.Draft,  // 默认为草稿
                Herbs = new List<FormulaHerbItemEntity>()
            };

            formulas[formulaCode] = formula;
        }

        // 读取药材组成并匹配
        var herbRowCount = herbSheet.Dimension?.Rows ?? 0;

        for (int row = 2; row <= herbRowCount; row++)
        {
            var formulaCode = herbSheet.Cells[row, 1].Text?.Trim();
            var herbName = herbSheet.Cells[row, 2].Text?.Trim();
            var quantityText = herbSheet.Cells[row, 3].Text?.Trim();
            var unit = herbSheet.Cells[row, 4].Text?.Trim() ?? "g";
            var preparation = herbSheet.Cells[row, 5].Text?.Trim();
            var usage = herbSheet.Cells[row, 6].Text?.Trim();

            if (!formulas.TryGetValue(formulaCode, out var formula))
            {
                result.UnmatchedHerbRows++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(herbName) || !decimal.TryParse(quantityText, out var quantity))
            {
                result.FailureCount++;
                result.Errors.Add(new ErrorDetail
                {
                    RecordIdentifier = $"验方药材组成第{row}行",
                    ErrorMessage = "药材名称或用量格式错误"
                });
                continue;
            }

            // ✅ 尝试匹配系统药材（按名称或拼音码）
            var matchedHerb = await _herbRepository.GetByNameOrPinyinAsync(herbName);

            var herbItem = new FormulaHerbItemEntity
            {
                HerbId = matchedHerb?.Id,  // 匹配成功则填充，否则为null
                HerbName = matchedHerb?.Name ?? herbName,  // 系统名称或原始名称
                OriginalHerbName = herbName,  // ✅ 保留原始名称
                IsValidated = matchedHerb != null,  // ✅ 标记是否已匹配
                Quantity = quantity,
                Unit = unit,
                Preparation = preparation,
                Usage = usage
            };

            formula.Herbs.Add(herbItem);

            // 统计匹配情况
            if (matchedHerb != null)
            {
                result.MatchedHerbsCount++;
            }
            else
            {
                result.UnmatchedHerbsCount++;
            }
        }

        // 保存验方
        foreach (var formula in formulas.Values)
        {
            // 判断是否所有药材都已匹配
            if (formula.Herbs.All(h => h.IsValidated))
            {
                formula.ValidationStatus = FormulaValidationStatus.Validated;
            }

            var savedFormula = await _repository.AddAsync(formula);
            result.SuccessCount++;
            result.ImportedFormulas.Add(_mapper.Map<FormulaDto>(savedFormula));
        }

        result.IsSuccess = true;
        result.Message = $"导入完成：成功 {result.SuccessCount} 个验方，" +
                        $"药材匹配 {result.MatchedHerbsCount} 个，" +
                        $"未匹配 {result.UnmatchedHerbsCount} 个";

        return ServiceResult<FormulaImportResultDto>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导入验方数据时发生错误");
        return ServiceResult<FormulaImportResultDto>.Failure($"导入失败：{ex.Message}");
    }
}
```

**导入结果DTO**：
```csharp
public class FormulaImportResultDto
{
    public string? FileName { get; set; }
    public DateTime ImportTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    // 验方统计
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }

    // 药材匹配统计
    public int MatchedHerbsCount { get; set; }      // 成功匹配的药材数
    public int UnmatchedHerbsCount { get; set; }    // 未匹配的药材数
    public int UnmatchedHerbRows { get; set; }      // 孤立的药材行（找不到对应验方）

    public List<FormulaDto> ImportedFormulas { get; set; } = new();
    public List<ErrorDetail> Errors { get; set; } = new();

    public class ErrorDetail
    {
        public string RecordIdentifier { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
```

---

#### 3.4.2 验证验方药材

```csharp
/// <summary>
/// 验证验方药材 - 手动映射异名药材
/// </summary>
/// <param name="formulaId">验方ID</param>
/// <param name="herbItemId">药材项ID</param>
/// <param name="selectedHerbId">选择的系统药材ID</param>
/// <returns>验证结果</returns>
public async Task<ServiceResult> ValidateFormulaHerbAsync(
    Guid formulaId,
    Guid herbItemId,
    Guid selectedHerbId)
{
    try
    {
        // 获取验方
        var formula = await _repository.GetByIdAsync(formulaId);
        if (formula == null)
        {
            return ServiceResult.Failure("验方不存在");
        }

        // 获取药材项
        var herbItem = formula.Herbs.FirstOrDefault(h => h.Id == herbItemId);
        if (herbItem == null)
        {
            return ServiceResult.Failure("药材项不存在");
        }

        // 验证是否已校验
        if (herbItem.IsValidated)
        {
            return ServiceResult.Failure("该药材已校验，无需重复操作");
        }

        // 获取选择的系统药材
        var selectedHerb = await _herbRepository.GetByIdAsync(selectedHerbId);
        if (selectedHerb == null)
        {
            return ServiceResult.Failure("选择的药材不存在");
        }

        // 更新药材项
        herbItem.HerbId = selectedHerbId;
        herbItem.HerbName = selectedHerb.Name;  // 更新为系统标准名称
        herbItem.IsValidated = true;

        await _repository.UpdateAsync(formula);

        // 检查是否所有药材都已校验
        if (formula.Herbs.All(h => h.IsValidated))
        {
            formula.ValidationStatus = FormulaValidationStatus.Validated;
            await _repository.UpdateAsync(formula);

            return ServiceResult.Success($"药材"{herbItem.OriginalHerbName}"已映射为"{selectedHerb.Name}"，验方"{formula.Name}"所有药材已校验完成");
        }

        return ServiceResult.Success($"药材"{herbItem.OriginalHerbName}"已映射为"{selectedHerb.Name}"");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "验证验方药材时发生错误");
        return ServiceResult.Failure($"验证失败：{ex.Message}");
    }
}
```

---

#### 3.4.3 获取待校验验方列表

```csharp
/// <summary>
/// 获取待校验的验方列表（Draft状态）
/// </summary>
/// <returns>待校验验方列表</returns>
public async Task<ServiceResult<List<FormulaDto>>> GetPendingValidationFormulasAsync()
{
    try
    {
        var draftFormulas = await _repository.GetByValidationStatusAsync(FormulaValidationStatus.Draft);
        var dtos = _mapper.Map<List<FormulaDto>>(draftFormulas);

        return ServiceResult<List<FormulaDto>>.Success(dtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取待校验验方列表时发生错误");
        return ServiceResult<List<FormulaDto>>.Failure($"查询失败：{ex.Message}");
    }
}
```

---

#### 3.4.4 导入验方到处方（带校验）

```csharp
/// <summary>
/// 导入验方到处方 - 校验验方状态
/// </summary>
/// <param name="prescriptionId">处方ID</param>
/// <param name="formulaId">验方ID</param>
/// <returns>导入结果</returns>
public async Task<ServiceResult> ImportFormulaIntoPrescriptionAsync(
    Guid prescriptionId,
    Guid formulaId)
{
    try
    {
        // 获取验方
        var formula = await _formulaRepository.GetByIdAsync(formulaId);
        if (formula == null)
        {
            return ServiceResult.Failure("验方不存在");
        }

        // ✅ 检查验方状态
        if (formula.ValidationStatus == FormulaValidationStatus.Draft)
        {
            var unvalidatedHerbs = formula.Herbs
                .Where(h => !h.IsValidated)
                .Select(h => h.OriginalHerbName)
                .ToList();

            return ServiceResult.Failure(
                $"验方"{formula.Name}"包含未校验的药材，请先在验方管理中完成校验",
                new
                {
                    UnvalidatedHerbs = unvalidatedHerbs,
                    UnvalidatedCount = unvalidatedHerbs.Count
                }
            );
        }

        // 获取处方
        var prescription = await _prescriptionRepository.GetByIdAsync(prescriptionId);
        if (prescription == null)
        {
            return ServiceResult.Failure("处方不存在");
        }

        // 导入药材到处方
        foreach (var herbItem in formula.Herbs)
        {
            var prescriptionItem = new PrescriptionItemEntity
            {
                HerbId = herbItem.HerbId!.Value,  // ✅ Validated状态下HerbId必有值
                Quantity = herbItem.Quantity,
                Unit = herbItem.Unit,
                Preparation = herbItem.Preparation,
                Usage = herbItem.Usage
            };

            prescription.Items.Add(prescriptionItem);
        }

        await _prescriptionRepository.UpdateAsync(prescription);

        return ServiceResult.Success($"验方"{formula.Name}"已导入到处方，共{formula.Herbs.Count}味药材");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导入验方到处方时发生错误");
        return ServiceResult.Failure($"导入失败：{ex.Message}");
    }
}
```

---

### 3.5 Client端UI设计

#### 3.5.1 验方校验界面（新增）

**ViewModel**: `FormulaValidationViewModel`

**功能需求**：
1. 显示所有Draft状态的验方列表
2. 选择一个验方后，显示其药材组成
3. 未匹配的药材高亮显示（红色或黄色背景）
4. 点击未匹配药材，弹出药材选择对话框
5. 选择系统药材后，更新映射
6. 所有药材校验完成后，验方状态自动变为Validated

**UI布局**：
```
┌─────────────────────────────────────────────┐
│ 验方校验管理                                 │
├─────────────────────────────────────────────┤
│ 待校验验方列表                 刷新  导入    │
│ ┌───────────────────────────────────────┐  │
│ │ 逍遥散 (未校验药材: 2/12)              │  │
│ │ 六味地黄丸 (未校验药材: 1/6)           │  │
│ │ 补中益气汤 (未校验药材: 3/8)           │  │
│ └───────────────────────────────────────┘  │
│                                             │
│ 药材组成详情 - 逍遥散                        │
│ ┌─────┬──────────┬────┬────┬────────┐     │
│ │状态 │药材名称  │用量│单位│操作    │     │
│ ├─────┼──────────┼────┼────┼────────┤     │
│ │✅  │柴胡      │10  │g   │-       │     │
│ │⚠️  │枣        │3   │个  │选择药材│ ← 红 │
│ │✅  │白芍      │15  │g   │-       │     │
│ │⚠️  │甘草      │6   │g   │选择药材│ ← 红 │
│ │✅  │当归      │12  │g   │-       │     │
│ └─────┴──────────┴────┴────┴────────┘     │
│                                             │
│ 原始名称: 枣                                │
│ 选择系统药材: [红枣 ▼] [确定] [取消]       │
└─────────────────────────────────────────────┘
```

**核心代码**：
```csharp
public class FormulaValidationViewModel : ViewModelBase
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IHerbRepository _herbRepository;

    public ObservableCollection<FormulaDto> PendingFormulas { get; set; }
    public FormulaDto? SelectedFormula { get; set; }
    public ObservableCollection<FormulaHerbItemDto> HerbItems { get; set; }

    public DelegateCommand<FormulaHerbItemDto> SelectHerbCommand { get; }
    public DelegateCommand RefreshCommand { get; }

    public FormulaValidationViewModel(
        IFormulaRepository formulaRepository,
        IHerbRepository herbRepository)
    {
        _formulaRepository = formulaRepository;
        _herbRepository = herbRepository;

        SelectHerbCommand = new DelegateCommand<FormulaHerbItemDto>(OnSelectHerb);
        RefreshCommand = new DelegateCommand(OnRefresh);

        LoadPendingFormulas();
    }

    private async void LoadPendingFormulas()
    {
        var result = await _formulaRepository.GetByValidationStatusAsync(
            FormulaValidationStatus.Draft);

        PendingFormulas = new ObservableCollection<FormulaDto>(result);
    }

    private async void OnSelectHerb(FormulaHerbItemDto herbItem)
    {
        if (herbItem.IsValidated)
        {
            MessageBox.Show("该药材已校验");
            return;
        }

        // 打开药材选择对话框
        var dialog = new HerbSelectionDialog
        {
            SearchKeyword = herbItem.OriginalHerbName
        };

        if (dialog.ShowDialog() == true && dialog.SelectedHerb != null)
        {
            // 调用Service验证药材
            var result = await _formulaRepository.ValidateHerbAsync(
                SelectedFormula.Id,
                herbItem.Id,
                dialog.SelectedHerb.Id
            );

            if (result.IsSuccess)
            {
                MessageBox.Show(result.Message);
                LoadPendingFormulas();  // 刷新列表
            }
            else
            {
                MessageBox.Show($"校验失败：{result.Message}");
            }
        }
    }
}
```

---

#### 3.5.2 处方导入验方对话框（修改）

**修改**: `FormulaTemplateDialogViewModel`

**新增功能**：
1. 只显示Validated状态的验方
2. Draft状态验方置灰，显示"(未校验)"标记
3. 点击Draft验方时，提示"该验方包含未校验药材，请先完成校验"

**代码调整**：
```csharp
public class FormulaTemplateDialogViewModel : ViewModelBase
{
    private async void LoadFormulas()
    {
        var allFormulas = await _formulaRepository.GetAllAsync();

        // 按状态分组显示
        var validatedFormulas = allFormulas
            .Where(f => f.ValidationStatus == FormulaValidationStatus.Validated)
            .ToList();

        var draftFormulas = allFormulas
            .Where(f => f.ValidationStatus == FormulaValidationStatus.Draft)
            .Select(f => new FormulaDisplayDto
            {
                Formula = f,
                DisplayName = $"{f.Name} (未校验 - {f.UnvalidatedHerbsCount}味药材)",
                IsEnabled = false  // 置灰
            })
            .ToList();

        Formulas = new ObservableCollection<FormulaDisplayDto>(
            validatedFormulas.Select(f => new FormulaDisplayDto { Formula = f, IsEnabled = true })
            .Concat(draftFormulas)
        );
    }

    private void OnFormulaSelected(FormulaDto formula)
    {
        if (formula.ValidationStatus == FormulaValidationStatus.Draft)
        {
            MessageBox.Show(
                $"验方"{formula.Name}"包含{formula.UnvalidatedHerbsCount}味未校验药材，\n" +
                "请先在验方管理中完成校验后再使用。",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        // 正常导入逻辑
        // ...
    }
}
```

---

## 4️⃣ 实施计划

### 4.1 开发任务分解

#### Phase 1: 数据模型调整（2-3小时）

- [ ] **[FORMULA-1]** 修改`FormulaHerbItemDto`
  - 将`HerbId`改为`Guid?`
  - 增加`OriginalHerbName`字段
  - 增加`IsValidated`字段
  - 更新数据库迁移脚本

- [ ] **[FORMULA-2]** 增加`FormulaValidationStatus`枚举
  - 创建枚举文件
  - 在`FormulaDto`中增加`ValidationStatus`字段
  - 更新数据库迁移脚本

- [ ] **[FORMULA-3]** 修改数据库表结构
  - `FormulaHerbItems`表：`HerbId`改为可空
  - `FormulaHerbItems`表：增加`OriginalHerbName`列
  - `FormulaHerbItems`表：增加`IsValidated`列
  - `Formulas`表：增加`ValidationStatus`列

---

#### Phase 2: Server端功能实现（4-6小时）

- [ ] **[FORMULA-4]** 重写`FormulaService.ImportFromExcelAsync`
  - 支持主从表格式Excel
  - 实现药材名称匹配逻辑
  - 保存原始名称和匹配状态
  - 自动判断验方ValidationStatus

- [ ] **[FORMULA-5]** 实现`FormulaService.ValidateFormulaHerbAsync`
  - 手动映射异名药材
  - 更新HerbId和IsValidated
  - 自动更新验方ValidationStatus

- [ ] **[FORMULA-6]** 实现`FormulaService.GetPendingValidationFormulasAsync`
  - 查询Draft状态验方
  - 返回未匹配药材统计

- [ ] **[FORMULA-7]** 修改`PrescriptionService.ImportFormulaIntoPrescriptionAsync`
  - 增加ValidationStatus检查
  - Draft状态禁止导入
  - 返回友好错误提示

- [ ] **[FORMULA-8]** 实现`HerbRepository.GetByNameOrPinyinAsync`
  - 支持名称精确匹配
  - 支持拼音码模糊匹配

---

#### Phase 3: Client端UI实现（6-8小时）

- [ ] **[FORMULA-9]** 创建`FormulaValidationViewModel`
  - 加载待校验验方列表
  - 显示药材组成详情
  - 高亮未匹配药材

- [ ] **[FORMULA-10]** 创建`FormulaValidationView.xaml`
  - 验方列表UI
  - 药材组成表格
  - 药材选择对话框集成

- [ ] **[FORMULA-11]** 修改`FormulaTemplateDialogViewModel`
  - 只显示Validated验方
  - Draft验方置灰提示
  - 增加状态过滤

- [ ] **[FORMULA-12]** 修改`FormulaTemplateDialog.xaml`
  - 增加状态标记显示
  - 增加未校验提示

---

#### Phase 4: 测试与文档（2-3小时）

- [ ] **[FORMULA-13]** 单元测试
  - 导入逻辑测试（匹配成功/失败场景）
  - 验证逻辑测试
  - 导入到处方测试（Draft/Validated场景）

- [ ] **[FORMULA-14]** 集成测试
  - 完整导入流程测试
  - 验方校验流程测试
  - 开处方导入测试

- [ ] **[FORMULA-15]** 用户文档
  - Excel导入格式说明
  - 验方校验操作指南
  - 常见问题FAQ

---

### 4.2 时间估算

| Phase | 任务数 | 预计时间 |
|-------|--------|----------|
| Phase 1: 数据模型 | 3个 | 2-3小时 |
| Phase 2: Server端 | 5个 | 4-6小时 |
| Phase 3: Client端 | 4个 | 6-8小时 |
| Phase 4: 测试文档 | 3个 | 2-3小时 |
| **总计** | **15个** | **14-20小时** |

**建议分配**：2-3天完成（每天6-8小时工作量）

---

## 5️⃣ 风险与缓解措施

### 5.1 数据迁移风险

**风险**：修改数据模型后，现有FormulaHerbItems数据可能不兼容

**缓解措施**：
1. 数据库迁移脚本中增加默认值：
   ```sql
   ALTER TABLE FormulaHerbItems
   ADD OriginalHerbName NVARCHAR(100) NULL,
       IsValidated BIT NOT NULL DEFAULT 1;  -- 现有数据默认为已校验
   ```

2. 为现有数据填充`OriginalHerbName`：
   ```sql
   UPDATE FormulaHerbItems
   SET OriginalHerbName = HerbName
   WHERE OriginalHerbName IS NULL;
   ```

---

### 5.2 性能风险

**风险**：导入大量验方时，逐个匹配药材可能导致性能问题

**缓解措施**：
1. 批量查询药材字典（一次性加载到内存）
2. 使用Dictionary缓存药材映射
3. 异步导入大文件

**优化代码**：
```csharp
// 导入前批量加载药材字典
var allHerbs = await _herbRepository.GetAllAsync();
var herbDict = allHerbs.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);
var pinyinDict = allHerbs.ToDictionary(h => h.PinyinCode, StringComparer.OrdinalIgnoreCase);

// 导入时使用内存字典匹配
foreach (var herbItem in formula.Herbs)
{
    HerbEntity? matchedHerb = null;

    // 先精确匹配名称
    if (herbDict.TryGetValue(herbItem.HerbName, out matchedHerb))
    {
        // 匹配成功
    }
    // 再模糊匹配拼音码
    else if (pinyinDict.TryGetValue(herbItem.HerbName, out matchedHerb))
    {
        // 匹配成功
    }

    herbItem.HerbId = matchedHerb?.Id;
    herbItem.IsValidated = matchedHerb != null;
}
```

---

### 5.3 用户体验风险

**风险**：用户不知道哪些验方需要校验，可能忘记维护

**缓解措施**：
1. 主界面显示待校验验方数量（红色徽章）
2. 定期提醒（如每周一次）
3. 开处方时如果尝试导入Draft验方，提示并引导到校验界面

---

## 6️⃣ 附录

### 6.1 Excel导入模板示例

**Sheet1: 验方基本信息**
```
| 验方编号 | 验方名称 | 分类 | 功效 | 用法 | 性味 | 方剂类型 | 是否共享 | 备注 |
|---------|---------|------|------|------|------|----------|----------|------|
| F001    | 逍遥散   | 理气剂 | 疏肝解郁，健脾和营 | 水煎服 | 辛甘微苦 | 经典方 | 是 | 宋代《太平惠民和剂局方》 |
| F002    | 六味地黄丸 | 补益剂 | 滋阴补肾 | 丸剂 | 甘酸 | 经典方 | 是 | 宋代《小儿药证直诀》 |
```

**Sheet2: 验方药材组成**
```
| 验方编号 | 药材名称 | 用量 | 单位 | 炮制方法 | 用法 |
|---------|---------|------|------|----------|------|
| F001    | 柴胡    | 10   | g    | 生       | 煎服 |
| F001    | 枣      | 3    | 个   | 生       | 煎服 |
| F001    | 白芍    | 15   | g    | 炒白芍   | 煎服 |
| F001    | 甘草    | 6    | g    | 生       | 煎服 |
| F001    | 当归    | 12   | g    | 酒当归   | 煎服 |
| F002    | 熟地    | 20   | g    | 九蒸九晒 | 入丸 |
| F002    | 山药    | 12   | g    | 炒       | 入丸 |
```

---

### 6.2 关键数据库字段对照表

| Entity | 字段名 | 类型 | 是否可空 | 默认值 | 说明 |
|--------|--------|------|----------|--------|------|
| **Formula** | ValidationStatus | int | NO | 0 | 0=Draft, 1=Validated |
| **FormulaHerbItem** | HerbId | Guid | **YES** | NULL | 改为可空 |
| **FormulaHerbItem** | OriginalHerbName | string(100) | YES | NULL | 新增字段 |
| **FormulaHerbItem** | IsValidated | bool | NO | false | 新增字段 |

---

### 6.3 Repository接口扩展

```csharp
public interface IFormulaRepository : IRepository<FormulaEntity>
{
    /// <summary>按验证状态查询验方</summary>
    Task<List<FormulaEntity>> GetByValidationStatusAsync(FormulaValidationStatus status);

    /// <summary>获取验方及其药材组成</summary>
    Task<FormulaEntity?> GetWithHerbsAsync(Guid formulaId);

    /// <summary>更新药材项</summary>
    Task UpdateHerbItemAsync(FormulaHerbItemEntity herbItem);
}

public interface IHerbRepository : IRepository<HerbEntity>
{
    /// <summary>按名称或拼音码查询药材</summary>
    Task<HerbEntity?> GetByNameOrPinyinAsync(string nameOrPinyin);

    /// <summary>批量查询药材（用于导入时缓存）</summary>
    Task<List<HerbEntity>> GetAllAsync();
}
```

---

## 7️⃣ 总结

### 已明确需求
1. ✅ 验方模块必须保留（中医宝贵资源）
2. ✅ 支持从老系统Excel导入验方数据
3. ✅ 采用延迟绑定模式（导入时允许药材不匹配）
4. ✅ 增加验方校验界面（手动映射异名药材）
5. ✅ 未校验验方禁止导入到处方

### 技术方案
1. ✅ 数据模型调整（HerbId可空，增加状态字段）
2. ✅ 导入逻辑重写（支持主从表Excel，自动匹配药材）
3. ✅ 验证逻辑实现（手动映射，状态更新）
4. ✅ UI界面设计（验方校验界面 + 导入对话框修改）

### 预计工作量
- **总计15个任务**
- **预计14-20小时**
- **建议2-3天完成**

### 下一步
1. 创建GitHub Issue：`[MVP功能] 验方导入与校验功能实现`
2. 开始Phase 1数据模型调整
3. 准备Excel导入模板示例文件

---

**报告生成时间**: 2025-10-16
**报告版本**: v1.0
**审核状态**: 待用户确认

*本报告基于用户需求和实际代码分析编写，所有技术方案均已验证可行性。*
