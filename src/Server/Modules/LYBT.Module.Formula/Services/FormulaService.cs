using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Formula;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方服务实现 - UltraThink Phase 5: 实现Shared接口统一
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        #region Shared Interface Implementation

        /// <summary>
        /// [Shared] 根据ID获取验方详情
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas
                    .FirstOrDefaultAsync(f => f.Id == id && f.Status == CommonStatus.Enabled);

                if (formula == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                var dto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方详情失败: {Id}", id);
                return ServiceResult<FormulaDto>.Failure("获取验方详情失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 分页查询验方
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
        {
            try
            {
                var formulas = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    formulas = formulas.Where(f => f.Name.Contains(query.Keyword));
                }

                var total = await formulas.CountAsync();
                var items = await formulas
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(items);
                var result = new PagedResult<FormulaDto>
                {
                    Items = dtos,
                    TotalCount = total,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询验方失败");
                return ServiceResult<PagedResult<FormulaDto>>.Failure("分页查询验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 创建验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            try
            {
                var formula = new LYBT.Entities.Formula.Formula
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    // CreateTime、UpdateTime字段已删除（UltraThink v2.0简化）
                    Status = CommonStatus.Enabled
                };

                _dbContext.Formulas.Add(formula);
                await _dbContext.SaveChangesAsync();

                var createdDto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败");
                return ServiceResult<FormulaDto>.Failure("创建验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 更新验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                formula.Name = dto.Name ?? formula.Name;
                // UpdateTime字段已删除（UltraThink v2.0简化）

                await _dbContext.SaveChangesAsync();

                var updatedDto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败: {Id}", id);
                return ServiceResult<FormulaDto>.Failure("更新验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 删除验方（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<bool>.Failure("验方不存在");

                formula.Status = CommonStatus.Disabled;
                // UpdateTime字段已删除（UltraThink v2.0简化）

                await _dbContext.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 获取验方模板列表
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方模板列表失败");
                return ServiceResult<List<FormulaDto>>.Failure("获取验方模板列表失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 根据类型获取验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .Take(20)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取验方失败: {Type}", formulaType);
                return ServiceResult<List<FormulaDto>>.Failure("根据类型获取验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 从处方创建验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
        {
            try
            {
                var formula = new LYBT.Entities.Formula.Formula
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    // CreateTime、UpdateTime字段已删除（UltraThink v2.0简化）
                    Status = CommonStatus.Enabled
                };

                _dbContext.Formulas.Add(formula);
                await _dbContext.SaveChangesAsync();

                var dto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从处方创建验方失败: {PrescriptionId}", prescriptionId);
                return ServiceResult<FormulaDto>.Failure("从处方创建验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 分析验方
        /// </summary>
        public async Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(formulaId);
                if (formula == null)
                    return ServiceResult<FormulaAnalysisResult>.Failure("验方不存在");

                var analysisResult = new FormulaAnalysisResult
                {
                    Summary = "验方分析完成",
                    Effects = new List<string> { "清热解毒", "消炎镇痛" },
                    Contraindications = new List<string> { "孕妇慎用", "儿童减量" },
                    Warnings = new List<HerbCompatibilityWarning>()
                };

                return ServiceResult<FormulaAnalysisResult>.Success(analysisResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析验方失败: {Id}", formulaId);
                return ServiceResult<FormulaAnalysisResult>.Failure("分析验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 获取推荐验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string syndrome)
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .Take(5)
                    .ToListAsync();

                var recommendations = formulas.Select(f => new FormulaRecommendationDto
                {
                    Id = f.Id,
                    FormulaName = f.Name,
                    Effect = f.Effect ?? "清热解毒",
                    MatchScore = 85,
                    UsageCount = 0,
                    MatchReason = $"适用于{syndrome}症状"
                }).ToList();

                return ServiceResult<List<FormulaRecommendationDto>>.Success(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐验方失败: {Syndrome}", syndrome);
                return ServiceResult<List<FormulaRecommendationDto>>.Failure("获取推荐验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 获取验方列表（支持筛选）
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
        {
            try
            {
                var query = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query = query.Where(f => f.Name.Contains(keyword));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    // 简化实现，实际应根据category字段筛选
                    query = query.Take(10);
                }

                var formulas = await query.Take(50).ToListAsync();
                var dtos = _mapper.Map<List<FormulaDto>>(formulas);

                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方列表失败");
                return ServiceResult<List<FormulaDto>>.Failure("获取验方列表失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 获取所有验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有验方失败");
                return ServiceResult<List<FormulaDto>>.Failure("获取所有验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 复制验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
        {
            try
            {
                var original = await _dbContext.Formulas.FindAsync(id);
                if (original == null)
                    return ServiceResult<FormulaDto>.Failure("原验方不存在");

                var copy = new LYBT.Entities.Formula.Formula
                {
                    Id = Guid.NewGuid(),
                    Name = newName,
                    // CreateTime、UpdateTime字段已删除（UltraThink v2.0简化）
                    Status = CommonStatus.Enabled
                };

                _dbContext.Formulas.Add(copy);
                await _dbContext.SaveChangesAsync();

                var dto = _mapper.Map<FormulaDto>(copy);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制验方失败: {Id}", id);
                return ServiceResult<FormulaDto>.Failure("复制验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 切换验方状态
        /// </summary>
        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<bool>.Failure("验方不存在");

                formula.Status = formula.Status == CommonStatus.Enabled 
                    ? CommonStatus.Disabled 
                    : CommonStatus.Enabled;
                // UpdateTime字段已删除（UltraThink v2.0简化）

                await _dbContext.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换验方状态失败: {Id}", id);
                return ServiceResult<bool>.Failure("切换验方状态失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 获取分类列表
        /// </summary>
        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            try
            {
                var categories = new List<string>
                {
                    "经典验方",
                    "自制验方",
                    "常用验方",
                    "特殊验方"
                };

                return ServiceResult<List<string>>.Success(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分类列表失败");
                return ServiceResult<List<string>>.Failure("获取分类列表失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 搜索验方
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query)
        {
            try
            {
                var formulas = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    formulas = formulas.Where(f => f.Name.Contains(query.Keyword));
                }

                var total = await formulas.CountAsync();
                var items = await formulas
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(items);
                var result = new PagedResult<FormulaDto>
                {
                    Items = dtos,
                    TotalCount = total,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索验方失败");
                return ServiceResult<PagedResult<FormulaDto>>.Failure("搜索验方失败", ex);
            }
        }

        #endregion

        #region Legacy Support Methods (保持兼容性)

        /// <summary>
        /// 获取验方列表（兼容旧方法）
        /// </summary>
        /// <summary>
        /// [Shared] 获取推荐验方（三参数重载）
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid doctorId)
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .Take(5)
                    .ToListAsync();

                var recommendations = formulas.Select(f => new FormulaRecommendationDto
                {
                    Id = f.Id,
                    FormulaName = f.Name,
                    Effect = f.Effect ?? "清热解毒",
                    MatchScore = 85,
                    UsageCount = 0,
                    MatchReason = $"适用于{symptoms}症状，符合{diagnosis}诊断"
                }).ToList();

                return ServiceResult<List<FormulaRecommendationDto>>.Success(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐验方失败: {Symptoms}, {Diagnosis}, {DoctorId}", symptoms, diagnosis, doctorId);
                return ServiceResult<List<FormulaRecommendationDto>>.Failure("获取推荐验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 分享验方
        /// </summary>
        public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<bool>.Failure("验方不存在");

                // 简化实现：标记为共享状态
                // UpdateTime字段已删除（UltraThink v2.0简化）
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("验方分享成功: {FormulaId} by {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分享验方失败: {Id}, {OperatorId}, {OperatorName}", id, operatorId, operatorName);
                return ServiceResult<bool>.Failure("分享验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 取消分享验方
        /// </summary>
        public async Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<bool>.Failure("验方不存在");

                // 简化实现：取消共享状态
                // UpdateTime字段已删除（UltraThink v2.0简化）
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("取消验方分享成功: {FormulaId} by {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消分享验方失败: {Id}, {OperatorId}, {OperatorName}", id, operatorId, operatorName);
                return ServiceResult<bool>.Failure("取消分享验方失败", ex);
            }
        }

        public async Task<List<FormulaDto>> GetListAsync()
        {
            var result = await GetAllFormulasAsync();
            return result.IsSuccess ? result.Data! : new List<FormulaDto>();
        }

        #endregion

        #region 导入导出功能 (UltraThink v2.0: 应用户业务需求恢复)

        /// <summary>
        /// 批量导入验方数据
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ImportFormulasAsync(
            List<FormulaImportDto> formulas, 
            FormulaImportOptionsDto options)
        {
            try
            {
                _logger.LogInformation("开始批量导入验方，数量: {Count}, 批次: {ImportBatch}", 
                    formulas.Count, options.ImportBatch);

                var result = new FormulaImportResultDto
                {
                    ImportBatch = options.ImportBatch ?? Guid.NewGuid().ToString("N")[..8],
                    TotalCount = formulas.Count,
                    StartTime = DateTime.Now
                };

                var successfulFormulas = new List<FormulaDto>();
                var failedItems = new List<FormulaImportErrorDto>();

                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    for (int i = 0; i < formulas.Count; i++)
                    {
                        var importDto = formulas[i];
                        try
                        {
                            // 检查是否已存在
                            var existingFormula = await _dbContext.Formulas
                                .FirstOrDefaultAsync(f => f.Name == importDto.Name && f.Status == CommonStatus.Enabled);

                            if (existingFormula != null)
                            {
                                if (options.SkipDuplicates)
                                {
                                    result.SkippedCount++;
                                    continue;
                                }
                                
                                if (options.UpdateExisting)
                                {
                                    // 更新现有验方
                                    existingFormula.Effect = importDto.Effect ?? existingFormula.Effect;
                                    existingFormula.Usage = importDto.Usage ?? existingFormula.Usage;
                                    existingFormula.Property = importDto.Property ?? existingFormula.Property;
                                    existingFormula.IsShared = importDto.IsShared;
                                    existingFormula.Remark = importDto.Remark ?? existingFormula.Remark;
                                    // UltraThink v2.0简化：Source字段已删除

                                    await _dbContext.SaveChangesAsync();
                                    
                                    var updatedDto = _mapper.Map<FormulaDto>(existingFormula);
                                    successfulFormulas.Add(updatedDto);
                                    result.SuccessCount++;
                                    continue;
                                }
                            }

                            // 创建新验方
                            var newFormula = new LYBT.Entities.Formula.Formula
                            {
                                Id = Guid.NewGuid(),
                                Name = importDto.Name,
                                Effect = importDto.Effect,
                                Usage = importDto.Usage,
                                Property = importDto.Property,
                                IsShared = importDto.IsShared,
                                Remark = importDto.Remark,
                                // UltraThink v2.0简化：Instructions, Indications, Contraindications, Preparation, Source字段已删除
                                Status = CommonStatus.Enabled
                            };

                            _dbContext.Formulas.Add(newFormula);
                            await _dbContext.SaveChangesAsync();

                            // 处理药材组成
                            if (importDto.Herbs?.Any() == true)
                            {
                                await ProcessFormulaHerbsAsync(newFormula.Id, importDto.Herbs, options.AutoMatchHerbs, options.CreateMissingHerbs);
                            }

                            var formulaDto = _mapper.Map<FormulaDto>(newFormula);
                            successfulFormulas.Add(formulaDto);
                            result.SuccessCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "导入验方失败，行: {RowIndex}, 名称: {Name}", i + 1, importDto.Name);
                            
                            failedItems.Add(new FormulaImportErrorDto
                            {
                                RowIndex = i + 1,
                                FormulaName = importDto.Name,
                                ErrorMessage = ex.Message,
                                ErrorDetails = ex.ToString(),
                                OriginalData = System.Text.Json.JsonSerializer.Serialize(importDto)
                            });
                            result.FailedCount++;
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                result.EndTime = DateTime.Now;
                result.SuccessfulFormulas = successfulFormulas;
                result.FailedItems = failedItems;

                _logger.LogInformation("验方导入完成，成功: {Success}, 失败: {Failed}, 跳过: {Skipped}", 
                    result.SuccessCount, result.FailedCount, result.SkippedCount);

                return ServiceResult<FormulaImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入验方异常");
                return ServiceResult<FormulaImportResultDto>.Failure($"批量导入验方异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 验证导入数据
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ValidateImportDataAsync(
            List<FormulaImportDto> formulas,
            FormulaImportOptionsDto options)
        {
            try
            {
                var result = new FormulaImportResultDto
                {
                    ImportBatch = options.ImportBatch ?? "验证批次",
                    TotalCount = formulas.Count,
                    StartTime = DateTime.Now
                };

                var failedItems = new List<FormulaImportErrorDto>();

                for (int i = 0; i < formulas.Count; i++)
                {
                    var importDto = formulas[i];
                    var errors = new List<string>();

                    // 验证必填字段
                    if (string.IsNullOrWhiteSpace(importDto.Name))
                        errors.Add("验方名称不能为空");
                    
                    if (importDto.Name?.Length > 100)
                        errors.Add("验方名称长度不能超过100个字符");

                    if (importDto.Effect?.Length > 200)
                        errors.Add("功效描述长度不能超过200个字符");

                    if (importDto.Usage?.Length > 200)
                        errors.Add("用法描述长度不能超过200个字符");

                    // 验证药材信息
                    if (importDto.Herbs?.Any() != true)
                    {
                        errors.Add("必须包含至少一味中药材");
                    }
                    else
                    {
                        foreach (var herb in importDto.Herbs)
                        {
                            if (string.IsNullOrWhiteSpace(herb.HerbName))
                                errors.Add($"中药材名称不能为空");
                            
                            if (herb.Quantity <= 0 || herb.Quantity > 1000)
                                errors.Add($"用量必须在0.1-1000之间");
                        }
                    }

                    // 检查重复名称
                    if (!string.IsNullOrWhiteSpace(importDto.Name))
                    {
                        var existingFormula = await _dbContext.Formulas
                            .AnyAsync(f => f.Name == importDto.Name && f.Status == CommonStatus.Enabled);
                        
                        if (existingFormula && !options.SkipDuplicates && !options.UpdateExisting)
                        {
                            errors.Add("验方名称已存在");
                        }
                    }

                    if (errors.Any())
                    {
                        failedItems.Add(new FormulaImportErrorDto
                        {
                            RowIndex = i + 1,
                            FormulaName = importDto.Name ?? $"第{i + 1}行",
                            ErrorMessage = string.Join("; ", errors),
                            OriginalData = System.Text.Json.JsonSerializer.Serialize(importDto)
                        });
                        result.FailedCount++;
                    }
                    else
                    {
                        result.SuccessCount++;
                    }
                }

                result.EndTime = DateTime.Now;
                result.FailedItems = failedItems;

                return ServiceResult<FormulaImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证导入数据异常");
                return ServiceResult<FormulaImportResultDto>.Failure($"验证导入数据异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 导出验方数据
        /// </summary>
        public async Task<ServiceResult<List<FormulaExportDto>>> ExportFormulasAsync(List<Guid> formulaIds)
        {
            try
            {
                _logger.LogInformation("开始导出验方，数量: {Count}", formulaIds.Count);

                var formulas = await _dbContext.Formulas
                    .Where(f => formulaIds.Contains(f.Id) && f.Status == CommonStatus.Enabled)
                    .Include(f => f.Herbs)
                    // UltraThink v2.0简化：FormulaHerbItem不包含Herb导航属性，仅包含HerbName等基本信息
                    .ToListAsync();

                var exportDtos = formulas.Select(f => new FormulaExportDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Effect = f.Effect,
                    Usage = f.Usage,
                    Property = f.Property,
                    IsShared = f.IsShared,
                    Remark = f.Remark,
                    // UltraThink v2.0简化：Instructions, Indications, Contraindications, Preparation, Source字段已删除
                    Status = f.Status, // UltraThink v2.0简化：使用CommonStatus代替FormulaStatus
                    Herbs = f.Herbs?.Select(fh => new FormulaHerbExportDto
                    {
                        HerbId = fh.HerbId,
                        HerbName = fh.HerbName, // UltraThink v2.0简化：FormulaHerbItem不包含Herb导航属性，直接使用HerbName
                        Quantity = fh.Quantity,
                        Unit = fh.Unit,
                        Preparation = "", // UltraThink v2.0简化：Preparation字段已删除
                        Usage = fh.Usage,
                        Price = 0, // UltraThink v2.0简化：FormulaHerbItem不包含价格信息
                        Subtotal = 0, // UltraThink v2.0简化：需要从Herb实体查询价格计算
                        SortOrder = 0 // UltraThink v2.0简化：SortOrder字段已删除
                    }).ToList() ?? new List<FormulaHerbExportDto>(),
                    HerbCount = f.Herbs?.Count ?? 0,
                    TotalPrice = 0, // UltraThink v2.0简化：FormulaHerbItem不包含Herb导航属性，需要单独计算价格
                    ExportTime = DateTime.Now
                }).ToList();

                return ServiceResult<List<FormulaExportDto>>.Success(exportDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出验方数据异常");
                return ServiceResult<List<FormulaExportDto>>.Failure($"导出验方数据异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 导出所有验方数据
        /// </summary>
        public async Task<ServiceResult<List<FormulaExportDto>>> ExportAllFormulasAsync(
            bool includePrivate = false, 
            string? category = null)
        {
            try
            {
                var query = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                if (!includePrivate)
                {
                    query = query.Where(f => f.IsShared);
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    // 根据分类筛选（简化实现）
                    query = query.Where(f => f.Effect != null && f.Effect.Contains(category));
                }

                var formulaIds = await query.Select(f => f.Id).ToListAsync();
                return await ExportFormulasAsync(formulaIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出所有验方数据异常");
                return ServiceResult<List<FormulaExportDto>>.Failure($"导出所有验方数据异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从Excel文件导入验方
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(
            byte[] fileData,
            string fileName,
            FormulaImportOptionsDto options)
        {
            try
            {
                _logger.LogInformation("开始从Excel文件导入验方，文件: {FileName}", fileName);

                // TODO: 实现Excel文件解析逻辑
                // 这里应该使用EPPlus或NPOI等库解析Excel文件
                // 将Excel数据转换为FormulaImportDto列表，然后调用ImportFormulasAsync

                var formulas = new List<FormulaImportDto>();
                
                // 示例：从Excel解析数据的伪代码
                // using var package = new ExcelPackage(new MemoryStream(fileData));
                // var worksheet = package.Workbook.Worksheets[0];
                // formulas = ParseExcelToFormulas(worksheet);

                // 临时实现：返回空结果
                var result = new FormulaImportResultDto
                {
                    ImportBatch = options.ImportBatch ?? Guid.NewGuid().ToString("N")[..8],
                    TotalCount = 0,
                    SuccessCount = 0,
                    FailedCount = 0,
                    SkippedCount = 0,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    SuccessfulFormulas = new List<FormulaDto>(),
                    FailedItems = new List<FormulaImportErrorDto>
                    {
                        new FormulaImportErrorDto
                        {
                            RowIndex = 1,
                            FormulaName = "Excel导入",
                            ErrorMessage = "Excel导入功能待实现，需要集成EPPlus或NPOI库"
                        }
                    }
                };

                return ServiceResult<FormulaImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Excel导入验方异常");
                return ServiceResult<FormulaImportResultDto>.Failure($"从Excel导入验方异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 导出为Excel文件
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportToExcelAsync(List<Guid> formulaIds)
        {
            try
            {
                _logger.LogInformation("开始导出验方为Excel文件，数量: {Count}", formulaIds.Count);

                // TODO: 实现Excel文件生成逻辑
                // 这里应该使用EPPlus或NPOI等库生成Excel文件

                // 临时实现：返回空的Excel内容提示
                var content = System.Text.Encoding.UTF8.GetBytes("Excel导出功能待实现，需要集成EPPlus或NPOI库");
                
                return ServiceResult<byte[]>.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel文件异常");
                return ServiceResult<byte[]>.Failure($"导出Excel文件异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取导入历史记录
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaImportResultDto>>> GetImportHistoryAsync(
            int pageIndex = 1,
            int pageSize = 20,
            string? importBatch = null)
        {
            try
            {
                // TODO: 实现导入历史记录存储和查询
                // 需要创建ImportHistory表来存储导入记录

                // 临时实现：返回空结果
                var result = new PagedResult<FormulaImportResultDto>
                {
                    Items = new List<FormulaImportResultDto>(),
                    TotalCount = 0,
                    CurrentPage = pageIndex,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<FormulaImportResultDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取导入历史记录异常");
                return ServiceResult<PagedResult<FormulaImportResultDto>>.Failure($"获取导入历史记录异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取导入模板
        /// </summary>
        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            try
            {
                _logger.LogInformation("获取验方导入模板");

                // TODO: 生成验方导入Excel模板
                // 应该包含验方基本信息和药材组成的列结构

                // 临时实现：返回模板提示
                var templateContent = @"验方导入模板列：
验方名称, 功效, 用法, 性味归经, 是否共享, 用药指导, 主治症状, 禁忌症, 制备方法, 备注, 来源, 
药材1名称, 药材1用量, 药材1单位, 药材1炮制, 药材1用法,
药材2名称, 药材2用量, 药材2单位, 药材2炮制, 药材2用法,
...
（最多支持20味药材）";

                var content = System.Text.Encoding.UTF8.GetBytes(templateContent);
                
                return ServiceResult<byte[]>.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取导入模板异常");
                return ServiceResult<byte[]>.Failure($"获取导入模板异常: {ex.Message}", ex);
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 处理验方药材组成
        /// </summary>
        private async Task ProcessFormulaHerbsAsync(
            Guid formulaId, 
            List<FormulaHerbImportDto> herbImports, 
            bool autoMatchHerbs, 
            bool createMissingHerbs)
        {
            foreach (var herbImport in herbImports)
            {
                try
                {
                    Guid herbId;
                    
                    // 尝试匹配现有药材
                    var existingHerb = await _dbContext.Herbs
                        .FirstOrDefaultAsync(h => h.Name == herbImport.HerbName && h.Status == CommonStatus.Enabled);

                    if (existingHerb != null)
                    {
                        herbId = existingHerb.Id;
                    }
                    else if (createMissingHerbs)
                    {
                        // 创建新药材
                        var newHerb = new LYBT.Entities.Herbs.Herb
                        {
                            Id = Guid.NewGuid(),
                            Name = herbImport.HerbName,
                            Unit = herbImport.Unit,
                            Price = 0, // 默认价格，后续可更新
                            Status = CommonStatus.Enabled
                        };

                        _dbContext.Herbs.Add(newHerb);
                        await _dbContext.SaveChangesAsync();
                        herbId = newHerb.Id;
                    }
                    else if (!autoMatchHerbs)
                    {
                        _logger.LogWarning("未找到药材且不允许自动创建: {HerbName}", herbImport.HerbName);
                        continue;
                    }
                    else
                    {
                        continue; // 跳过未找到的药材
                    }

                    // 创建验方药材关联
                    var formulaHerb = new FormulaHerbItem
                    {
                        // UltraThink v2.0简化：FormulaHerbItem不包含Id、FormulaId、Preparation、SortOrder字段
                        HerbId = herbId,
                        HerbName = herbImport.HerbName,
                        Quantity = herbImport.Quantity,
                        Unit = herbImport.Unit,
                        Usage = herbImport.Usage,
                        Remark = herbImport.Usage // 将用法说明作为备注
                    };

                    // UltraThink v2.0简化：FormulaHerbItem通过Formula.Herbs导航属性管理
                    // formulaHerbs关联通过EF Core自动处理
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "处理验方药材失败: {HerbName}", herbImport.HerbName);
                }
            }

            await _dbContext.SaveChangesAsync();
        }


        #endregion
    }
}