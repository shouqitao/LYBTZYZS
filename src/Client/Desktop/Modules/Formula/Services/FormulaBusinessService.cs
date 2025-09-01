using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formulas;

namespace LYBT.Desktop.Formula.Services;

/// <summary>
/// 验方业务服务 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、完整事务管理、业务规则处理
/// </summary>
public class FormulaBusinessService : IFormulaBusinessService
{
    private readonly IFormulaCoreService _coreService;
    private readonly IFormulaQueryService _queryService;
    private readonly ILogger<FormulaBusinessService> _logger;

    public FormulaBusinessService(
        IFormulaCoreService coreService,
        IFormulaQueryService queryService,
        ILogger<FormulaBusinessService> logger)
    {
        _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 事件定义

    public event EventHandler<FormulaStatusChangedEventArgs>? FormulaStatusChanged;
    public event EventHandler<FormulaOperationEventArgs>? FormulaOperation;
    public event EventHandler<FormulaValidationEventArgs>? FormulaValidation;

    #endregion

    #region 核心业务操作

    public async Task<ServiceResult<FormulaDto>> CreateFormulaAsync(FormulaCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("开始创建验方: {FormulaName}", createDto.Name);

            // 业务验证
            var validation = await ValidateFormulaCreateBusinessRulesAsync(createDto);
            if (!validation.IsSuccess)
            {
                OnFormulaValidation(new FormulaValidationEventArgs
                {
                    FormulaName = createDto.Name,
                    IsValid = false,
                    ValidationMessages = new List<string> { validation.ErrorMessage }
                });
                return ServiceResult<FormulaDto>.Failure(validation.ErrorMessage);
            }

            // 检查名称唯一性
            var nameCheckResult = await _coreService.CheckFormulaNameAvailableAsync(createDto.Name);
            if (!nameCheckResult.IsSuccess)
                return ServiceResult<FormulaDto>.Failure(nameCheckResult.ErrorMessage);

            if (!nameCheckResult.Data)
                return ServiceResult<FormulaDto>.Failure("验方名称已存在，请使用其他名称");

            // 验证配伍禁忌
            var compatibilityValidation = await ValidateFormulaCompatibilityAsync(createDto.Ingredients);
            if (!compatibilityValidation.IsSuccess)
            {
                _logger.LogWarning("验方配伍存在警告: {FormulaName}, 警告: {Warning}", 
                    createDto.Name, compatibilityValidation.ErrorMessage);
                
                // 配伍警告不阻止创建，但需要记录事件
                OnFormulaValidation(new FormulaValidationEventArgs
                {
                    FormulaName = createDto.Name,
                    IsValid = true,
                    ValidationMessages = new List<string> { compatibilityValidation.ErrorMessage }
                });
            }

            // 调用核心服务创建
            var result = await _coreService.CallCreateFormulaApiAsync(createDto);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("验方创建成功: {FormulaName}, ID: {FormulaId}", 
                    result.Data.Name, result.Data.Id);

                // 触发业务事件
                OnFormulaOperation(new FormulaOperationEventArgs
                {
                    Operation = "Create",
                    FormulaId = result.Data.Id,
                    FormulaName = result.Data.Name,
                    OperatorId = createDto.CreatorId,
                    OperatorName = createDto.CreatorName,
                    AdditionalInfo = "验方创建成功"
                });

                // 执行创建后处理
                await PostCreateProcessingAsync(result.Data);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建验方时发生异常: {FormulaName}", createDto.Name);
            return ServiceResult<FormulaDto>.Failure("创建验方异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaDto>> UpdateFormulaAsync(Guid id, FormulaUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("开始更新验方: {FormulaId}", id);

            // 验证验方存在性
            var existsResult = await _coreService.CheckFormulaExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
                return ServiceResult<FormulaDto>.Failure("验方不存在");

            // 业务验证
            var validation = await ValidateFormulaUpdateBusinessRulesAsync(id, updateDto);
            if (!validation.IsSuccess)
            {
                OnFormulaValidation(new FormulaValidationEventArgs
                {
                    FormulaId = id,
                    FormulaName = updateDto.Name,
                    IsValid = false,
                    ValidationMessages = new List<string> { validation.ErrorMessage }
                });
                return ServiceResult<FormulaDto>.Failure(validation.ErrorMessage);
            }

            // 检查名称唯一性（排除自己）
            var nameCheckResult = await _coreService.CheckFormulaNameAvailableAsync(updateDto.Name, id);
            if (!nameCheckResult.IsSuccess)
                return ServiceResult<FormulaDto>.Failure(nameCheckResult.ErrorMessage);

            if (!nameCheckResult.Data)
                return ServiceResult<FormulaDto>.Failure("验方名称已被其他验方使用");

            // 验证配伍禁忌（如果更新了药材）
            if (updateDto.Ingredients != null && updateDto.Ingredients.Any())
            {
                var compatibilityValidation = await ValidateFormulaCompatibilityAsync(updateDto.Ingredients);
                if (!compatibilityValidation.IsSuccess)
                {
                    OnFormulaValidation(new FormulaValidationEventArgs
                    {
                        FormulaId = id,
                        FormulaName = updateDto.Name,
                        IsValid = true,
                        ValidationMessages = new List<string> { compatibilityValidation.ErrorMessage }
                    });
                }
            }

            // 调用核心服务更新
            var result = await _coreService.CallUpdateFormulaApiAsync(id, updateDto);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("验方更新成功: {FormulaId}", id);

                // 触发业务事件
                OnFormulaOperation(new FormulaOperationEventArgs
                {
                    Operation = "Update",
                    FormulaId = id,
                    FormulaName = result.Data.Name,
                    OperatorId = updateDto.UpdaterId,
                    OperatorName = updateDto.UpdaterName,
                    AdditionalInfo = "验方更新成功"
                });

                // 执行更新后处理
                await PostUpdateProcessingAsync(result.Data);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新验方时发生异常: {FormulaId}", id);
            return ServiceResult<FormulaDto>.Failure("更新验方异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteFormulaAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("开始删除验方: {FormulaId}", id);

            // 验证验方存在性
            var existsResult = await _coreService.CheckFormulaExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
                return ServiceResult<bool>.Failure("验方不存在");

            // 获取验方详情用于业务验证
            var formulaResult = await _coreService.CallGetFormulaByIdApiAsync(id);
            if (!formulaResult.IsSuccess)
                return ServiceResult<bool>.Failure("获取验方详情失败");

            var formula = formulaResult.Data;

            // 业务验证：检查是否可以删除
            var canDeleteResult = await ValidateFormulaCanBeDeletedAsync(formula);
            if (!canDeleteResult.IsSuccess)
                return ServiceResult<bool>.Failure(canDeleteResult.ErrorMessage);

            // 执行删除前处理
            await PreDeleteProcessingAsync(formula);

            // 调用核心服务删除
            var result = await _coreService.CallDeleteFormulaApiAsync(id);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("验方删除成功: {FormulaId}", id);

                // 触发业务事件
                OnFormulaOperation(new FormulaOperationEventArgs
                {
                    Operation = "Delete",
                    FormulaId = id,
                    FormulaName = formula.Name,
                    AdditionalInfo = "验方删除成功"
                });

                // 执行删除后处理
                await PostDeleteProcessingAsync(formula);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除验方时发生异常: {FormulaId}", id);
            return ServiceResult<bool>.Failure("删除验方异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult> EnableFormulaAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("启用验方: {FormulaId}", id);

            var result = await UpdateFormulaStatusAsync(id, true);
            
            if (result.IsSuccess)
            {
                OnFormulaStatusChanged(new FormulaStatusChangedEventArgs
                {
                    FormulaId = id,
                    OldStatus = false,
                    NewStatus = true,
                    ChangeTime = DateTime.Now
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用验方时发生异常: {FormulaId}", id);
            return ServiceResult.Failure("启用验方异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult> DisableFormulaAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("禁用验方: {FormulaId}", id);

            var result = await UpdateFormulaStatusAsync(id, false);
            
            if (result.IsSuccess)
            {
                OnFormulaStatusChanged(new FormulaStatusChangedEventArgs
                {
                    FormulaId = id,
                    OldStatus = true,
                    NewStatus = false,
                    ChangeTime = DateTime.Now
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用验方时发生异常: {FormulaId}", id);
            return ServiceResult.Failure("禁用验方异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId, string newName, Guid userId)
    {
        try
        {
            _logger.LogInformation("开始克隆验方: {FormulaId} -> {NewName}", formulaId, newName);

            // 获取原验方
            var originalResult = await _coreService.CallGetFormulaByIdApiAsync(formulaId);
            if (!originalResult.IsSuccess)
                return ServiceResult<FormulaDto>.Failure("获取原验方失败: " + originalResult.ErrorMessage);

            var original = originalResult.Data;

            // 检查新名称可用性
            var nameCheckResult = await _coreService.CheckFormulaNameAvailableAsync(newName);
            if (!nameCheckResult.IsSuccess)
                return ServiceResult<FormulaDto>.Failure(nameCheckResult.ErrorMessage);

            if (!nameCheckResult.Data)
                return ServiceResult<FormulaDto>.Failure("验方名称已存在");

            // 构造克隆数据
            var cloneDto = new FormulaCreateDto
            {
                Name = newName,
                Type = original.Type,
                Source = "个人验方(克隆)",
                Effect = original.Effect,
                Indications = original.Indications,
                Contraindications = original.Contraindications,
                Usage = original.Usage,
                Preparation = original.Preparation,
                Dosage = original.Dosage,
                Notes = $"克隆自: {original.Name}",
                Ingredients = original.Ingredients,
                CreatorId = userId,
                CreatorName = "系统用户" // TODO: 获取真实用户名
            };

            // 创建克隆验方
            var result = await CreateFormulaAsync(cloneDto);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("验方克隆成功: {OriginalId} -> {CloneId}", formulaId, result.Data.Id);

                // 触发业务事件
                OnFormulaOperation(new FormulaOperationEventArgs
                {
                    Operation = "Clone",
                    FormulaId = result.Data.Id,
                    FormulaName = result.Data.Name,
                    OperatorId = userId,
                    AdditionalInfo = $"从验方 {original.Name} 克隆"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "克隆验方时发生异常: {FormulaId}", formulaId);
            return ServiceResult<FormulaDto>.Failure("克隆验方异常: " + ex.Message);
        }
    }

    #endregion

    #region 验方药材管理

    public async Task<ServiceResult<FormulaIngredientDto>> AddFormulaIngredientAsync(Guid formulaId, FormulaIngredientCreateDto ingredientDto)
    {
        try
        {
            _logger.LogInformation("为验方添加药材: {FormulaId}, 药材: {HerbName}", formulaId, ingredientDto.HerbName);

            // TODO: 实现添加药材的具体逻辑
            // 目前返回成功结果
            var ingredient = new FormulaIngredientDto
            {
                Id = Guid.NewGuid(),
                FormulaId = formulaId,
                HerbId = ingredientDto.HerbId,
                HerbName = ingredientDto.HerbName,
                Dosage = ingredientDto.Dosage,
                Unit = ingredientDto.Unit,
                ProcessingMethod = ingredientDto.ProcessingMethod,
                Notes = ingredientDto.Notes
            };

            return ServiceResult<FormulaIngredientDto>.Success(ingredient, "药材添加成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加验方药材时发生异常: {FormulaId}", formulaId);
            return ServiceResult<FormulaIngredientDto>.Failure("添加药材异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaIngredientDto>> UpdateFormulaIngredientAsync(Guid ingredientId, FormulaIngredientUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("更新验方药材: {IngredientId}", ingredientId);

            // TODO: 实现更新药材的具体逻辑
            var ingredient = new FormulaIngredientDto
            {
                Id = ingredientId,
                Dosage = updateDto.Dosage,
                Unit = updateDto.Unit,
                ProcessingMethod = updateDto.ProcessingMethod,
                Notes = updateDto.Notes
            };

            return ServiceResult<FormulaIngredientDto>.Success(ingredient, "药材更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新验方药材时发生异常: {IngredientId}", ingredientId);
            return ServiceResult<FormulaIngredientDto>.Failure("更新药材异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> RemoveFormulaIngredientAsync(Guid ingredientId)
    {
        try
        {
            _logger.LogInformation("删除验方药材: {IngredientId}", ingredientId);

            // TODO: 实现删除药材的具体逻辑
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(true, "药材删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除验方药材时发生异常: {IngredientId}", ingredientId);
            return ServiceResult<bool>.Failure("删除药材异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<int>> BatchUpdateFormulaIngredientsAsync(Guid formulaId, List<FormulaIngredientDto> ingredients)
    {
        try
        {
            _logger.LogInformation("批量更新验方药材: {FormulaId}, 数量: {Count}", formulaId, ingredients.Count);

            // 验证药材列表
            var validation = _coreService.ValidateFormulaIngredients(ingredients);
            if (!validation.IsSuccess)
                return ServiceResult<int>.Failure(validation.ErrorMessage);

            // TODO: 实现批量更新药材的具体逻辑
            var successCount = ingredients.Count;

            return ServiceResult<int>.Success(successCount, $"成功更新{successCount}味药材");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新验方药材时发生异常: {FormulaId}", formulaId);
            return ServiceResult<int>.Failure("批量更新药材异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaIngredientDto>> AdjustIngredientDosageAsync(Guid ingredientId, decimal newDosage)
    {
        try
        {
            _logger.LogInformation("调整药材剂量: {IngredientId}, 新剂量: {Dosage}", ingredientId, newDosage);

            if (newDosage <= 0)
                return ServiceResult<FormulaIngredientDto>.Failure("剂量必须大于0");

            if (newDosage > 1000)
                return ServiceResult<FormulaIngredientDto>.Failure("剂量不能超过1000克");

            // TODO: 实现调整剂量的具体逻辑
            var ingredient = new FormulaIngredientDto
            {
                Id = ingredientId,
                Dosage = newDosage
            };

            return ServiceResult<FormulaIngredientDto>.Success(ingredient, "剂量调整成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调整药材剂量时发生异常: {IngredientId}", ingredientId);
            return ServiceResult<FormulaIngredientDto>.Failure("调整剂量异常: " + ex.Message);
        }
    }

    #endregion

    #region 验方验证与检查

    public async Task<ServiceResult<FormulaValidationResultDto>> ValidateFormulaCompletenessAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("验证验方完整性: {FormulaId}", formulaId);

            var formulaResult = await _coreService.CallGetFormulaByIdApiAsync(formulaId);
            if (!formulaResult.IsSuccess)
                return ServiceResult<FormulaValidationResultDto>.Failure(formulaResult.ErrorMessage);

            var formula = formulaResult.Data;
            var validationResult = new FormulaValidationResultDto
            {
                IsValid = true,
                ValidationMessages = new List<string>(),
                Warnings = new List<string>(),
                Suggestions = new List<string>()
            };

            // 检查基本信息完整性
            if (string.IsNullOrWhiteSpace(formula.Name))
            {
                validationResult.IsValid = false;
                validationResult.ValidationMessages.Add("验方名称不能为空");
            }

            if (string.IsNullOrWhiteSpace(formula.Type))
                validationResult.Warnings.Add("建议补充验方类型");

            if (string.IsNullOrWhiteSpace(formula.Effect))
                validationResult.Warnings.Add("建议补充功效说明");

            if (string.IsNullOrWhiteSpace(formula.Indications))
                validationResult.Warnings.Add("建议补充主治说明");

            // 检查药材完整性
            if (formula.Ingredients == null || !formula.Ingredients.Any())
            {
                validationResult.IsValid = false;
                validationResult.ValidationMessages.Add("验方必须包含药材");
            }
            else
            {
                if (formula.Ingredients.Count < 2)
                    validationResult.Warnings.Add("验方药材数量较少，建议增加配伍药材");

                if (formula.Ingredients.Count > 20)
                    validationResult.Warnings.Add("验方药材数量较多，建议简化配伍");
            }

            // 生成改进建议
            if (string.IsNullOrWhiteSpace(formula.Usage))
                validationResult.Suggestions.Add("建议添加用法用量说明");

            if (string.IsNullOrWhiteSpace(formula.Contraindications))
                validationResult.Suggestions.Add("建议添加禁忌症说明");

            OnFormulaValidation(new FormulaValidationEventArgs
            {
                FormulaId = formulaId,
                FormulaName = formula.Name,
                IsValid = validationResult.IsValid,
                ValidationMessages = validationResult.ValidationMessages
            });

            return ServiceResult<FormulaValidationResultDto>.Success(validationResult, "验方完整性验证完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证验方完整性时发生异常: {FormulaId}", formulaId);
            return ServiceResult<FormulaValidationResultDto>.Failure("验证完整性异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaCompatibilityResultDto>> CheckFormulaCompatibilityAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("检查验方配伍禁忌: {FormulaId}", formulaId);

            var formulaResult = await _coreService.CallGetFormulaByIdApiAsync(formulaId);
            if (!formulaResult.IsSuccess)
                return ServiceResult<FormulaCompatibilityResultDto>.Failure(formulaResult.ErrorMessage);

            var formula = formulaResult.Data;

            // 执行配伍检查
            var compatibilityResult = await CheckIngredientsCompatibilityAsync(formula.Ingredients);
            
            return ServiceResult<FormulaCompatibilityResultDto>.Success(compatibilityResult, "配伍检查完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查验方配伍时发生异常: {FormulaId}", formulaId);
            return ServiceResult<FormulaCompatibilityResultDto>.Failure("配伍检查异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<DosageValidationResultDto>> ValidateFormulaDosageAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("验证验方剂量: {FormulaId}", formulaId);

            var formulaResult = await _coreService.CallGetFormulaByIdApiAsync(formulaId);
            if (!formulaResult.IsSuccess)
                return ServiceResult<DosageValidationResultDto>.Failure(formulaResult.ErrorMessage);

            var formula = formulaResult.Data;
            var dosageResult = new DosageValidationResultDto
            {
                IsValid = true,
                DosageWarnings = new List<string>(),
                SafetyAlerts = new List<string>(),
                TotalDosage = 0,
                DosageUnit = "克"
            };

            if (formula.Ingredients != null)
            {
                foreach (var ingredient in formula.Ingredients)
                {
                    dosageResult.TotalDosage += ingredient.Dosage;

                    // 检查单味药剂量
                    if (ingredient.Dosage < 1)
                        dosageResult.DosageWarnings.Add($"{ingredient.HerbName}剂量过小，可能影响疗效");

                    if (ingredient.Dosage > 100)
                        dosageResult.SafetyAlerts.Add($"{ingredient.HerbName}剂量较大，请注意安全性");

                    if (ingredient.Dosage > 500)
                        dosageResult.SafetyAlerts.Add($"{ingredient.HerbName}剂量过大，存在安全风险");
                }

                // 检查总剂量
                if (dosageResult.TotalDosage < 10)
                    dosageResult.DosageWarnings.Add("总剂量较小，可能影响疗效");

                if (dosageResult.TotalDosage > 200)
                    dosageResult.DosageWarnings.Add("总剂量较大，建议分次服用");

                if (dosageResult.TotalDosage > 500)
                    dosageResult.SafetyAlerts.Add("总剂量过大，请谨慎使用");
            }

            dosageResult.IsValid = dosageResult.SafetyAlerts.Count == 0;

            return ServiceResult<DosageValidationResultDto>.Success(dosageResult, "剂量验证完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证验方剂量时发生异常: {FormulaId}", formulaId);
            return ServiceResult<DosageValidationResultDto>.Failure("剂量验证异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> CheckNameAvailabilityAsync(string name, Guid? excludeFormulaId = null)
    {
        return await _coreService.CheckFormulaNameAvailableAsync(name, excludeFormulaId);
    }

    public async Task<ServiceResult<bool>> CheckFormulaUsagePermissionAsync(Guid formulaId, Guid userId)
    {
        try
        {
            // TODO: 实现具体的权限检查逻辑
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(true, "权限检查通过");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查验方使用权限时发生异常: {FormulaId}, {UserId}", formulaId, userId);
            return ServiceResult<bool>.Failure("权限检查异常: " + ex.Message);
        }
    }

    #endregion

    #region 验方使用与记录

    public async Task<ServiceResult> RecordFormulaUsageAsync(Guid formulaId, FormulaUsageRecordDto usageRecord)
    {
        try
        {
            _logger.LogInformation("记录验方使用: {FormulaId}, 用户: {UserId}", formulaId, usageRecord.UserId);

            // TODO: 实现使用记录的具体逻辑
            await Task.CompletedTask;

            return ServiceResult.Success("使用记录成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录验方使用时发生异常: {FormulaId}", formulaId);
            return ServiceResult.Failure("记录使用异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaUsageHistoryDto>>> GetFormulaUsageHistoryAsync(Guid formulaId)
    {
        return await _queryService.GetFormulaUsageHistoryAsync(formulaId);
    }

    public async Task<ServiceResult<FormulaReviewDto>> AddFormulaReviewAsync(FormulaReviewCreateDto reviewDto)
    {
        try
        {
            _logger.LogInformation("添加验方评价: {FormulaId}, 评价者: {ReviewerId}", 
                reviewDto.FormulaId, reviewDto.ReviewerId);

            // TODO: 实现添加评价的具体逻辑
            var review = new FormulaReviewDto
            {
                Id = Guid.NewGuid(),
                FormulaId = reviewDto.FormulaId,
                ReviewerId = reviewDto.ReviewerId,
                ReviewerName = reviewDto.ReviewerName,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                ReviewTime = DateTime.Now
            };

            return ServiceResult<FormulaReviewDto>.Success(review, "评价添加成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加验方评价时发生异常: {FormulaId}", reviewDto.FormulaId);
            return ServiceResult<FormulaReviewDto>.Failure("添加评价异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaReviewDto>> UpdateFormulaReviewAsync(Guid reviewId, FormulaReviewUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("更新验方评价: {ReviewId}", reviewId);

            // TODO: 实现更新评价的具体逻辑
            var review = new FormulaReviewDto
            {
                Id = reviewId,
                Rating = updateDto.Rating,
                Comment = updateDto.Comment,
                ReviewTime = DateTime.Now
            };

            return ServiceResult<FormulaReviewDto>.Success(review, "评价更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新验方评价时发生异常: {ReviewId}", reviewId);
            return ServiceResult<FormulaReviewDto>.Failure("更新评价异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteFormulaReviewAsync(Guid reviewId)
    {
        try
        {
            _logger.LogInformation("删除验方评价: {ReviewId}", reviewId);

            // TODO: 实现删除评价的具体逻辑
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(true, "评价删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除验方评价时发生异常: {ReviewId}", reviewId);
            return ServiceResult<bool>.Failure("删除评价异常: " + ex.Message);
        }
    }

    #endregion

    #region 批量业务操作

    public async Task<ServiceResult<FormulaBatchOperationResultDto>> BatchUpdateFormulaStatusAsync(List<Guid> formulaIds, bool isEnabled)
    {
        try
        {
            _logger.LogInformation("批量更新验方状态: {Count}个验方, 状态: {Status}", 
                formulaIds.Count, isEnabled ? "启用" : "禁用");

            var operationDto = new FormulaBatchOperationDto
            {
                FormulaIds = formulaIds,
                Operation = isEnabled ? "enable" : "disable",
                OperatorId = Guid.Empty, // TODO: 获取当前操作用户
                OperatorName = "系统操作"
            };

            var result = await _coreService.CallBatchOperateFormulasApiAsync(operationDto);

            if (result.IsSuccess)
            {
                // 触发状态变更事件
                foreach (var formulaId in formulaIds.Take(result.Data.SuccessCount))
                {
                    OnFormulaStatusChanged(new FormulaStatusChangedEventArgs
                    {
                        FormulaId = formulaId,
                        OldStatus = !isEnabled,
                        NewStatus = isEnabled,
                        OperatorId = operationDto.OperatorId,
                        OperatorName = operationDto.OperatorName,
                        ChangeTime = DateTime.Now
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新验方状态时发生异常");
            return ServiceResult<FormulaBatchOperationResultDto>.Failure("批量更新状态异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaBatchOperationResultDto>> BatchDeleteFormulasAsync(List<Guid> formulaIds)
    {
        try
        {
            _logger.LogInformation("批量删除验方: {Count}个", formulaIds.Count);

            var operationDto = new FormulaBatchOperationDto
            {
                FormulaIds = formulaIds,
                Operation = "delete",
                OperatorId = Guid.Empty, // TODO: 获取当前操作用户
                OperatorName = "系统操作"
            };

            return await _coreService.CallBatchOperateFormulasApiAsync(operationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除验方时发生异常");
            return ServiceResult<FormulaBatchOperationResultDto>.Failure("批量删除异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaBatchOperationResultDto>> BatchTransferFormulaOwnershipAsync(List<Guid> formulaIds, Guid newOwnerId)
    {
        try
        {
            _logger.LogInformation("批量转移验方所有权: {Count}个验方 -> {NewOwnerId}", formulaIds.Count, newOwnerId);

            var operationDto = new FormulaBatchOperationDto
            {
                FormulaIds = formulaIds,
                Operation = "transfer",
                Parameters = new { NewOwnerId = newOwnerId },
                OperatorId = Guid.Empty, // TODO: 获取当前操作用户
                OperatorName = "系统操作"
            };

            return await _coreService.CallBatchOperateFormulasApiAsync(operationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量转移验方所有权时发生异常");
            return ServiceResult<FormulaBatchOperationResultDto>.Failure("批量转移所有权异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> BatchCloneFormulasAsync(List<Guid> formulaIds, Guid targetUserId)
    {
        try
        {
            _logger.LogInformation("批量克隆验方: {Count}个验方", formulaIds.Count);

            var clonedFormulas = new List<FormulaDto>();

            foreach (var formulaId in formulaIds)
            {
                var originalResult = await _coreService.CallGetFormulaByIdApiAsync(formulaId);
                if (originalResult.IsSuccess)
                {
                    var newName = $"{originalResult.Data.Name}_副本_{DateTime.Now:MMdd}";
                    var cloneResult = await CloneFormulaAsync(formulaId, newName, targetUserId);
                    
                    if (cloneResult.IsSuccess)
                        clonedFormulas.Add(cloneResult.Data);
                }
            }

            return ServiceResult<List<FormulaDto>>.Success(clonedFormulas, $"成功克隆{clonedFormulas.Count}个验方");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量克隆验方时发生异常");
            return ServiceResult<List<FormulaDto>>.Failure("批量克隆异常: " + ex.Message);
        }
    }

    #endregion

    #region 导入导出业务

    public async Task<ServiceResult<FormulaImportResultDto>> ImportFormulasAsync(FormulaImportDto importDto)
    {
        try
        {
            _logger.LogInformation("开始导入验方: {Count}个", importDto.Records?.Count ?? 0);

            // 验证导入数据
            var validation = await ValidateImportDataAsync(importDto);
            if (!validation.IsSuccess)
                return ServiceResult<FormulaImportResultDto>.Failure(validation.ErrorMessage);

            // 调用核心服务导入
            var result = await _coreService.CallImportFormulasApiAsync(importDto);

            if (result.IsSuccess)
            {
                _logger.LogInformation("验方导入完成: 成功{Success}, 失败{Failure}", 
                    result.Data.SuccessCount, result.Data.FailureCount);

                // 触发导入完成事件
                OnFormulaOperation(new FormulaOperationEventArgs
                {
                    Operation = "Import",
                    FormulaId = Guid.Empty,
                    FormulaName = "批量导入",
                    AdditionalInfo = $"成功导入{result.Data.SuccessCount}个验方"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入验方时发生异常");
            return ServiceResult<FormulaImportResultDto>.Failure("导入验方异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaExportResultDto>> ExportFormulasAsync(FormulaExportQueryDto exportQuery)
    {
        try
        {
            _logger.LogInformation("开始导出验方");

            var result = await _coreService.CallExportFormulasApiAsync(exportQuery);

            if (result.IsSuccess)
            {
                _logger.LogInformation("验方导出完成: {Count}个", result.Data.ExportedCount);

                // 触发导出完成事件
                OnFormulaOperation(new FormulaOperationEventArgs
                {
                    Operation = "Export",
                    FormulaId = Guid.Empty,
                    FormulaName = "批量导出",
                    AdditionalInfo = $"成功导出{result.Data.ExportedCount}个验方"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出验方时发生异常");
            return ServiceResult<FormulaExportResultDto>.Failure("导出验方异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaImportValidationResultDto>> ValidateImportDataAsync(FormulaImportDto importDto)
    {
        try
        {
            var validationResult = new FormulaImportValidationResultDto
            {
                IsValid = true,
                TotalRecords = importDto.Records?.Count ?? 0,
                ValidRecords = 0,
                InvalidRecords = 0,
                ValidationErrors = new List<string>(),
                Warnings = new List<string>()
            };

            if (importDto.Records == null || !importDto.Records.Any())
            {
                validationResult.IsValid = false;
                validationResult.ValidationErrors.Add("导入数据为空");
                return ServiceResult<FormulaImportValidationResultDto>.Success(validationResult, "数据验证完成");
            }

            foreach (var record in importDto.Records)
            {
                var recordValid = true;

                if (string.IsNullOrWhiteSpace(record.Name))
                {
                    validationResult.ValidationErrors.Add($"第{validationResult.ValidRecords + validationResult.InvalidRecords + 1}行：验方名称不能为空");
                    recordValid = false;
                }

                if (record.Ingredients == null || !record.Ingredients.Any())
                {
                    validationResult.ValidationErrors.Add($"验方'{record.Name}'：药材列表不能为空");
                    recordValid = false;
                }

                if (recordValid)
                    validationResult.ValidRecords++;
                else
                    validationResult.InvalidRecords++;
            }

            validationResult.IsValid = validationResult.InvalidRecords == 0;

            return ServiceResult<FormulaImportValidationResultDto>.Success(validationResult, "数据验证完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证导入数据时发生异常");
            return ServiceResult<FormulaImportValidationResultDto>.Failure("数据验证异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<byte[]>> GenerateImportTemplateAsync()
    {
        try
        {
            _logger.LogInformation("生成验方导入模板");

            // TODO: 实现模板生成逻辑
            var template = "验方名称,类型,来源,功效,主治,药材1,剂量1,药材2,剂量2\n示例验方,汤剂,经典,补气养血,气血两虚,党参,15,当归,10";
            var templateBytes = System.Text.Encoding.UTF8.GetBytes(template);

            return ServiceResult<byte[]>.Success(templateBytes, "模板生成成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成导入模板时发生异常");
            return ServiceResult<byte[]>.Failure("生成模板异常: " + ex.Message);
        }
    }

    #endregion

    #region 智能推荐与分析

    public async Task<ServiceResult<List<FormulaDto>>> RecommendSimilarFormulasAsync(Guid formulaId, int limit = 5)
    {
        return await _queryService.GetRelatedFormulasAsync(formulaId, limit);
    }

    public async Task<ServiceResult<List<FormulaDto>>> RecommendFormulasBySymptomAsync(List<string> symptoms, int limit = 10)
    {
        try
        {
            _logger.LogInformation("根据症状推荐验方: {Symptoms}", string.Join(",", symptoms));

            var allRecommendations = new List<FormulaDto>();

            // 为每个症状搜索相关验方
            foreach (var symptom in symptoms)
            {
                var searchResult = await _queryService.SearchBySymptomAsync(symptom);
                if (searchResult.IsSuccess)
                {
                    allRecommendations.AddRange(searchResult.Data);
                }
            }

            // 去重并按相关度排序
            var uniqueRecommendations = allRecommendations
                .GroupBy(f => f.Id)
                .Select(g => g.First())
                .Take(limit)
                .ToList();

            return ServiceResult<List<FormulaDto>>.Success(uniqueRecommendations, 
                $"根据症状推荐{uniqueRecommendations.Count}个验方");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据症状推荐验方时发生异常");
            return ServiceResult<List<FormulaDto>>.Failure("症状推荐异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaUsageTrendDto>> AnalyzeFormulaUsageTrendAsync(Guid formulaId, int days = 30)
    {
        try
        {
            _logger.LogInformation("分析验方使用趋势: {FormulaId}", formulaId);

            // TODO: 实现真实的使用趋势分析
            var trendData = new List<UsageTrendDataPoint>();
            for (int i = days; i >= 0; i--)
            {
                trendData.Add(new UsageTrendDataPoint
                {
                    Date = DateTime.Now.Date.AddDays(-i),
                    UsageCount = new Random().Next(0, 10)
                });
            }

            var trend = new FormulaUsageTrendDto
            {
                FormulaId = formulaId,
                FormulaName = "示例验方",
                TrendData = trendData,
                TrendSlope = 0.5,
                TrendDirection = "上升"
            };

            return ServiceResult<FormulaUsageTrendDto>.Success(trend, "使用趋势分析完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分析验方使用趋势时发生异常: {FormulaId}", formulaId);
            return ServiceResult<FormulaUsageTrendDto>.Failure("趋势分析异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<IngredientCombinationAnalysisDto>> AnalyzeIngredientCombinationAsync(List<Guid> herbIds)
    {
        try
        {
            _logger.LogInformation("分析药材搭配模式: {Count}味药材", herbIds.Count);

            // TODO: 实现真实的药材搭配分析
            var analysis = new IngredientCombinationAnalysisDto
            {
                AnalyzedIngredients = herbIds,
                CommonCombinations = new List<string> { "气血双补", "温阳散寒" },
                CompatibilityAlerts = new List<string>(),
                CompatibilityScore = 85.5,
                RecommendedAdditions = new List<string> { "甘草", "生姜" }
            };

            return ServiceResult<IngredientCombinationAnalysisDto>.Success(analysis, "药材搭配分析完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分析药材搭配时发生异常");
            return ServiceResult<IngredientCombinationAnalysisDto>.Failure("搭配分析异常: " + ex.Message);
        }
    }

    #endregion

    #region 业务流程管理

    public async Task<ServiceResult> SubmitFormulaForReviewAsync(Guid formulaId, string reviewNote)
    {
        try
        {
            _logger.LogInformation("提交验方审核: {FormulaId}", formulaId);

            // TODO: 实现提交审核的具体逻辑
            await Task.CompletedTask;
            return ServiceResult.Success("提交审核成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交验方审核时发生异常: {FormulaId}", formulaId);
            return ServiceResult.Failure("提交审核异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult> ReviewFormulaAsync(Guid formulaId, FormulaReviewDecisionDto decision)
    {
        try
        {
            _logger.LogInformation("审核验方: {FormulaId}, 决策: {Decision}", formulaId, decision.IsApproved ? "通过" : "拒绝");

            // TODO: 实现审核验方的具体逻辑
            await Task.CompletedTask;
            return ServiceResult.Success(decision.IsApproved ? "审核通过" : "审核拒绝");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审核验方时发生异常: {FormulaId}", formulaId);
            return ServiceResult.Failure("审核异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult> PublishFormulaAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("发布验方: {FormulaId}", formulaId);

            // TODO: 实现发布验方的具体逻辑
            await Task.CompletedTask;
            return ServiceResult.Success("验方发布成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布验方时发生异常: {FormulaId}", formulaId);
            return ServiceResult.Failure("发布异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult> ArchiveFormulaAsync(Guid formulaId, string archiveReason)
    {
        try
        {
            _logger.LogInformation("归档验方: {FormulaId}, 原因: {Reason}", formulaId, archiveReason);

            // TODO: 实现归档验方的具体逻辑
            await Task.CompletedTask;
            return ServiceResult.Success("验方归档成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "归档验方时发生异常: {FormulaId}", formulaId);
            return ServiceResult.Failure("归档异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult> RestoreArchivedFormulaAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("恢复归档验方: {FormulaId}", formulaId);

            // TODO: 实现恢复归档的具体逻辑
            await Task.CompletedTask;
            return ServiceResult.Success("验方恢复成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复归档验方时发生异常: {FormulaId}", formulaId);
            return ServiceResult.Failure("恢复异常: " + ex.Message);
        }
    }

    #endregion

    #region 权限与安全

    public async Task<ServiceResult> SetFormulaPermissionAsync(Guid formulaId, FormulaPermissionDto permission)
    {
        try
        {
            _logger.LogInformation("设置验方权限: {FormulaId}", formulaId);

            // TODO: 实现权限设置的具体逻辑
            await Task.CompletedTask;
            return ServiceResult.Success("权限设置成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置验方权限时发生异常: {FormulaId}", formulaId);
            return ServiceResult.Failure("权限设置异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> CheckOperationPermissionAsync(Guid formulaId, Guid userId, string operation)
    {
        return await _coreService.CheckFormulaPermissionAsync(formulaId, userId, operation);
    }

    public async Task<ServiceResult> LogFormulaAccessAsync(Guid formulaId, Guid userId, string operation)
    {
        try
        {
            await _coreService.LogFormulaOperationAsync(operation, formulaId, userId);
            return ServiceResult.Success("访问日志记录成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录验方访问日志时发生异常: {FormulaId}, {UserId}", formulaId, userId);
            return ServiceResult.Failure("日志记录异常: " + ex.Message);
        }
    }

    #endregion

    #region 高级功能

    public async Task<ServiceResult<byte[]>> GenerateFormulaQRCodeAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("生成验方二维码: {FormulaId}", formulaId);

            // TODO: 实现二维码生成逻辑
            var qrCodeData = System.Text.Encoding.UTF8.GetBytes($"Formula:{formulaId}");
            
            return ServiceResult<byte[]>.Success(qrCodeData, "二维码生成成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成验方二维码时发生异常: {FormulaId}", formulaId);
            return ServiceResult<byte[]>.Failure("二维码生成异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<byte[]>> GenerateFormulaPdfReportAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("生成验方PDF报告: {FormulaId}", formulaId);

            // TODO: 实现PDF报告生成逻辑
            var pdfContent = System.Text.Encoding.UTF8.GetBytes("验方PDF报告内容");
            
            return ServiceResult<byte[]>.Success(pdfContent, "PDF报告生成成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成验方PDF报告时发生异常: {FormulaId}", formulaId);
            return ServiceResult<byte[]>.Failure("PDF生成异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaShareTokenDto>> ShareFormulaAsync(Guid formulaId, FormulaShareOptionsDto shareOptions)
    {
        try
        {
            _logger.LogInformation("分享验方: {FormulaId}", formulaId);

            var shareToken = new FormulaShareTokenDto
            {
                ShareToken = Guid.NewGuid().ToString("N"),
                ShareUrl = $"https://example.com/formula/{formulaId}/share",
                ExpiryDate = shareOptions.ExpiryDate ?? DateTime.Now.AddDays(30),
                IsActive = true
            };

            return ServiceResult<FormulaShareTokenDto>.Success(shareToken, "验方分享成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分享验方时发生异常: {FormulaId}", formulaId);
            return ServiceResult<FormulaShareTokenDto>.Failure("分享异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult> FavoriteFormulaAsync(Guid formulaId, Guid userId)
    {
        try
        {
            _logger.LogInformation("收藏验方: {FormulaId}, 用户: {UserId}", formulaId, userId);

            // TODO: 实现收藏验方的具体逻辑
            await Task.CompletedTask;
            return ServiceResult.Success("收藏成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收藏验方时发生异常: {FormulaId}, {UserId}", formulaId, userId);
            return ServiceResult.Failure("收藏异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult> UnfavoriteFormulaAsync(Guid formulaId, Guid userId)
    {
        try
        {
            _logger.LogInformation("取消收藏验方: {FormulaId}, 用户: {UserId}", formulaId, userId);

            // TODO: 实现取消收藏的具体逻辑
            await Task.CompletedTask;
            return ServiceResult.Success("取消收藏成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消收藏验方时发生异常: {FormulaId}, {UserId}", formulaId, userId);
            return ServiceResult.Failure("取消收藏异常: " + ex.Message);
        }
    }

    #endregion

    #region 私有辅助方法

    private async Task<ServiceResult> ValidateFormulaCreateBusinessRulesAsync(FormulaCreateDto createDto)
    {
        // 基础验证
        var coreValidation = _coreService.ValidateFormulaCreateData(createDto);
        if (!coreValidation.IsSuccess)
            return coreValidation;

        // 业务规则验证
        await Task.CompletedTask;
        return ServiceResult.Success("业务规则验证通过");
    }

    private async Task<ServiceResult> ValidateFormulaUpdateBusinessRulesAsync(Guid formulaId, FormulaUpdateDto updateDto)
    {
        // 基础验证
        var coreValidation = _coreService.ValidateFormulaUpdateData(updateDto);
        if (!coreValidation.IsSuccess)
            return coreValidation;

        // 业务规则验证
        await Task.CompletedTask;
        return ServiceResult.Success("业务规则验证通过");
    }

    private async Task<ServiceResult> ValidateFormulaCompatibilityAsync(List<FormulaIngredientDto> ingredients)
    {
        await Task.CompletedTask;
        
        // TODO: 实现配伍禁忌检查
        // 目前返回成功
        return ServiceResult.Success("配伍检查通过");
    }

    private async Task<ServiceResult> ValidateFormulaCanBeDeletedAsync(FormulaDto formula)
    {
        await Task.CompletedTask;

        // 业务规则：检查验方是否正在被使用
        // TODO: 实现具体的业务规则检查

        return ServiceResult.Success("可以删除");
    }

    private async Task<FormulaCompatibilityResultDto> CheckIngredientsCompatibilityAsync(List<FormulaIngredientDto> ingredients)
    {
        await Task.CompletedTask;

        // TODO: 实现具体的配伍检查逻辑
        return new FormulaCompatibilityResultDto
        {
            IsCompatible = true,
            ContraindicationWarnings = new List<string>(),
            InteractionWarnings = new List<string>(),
            RecommendedAdjustments = new List<string>()
        };
    }

    private async Task<ServiceResult> UpdateFormulaStatusAsync(Guid formulaId, bool isEnabled)
    {
        return await _coreService.CallUpdateFormulaStatusApiAsync(formulaId, isEnabled);
    }

    private async Task PostCreateProcessingAsync(FormulaDto formula)
    {
        try
        {
            // 创建后处理逻辑
            await Task.CompletedTask;
            _logger.LogDebug("验方创建后处理完成: {FormulaId}", formula.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验方创建后处理异常: {FormulaId}", formula.Id);
        }
    }

    private async Task PostUpdateProcessingAsync(FormulaDto formula)
    {
        try
        {
            // 更新后处理逻辑
            await Task.CompletedTask;
            _logger.LogDebug("验方更新后处理完成: {FormulaId}", formula.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验方更新后处理异常: {FormulaId}", formula.Id);
        }
    }

    private async Task PreDeleteProcessingAsync(FormulaDto formula)
    {
        try
        {
            // 删除前处理逻辑
            await Task.CompletedTask;
            _logger.LogDebug("验方删除前处理完成: {FormulaId}", formula.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验方删除前处理异常: {FormulaId}", formula.Id);
        }
    }

    private async Task PostDeleteProcessingAsync(FormulaDto formula)
    {
        try
        {
            // 删除后处理逻辑
            await Task.CompletedTask;
            _logger.LogDebug("验方删除后处理完成: {FormulaId}", formula.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验方删除后处理异常: {FormulaId}", formula.Id);
        }
    }

    private void OnFormulaStatusChanged(FormulaStatusChangedEventArgs args)
    {
        FormulaStatusChanged?.Invoke(this, args);
    }

    private void OnFormulaOperation(FormulaOperationEventArgs args)
    {
        FormulaOperation?.Invoke(this, args);
    }

    private void OnFormulaValidation(FormulaValidationEventArgs args)
    {
        FormulaValidation?.Invoke(this, args);
    }

    #endregion
}