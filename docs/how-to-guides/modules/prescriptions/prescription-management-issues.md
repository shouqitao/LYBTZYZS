# 处方管理问题解决指南
**10个常见处方管理问题的系统性解决方案**

## 📋 问题目录

1. [处方创建失败 - 医疗案例前置条件验证](#1-处方创建失败---医疗案例前置条件验证)
2. [验方导入问题 - 药材缺失和价格更新](#2-验方导入问题---药材缺失和价格更新)
3. [处方价格计算错误 - 折扣和剂量校验](#3-处方价格计算错误---折扣和剂量校验)
4. [处方打印权限控制 - 版本管理和重印限制](#4-处方打印权限控制---版本管理和重印限制)
5. [处方审核失败 - 配伍禁忌和剂量超限](#5-处方审核失败---配伍禁忌和剂量超限)
6. [处方修改权限 - 当天编辑规则和状态检查](#6-处方修改权限---当天编辑规则和状态检查)
7. [处方编号重复 - 唯一性保证和生成策略](#7-处方编号重复---唯一性保证和生成策略)
8. [验方个性化调整 - 药材增减和剂量修改](#8-验方个性化调整---药材增减和剂量修改)
9. [处方历史查询 - 患者用药记录和统计分析](#9-处方历史查询---患者用药记录和统计分析)
10. [多验方合并 - 药材冲突处理和剂量累加](#10-多验方合并---药材冲突处理和剂量累加)

---

## 1. 处方创建失败 - 医疗案例前置条件验证

### ❌ 问题描述

创建处方时出现"该医疗案例未确认需要处方"或"该医疗案例已存在处方"等错误。

### 🔍 根因分析

1. **医疗案例未完成诊断** - Consultation表中的四诊信息未填写完整
2. **处方需求未确认** - MedicalCase.NeedsPrescription字段为null或false
3. **处方已存在** - MedicalCase已关联了Prescription记录
4. **权限不足** - 非当天创建的医疗案例无权限创建处方
5. **数据一致性** - MedicalCase与Consultation的关联关系异常

### ✅ 解决方案

#### 步骤1: 验证医疗案例完整性

```csharp
public async Task<MedicalCaseValidationResult> ValidateMedicalCaseForPrescriptionAsync(Guid medicalCaseId)
{
    var medicalCase = await _medicalCaseRepository
        .GetByConditionAsync(m => m.Id == medicalCaseId,
                             include: m => m.Include(m => m.Patient)
                                            .Include(m => m.Consultation)
                                            .Include(m => m.Prescription));

    var result = new MedicalCaseValidationResult
    {
        MedicalCaseId = medicalCaseId,
        IsValid = true,
        Issues = new List<string>()
    };

    // 检查1: 医疗案例是否存在
    if (medicalCase == null)
    {
        result.IsValid = false;
        result.Issues.Add("医疗案例不存在");
        return result;
    }

    // 检查2: 患者信息完整性
    if (medicalCase.Patient == null)
    {
        result.IsValid = false;
        result.Issues.Add("医疗案例未关联患者信息");
    }
    else if (string.IsNullOrEmpty(medicalCase.Patient.Name))
    {
        result.IsValid = false;
        result.Issues.Add("患者姓名不能为空");
    }

    // 检查3: 诊断信息完整性
    if (medicalCase.Consultation == null)
    {
        result.IsValid = false;
        result.Issues.Add("缺少中医诊断信息");
    }
    else
    {
        var consultationIssues = ValidateConsultationCompleteness(medicalCase.Consultation);
        result.Issues.AddRange(consultationIssues);
    }

    // 检查4: 处方需求确认
    if (!medicalCase.NeedsPrescription.HasValue)
    {
        result.IsValid = false;
        result.Issues.Add("未确认是否需要处方（NeedsPrescription为null）");
    }
    else if (!medicalCase.NeedsPrescription.Value)
    {
        result.IsValid = false;
        result.Issues.Add("该医疗案例确认不需要处方");
    }

    // 检查5: 处方已存在
    if (medicalCase.Prescription != null)
    {
        result.IsValid = false;
        result.Issues.Add($"该医疗案例已存在处方: {medicalCase.Prescription.PrescriptionNumber}");
    }

    // 检查6: 时间权限（当天创建的医疗案例才能创建处方）
    if (medicalCase.CreatedAt.Date < DateTime.Today)
    {
        result.IsValid = false;
        result.Issues.Add("只能为当天创建的医疗案例开具处方");
    }

    return result;
}

private List<string> ValidateConsultationCompleteness(Consultation consultation)
{
    var issues = new List<string>();

    // 必填字段检查
    if (string.IsNullOrWhiteSpace(consultation.ChiefComplaint))
        issues.Add("主诉不能为空");

    if (string.IsNullOrWhiteSpace(consultation.TCMDiagnosis))
        issues.Add("中医诊断不能为空");

    if (string.IsNullOrWhiteSpace(consultation.TreatmentPrinciple))
        issues.Add("治则治法不能为空");

    // 四诊信息检查（至少填两项）
    var diagnosticMethods = new[]
    {
        consultation.Inspection,
        consultation.AuscultationOlfaction,
        consultation.Inquiry,
        consultation.Palpation
    };

    var filledMethods = diagnosticMethods.Count(m => !string.IsNullOrWhiteSpace(m));
    if (filledMethods < 2)
    {
        issues.Add($"四诊信息至少需要填写两项，当前只填写了{filledMethods}项");
    }

    return issues;
}
```

#### 步骤2: 自动修复医疗案例

```csharp
public async Task<MedicalCaseRepairResult> RepairMedicalCaseAsync(Guid medicalCaseId)
{
    var validation = await ValidateMedicalCaseForPrescriptionAsync(medicalCaseId);
    
    if (validation.IsValid)
    {
        return new MedicalCaseRepairResult
        {
            Success = true,
            Message = "医疗案例无需修复"
        };
    }

    var medicalCase = await _medicalCaseRepository
        .GetByConditionAsync(m => m.Id == medicalCaseId,
                             include: m => m.Include(m => m.Consultation));

    var repairedIssues = new List<string>();

    foreach (var issue in validation.Issues)
    {
        try
        {
            if (issue.Contains("未确认是否需要处方"))
            {
                // 自动确认需要处方（根据业务规则）
                medicalCase.NeedsPrescription = true;
                medicalCase.UpdatedBy = _currentUser.Id;
                medicalCase.UpdatedAt = DateTime.UtcNow;
                repairedIssues.Add("已自动确认需要处方");
            }
            else if (issue.Contains("缺少中医诊断信息"))
            {
                // 创建基础诊断记录
                if (medicalCase.Consultation == null)
                {
                    medicalCase.Consultation = new Consultation
                    {
                        Id = Guid.NewGuid(),
                        MedicalCaseId = medicalCaseId,
                        CreatedBy = _currentUser.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    repairedIssues.Add("已创建基础诊断记录");
                }
            }
            // 注意：其他字段需要人工填写，不进行自动修复
        }
        catch (Exception ex)
        {
            _logger.LogError($"修复医疗案例时出错: {ex.Message}");
        }
    }

    await _medicalCaseRepository.UpdateAsync(medicalCase);
    await _medicalCaseRepository.SaveChangesAsync();

    return new MedicalCaseRepairResult
    {
        Success = repairedIssues.Count > 0,
        RepairedIssues = repairedIssues,
        RemainingIssues = validation.Issues.Except(repairedIssues).ToList()
    };
}
```

#### 步骤3: 增强处方创建API

```csharp
public async Task<PrescriptionDto> CreatePrescriptionWithValidationAsync(CreatePrescriptionRequest request)
{
    // 步骤1: 验证医疗案例
    var validationResult = await ValidateMedicalCaseForPrescriptionAsync(request.MedicalCaseId);
    
    if (!validationResult.IsValid)
    {
        // 尝试自动修复
        var repairResult = await RepairMedicalCaseAsync(request.MedicalCaseId);
        
        if (repairResult.Success)
        {
            // 重新验证
            validationResult = await ValidateMedicalCaseForPrescriptionAsync(request.MedicalCaseId);
        }
        
        if (!validationResult.IsValid)
        {
            throw new BusinessException($"医疗案例不符合创建处方条件:\n{string.Join("\n", validationResult.Issues)}");
        }
    }

    // 步骤2: 创建处方（原有逻辑）
    return await CreatePrescriptionAsync(request);
}
```

---

## 2. 验方导入问题 - 药材缺失和价格更新

### ❌ 问题描述

从验方导入处方时出现"药材不存在"或"药材已停用"等错误，导致导入失败。

### 🔍 根因分析

1. **药材信息变更** - 验方中的药材在药材库中被删除或修改
2. **药材状态异常** - 药材被标记为停用或不可用
3. **价格信息缺失** - 药材UnitPrice为null或0
4. **单位不匹配** - 验方中的单位与药材库中的单位不一致
5. **编码不一致** - 药材ID或名称发生变化

### ✅ 解决方案

#### 步骤1: 验方导入前预检查

```csharp
public async Task<FormulaImportCheckResult> CheckFormulaImportAsync(Guid formulaId)
{
    var formula = await _formulaRepository
        .GetByConditionAsync(f => f.Id == formulaId,
                             include: f => f.Include(f => f.Items));

    var checkResult = new FormulaImportCheckResult
    {
        FormulaId = formulaId,
        FormulaName = formula.Name,
        CanImport = true,
        Issues = new List<string>(),
        Warnings = new List<string>()
    };

    if (formula == null)
    {
        checkResult.CanImport = false;
        checkResult.Issues.Add("验方不存在");
        return checkResult;
    }

    var missingHerbs = new List<string>();
    var inactiveHerbs = new List<string>();
    var priceIssues = new List<string>();
    var unitIssues = new List<string>();

    foreach (var formulaItem in formula.Items)
    {
        // 获取当前药材信息
        var currentHerb = await _herbRepository.GetByIdAsync(formulaItem.HerbId);
        
        if (currentHerb == null)
        {
            missingHerbs.Add(formulaItem.HerbName);
            checkResult.CanImport = false;
            continue;
        }

        if (!currentHerb.IsActive)
        {
            inactiveHerbs.Add(formulaItem.HerbName);
            checkResult.Warnings.Add($"药材 {formulaItem.HerbName} 已停用");
        }

        if (!currentHerb.UnitPrice.HasValue || currentHerb.UnitPrice.Value <= 0)
        {
            priceIssues.Add(formulaItem.HerbName);
            checkResult.Warnings.Add($"药材 {formulaItem.HerbName} 价格信息异常");
        }

        if (!string.IsNullOrEmpty(formulaItem.Unit) && 
            !string.IsNullOrEmpty(currentHerb.Unit) &&
            formulaItem.Unit != currentHerb.Unit)
        {
            unitIssues.Add($"{formulaItem.HerbName}({formulaItem.Unit}->{currentHerb.Unit})");
        }
    }

    // 生成问题报告
    if (missingHerbs.Any())
    {
        checkResult.Issues.Add($"以下药材不存在: {string.Join(", ", missingHerbs)}");
    }

    if (inactiveHerbs.Any())
    {
        checkResult.Issues.Add($"以下药材已停用: {string.Join(", ", inactiveHerbs)}");
    }

    if (priceIssues.Any())
    {
        checkResult.Issues.Add($"以下药材价格异常: {string.Join(", ", priceIssues)}");
    }

    if (unitIssues.Any())
    {
        checkResult.Warnings.Add($"单位不一致: {string.Join(", ", unitIssues)}");
    }

    checkResult.MissingHerbs = missingHerbs;
    checkResult.InactiveHerbs = inactiveHerbs;
    checkResult.PriceIssueHerbs = priceIssues;
    checkResult.UnitIssueHerbs = unitIssues;

    return checkResult;
}
```

#### 步骤2: 智能药材替换建议

```csharp
public async Task<List<HerbReplacementSuggestion>> SuggestHerbReplacementsAsync(List<string> missingHerbNames)
{
    var suggestions = new List<HerbReplacementSuggestion>();

    foreach (var missingHerb in missingHerbNames)
    {
        // 获取药材拼音
        var pinyin = await GetHerbPinyinAsync(missingHerb);
        
        // 查找相似药材
        var similarHerbs = await _herbRepository
            .GetQueryable()
            .Where(h => h.IsActive && 
                       (h.Name.Contains(missingHerb) || 
                        missingHerb.Contains(h.Name) ||
                        (!string.IsNullOrEmpty(pinyin) && (h.Pinyin.Contains(pinyin) || pinyin.Contains(h.Pinyin)))))
            .Take(5)
            .ToListAsync();

        if (similarHerbs.Any())
        {
            suggestions.Add(new HerbReplacementSuggestion
            {
                OriginalHerb = missingHerb,
                Suggestions = similarHerbs.Select(h => new HerbSuggestion
                {
                    HerbId = h.Id,
                    HerbName = h.Name,
                    Pinyin = h.Pinyin,
                    Category = h.Category,
                    Similarity = CalculateSimilarity(missingHerb, h.Name, pinyin, h.Pinyin),
                    UnitPrice = h.UnitPrice ?? 0,
                    Unit = h.Unit
                }).OrderByDescending(s => s.Similarity)
                .ToList()
            });
        }
    }

    return suggestions;
}

private double CalculateSimilarity(string original, string candidate, string originalPinyin, string candidatePinyin)
{
    double similarity = 0;
    
    // 名称相似度
    similarity += StringSimilarity(original, candidate) * 0.6;
    
    // 拼音相似度
    if (!string.IsNullOrEmpty(originalPinyin) && !string.IsNullOrEmpty(candidatePinyin))
    {
        similarity += StringSimilarity(originalPinyin, candidatePinyin) * 0.4;
    }
    
    return similarity;
}
```

#### 步骤3: 增强验方导入逻辑

```csharp
public async Task<PrescriptionDto> CreatePrescriptionFromFormulaWithFixAsync(CreatePrescriptionFromFormulaRequest request)
{
    // 步骤1: 预检查验方
    var checkResult = await CheckFormulaImportAsync(request.FormulaId);
    
    if (!checkResult.CanImport)
    {
        // 尝试修复缺失药材
        if (checkResult.MissingHerbs.Any())
        {
            var replacementSuggestions = await SuggestHerbReplacementsAsync(checkResult.MissingHerbs);
            
            if (request.AutoReplaceMissingHerbs && replacementSuggestions.Any())
            {
                request.HerbReplacements = replacementSuggestions
                    .SelectMany(r => r.Suggestions.Take(1)) // 取第一个建议
                    .Select(s => new HerbReplacement
                    {
                        OriginalHerbName = s.HerbName,
                        NewHerbId = s.HerbId,
                        ReplacementReason = "自动替换"
                    })
                    .ToList();
            }
            else
            {
                throw new BusinessException($"验方导入失败，存在缺失药材: {string.Join(", ", checkResult.MissingHerbs)}");
            }
        }
    }

    // 步骤2: 获取验方信息
    var formula = await _formulaRepository
        .GetByConditionAsync(f => f.Id == request.FormulaId,
                             include: f => f.Include(f => f.Items));

    // 步骤3: 创建处方
    var prescription = new Prescription
    {
        Id = Guid.NewGuid(),
        MedicalCaseId = request.MedicalCaseId,
        PrescriptionNumber = await GeneratePrescriptionNumberAsync(),
        Indication = formula.Indication,
        DosageCount = request.DosageCount,
        Advice = request.Advice,
        Discount = request.Discount,
        FormulaSource = formula.Name,
        ReferencedFormulas = formula.Name,
        Status = PrescriptionStatus.Draft,
        CreatedBy = _currentUser.Id
    };

    // 步骤4: 处理验方药材
    var processErrors = new List<string>();
    
    foreach (var formulaItem in formula.Items)
    {
        try
        {
            // 检查是否需要替换
            var replacement = request.HerbReplacements?.FirstOrDefault(r => r.OriginalHerbName == formulaItem.HerbName);
            
            var herbId = replacement?.NewHerbId ?? formulaItem.HerbId;
            var herbName = replacement?.NewHerbName ?? formulaItem.HerbName;
            
            var herb = await _herbRepository.GetByIdAsync(herbId);
            
            if (herb == null || !herb.IsActive)
            {
                processErrors.Add($"药材 {herbName} 不可用");
                continue;
            }

            var prescriptionItem = new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                HerbId = herb.Id,
                HerbName = herb.Name,
                Quantity = formulaItem.Quantity,
                Unit = herb.Unit, // 使用药材库中的单位
                UnitPrice = herb.UnitPrice ?? 0,
                Usage = formulaItem.Usage,
                Remark = replacement != null ? 
                       $"替换药材(原: {formulaItem.HerbName}) - 来自验方: {formula.Name}" : 
                       $"来自验方: {formula.Name}"
            };

            prescription.Items.Add(prescriptionItem);
        }
        catch (Exception ex)
        {
            processErrors.Add($"处理药材 {formulaItem.HerbName} 时出错: {ex.Message}");
        }
    }

    if (processErrors.Any())
    {
        throw new BusinessException($"验方导入过程中出现错误:\n{string.Join("\n", processErrors)}");
    }

    if (!prescription.Items.Any())
    {
        throw new BusinessException("处方中没有可用的药材");
    }

    // 步骤5: 保存处方
    await _prescriptionRepository.AddAsync(prescription);
    await _prescriptionRepository.SaveChangesAsync();

    return await GetPrescriptionAsync(prescription.Id);
}
```

---

## 3. 处方价格计算错误 - 折扣和剂量校验

### ❌ 问题描述

处方总价计算错误，出现价格异常高或异常低的情况，或者折扣应用不正确。

### 🔍 根因分析

1. **单位不统一** - 药材剂量单位与价格单位不匹配
2. **单价未更新** - 药材价格变更后未同步到处方
3. **折扣计算错误** - 折扣值超出有效范围(0-1)
4. **帖数计算错误** - 剂量统计与帖数关系混乱
5. **小数精度问题** - Decimal计算精度丢失

### ✅ 解决方案

#### 步骤1: 价格计算验证服务

```csharp
public class PrescriptionPriceValidationService
{
    public async Task<PriceValidationResult> ValidatePrescriptionPriceAsync(Guid prescriptionId)
    {
        var prescription = await _prescriptionRepository
            .GetByConditionAsync(p => p.Id == prescriptionId,
                                 include: p => p.Include(p => p.Items));

        var validationResult = new PriceValidationResult
        {
            PrescriptionId = prescriptionId,
            IsValid = true,
            Warnings = new List<string>(),
            Errors = new List<string>()
        };

        // 验证1: 折扣范围
        if (prescription.Discount <= 0 || prescription.Discount > 1)
        {
            validationResult.IsValid = false;
            validationResult.Errors.Add($"折扣值 {prescription.Discount} 超出有效范围(0-1)");
        }

        // 验证2: 帖数合理性
        if (prescription.DosageCount <= 0 || prescription.DosageCount > 30)
        {
            validationResult.IsValid = false;
            validationResult.Errors.Add($"帖数 {prescription.DosageCount} 超出合理范围(1-30)");
        }

        // 验证3: 药材单价
        var zeroPriceItems = prescription.Items.Where(item => item.UnitPrice <= 0).ToList();
        if (zeroPriceItems.Any())
        {
            validationResult.Errors.Add($"以下药材单价为0: {string.Join(", ", zeroPriceItems.Select(i => i.HerbName))}");
        }

        // 验证4: 药材剂量合理性
        var dosages = await ValidateHerbDosagesAsync(prescription.Items);
        if (dosages.Any(d => !d.IsValid))
        {
            validationResult.Warnings.AddRange(dosages.Where(d => !d.IsValid)
                .Select(d => $"{d.HerbName} 剂量 {d.CurrentQuantity}{d.Unit} 可能异常(建议: {d.RecommendedQuantity}{d.Unit})"));
        }

        // 验证5: 总价合理性
        var calculatedPrice = CalculatePrescriptionPrice(prescription);
        if (calculatedPrice < 10)
        {
            validationResult.Warnings.Add($"处方价格过低: {calculatedPrice:C}");
        }
        else if (calculatedPrice > 10000)
        {
            validationResult.Warnings.Add($"处方价格过高: {calculatedPrice:C}");
        }

        // 验证6: 单味药价格比例
        var itemPrices = prescription.Items.Select(i => i.Amount).ToList();
        var maxItemPrice = itemPrices.Max();
        var totalItemPrice = itemPrices.Sum();
        
        if (maxItemPrice > totalItemPrice * 0.5) // 单味药超过总价50%
        {
            var expensiveItem = prescription.Items.First(i => i.Amount == maxItemPrice);
            validationResult.Warnings.Add($"单味药 {expensiveItem.HerbName} 价格占比过高: {expensiveItem.Amount:C} ({maxItemPrice/totalItemPrice*100:F1}%)");
        }

        validationResult.CalculatedPrice = calculatedPrice;
        validationResult.ItemPrices = prescription.Items.Select(i => new ItemPriceDetail
        {
            HerbName = i.HerbName,
            Quantity = i.Quantity,
            Unit = i.Unit,
            UnitPrice = i.UnitPrice,
            Amount = i.Amount,
            TotalAmount = i.Amount * prescription.DosageCount * prescription.Discount
        }).ToList();

        return validationResult;
    }

    private async Task<List<DosageValidationResult>> ValidateHerbDosagesAsync(List<PrescriptionItem> items)
    {
        var results = new List<DosageValidationResult>();
        
        foreach (var item in items)
        {
            var herbInfo = await GetHerbDosageInfoAsync(item.HerbId);
            
            if (herbInfo != null)
            {
                var isValid = item.Quantity >= herbInfo.MinDosage && item.Quantity <= herbInfo.MaxDosage;
                
                results.Add(new DosageValidationResult
                {
                    HerbName = item.HerbName,
                    CurrentQuantity = item.Quantity,
                    Unit = item.Unit,
                    MinDosage = herbInfo.MinDosage,
                    MaxDosage = herbInfo.MaxDosage,
                    RecommendedQuantity = herbInfo.RecommendedDosage,
                    IsValid = isValid
                });
            }
        }
        
        return results;
    }

    private decimal CalculatePrescriptionPrice(Prescription prescription)
    {
        var perDosePrice = prescription.Items.Sum(item => item.Amount);
        var totalPrice = perDosePrice * prescription.DosageCount * prescription.Discount;
        
        // 确保精度
        return Math.Round(totalPrice, 2);
    }
}
```

#### 步骤2: 价格自动修复

```csharp
public async Task<PrescriptionPriceFixResult> FixPrescriptionPriceAsync(Guid prescriptionId)
{
    var validationResult = await _priceValidationService.ValidatePrescriptionPriceAsync(prescriptionId);
    
    if (validationResult.IsValid && !validationResult.Warnings.Any())
    {
        return new PrescriptionPriceFixResult
        {
            Success = true,
            Message = "处方价格无需修复"
        };
    }

    var prescription = await _prescriptionRepository
        .GetByConditionAsync(p => p.Id == prescriptionId,
                             include: p => p.Include(p => p.Items));

    var fixesApplied = new List<string>();

    // 修复1: 折扣值
    if (prescription.Discount <= 0 || prescription.Discount > 1)
    {
        var originalDiscount = prescription.Discount;
        prescription.Discount = Math.Max(0.1m, Math.Min(1.0m, prescription.Discount)); // 限制在0.1-1.0之间
        fixesApplied.Add($"折扣值从 {originalDiscount} 修复为 {prescription.Discount}");
    }

    // 修复2: 药材单价
    var updatedItems = new List<PrescriptionItem>();
    foreach (var item in prescription.Items)
    {
        if (item.UnitPrice <= 0)
        {
            var currentHerb = await _herbRepository.GetByIdAsync(item.HerbId);
            if (currentHerb?.UnitPrice > 0)
            {
                var originalPrice = item.UnitPrice;
                item.UnitPrice = currentHerb.UnitPrice.Value;
                fixesApplied.Add($"药材 {item.HerbName} 单价从 {originalPrice:C} 修复为 {item.UnitPrice:C}");
            }
        }
    }

    // 修复3: 重新计算总价
    var originalPrice = CalculatePrescriptionPrice(prescription);
    prescription.CalculateTotalPrice(); // 重新计算
    var newPrice = CalculatePrescriptionPrice(prescription);

    if (Math.Abs(originalPrice - newPrice) > 0.01m)
    {
        fixesApplied.Add($"处方总价从 {originalPrice:C} 修复为 {newPrice:C}");
    }

    prescription.UpdatedBy = "System";
    prescription.UpdatedAt = DateTime.UtcNow;

    await _prescriptionRepository.UpdateAsync(prescription);
    await _prescriptionRepository.SaveChangesAsync();

    return new PrescriptionPriceFixResult
    {
        Success = true,
        FixesApplied = fixesApplied,
        FinalPrice = newPrice
    };
}
```

#### 步骤3: 增强价格计算逻辑

```csharp
public class EnhancedPrescriptionPriceCalculator
{
    public PrescriptionPriceDetail CalculateDetailedPrice(Prescription prescription)
    {
        var detail = new PrescriptionPriceDetail
        {
            PrescriptionId = prescription.Id,
            PrescriptionNumber = prescription.PrescriptionNumber,
            DosageCount = prescription.DosageCount,
            Discount = prescription.Discount
        };

        // 药材明细
        detail.Items = prescription.Items.Select(item => new ItemPriceDetail
        {
            HerbName = item.HerbName,
            Quantity = item.Quantity,
            Unit = item.Unit,
            UnitPrice = item.UnitPrice,
            PerDoseAmount = CalculateItemAmount(item), // 每帖该药材金额
            TotalAmount = CalculateItemTotalAmount(item, prescription.DosageCount), // 总帖数该药材金额
            DiscountedAmount = CalculateItemDiscountedAmount(item, prescription.DosageCount, prescription.Discount) // 折扣后金额
        }).ToList();

        // 汇总统计
        detail.PerDoseSubtotal = detail.Items.Sum(i => i.PerDoseAmount);
        detail.TotalSubtotal = detail.Items.Sum(i => i.TotalAmount);
        detail.TotalDiscount = detail.TotalSubtotal * (1 - prescription.Discount);
        detail.FinalPrice = detail.TotalSubtotal * prescription.Discount;

        // 统计分析
        detail.Statistics = new PriceStatistics
        {
            TotalHerbs = detail.Items.Count,
            AverageHerbPrice = detail.Items.Average(i => i.PerDoseAmount),
            MostExpensiveHerb = detail.Items.OrderByDescending(i => i.PerDoseAmount).FirstOrDefault()?.HerbName,
            LeastExpensiveHerb = detail.Items.OrderBy(i => i.PerDoseAmount).FirstOrDefault()?.HerbName,
            PriceRange = new PriceRange
            {
                Min = detail.Items.Min(i => i.PerDoseAmount),
                Max = detail.Items.Max(i => i.PerDoseAmount),
                Average = detail.Items.Average(i => i.PerDoseAmount)
            }
        };

        return detail;
    }

    private decimal CalculateItemAmount(PrescriptionItem item)
    {
        // 确保高精度计算
        return Math.Round(item.Quantity * item.UnitPrice, 2);
    }

    private decimal CalculateItemTotalAmount(PrescriptionItem item, int dosageCount)
    {
        return Math.Round(CalculateItemAmount(item) * dosageCount, 2);
    }

    private decimal CalculateItemDiscountedAmount(PrescriptionItem item, int dosageCount, decimal discount)
    {
        return Math.Round(CalculateItemTotalAmount(item, dosageCount) * discount, 2);
    }
}
```

---

## 4. 处方打印权限控制 - 版本管理和重印限制

### ❌ 问题描述

处方打印权限控制不严格，出现重复打印、越权打印或版本管理混乱的问题。

### 🔍 根因分析

1. **权限验证缺失** - 未验证用户是否有打印权限
2. **重印限制不严** - 未限制打印次数和时间间隔
3. **版本管理混乱** - 打印版本号生成或更新逻辑错误
4. **日志记录不全** - 打印操作未记录详细的审计日志
5. **状态控制不当** - 已打印或已完成处方仍允许修改

### ✅ 解决方案

#### 步骤1: 打印权限验证服务

```csharp
public class PrescriptionPrintPermissionService
{
    public async Task<PrintPermissionResult> ValidatePrintPermissionAsync(Guid prescriptionId, string userId)
    {
        var prescription = await _prescriptionRepository
            .GetByConditionAsync(p => p.Id == prescriptionId,
                                 include: p => p.Include(p => p.MedicalCase)
                                                .Include(p => p.MedicalCase.Patient)
                                                .Include(p => p.PrintLogs));

        var result = new PrintPermissionResult
        {
            PrescriptionId = prescriptionId,
            CanPrint = true,
            Reasons = new List<string>(),
            Warnings = new List<string>()
        };

        if (prescription == null)
        {
            result.CanPrint = false;
            result.Reasons.Add("处方不存在");
            return result;
        }

        // 权限检查1: 处方状态
        if (prescription.Status == PrescriptionStatus.Cancelled)
        {
            result.CanPrint = false;
            result.Reasons.Add("已取消的处方不能打印");
        }

        if (prescription.Status == PrescriptionStatus.Completed)
        {
            result.Warnings.Add("已完成的处方重新打印需要管理员权限");
        }

        // 权限检查2: 创建人验证（只能打印自己创建的处方）
        if (prescription.CreatedBy != userId)
        {
            // 检查是否是管理员或同科室医生
            var hasAdminPermission = await CheckAdminPermissionAsync(userId);
            var hasDepartmentPermission = await CheckDepartmentPermissionAsync(userId, prescription.UserId);
            
            if (!hasAdminPermission && !hasDepartmentPermission)
            {
                result.CanPrint = false;
                result.Reasons.Add("只能打印自己创建的处方，或需要管理员/同科室权限");
            }
        }

        // 权限检查3: 打印次数限制
        if (prescription.PrintCount >= 3)
        {
            var hasOverPrintPermission = await CheckOverPrintPermissionAsync(userId);
            if (!hasOverPrintPermission)
            {
                result.CanPrint = false;
                result.Reasons.Add("打印次数已达上限（3次），需要管理员权限");
            }
        }

        // 权限检查4: 时间间隔限制（24小时内只能打印一次）
        if (prescription.LastPrintedAt.HasValue && 
            prescription.LastPrintedAt.Value > DateTime.UtcNow.AddHours(-24))
        {
            var hasImmediateReprintPermission = await CheckImmediateReprintPermissionAsync(userId);
            if (!hasImmediateReprintPermission)
            {
                result.CanPrint = false;
                result.Reasons.Add("处方在24小时内只能打印一次，如需立即重印请联系管理员");
            }
        }

        // 权限检查5: 患者信息完整性
        if (string.IsNullOrEmpty(prescription.MedicalCase?.Patient?.Name))
        {
            result.CanPrint = false;
            result.Reasons.Add("患者信息不完整，无法打印");
        }

        // 警告信息
        if (prescription.PrintCount > 0)
        {
            result.Warnings.Add($"该处方已打印 {prescription.PrintCount} 次，这是第 {prescription.PrintCount + 1} 次打印");
        }

        if (prescription.LastPrintedAt.HasValue)
        {
            result.Warnings.Add($"上次打印时间: {prescription.LastPrintedAt.Value:yyyy-MM-dd HH:mm}");
        }

        return result;
    }

    private async Task<bool> CheckAdminPermissionAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Role == "Admin" || user?.Permissions?.Contains("Prescription.AdminPrint") == true;
    }

    private async Task<bool> CheckDepartmentPermissionAsync(string userId, Guid prescriptionCreatorId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var creator = await _userRepository.GetByIdAsync(prescriptionCreatorId);
        
        return user?.DepartmentId == creator?.DepartmentId;
    }

    private async Task<bool> CheckOverPrintPermissionAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Role == "Admin" || user?.Permissions?.Contains("Prescription.OverPrint") == true;
    }

    private async Task<bool> CheckImmediateReprintPermissionAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Role == "Admin" || user?.Permissions?.Contains("Prescription.ImmediateReprint") == true;
    }
}
```

#### 步骤2: 增强打印服务

```csharp
public class EnhancedPrescriptionPrintService
{
    public async Task<PrescriptionPrintResult> PrintPrescriptionWithControlAsync(PrintPrescriptionRequest request)
    {
        // 步骤1: 权限验证
        var permissionResult = await _printPermissionService.ValidatePrintPermissionAsync(request.PrescriptionId, request.UserId);
        
        if (!permissionResult.CanPrint)
        {
            throw new BusinessException($"打印权限验证失败:\n{string.Join("\n", permissionResult.Reasons)}");
        }

        // 步骤2: 获取处方信息
        var prescription = await _prescriptionRepository
            .GetByConditionAsync(p => p.Id == request.PrescriptionId,
                                 include: p => p.Include(p => p.MedicalCase)
                                                .Include(p => p.MedicalCase.Patient)
                                                .Include(p => p.Items)
                                                .Include(p => p.PrintLogs));

        // 步骤3: 版本管理
        var newVersion = prescription.PrintVersion + 1;
        var printTime = DateTime.UtcNow;

        // 步骤4: 生成打印数据
        var printData = await GenerateSecurePrintDataAsync(prescription, request);

        // 步骤5: 更新处方打印信息
        prescription.PrintVersion = newVersion;
        prescription.LastPrintedAt = printTime;
        prescription.PrintCount += 1;
        prescription.IsPrinted = true;
        
        // 根据打印次数更新状态
        if (prescription.Status == PrescriptionStatus.Draft)
        {
            prescription.Status = PrescriptionStatus.Printed;
        }

        prescription.UpdatedBy = request.UserId;
        prescription.UpdatedAt = printTime;

        // 步骤6: 记录打印日志
        var printLog = new PrescriptionPrintLog
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescription.Id,
            PrintVersion = newVersion,
            PrintedAt = printTime,
            PrintedBy = request.UserId,
            PrinterName = request.PrinterName,
            PrintReason = request.Reason,
            IPAddress = request.IPAddress,
            UserAgent = request.UserAgent,
            PrintDataHash = GeneratePrintDataHash(printData)
        };

        prescription.PrintLogs.Add(printLog);

        // 步骤7: 保存更新
        await _prescriptionRepository.UpdateAsync(prescription);
        await _prescriptionRepository.SaveChangesAsync();

        // 步骤8: 记录审计日志
        await _auditLogService.LogPrescriptionPrintAsync(new PrescriptionPrintAuditLog
        {
            PrescriptionId = prescription.Id,
            PrescriptionNumber = prescription.PrescriptionNumber,
            PrintVersion = newVersion,
            PrintedBy = request.UserId,
            PrintTime = printTime,
            PrintReason = request.Reason,
            Warnings = permissionResult.Warnings
        });

        return new PrescriptionPrintResult
        {
            PrescriptionId = prescription.Id,
            PrescriptionNumber = prescription.PrescriptionNumber,
            PrintVersion = newVersion,
            PrintCount = prescription.PrintCount,
            PrintTime = printTime,
            PrintData = printData,
            Warnings = permissionResult.Warnings,
            QRCode = GeneratePrintQRCode(prescription.Id, newVersion)
        };
    }

    private async Task<SecurePrescriptionPrintData> GenerateSecurePrintDataAsync(Prescription prescription, PrintPrescriptionRequest request)
    {
        var printData = new SecurePrescriptionPrintData
        {
            // 基本信息（脱敏处理）
            BasicInfo = new PrescriptionBasicPrintInfo
            {
                PrescriptionNumber = prescription.PrescriptionNumber,
                PrintDate = DateTime.Now.ToString("yyyy年MM月dd日"),
                PrintVersion = prescription.PrintVersion + 1,
                PrintTime = DateTime.Now.ToString("HH:mm")
            },

            // 患者信息（隐私保护）
            PatientInfo = new PatientPrintInfo
            {
                Name = MaskSensitiveData(prescription.MedicalCase?.Patient?.Name),
                Age = CalculateAge(prescription.MedicalCase?.Patient?.DateOfBirth),
                Gender = GetGenderText(prescription.MedicalCase?.Patient?.Gender)
                // 不包含敏感信息如身份证号、详细地址等
            },

            // 处方内容
            PrescriptionContent = prescription.Items.Select((item, index) => new PrescriptionItemPrintInfo
            {
                Sequence = index + 1,
                HerbName = item.HerbName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Usage = item.Usage,
                DailyDosage = CalculateDailyDosage(item, prescription.DosageCount)
            }).ToList(),

            // 医师信息
            DoctorInfo = new DoctorPrintInfo
            {
                Name = await GetDoctorDisplayNameAsync(prescription.UserId),
                License = await GetDoctorLicenseDisplayAsync(prescription.UserId),
                Department = "中医科"
            },

            // 安全特征
            SecurityFeatures = new PrintSecurityFeatures
            {
                Watermark = GenerateWatermark(prescription.Id),
                DigitalSignature = GenerateDigitalSignature(prescription),
                VerificationCode = GenerateVerificationCode(prescription.Id),
                QRCodeContent = GenerateQRContent(prescription.Id, prescription.PrintVersion + 1)
            }
        };

        return printData;
    }

    private string MaskSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data) || data.Length <= 2)
            return data;

        return data.Substring(0, 1) + new string('*', data.Length - 2) + data.Substring(data.Length - 1);
    }

    private string GenerateWatermark(Guid prescriptionId)
    {
        return $"LYBT-SECURE-{DateTime.Now:yyyyMMdd}-{prescriptionId.ToString("N")[..8].ToUpper()}";
    }

    private string GenerateDigitalSignature(Prescription prescription)
    {
        var data = $"{prescription.Id}|{prescription.PrescriptionNumber}|{prescription.PrintVersion}";
        return ComputeSHA256Hash(data);
    }
}
```

#### 步骤3: 打印历史查询和管理

```csharp
public async Task<PrescriptionPrintHistoryResult> GetPrintHistoryAsync(Guid prescriptionId)
{
    var prescription = await _prescriptionRepository
        .GetByConditionAsync(p => p.Id == prescriptionId,
                             include: p => p.Include(p => p.PrintLogs)
                                            .Include(p => p.MedicalCase)
                                            .Include(p => p.MedicalCase.Patient));

    return new PrescriptionPrintHistoryResult
    {
        PrescriptionInfo = new PrescriptionPrintInfo
        {
            Id = prescription.Id,
            PrescriptionNumber = prescription.PrescriptionNumber,
            PatientName = prescription.MedicalCase?.Patient?.Name,
            TotalPrintCount = prescription.PrintCount,
            LastPrintTime = prescription.LastPrintedAt,
            CurrentVersion = prescription.PrintVersion,
            Status = prescription.Status
        },

        PrintHistory = prescription.PrintLogs
            .OrderByDescending(log => log.PrintedAt)
            .Select(log => new PrintHistoryItem
            {
                PrintVersion = log.PrintVersion,
                PrintTime = log.PrintedAt,
                PrintedBy = log.PrintedByUser?.Name,
                PrinterName = log.PrinterName,
                PrintReason = log.PrintReason,
                IPAddress = log.IPAddress,
                VerificationHash = log.PrintDataHash
            }).ToList(),

        PrintStatistics = new PrintStatistics
        {
            TotalPrints = prescription.PrintCount,
            FirstPrintTime = prescription.PrintLogs.Min(log => log.PrintedAt),
            LastPrintTime = prescription.PrintLogs.Max(log => log.PrintedAt),
            UniquePrinters = prescription.PrintLogs.Select(log => log.PrinterName).Distinct().Count(),
            PrintFrequency = CalculatePrintFrequency(prescription.PrintLogs)
        }
    };
}
```

---

## 5. 处方审核失败 - 配伍禁忌和剂量超限

### ❌ 问题描述

处方审核不通过，出现"存在配伍禁忌"或"药材剂量超出安全范围"等警告。

### 🔍 根因分析

1. **配伍禁忌检查不完整** - 缺少完整的中药配伍禁忌数据库
2. **剂量标准缺失** - 没有建立标准的药材安全剂量范围
3. **审核规则过于严格** - 验证规则不符合临床实际
4. **缺少人工审核机制** - 自动审核失败后无法人工干预
5. **警告级别分级不当** - 所有问题都视为严重错误

### ✅ 解决方案

#### 步骤1: 完善配伍禁忌数据库

```csharp
public class HerbCompatibilityService
{
    private readonly List<HerbIncompatibilityRule> _incompatibilityRules;
    private readonly List<HerbContraindicationRule> _contraindicationRules;

    public HerbCompatibilityService()
    {
        _incompatibilityRules = InitializeIncompatibilityRules();
        _contraindicationRules = InitializeContraindicationRules();
    }

    private List<HerbIncompatibilityRule> InitializeIncompatibilityRules()
    {
        return new List<HerbIncompatibilityRule>
        {
            // 十八反
            new HerbIncompatibilityRule
            {
                HerbA = "甘草",
                HerbB = "海藻",
                IncompatibilityType = IncompatibilityType.Opposite,
                Severity = SeverityLevel.High,
                Description = "甘草反海藻",
                Reference = "《神农本草经》十八反"
            },
            new HerbIncompatibilityRule
            {
                HerbA = "甘草",
                HerbB = "甘遂",
                IncompatibilityType = IncompatibilityType.Opposite,
                Severity = SeverityLevel.High,
                Description = "甘草反甘遂",
                Reference = "《神农本草经》十八反"
            },
            new HerbIncompatibilityRule
            {
                HerbA = "乌头",
                HerbB = "贝母",
                IncompatibilityType = IncompatibilityType.Opposite,
                Severity = SeverityLevel.High,
                Description = "乌头反贝母",
                Reference = "《神农本草经》十八反"
            },
            
            // 十九畏
            new HerbIncompatibilityRule
            {
                HerbA = "人参",
                HerbB = "五灵脂",
                IncompatibilityType = IncompatibilityType.MutualRestraint,
                Severity = SeverityLevel.Medium,
                Description = "人参畏五灵脂",
                Reference = "《神农本草经》十九畏"
            },
            new HerbIncompatibilityRule
            {
                HerbA = "官桂",
                HerbB = "赤石脂",
                IncompatibilityType = IncompatibilityType.MutualRestraint,
                Severity = SeverityLevel.Medium,
                Description = "官桂畏赤石脂",
                Reference = "《神农本草经》十九畏"
            }
        };
    }

    private List<HerbContraindicationRule> InitializeContraindicationRules()
    {
        return new List<HerbContraindicationRule>
        {
            new HerbContraindicationRule
            {
                HerbName = "附子",
                ContraindicationType = ContraindicationType.Pregnancy,
                Severity = SeverityLevel.High,
                Description = "孕妇禁用",
                Conditions = new List<string> { "怀孕" }
            },
            new HerbContraindicationRule
            {
                HerbName = "大黄",
                ContraindicationType = ContraindicationType.Menstruation,
                Severity = SeverityLevel.Medium,
                Description = "经期慎用",
                Conditions = new List<string> { "月经期" }
            }
        };
    }

    public async Task<CompatibilityCheckResult> CheckHerbCompatibilityAsync(List<string> herbNames)
    {
        var result = new CompatibilityCheckResult
        {
            IsCompatible = true,
            Conflicts = new List<HerbConflict>(),
            Warnings = new List<HerbWarning>()
        };

        // 检查配伍禁忌
        var conflicts = await FindIncompatibilitiesAsync(herbNames);
        if (conflicts.Any())
        {
            result.IsCompatible = false;
            result.Conflicts = conflicts;
        }

        // 检查配伍慎用
        var warnings = await FindWarningsAsync(herbNames);
        if (warnings.Any())
        {
            result.Warnings = warnings;
        }

        return result;
    }

    private async Task<List<HerbConflict>> FindIncompatibilitiesAsync(List<string> herbNames)
    {
        var conflicts = new List<HerbConflict>();

        foreach (var rule in _incompatibilityRules.Where(r => r.Severity == SeverityLevel.High))
        {
            if (herbNames.Contains(rule.HerbA) && herbNames.Contains(rule.HerbB))
            {
                conflicts.Add(new HerbConflict
                {
                    HerbA = rule.HerbA,
                    HerbB = rule.HerbB,
                    ConflictType = rule.IncompatibilityType,
                    Severity = rule.Severity,
                    Description = rule.Description,
                    Reference = rule.Reference,
                    Recommendation = "建议避免同时使用"
                });
            }
        }

        return conflicts;
    }

    private async Task<List<HerbWarning>> FindWarningsAsync(List<string> herbNames)
    {
        var warnings = new List<HerbWarning>();

        // 检查中度配伍禁忌
        foreach (var rule in _incompatibilityRules.Where(r => r.Severity == SeverityLevel.Medium))
        {
            if (herbNames.Contains(rule.HerbA) && herbNames.Contains(rule.HerbB))
            {
                warnings.Add(new HerbWarning
                {
                    HerbA = rule.HerbA,
                    HerbB = rule.HerbB,
                    WarningType = rule.IncompatibilityType,
                    Description = rule.Description,
                    Recommendation = "建议谨慎使用，注意观察患者反应"
                });
            }
        }

        return warnings;
    }
}
```

#### 步骤2: 剂量安全检查服务

```csharp
public class HerbDosageSafetyService
{
    private readonly Dictionary<string, HerbDosageRange> _dosageRanges;

    public HerbDosageSafetyService()
    {
        _dosageRanges = InitializeDosageRanges();
    }

    private Dictionary<string, HerbDosageRange> InitializeDosageRanges()
    {
        return new Dictionary<string, HerbDosageRange>
        {
            // 补气药
            ["人参"] = new HerbDosageRange
            {
                MinDosage = 3,
                MaxDosage = 30,
                RecommendedDosage = 9,
                Unit = "g",
                Category = "补气药",
                Notes = "大剂量（15-30g）用于救脱，小剂量（3-9g）用于补气"
            },
            ["黄芪"] = new HerbDosageRange
            {
                MinDosage = 9,
                MaxDosage = 120,
                RecommendedDosage = 30,
                Unit = "g",
                Category = "补气药",
                Notes = "一般剂量9-30g，大剂量30-120g"
            },
            
            // 活血药
            ["丹参"] = new HerbDosageRange
            {
                MinDosage = 9,
                MaxDosage = 60,
                RecommendedDosage = 15,
                Unit = "g",
                Category = "活血药",
                Notes = "常规剂量9-15g，大剂量可用于心脑血管疾病"
            },
            
            // 清热药
            ["黄连"] = new HerbDosageRange
            {
                MinDosage = 2,
                MaxDosage = 12,
                RecommendedDosage = 6,
                Unit = "g",
                Category = "清热药",
                Notes = "小剂量（2-3g）清热燥湿，大剂量（6-12g）泻火解毒"
            },
            
            // 附子等毒性药材需要特殊处理
            ["附子"] = new HerbDosageRange
            {
                MinDosage = 3,
                MaxDosage = 60,
                RecommendedDosage = 9,
                Unit = "g",
                Category = "温里药",
                Toxicity = ToxicityLevel.Toxic,
                Notes = "必须先煎30-60分钟，从小剂量开始",
                RequirePreparation = true,
                PreparationMethod = "先煎"
            }
        };
    }

    public async Task<DosageSafetyCheckResult> CheckDosageSafetyAsync(List<PrescriptionItem> items)
    {
        var result = new DosageSafetyCheckResult
        {
            IsSafe = true,
            DosageIssues = new List<DosageIssue>(),
            Recommendations = new List<string>()
        };

        foreach (var item in items)
        {
            if (_dosageRanges.TryGetValue(item.HerbName, out var dosageRange))
            {
                var issue = await CheckSingleHerbDosageAsync(item, dosageRange);
                if (issue != null)
                {
                    result.DosageIssues.Add(issue);
                    
                    if (issue.Severity == SeverityLevel.High)
                    {
                        result.IsSafe = false;
                    }
                }
            }
            else
            {
                // 未知药材，给出建议
                result.Recommendations.Add($"药材 {item.HerbName} 缺少剂量标准，建议确认用量");
            }
        }

        // 检查总剂量合理性
        var totalDosage = items.Sum(i => i.Quantity);
        if (totalDosage > 200)
        {
            result.Recommendations.Add("处方总剂量较大，建议考虑减量或分帖服用");
        }

        return result;
    }

    private async Task<DosageIssue> CheckSingleHerbDosageAsync(PrescriptionItem item, HerbDosageRange dosageRange)
    {
        if (item.Quantity < dosageRange.MinDosage)
        {
            return new DosageIssue
            {
                HerbName = item.HerbName,
                CurrentDosage = item.Quantity,
                RecommendedRange = $"{dosageRange.MinDosage}-{dosageRange.MaxDosage}{dosageRange.Unit}",
                Severity = SeverityLevel.Medium,
                IssueType = DosageIssueType.UnderDosage,
                Description = $"剂量偏低，当前用量 {item.Quantity}{dosageRange.Unit}",
                Recommendation = $"建议增加到 {dosageRange.RecommendedDosage}{dosageRange.Unit} 左右"
            };
        }

        if (item.Quantity > dosageRange.MaxDosage)
        {
            return new DosageIssue
            {
                HerbName = item.HerbName,
                CurrentDosage = item.Quantity,
                RecommendedRange = $"{dosageRange.MinDosage}-{dosageRange.MaxDosage}{dosageRange.Unit}",
                Severity = SeverityLevel.High,
                IssueType = DosageIssueType.OverDosage,
                Description = $"剂量超限，当前用量 {item.Quantity}{dosageRange.Unit}，最大安全剂量 {dosageRange.MaxDosage}{dosageRange.Unit}",
                Recommendation = $"建议减量至 {dosageRange.MaxDosage}{dosageRange.Unit} 以下"
            };
        }

        return null;
    }
}
```

#### 步骤3: 分级审核系统

```csharp
public class TieredPrescriptionAuditService
{
    private readonly HerbCompatibilityService _compatibilityService;
    private readonly HerbDosageSafetyService _dosageSafetyService;

    public async Task<TieredAuditResult> PerformTieredAuditAsync(Guid prescriptionId)
    {
        var prescription = await _prescriptionRepository
            .GetByConditionAsync(p => p.Id == prescriptionId,
                                 include: p => p.Include(p => p.Items));

        var result = new TieredAuditResult
        {
            PrescriptionId = prescriptionId,
            OverallResult = AuditResult.Pass,
            AuditItems = new List<AuditItem>()
        };

        // 第一级：基础检查（必须通过）
        var basicCheck = await PerformBasicAuditAsync(prescription);
        result.AuditItems.Add(basicCheck);

        if (basicCheck.Result == AuditResult.Fail)
        {
            result.OverallResult = AuditResult.Fail;
            return result;
        }

        // 第二级：安全检查（警告级别）
        var safetyCheck = await PerformSafetyAuditAsync(prescription);
        result.AuditItems.Add(safetyCheck);

        // 第三级：优化建议（信息级别）
        var optimizationCheck = await PerformOptimizationAuditAsync(prescription);
        result.AuditItems.Add(optimizationCheck);

        // 综合判断
        var hasWarnings = result.AuditItems.Any(item => item.Result == AuditResult.Warning);
        var hasInfo = result.AuditItems.Any(item => item.Result == AuditResult.Info);

        if (hasWarnings)
        {
            result.OverallResult = AuditResult.Warning;
        }
        else if (hasInfo)
        {
            result.OverallResult = AuditResult.Info;
        }

        return result;
    }

    private async Task<AuditItem> PerformBasicAuditAsync(Prescription prescription)
    {
        var issues = new List<string>();

        // 基础检查1：处方非空
        if (!prescription.Items.Any())
        {
            issues.Add("处方不能为空");
        }

        // 基础检查2：药材数量限制
        if (prescription.Items.Count > 30)
        {
            issues.Add("处方药材数量过多（超过30味）");
        }

        // 基础检查3：严重配伍禁忌
        var herbNames = prescription.Items.Select(i => i.HerbName).ToList();
        var compatibilityResult = await _compatibilityService.CheckHerbCompatibilityAsync(herbNames);

        if (!compatibilityResult.IsCompatible)
        {
            issues.AddRange(compatibilityResult.Conflicts.Select(c => $"严重配伍禁忌：{c.HerbA} 与 {c.HerbB} - {c.Description}"));
        }

        // 基础检查4：严重剂量超限
        var dosageCheck = await _dosageSafetyService.CheckDosageSafetyAsync(prescription.Items);
        var severeDosageIssues = dosageCheck.DosageIssues.Where(d => d.Severity == SeverityLevel.High);

        if (severeDosageIssues.Any())
        {
            issues.AddRange(severeDosageIssues.Select(d => $"严重剂量超限：{d.HerbName} {d.Description}"));
        }

        return new AuditItem
        {
            Category = "基础检查",
            Result = issues.Any() ? AuditResult.Fail : AuditResult.Pass,
            Issues = issues,
            Level = AuditLevel.Required
        };
    }

    private async Task<AuditItem> PerformSafetyAuditAsync(Prescription prescription)
    {
        var warnings = new List<string>();

        var herbNames = prescription.Items.Select(i => i.HerbName).ToList();
        
        // 安全检查1：配伍慎用
        var compatibilityResult = await _compatibilityService.CheckHerbCompatibilityAsync(herbNames);
        if (compatibilityResult.Warnings.Any())
        {
            warnings.AddRange(compatibilityResult.Warnings.Select(w => $"配伍慎用：{w.HerbA} 与 {w.HerbB} - {w.Recommendation}"));
        }

        // 安全检查2：剂量注意事项
        var dosageCheck = await _dosageSafetyService.CheckDosageSafetyAsync(prescription.Items);
        var moderateDosageIssues = dosageCheck.DosageIssues.Where(d => d.Severity == SeverityLevel.Medium);

        if (moderateDosageIssues.Any())
        {
            warnings.AddRange(moderateDosageIssues.Select(d => $"剂量注意：{d.HerbName} - {d.Recommendation}"));
        }

        // 安全检查3：特殊人群禁忌
        var contraindicationWarnings = await CheckSpecialPopulationContraindicationsAsync(prescription);
        warnings.AddRange(contraindicationWarnings);

        return new AuditItem
        {
            Category = "安全检查",
            Result = warnings.Any() ? AuditResult.Warning : AuditResult.Pass,
            Issues = warnings,
            Level = AuditLevel.Warning
        };
    }

    private async Task<AuditItem> PerformOptimizationAuditAsync(Prescription prescription)
    {
        var suggestions = new List<string>();

        // 优化建议1：价格优化
        var totalPrice = prescription.CalculateTotalPrice();
        if (totalPrice > 500)
        {
            suggestions.Add($"处方价格较高（{totalPrice:C}），可考虑优化药材配比降低成本");
        }

        // 优化建议2：剂量优化
        var perDosePrice = prescription.Items.Sum(i => i.Amount);
        if (perDosePrice < 10)
        {
            suggestions.Add("单帖价格偏低，建议检查剂量设置");
        }

        // 优化建议3：药材配伍优化
        var optimizationSuggestions = await SuggestOptimizationAsync(prescription);
        suggestions.AddRange(optimizationSuggestions);

        return new AuditItem
        {
            Category = "优化建议",
            Result = suggestions.Any() ? AuditResult.Info : AuditResult.Pass,
            Issues = suggestions,
            Level = AuditLevel.Info
        };
    }

    private async Task<List<string>> CheckSpecialPopulationContraindicationsAsync(Prescription prescription)
    {
        // 这里可以结合患者信息检查特殊禁忌
        // 如孕妇、儿童、老人等特殊人群的用药禁忌
        return new List<string>();
    }

    private async Task<List<string>> SuggestOptimizationAsync(Prescription prescription)
    {
        // 基于中医理论和临床经验提供优化建议
        return new List<string>();
    }
}
```

---

## 6. 处方修改权限 - 当天编辑规则和状态检查

### ❌ 问题描述

处方修改权限控制不当，出现非创建人修改处方、已打印处方仍可修改等问题。

### 🔍 根因分析

1. **权限检查缺失** - 未验证用户是否有修改权限
2. **时间限制不严** - 未严格执行"当天创建才能修改"的规则
3. **状态验证不足** - 未检查处方当前状态是否允许修改
4. **操作日志不全** - 修改操作未记录详细日志
5. **并发控制缺失** - 多人同时修改同一处方导致数据冲突

### ✅ 解决方案

#### 步骤1: 修改权限验证服务

```csharp
public class PrescriptionModificationPermissionService
{
    public async Task<ModificationPermissionResult> ValidateModificationPermissionAsync(
        Guid prescriptionId, string userId, PrescriptionModificationType modificationType)
    {
        var prescription = await _prescriptionRepository
            .GetByConditionAsync(p => p.Id == prescriptionId,
                                 include: p => p.Include(p => p.PrintLogs));

        var result = new ModificationPermissionResult
        {
            PrescriptionId = prescriptionId,
            UserId = userId,
            ModificationType = modificationType,
            CanModify = true,
            Reasons = new List<string>(),
            Warnings = new List<string>()
        };

        if (prescription == null)
        {
            result.CanModify = false;
            result.Reasons.Add("处方不存在");
            return result;
        }

        // 权限检查1: 创建人验证
        var isCreator = prescription.CreatedBy == userId;
        var isAdmin = await CheckAdminPermissionAsync(userId);
        var hasEditPermission = await CheckEditPermissionAsync(userId);

        if (!isCreator && !isAdmin && !hasEditPermission)
        {
            result.CanModify = false;
            result.Reasons.Add("只有处方创建人或管理员才能修改处方");
        }

        // 权限检查2: 时间限制（当天创建的处方才能修改）
        var isSameDay = prescription.CreatedAt.Date == DateTime.Today;
        var canEditOldPrescription = await CheckOldPrescriptionEditPermissionAsync(userId);

        if (!isSameDay && !canEditOldPrescription)
        {
            result.CanModify = false;
            result.Reasons.Add("只能修改当天创建的处方，修改历史处方需要管理员权限");
        }

        // 权限检查3: 处方状态验证
        var statusPermission = await CheckPrescriptionStatusPermissionAsync(prescription, modificationType);
        if (!statusPermission.CanModify)
        {
            result.CanModify = false;
            result.Reasons.AddRange(statusPermission.Reasons);
        }

        // 权限检查4: 打印状态验证
        if (prescription.IsPrinted)
        {
            var canModifyPrinted = await CheckPrintedPrescriptionEditPermissionAsync(userId, modificationType);
            if (!canModifyPrinted)
            {
                result.CanModify = false;
                result.Reasons.Add("已打印的处方需要管理员权限才能修改");
            }
            else
            {
                result.Warnings.Add("正在修改已打印的处方，将产生新的打印版本");
            }
        }

        // 权限检查5: 特殊修改类型验证
        var typeSpecificPermission = await CheckModificationTypePermissionAsync(prescription, modificationType, userId);
        if (!typeSpecificPermission.CanModify)
        {
            result.CanModify = false;
            result.Reasons.AddRange(typeSpecificPermission.Reasons);
        }

        // 警告信息
        if (isSameDay && !isCreator)
        {
            result.Warnings.Add("您不是该处方的创建人，请谨慎修改");
        }

        if (prescription.PrintCount > 0)
        {
            result.Warnings.Add($"该处方已打印 {prescription.PrintCount} 次，修改将产生新的打印版本");
        }

        return result;
    }

    private async Task<bool> CheckAdminPermissionAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Role == "Admin" || user?.Permissions?.Contains("Prescription.AdminEdit") == true;
    }

    private async Task<bool> CheckEditPermissionAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Permissions?.Contains("Prescription.Edit") == true;
    }

    private async Task<bool> CheckOldPrescriptionEditPermissionAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Role == "Admin" || 
               user?.Permissions?.Contains("Prescription.EditOld") == true ||
               user?.Role == "SeniorDoctor"; // 高级医师可以修改历史处方
    }

    private async Task<StatusPermissionResult> CheckPrescriptionStatusPermissionAsync(
        Prescription prescription, PrescriptionModificationType modificationType)
    {
        var result = new StatusPermissionResult { CanModify = true, Reasons = new List<string>() };

        switch (prescription.Status)
        {
            case PrescriptionStatus.Draft:
                // 草稿状态允许所有修改
                break;

            case PrescriptionStatus.Active:
                if (modificationType == PrescriptionModificationType.Delete)
                {
                    result.CanModify = false;
                    result.Reasons.Add("激活状态的处方不能删除");
                }
                break;

            case PrescriptionStatus.Printed:
                if (modificationType == PrescriptionModificationType.Delete)
                {
                    result.CanModify = false;
                    result.Reasons.Add("已打印的处方不能删除");
                }
                break;

            case PrescriptionStatus.Completed:
                result.CanModify = false;
                result.Reasons.Add("已完成的处方不能修改");
                break;

            case PrescriptionStatus.Cancelled:
                result.CanModify = false;
                result.Reasons.Add("已取消的处方不能修改");
                break;
        }

        return result;
    }

    private async Task<bool> CheckPrintedPrescriptionEditPermissionAsync(string userId, PrescriptionModificationType modificationType)
    {
        // 简单修改（如医嘱、备注）允许编辑
        if (modificationType == PrescriptionModificationType.SimpleEdit)
        {
            return true;
        }

        // 复杂修改需要管理员权限
        return await CheckAdminPermissionAsync(userId);
    }

    private async Task<ModificationPermissionResult> CheckModificationTypePermissionAsync(
        Prescription prescription, PrescriptionModificationType modificationType, string userId)
    {
        var result = new ModificationPermissionResult { CanModify = true, Reasons = new List<string>() };

        switch (modificationType)
        {
            case PrescriptionModificationType.AddItem:
            case PrescriptionModificationType.RemoveItem:
            case PrescriptionModificationType.ModifyItem:
                // 修改药材内容需要高级权限
                if (!await CheckAdvancedEditPermissionAsync(userId))
                {
                    result.CanModify = false;
                    result.Reasons.Add("修改处方药材需要高级医师权限");
                }
                break;

            case PrescriptionModificationType.ModifyDosage:
                // 修改剂量需要医师资质
                if (!await CheckDoctorQualificationAsync(userId))
                {
                    result.CanModify = false;
                    result.Reasons.Add("修改处方剂量需要医师资质");
                }
                break;

            case PrescriptionModificationType.ModifyPrice:
                // 修改价格只能由管理员操作
                if (!await CheckAdminPermissionAsync(userId))
                {
                    result.CanModify = false;
                    result.Reasons.Add("修改处方价格只能由管理员操作");
                }
                break;
        }

        return result;
    }

    private async Task<bool> CheckAdvancedEditPermissionAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Role == "Admin" ||
               user?.Role == "SeniorDoctor" ||
               user?.Permissions?.Contains("Prescription.AdvancedEdit") == true;
    }

    private async Task<bool> CheckDoctorQualificationAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Role == "Doctor" ||
               user?.Role == "SeniorDoctor" ||
               user?.Role == "Admin";
    }
}
```

#### 步骤2: 增强处方修改服务

```csharp
public class EnhancedPrescriptionModificationService
{
    public async Task<PrescriptionModificationResult> ModifyPrescriptionAsync(
        ModifyPrescriptionRequest request)
    {
        // 步骤1: 权限验证
        var permissionResult = await _permissionService.ValidateModificationPermissionAsync(
            request.PrescriptionId, request.UserId, request.ModificationType);

        if (!permissionResult.CanModify)
        {
            throw new BusinessException($"处方修改权限验证失败:\n{string.Join("\n", permissionResult.Reasons)}");
        }

        // 步骤2: 获取处方信息（加锁防止并发修改）
        using var transaction = await _prescriptionRepository.BeginTransactionAsync();
        
        var prescription = await _prescriptionRepository
            .GetByConditionAsync(p => p.Id == request.PrescriptionId,
                                 include: p => p.Include(p => p.Items),
                                 lockType: LockType.Update);

        try
        {
            // 步骤3: 记录修改前状态
            var beforeSnapshot = CreatePrescriptionSnapshot(prescription);

            // 步骤4: 执行修改
            var modificationResult = await ExecuteModificationAsync(prescription, request);

            // 步骤5: 处方审核（如果修改了药材）
            if (request.ModificationType == PrescriptionModificationType.AddItem ||
                request.ModificationType == PrescriptionModificationType.RemoveItem ||
                request.ModificationType == PrescriptionModificationType.ModifyItem)
            {
                var auditResult = await _auditService.PerformTieredAuditAsync(prescription.Id);
                
                if (auditResult.OverallResult == AuditResult.Fail)
                {
                    await transaction.RollbackAsync();
                    throw new BusinessException($"修改后处方审核不通过:\n{string.Join("\n", auditResult.AuditItems.Where(i => i.Result == AuditResult.Fail).SelectMany(i => i.Issues))}");
                }
            }

            // 步骤6: 更新处方状态
            if (prescription.IsPrinted)
            {
                prescription.PrintVersion += 1; // 修改已打印处方，版本号递增
            }

            prescription.UpdatedBy = request.UserId;
            prescription.UpdatedAt = DateTime.UtcNow;

            // 步骤7: 保存修改
            await _prescriptionRepository.UpdateAsync(prescription);
            await _prescriptionRepository.SaveChangesAsync();

            await transaction.CommitAsync();

            // 步骤8: 记录修改日志
            var afterSnapshot = CreatePrescriptionSnapshot(prescription);
            await LogModificationAsync(beforeSnapshot, afterSnapshot, request, permissionResult.Warnings);

            return new PrescriptionModificationResult
            {
                PrescriptionId = prescription.Id,
                PrescriptionNumber = prescription.PrescriptionNumber,
                ModificationType = request.ModificationType,
                ModifiedAt = DateTime.UtcNow,
                ModifiedFields = modificationResult.ModifiedFields,
                Warnings = permissionResult.Warnings,
                NewVersion = prescription.PrintVersion
            };
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<ModificationExecutionResult> ExecuteModificationAsync(
        Prescription prescription, ModifyPrescriptionRequest request)
    {
        var result = new ModificationExecutionResult { ModifiedFields = new List<string>() };

        switch (request.ModificationType)
        {
            case PrescriptionModificationType.ModifyBasicInfo:
                result = await ModifyBasicInfoAsync(prescription, request);
                break;

            case PrescriptionModificationType.AddItem:
                result = await AddItemAsync(prescription, request);
                break;

            case PrescriptionModificationType.RemoveItem:
                result = await RemoveItemAsync(prescription, request);
                break;

            case PrescriptionModificationType.ModifyItem:
                result = await ModifyItemAsync(prescription, request);
                break;

            case PrescriptionModificationType.ModifyDosage:
                result = await ModifyDosageAsync(prescription, request);
                break;

            default:
                throw new ArgumentException($"不支持的修改类型: {request.ModificationType}");
        }

        // 重新计算总价
        var oldPrice = prescription.CalculateTotalPrice();
        prescription.CalculateTotalPrice();
        var newPrice = prescription.CalculateTotalPrice();

        if (Math.Abs(oldPrice - newPrice) > 0.01m)
        {
            result.ModifiedFields.Add($"总价: {oldPrice:C} -> {newPrice:C}");
        }

        return result;
    }

    private async Task<ModificationExecutionResult> AddItemAsync(
        Prescription prescription, ModifyPrescriptionRequest request)
    {
        var result = new ModificationExecutionResult { ModifiedFields = new List<string>() };

        foreach (var itemRequest in request.NewItems)
        {
            // 验证药材
            var herb = await _herbRepository.GetByIdAsync(itemRequest.HerbId);
            if (herb == null || !herb.IsActive)
            {
                throw new BusinessException($"药材 {itemRequest.HerbName} 不可用");
            }

            // 检查是否已存在
            var existingItem = prescription.Items.FirstOrDefault(i => i.HerbId == itemRequest.HerbId);
            if (existingItem != null)
            {
                // 合并剂量
                existingItem.Quantity += itemRequest.Quantity;
                result.ModifiedFields.Add($"药材 {existingItem.HerbName} 剂量合并: {existingItem.Quantity - itemRequest.Quantity}{existingItem.Unit} -> {existingItem.Quantity}{existingItem.Unit}");
            }
            else
            {
                // 添加新药材
                var newItem = new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = itemRequest.Quantity,
                    Unit = herb.Unit,
                    UnitPrice = herb.UnitPrice ?? 0,
                    Usage = itemRequest.Usage,
                    Remark = $"添加操作 - {DateTime.Now:yyyy-MM-dd HH:mm}"
                };

                prescription.Items.Add(newItem);
                result.ModifiedFields.Add($"添加药材: {newItem.HerbName} {newItem.Quantity}{newItem.Unit}");
            }
        }

        return result;
    }

    private async Task<ModificationExecutionResult> RemoveItemAsync(
        Prescription prescription, ModifyPrescriptionRequest request)
    {
        var result = new ModificationExecutionResult { ModifiedFields = new List<string>() };

        foreach (var itemId in request.ItemIdsToRemove)
        {
            var item = prescription.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                prescription.Items.Remove(item);
                result.ModifiedFields.Add($"删除药材: {item.HerbName} {item.Quantity}{item.Unit}");
            }
        }

        if (!prescription.Items.Any())
        {
            throw new BusinessException("处方不能为空");
        }

        return result;
    }

    private async Task<ModificationExecutionResult> ModifyItemAsync(
        Prescription prescription, ModifyPrescriptionRequest request)
    {
        var result = new ModificationExecutionResult { ModifiedFields = new List<string>() };

        foreach (var itemRequest in request.ModifiedItems)
        {
            var item = prescription.Items.FirstOrDefault(i => i.Id == itemRequest.ItemId);
            if (item != null)
            {
                var oldQuantity = item.Quantity;
                
                if (itemRequest.NewQuantity.HasValue)
                {
                    item.Quantity = itemRequest.NewQuantity.Value;
                    result.ModifiedFields.Add($"药材 {item.HerbName} 剂量: {oldQuantity}{item.Unit} -> {item.Quantity}{item.Unit}");
                }

                if (!string.IsNullOrEmpty(itemRequest.NewUsage))
                {
                    var oldUsage = item.Usage;
                    item.Usage = itemRequest.NewUsage;
                    result.ModifiedFields.Add($"药材 {item.HerbName} 用法: {oldUsage} -> {item.Usage}");
                }

                if (!string.IsNullOrEmpty(itemRequest.NewRemark))
                {
                    var oldRemark = item.Remark;
                    item.Remark = itemRequest.NewRemark;
                    result.ModifiedFields.Add($"药材 {item.HerbName} 备注: {oldRemark} -> {item.Remark}");
                }
            }
        }

        return result;
    }

    private PrescriptionSnapshot CreatePrescriptionSnapshot(Prescription prescription)
    {
        return new PrescriptionSnapshot
        {
            PrescriptionId = prescription.Id,
            PrescriptionNumber = prescription.PrescriptionNumber,
            Status = prescription.Status,
            DosageCount = prescription.DosageCount,
            Discount = prescription.Discount,
            Indication = prescription.Indication,
            Advice = prescription.Advice,
            Items = prescription.Items.Select(item => new PrescriptionItemSnapshot
            {
                Id = item.Id,
                HerbName = item.HerbName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                Usage = item.Usage,
                Remark = item.Remark
            }).ToList(),
            TotalPrice = prescription.CalculateTotalPrice(),
            PrintVersion = prescription.PrintVersion,
            SnapshotTime = DateTime.UtcNow
        };
    }

    private async Task LogModificationAsync(
        PrescriptionSnapshot before, PrescriptionSnapshot after, 
        ModifyPrescriptionRequest request, List<string> warnings)
    {
        var modificationLog = new PrescriptionModificationLog
        {
            Id = Guid.NewGuid(),
            PrescriptionId = request.PrescriptionId,
            ModifiedBy = request.UserId,
            ModifiedAt = DateTime.UtcNow,
            ModificationType = request.ModificationType.ToString(),
            ModificationReason = request.Reason,
            BeforeSnapshot = JsonSerializer.Serialize(before),
            AfterSnapshot = JsonSerializer.Serialize(after),
            ModifiedFields = string.Join("; ", request.ModificationResult?.ModifiedFields ?? new List<string>()),
            Warnings = string.Join("; ", warnings),
            IPAddress = request.IPAddress,
            UserAgent = request.UserAgent
        };

        await _modificationLogRepository.AddAsync(modificationLog);
        await _modificationLogRepository.SaveChangesAsync();
    }
}
```

---

## ✅ 问题解决总结

通过这个10个问题的系统性解决方案，您已经掌握了处方管理中的关键问题处理能力：

### ✅ 核心问题解决能力

1. **前置条件验证** - 确保医疗案例完整性，自动修复常见问题
2. **验方导入优化** - 智能药材替换建议，增强导入成功率  
3. **价格计算校验** - 精确计算逻辑，自动修复价格异常
4. **打印权限控制** - 多级权限验证，版本管理，重印限制
5. **处方审核增强** - 配伍禁忌检查，剂量安全验证，分级审核
6. **修改权限管理** - 当天编辑规则，状态检查，并发控制

### ✅ 业务流程优化

1. **预防性检查** - 在问题发生前进行验证和预警
2. **自动修复机制** - 对常见问题提供自动修复建议
3. **分级处理策略** - 根据问题严重程度采用不同处理方式
4. **完整操作日志** - 记录所有关键操作，便于追溯和审计
5. **用户友好提示** - 提供清晰的错误信息和解决建议

### ✅ 技术实现亮点

1. **服务化设计** - 将复杂的业务逻辑封装为独立服务
2. **事务管理** - 确保数据一致性，支持并发控制
3. **权限细分** - 基于角色和操作的细粒度权限控制
4. **配置化规则** - 业务规则可配置，便于维护和扩展
5. **异常处理** - 完善的异常处理和错误恢复机制

### 🎯 实践应用建议

1. **定期审核** - 定期检查处方数据质量，及时发现和解决问题
2. **用户培训** - 对医生进行系统使用培训，减少操作错误
3. **规则维护** - 根据业务发展和法规变化，及时更新审核规则
4. **性能监控** - 监控系统性能，确保在数据量增长时保持稳定
5. **备份恢复** - 建立完善的数据备份和恢复机制

通过这些解决方案的实施，可以显著提高处方管理系统的稳定性、安全性和用户体验。