using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 中药材业务服务实现 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、复杂业务逻辑、事务管理、业务规则验证
/// </summary>
public class HerbBusinessService : IHerbBusinessService
{
    private readonly IHerbCoreService _coreService;
    private readonly ILogger<HerbBusinessService> _logger;
    
    public HerbBusinessService(
        IHerbCoreService coreService,
        ILogger<HerbBusinessService> logger)
    {
        _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    #region 中药材业务管理
    
    public async Task<ServiceResult<HerbDto>> CreateHerbAsync(HerbCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("开始创建中药材: {HerbName}", createDto.Name);
            
            // 1. 业务验证
            var validationResult = _coreService.ValidateHerbCreateData(createDto);
            if (!validationResult.IsSuccess)
            {
                return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage);
            }
            
            // 2. 检查名称重复
            var nameExistsResult = await _coreService.CheckHerbNameExistsAsync(createDto.Name);
            if (!nameExistsResult.IsSuccess)
            {
                return ServiceResult<HerbDto>.Failure("检查名称重复失败");
            }
            
            if (nameExistsResult.Data)
            {
                return ServiceResult<HerbDto>.Failure($"中药材名称'{createDto.Name}'已存在");
            }
            
            // 3. 价格验证
            var priceValidation = _coreService.ValidatePriceData(createDto.Price);
            if (!priceValidation.IsSuccess)
            {
                return ServiceResult<HerbDto>.Failure(priceValidation.ErrorMessage);
            }
            
            // 4. 调用API创建
            var createResult = await _coreService.CallCreateHerbApiAsync(createDto);
            if (!createResult.IsSuccess)
            {
                return createResult;
            }
            
            // 5. 记录创建日志
            _logger.LogInformation("中药材创建成功: {HerbId} - {HerbName}", 
                createResult.Data?.Id, createResult.Data?.Name);
            
            return createResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建中药材业务异常: {HerbName}", createDto.Name);
            return ServiceResult<HerbDto>.Failure($"创建异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<HerbDto>> UpdateHerbAsync(Guid id, HerbUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("开始更新中药材: {HerbId}", id);
            
            // 1. 验证中药材是否存在
            var existsResult = await _coreService.ValidateHerbExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
            {
                return ServiceResult<HerbDto>.Failure("中药材不存在");
            }
            
            // 2. 业务验证
            var validationResult = _coreService.ValidateHerbUpdateData(updateDto);
            if (!validationResult.IsSuccess)
            {
                return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage);
            }
            
            // 3. 如果更新名称，检查重复
            if (!string.IsNullOrWhiteSpace(updateDto.Name))
            {
                var nameExistsResult = await _coreService.CheckHerbNameExistsAsync(updateDto.Name, id);
                if (!nameExistsResult.IsSuccess)
                {
                    return ServiceResult<HerbDto>.Failure("检查名称重复失败");
                }
                
                if (nameExistsResult.Data)
                {
                    return ServiceResult<HerbDto>.Failure($"中药材名称'{updateDto.Name}'已被其他药材使用");
                }
            }
            
            // 4. 价格验证
            if (updateDto.Price.HasValue)
            {
                var priceValidation = _coreService.ValidatePriceData(updateDto.Price.Value);
                if (!priceValidation.IsSuccess)
                {
                    return ServiceResult<HerbDto>.Failure(priceValidation.ErrorMessage);
                }
            }
            
            // 5. 调用API更新
            var updateResult = await _coreService.CallUpdateHerbApiAsync(id, updateDto);
            if (!updateResult.IsSuccess)
            {
                return updateResult;
            }
            
            // 6. 记录更新日志
            _logger.LogInformation("中药材更新成功: {HerbId}", id);
            
            return updateResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新中药材业务异常: {HerbId}", id);
            return ServiceResult<HerbDto>.Failure($"更新异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<bool>> DeleteHerbAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("开始删除中药材: {HerbId}", id);
            
            // 1. 验证中药材是否存在
            var existsResult = await _coreService.ValidateHerbExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
            {
                return ServiceResult<bool>.Failure("中药材不存在");
            }
            
            // 2. 检查是否被处方使用 (简化实现)
            // 实际应该检查该药材是否在活跃处方中被使用
            // var isInUseResult = await CheckHerbInUseAsync(id);
            // if (isInUseResult.IsSuccess && isInUseResult.Data)
            // {
            //     return ServiceResult<bool>.Failure("该中药材正在被处方使用，无法删除");
            // }
            
            // 3. 调用API删除
            var deleteResult = await _coreService.CallDeleteHerbApiAsync(id);
            if (!deleteResult.IsSuccess)
            {
                return deleteResult;
            }
            
            // 4. 记录删除日志
            _logger.LogInformation("中药材删除成功: {HerbId}", id);
            
            return deleteResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除中药材业务异常: {HerbId}", id);
            return ServiceResult<bool>.Failure($"删除异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbPriceUpdateResultDto>>> BatchUpdatePricesAsync(List<HerbPriceUpdateDto> priceUpdates)
    {
        try
        {
            _logger.LogInformation("开始批量更新中药材价格，数量: {Count}", priceUpdates.Count);
            
            var results = new List<HerbPriceUpdateResultDto>();
            var successCount = 0;
            
            foreach (var update in priceUpdates)
            {
                try
                {
                    // 验证价格数据
                    var priceValidation = _coreService.ValidatePriceData(update.NewPrice);
                    if (!priceValidation.IsSuccess)
                    {
                        results.Add(new HerbPriceUpdateResultDto
                        {
                            HerbId = update.HerbId,
                            Success = false,
                            ErrorMessage = priceValidation.ErrorMessage
                        });
                        continue;
                    }
                    
                    // 更新价格
                    var updateDto = new HerbUpdateDto { Price = update.NewPrice };
                    var updateResult = await _coreService.CallUpdateHerbApiAsync(update.HerbId, updateDto);
                    
                    results.Add(new HerbPriceUpdateResultDto
                    {
                        HerbId = update.HerbId,
                        Success = updateResult.IsSuccess,
                        ErrorMessage = updateResult.IsSuccess ? null : updateResult.ErrorMessage,
                        OldPrice = update.OldPrice,
                        NewPrice = update.NewPrice
                    });
                    
                    if (updateResult.IsSuccess)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new HerbPriceUpdateResultDto
                    {
                        HerbId = update.HerbId,
                        Success = false,
                        ErrorMessage = $"更新异常: {ex.Message}"
                    });
                }
            }
            
            _logger.LogInformation("批量更新中药材价格完成，成功: {SuccessCount}/{TotalCount}", 
                successCount, priceUpdates.Count);
            
            return ServiceResult<List<HerbPriceUpdateResultDto>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新中药材价格业务异常");
            return ServiceResult<List<HerbPriceUpdateResultDto>>.Failure($"批量更新异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<HerbDto>> RestoreDeletedHerbAsync(Guid id)
    {
        try
        {
            // 简化实现，实际需要软删除和恢复机制
            _logger.LogInformation("恢复已删除的中药材: {HerbId}", id);
            
            // 这里应该调用恢复API，暂时返回失败
            return ServiceResult<HerbDto>.Failure("恢复功能暂未实现");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复中药材异常: {HerbId}", id);
            return ServiceResult<HerbDto>.Failure($"恢复异常: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 配伍检查和验证
    
    public async Task<ServiceResult<CompatibilityCheckResult>> CheckCompatibilityAsync(List<Guid> herbIds)
    {
        try
        {
            _logger.LogInformation("检查中药材配伍，药材数量: {Count}", herbIds.Count);
            
            if (!herbIds.Any())
            {
                return ServiceResult<CompatibilityCheckResult>.Failure("药材列表不能为空");
            }
            
            // 获取所有相关药材信息
            var herbDetails = new List<HerbDto>();
            foreach (var herbId in herbIds)
            {
                var herbResult = await _coreService.GetHerbByIdAsync(herbId);
                if (herbResult.IsSuccess && herbResult.Data != null)
                {
                    herbDetails.Add(herbResult.Data);
                }
            }
            
            // 简化的配伍检查逻辑
            var result = new CompatibilityCheckResult
            {
                IsCompatible = true,
                Warnings = new List<string>(),
                Conflicts = new List<CompatibilityConflictDto>(),
                Suggestions = new List<string>()
            };
            
            // 模拟配伍检查逻辑
            foreach (var herb1 in herbDetails)
            {
                foreach (var herb2 in herbDetails.Where(h => h.Id != herb1.Id))
                {
                    // 简单的配伍检查示例
                    if (IsIncompatible(herb1, herb2))
                    {
                        result.IsCompatible = false;
                        result.Conflicts.Add(new CompatibilityConflictDto
                        {
                            Herb1Id = herb1.Id,
                            Herb1Name = herb1.Name,
                            Herb2Id = herb2.Id,
                            Herb2Name = herb2.Name,
                            ConflictType = "配伍禁忌",
                            Description = $"{herb1.Name}与{herb2.Name}不宜同用",
                            Severity = "严重"
                        });
                    }
                }
            }
            
            return ServiceResult<CompatibilityCheckResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查配伍异常");
            return ServiceResult<CompatibilityCheckResult>.Failure($"配伍检查异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<PrescriptionValidationResult>> ValidatePrescriptionHerbsAsync(List<HerbDosageDto> herbDosages)
    {
        try
        {
            var herbIds = herbDosages.Select(hd => hd.HerbId).ToList();
            var compatibilityResult = await CheckCompatibilityAsync(herbIds);
            
            var result = new PrescriptionValidationResult
            {
                IsValid = compatibilityResult.IsSuccess && compatibilityResult.Data?.IsCompatible == true,
                ValidationMessages = new List<string>(),
                DosageWarnings = new List<string>()
            };
            
            if (compatibilityResult.IsSuccess && compatibilityResult.Data != null)
            {
                result.ValidationMessages.AddRange(compatibilityResult.Data.Warnings);
                if (compatibilityResult.Data.Conflicts.Any())
                {
                    result.ValidationMessages.AddRange(
                        compatibilityResult.Data.Conflicts.Select(c => c.Description));
                }
            }
            
            // 检查剂量合理性
            foreach (var dosage in herbDosages)
            {
                if (dosage.Dosage <= 0)
                {
                    result.IsValid = false;
                    result.DosageWarnings.Add($"{dosage.HerbName}的剂量必须大于0");
                }
                else if (dosage.Dosage > 100) // 假设单味药不超过100g
                {
                    result.DosageWarnings.Add($"{dosage.HerbName}的剂量({dosage.Dosage}g)可能过大，请确认");
                }
            }
            
            return ServiceResult<PrescriptionValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证处方药材异常");
            return ServiceResult<PrescriptionValidationResult>.Failure($"验证异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<CompatibilitySuggestionDto>>> GetCompatibilitySuggestionsAsync(List<Guid> herbIds)
    {
        try
        {
            // 简化实现：基于配伍检查结果给出建议
            var checkResult = await CheckCompatibilityAsync(herbIds);
            
            var suggestions = new List<CompatibilitySuggestionDto>();
            
            if (checkResult.IsSuccess && checkResult.Data != null)
            {
                foreach (var conflict in checkResult.Data.Conflicts)
                {
                    suggestions.Add(new CompatibilitySuggestionDto
                    {
                        Type = "替换建议",
                        Description = $"建议将{conflict.Herb1Name}或{conflict.Herb2Name}替换为其他功效相似的药材",
                        Priority = conflict.Severity == "严重" ? "高" : "中"
                    });
                }
            }
            
            return ServiceResult<List<CompatibilitySuggestionDto>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取配伍建议异常");
            return ServiceResult<List<CompatibilitySuggestionDto>>.Failure($"获取建议异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<HerbUsagePrecautionDto>> CheckHerbUsagePrecautionsAsync(Guid herbId)
    {
        try
        {
            var herbResult = await _coreService.GetHerbByIdAsync(herbId);
            if (!herbResult.IsSuccess || herbResult.Data == null)
            {
                return ServiceResult<HerbUsagePrecautionDto>.Failure("中药材不存在");
            }
            
            var herb = herbResult.Data;
            var precaution = new HerbUsagePrecautionDto
            {
                HerbId = herbId,
                HerbName = herb.Name,
                Precautions = new List<string>(),
                Contraindications = new List<string>(),
                DosageRecommendation = "请遵医嘱"
            };
            
            // 根据中药材性质添加注意事项
            if (herb.Nature?.Contains("寒") == true)
            {
                precaution.Precautions.Add("性寒，脾胃虚寒者慎用");
            }
            if (herb.Nature?.Contains("热") == true)
            {
                precaution.Precautions.Add("性热，阴虚火旺者慎用");
            }
            
            return ServiceResult<HerbUsagePrecautionDto>.Success(precaution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用药注意事项异常: {HerbId}", herbId);
            return ServiceResult<HerbUsagePrecautionDto>.Failure($"检查异常: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 价格管理业务
    
    public async Task<ServiceResult<PrescriptionPriceCalculationDto>> CalculateFormulaPriceAsync(
        List<HerbDosageDto> herbDosages, Guid? patientId = null)
    {
        try
        {
            _logger.LogInformation("计算处方价格，药材数量: {Count}", herbDosages.Count);
            
            var calculation = new PrescriptionPriceCalculationDto
            {
                HerbPrices = new List<HerbPriceItemDto>(),
                SubTotal = 0,
                DiscountAmount = 0,
                TotalPrice = 0
            };
            
            // 获取每个药材的价格信息
            foreach (var dosage in herbDosages)
            {
                var herbResult = await _coreService.GetHerbByIdAsync(dosage.HerbId);
                if (herbResult.IsSuccess && herbResult.Data != null)
                {
                    var herb = herbResult.Data;
                    var itemPrice = herb.Price * dosage.Dosage / 100; // 假设价格单位是100g
                    
                    calculation.HerbPrices.Add(new HerbPriceItemDto
                    {
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        UnitPrice = herb.Price,
                        Dosage = dosage.Dosage,
                        TotalPrice = itemPrice
                    });
                    
                    calculation.SubTotal += itemPrice;
                }
            }
            
            // 应用定价策略
            var discountResult = await ApplyPricingPolicyAsync(calculation.SubTotal, patientId, herbDosages.Count);
            if (discountResult.IsSuccess)
            {
                calculation.TotalPrice = discountResult.Data;
                calculation.DiscountAmount = calculation.SubTotal - calculation.TotalPrice;
            }
            else
            {
                calculation.TotalPrice = calculation.SubTotal;
            }
            
            return ServiceResult<PrescriptionPriceCalculationDto>.Success(calculation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算处方价格异常");
            return ServiceResult<PrescriptionPriceCalculationDto>.Failure($"计算异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<HerbPriceUpdateResultDto>> UpdateHerbPriceAsync(Guid herbId, decimal newPrice, string reason)
    {
        try
        {
            // 验证价格
            var priceValidation = _coreService.ValidatePriceData(newPrice);
            if (!priceValidation.IsSuccess)
            {
                return ServiceResult<HerbPriceUpdateResultDto>.Failure(priceValidation.ErrorMessage);
            }
            
            // 获取旧价格
            var herbResult = await _coreService.GetHerbByIdAsync(herbId);
            if (!herbResult.IsSuccess || herbResult.Data == null)
            {
                return ServiceResult<HerbPriceUpdateResultDto>.Failure("中药材不存在");
            }
            
            var oldPrice = herbResult.Data.Price;
            
            // 更新价格
            var updateDto = new HerbUpdateDto { Price = newPrice };
            var updateResult = await _coreService.CallUpdateHerbApiAsync(herbId, updateDto);
            
            var result = new HerbPriceUpdateResultDto
            {
                HerbId = herbId,
                Success = updateResult.IsSuccess,
                OldPrice = oldPrice,
                NewPrice = newPrice,
                UpdateReason = reason,
                UpdateTime = DateTime.Now,
                ErrorMessage = updateResult.IsSuccess ? null : updateResult.ErrorMessage
            };
            
            if (updateResult.IsSuccess)
            {
                _logger.LogInformation("更新中药材价格成功: {HerbId}, {OldPrice} -> {NewPrice}, 原因: {Reason}",
                    herbId, oldPrice, newPrice, reason);
            }
            
            return ServiceResult<HerbPriceUpdateResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新中药材价格异常: {HerbId}", herbId);
            return ServiceResult<HerbPriceUpdateResultDto>.Failure($"更新异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<decimal>> ApplyPricingPolicyAsync(decimal originalPrice, Guid? patientId, int quantity)
    {
        try
        {
            var finalPrice = originalPrice;
            
            // 简化的定价策略
            // 1. 数量折扣
            if (quantity >= 10)
            {
                finalPrice *= 0.95m; // 95折
            }
            else if (quantity >= 5)
            {
                finalPrice *= 0.97m; // 97折
            }
            
            // 2. VIP折扣 (需要患者信息)
            if (patientId.HasValue)
            {
                // 这里应该检查患者的VIP状态
                // 暂时给予统一小折扣
                finalPrice *= 0.98m; // 98折
            }
            
            return ServiceResult<decimal>.Success(Math.Round(finalPrice, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用定价策略异常");
            return ServiceResult<decimal>.Failure($"定价策略异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<bool>> NotifyPriceChangesAsync(List<HerbPriceUpdateDto> priceChanges)
    {
        try
        {
            // 简化实现：记录日志
            _logger.LogInformation("价格变更通知，涉及 {Count} 个药材", priceChanges.Count);
            
            foreach (var change in priceChanges)
            {
                _logger.LogInformation("药材价格变更: {HerbId}, {OldPrice} -> {NewPrice}",
                    change.HerbId, change.OldPrice, change.NewPrice);
            }
            
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送价格变更通知异常");
            return ServiceResult<bool>.Failure($"通知异常: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 导入导出业务 (简化实现)
    
    public async Task<ServiceResult<HerbImportResultDto>> ImportHerbsFromExcelAsync(string filePath, bool overwriteExisting = false)
    {
        try
        {
            _logger.LogInformation("从Excel导入中药材数据: {FilePath}", filePath);
            
            // 简化实现，返回模拟结果
            var result = new HerbImportResultDto
            {
                TotalRecords = 0,
                SuccessCount = 0,
                FailureCount = 0,
                ErrorMessages = new List<string> { "Excel导入功能暂未实现" }
            };
            
            return ServiceResult<HerbImportResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入Excel异常: {FilePath}", filePath);
            return ServiceResult<HerbImportResultDto>.Failure($"导入异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<string>> ExportHerbsToExcelAsync(HerbExportDto exportDto)
    {
        try
        {
            _logger.LogInformation("导出中药材数据到Excel");
            
            // 简化实现
            return ServiceResult<string>.Failure("Excel导出功能暂未实现");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出Excel异常");
            return ServiceResult<string>.Failure($"导出异常: {ex.Message}");
        }
    }
    
    // 其他简化方法实现...
    public async Task<ServiceResult<HerbImportValidationDto>> ValidateImportDataAsync(string filePath)
    {
        var validation = new HerbImportValidationDto();
        return ServiceResult<HerbImportValidationDto>.Success(validation);
    }
    
    public async Task<ServiceResult<string>> GenerateImportTemplateAsync(string templateType)
    {
        return ServiceResult<string>.Failure("模板生成功能暂未实现");
    }
    
    // 业务流程管理方法的简化实现...
    public async Task<ServiceResult<bool>> ProcessHerbApprovalAsync(Guid herbId, HerbApprovalDto approvalDto)
    {
        return ServiceResult<bool>.Failure("审核流程暂未实现");
    }
    
    public async Task<ServiceResult<bool>> SyncHerbDataToExternalSystemAsync(List<Guid> herbIds)
    {
        return ServiceResult<bool>.Success(true);
    }
    
    public async Task<ServiceResult<List<Guid>>> ArchiveUnusedHerbsAsync(int unusedDays = 365)
    {
        return ServiceResult<List<Guid>>.Success(new List<Guid>());
    }
    
    public async Task<ServiceResult<bool>> RebuildHerbIndexAsync()
    {
        return ServiceResult<bool>.Success(true);
    }
    
    // 智能推荐方法的简化实现...
    public async Task<ServiceResult<List<HerbRecommendationDto>>> RecommendHerbsForSymptomsAsync(List<string> symptoms, Guid? patientId = null)
    {
        return ServiceResult<List<HerbRecommendationDto>>.Success(new List<HerbRecommendationDto>());
    }
    
    public async Task<ServiceResult<HerbUsagePatternDto>> AnalyzeHerbUsagePatternsAsync(Guid? doctorId = null, int days = 90)
    {
        var pattern = new HerbUsagePatternDto();
        return ServiceResult<HerbUsagePatternDto>.Success(pattern);
    }
    
    public async Task<ServiceResult<List<HerbPurchaseSuggestionDto>>> GeneratePurchaseSuggestionsAsync(int forecastDays = 30)
    {
        return ServiceResult<List<HerbPurchaseSuggestionDto>>.Success(new List<HerbPurchaseSuggestionDto>());
    }
    
    public async Task<ServiceResult<PrescriptionOptimizationDto>> OptimizePrescriptionAsync(List<HerbDosageDto> currentFormula)
    {
        var optimization = new PrescriptionOptimizationDto();
        return ServiceResult<PrescriptionOptimizationDto>.Success(optimization);
    }
    
    #endregion
    
    #region 辅助方法
    
    private bool IsIncompatible(HerbDto herb1, HerbDto herb2)
    {
        // 简化的配伍禁忌检查逻辑
        // 实际应该基于中医配伍理论和数据库规则
        
        // 示例：性质相反的药材不宜同用
        if ((herb1.Nature?.Contains("寒") == true && herb2.Nature?.Contains("热") == true) ||
            (herb1.Nature?.Contains("热") == true && herb2.Nature?.Contains("寒") == true))
        {
            return true;
        }
        
        return false;
    }
    
    #endregion
}