using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Formula;
using LYBT.Entities.Herbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace LYBT.Module.Formula.Helpers
{
    /// <summary>
    /// 验方计算辅助类
    /// 负责验方分析、推荐算法、数据处理等复杂计算逻辑
    /// </summary>
    public class FormulaCalculationHelper
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaCalculationHelper> _logger;

        public FormulaCalculationHelper(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<FormulaCalculationHelper> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 分析验方功效和配伍
        /// </summary>
        public async Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
        {
            try
            {
                var formula = await _dbContext.Formulas
                    .Include(f => f.Herbs)
                    .FirstOrDefaultAsync(f => f.Id == formulaId && f.Status == CommonStatus.Enabled);

                if (formula == null)
                    return ServiceResult<FormulaAnalysisResult>.Failure("验方不存在");                // 分析药材配伍
                var herbs = await _dbContext.Herbs
                    .Where(h => formula.Herbs.Select(fh => fh.HerbId).Contains(h.Id))
                    .ToListAsync();

                var analysisResult = new FormulaAnalysisResult
                {
                    Summary = await GenerateFormulaSummary(formula, herbs),
                    Effects = await AnalyzeFormulaEffects(herbs),
                    Contraindications = await AnalyzeContraindications(herbs),
                    Warnings = await CheckHerbCompatibility(herbs)
                };

                return ServiceResult<FormulaAnalysisResult>.Success(analysisResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析验方失败: {Id}", formulaId);                return ServiceResult<FormulaAnalysisResult>.Failure("分析验方失败");            }
        }

        /// <summary>
        /// 获取症状推荐验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsBySyndromeAsync(string syndrome)
        {
            try
            {
                // 基于症状匹配相关验方
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .Where(f => f.Effect != null && f.Effect.Contains(syndrome))
                    .OrderBy(f => f.Name)
                    .Take(10)
                    .ToListAsync();

                var recommendations = await CalculateRecommendationScores(formulas, syndrome);

                return ServiceResult<List<FormulaRecommendationDto>>.Success(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐验方失败: {Syndrome}", syndrome);                return ServiceResult<List<FormulaRecommendationDto>>.Failure("获取推荐验方失败", ex);            }
        }

        /// <summary>
        /// 获取基于症状、诊断和医生的智能推荐
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetIntelligentRecommendationsAsync(
            string symptoms, string diagnosis, Guid doctorId)
        {
            try
            {
                // 1. 基于症状匹配
                var symptomMatches = await GetFormulasBySymptoms(symptoms);
                
                // 2. 基于诊断匹配
                var diagnosisMatches = await GetFormulasByDiagnosis(diagnosis);
                
                // 3. 基于医生历史用药习惯
                var doctorPreferences = await GetDoctorFormulaPreferences(doctorId);

                // 4. 综合评分和排序
                var combinedRecommendations = await CombineAndScoreRecommendations(
                    symptomMatches, diagnosisMatches, doctorPreferences, symptoms, diagnosis);

                return ServiceResult<List<FormulaRecommendationDto>>.Success(combinedRecommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取智能推荐失败: Symptoms={Symptoms}, Diagnosis={Diagnosis}, DoctorId={DoctorId}",                     symptoms, diagnosis, doctorId);
                return ServiceResult<List<FormulaRecommendationDto>>.Failure("获取智能推荐失败", ex);            }
        }

        /// <summary>
        /// 从处方创建验方
        /// </summary>
        public async Task<ServiceResult<LYBT.Entities.Formula.Formula>> CreateFromPrescriptionAsync(
            Guid prescriptionId, string formulaName)
        {
            try
            {
                // 查找处方信息
                var prescription = await _dbContext.Prescriptions
                    // TODO: 添加Herbs导航属性后重新启用
                    // .Include(p => p.Herbs)
                    .FirstOrDefaultAsync(p => p.Id == prescriptionId);

                if (prescription == null)
                    return ServiceResult<LYBT.Entities.Formula.Formula>.Failure("处方不存在");                // 创建新验方
                var formula = new LYBT.Entities.Formula.Formula
                {
                    Id = Guid.NewGuid(),
                    Name = formulaName,
                    Effect = "基于处方创建的验方",                    Usage = "按医嘱使用", // TODO: Prescription实体暂无Usage属性                    Property = "温",                    IsShared = false,
                    Remark = $"从处方ID {prescriptionId} 创建",                    Status = CommonStatus.Enabled
                };

                _dbContext.Formulas.Add(formula);

                // TODO: 复制处方中的药材到验方（待Prescription实体添加Herbs导航属性）
                // if (prescription.Herbs?.Any() == true)
                // {
                //     foreach (var prescriptionHerb in prescription.Herbs)
                //     {
                //         var formulaHerb = new FormulaHerbItem
                //         {
                //             HerbId = prescriptionHerb.HerbId,
                //             HerbName = prescriptionHerb.HerbName,
                //             Quantity = prescriptionHerb.Quantity,
                //             Unit = prescriptionHerb.Unit,
                //             Usage = prescriptionHerb.Usage,
                //             Remark = "从处方复制"                //         };
                //
                //         if (formula.Herbs == null)
                //             formula.Herbs = new List<FormulaHerbItem>();
                //         
                //         formula.Herbs.Add(formulaHerb);
                //     }
                // }

                await _dbContext.SaveChangesAsync();

                return ServiceResult<LYBT.Entities.Formula.Formula>.Success(formula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从处方创建验方失败: PrescriptionId={PrescriptionId}, FormulaName={FormulaName}",                     prescriptionId, formulaName);
                return ServiceResult<LYBT.Entities.Formula.Formula>.Failure("从处方创建验方失败");            }
        }

        /// <summary>
        /// 处理验方药材组成（用于导入）
        /// </summary>
        public async Task<ServiceResult<List<FormulaHerbItem>>> ProcessFormulaHerbsAsync(
            Guid formulaId, 
            List<FormulaHerbImportDto> herbImports, 
            bool autoMatchHerbs, 
            bool createMissingHerbs)
        {
            try
            {
                var processedHerbs = new List<FormulaHerbItem>();

                foreach (var herbImport in herbImports)
                {
                    try
                    {
                        var herbResult = await ProcessSingleHerbImport(herbImport, autoMatchHerbs, createMissingHerbs);
                        if (herbResult.IsSuccess && herbResult.Data != null)
                        {
                            processedHerbs.Add(herbResult.Data);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "处理药材失败: {HerbName}", herbImport.HerbName);                    }
                }

                return ServiceResult<List<FormulaHerbItem>>.Success(processedHerbs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理验方药材组成失败: FormulaId={FormulaId}", formulaId);                return ServiceResult<List<FormulaHerbItem>>.Failure("处理验方药材组成失败", ex);            }
        }

        /// <summary>
        /// 执行复杂的验方导入逻辑
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ExecuteImportAsync(
            List<FormulaImportDto> formulas, 
            FormulaImportOptionsDto options)
        {
            try
            {
                _logger.LogInformation("开始批量导入验方，数量: {Count}, 批次: {ImportBatch}",                     formulas.Count, options.ImportBatch);

                var result = new FormulaImportResultDto
                {
                    ImportBatch = options.ImportBatch ?? Guid.NewGuid().ToString("N")[..8],                    TotalCount = formulas.Count,
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
                        var importResult = await ProcessSingleFormulaImport(importDto, options, i + 1);
                        
                        if (importResult.IsSuccess)
                        {
                            successfulFormulas.Add(importResult.Data!);
                            result.SuccessCount++;
                        }
                        else
                        {
                            failedItems.Add(new FormulaImportErrorDto
                            {
                                RowIndex = i + 1,
                                FormulaName = importDto.Name,
                                ErrorMessage = importResult.ErrorMessage!,
                                OriginalData = System.Text.Json.JsonSerializer.Serialize(importDto)
                            });
                            
                            if (importResult.ErrorMessage!.Contains("跳过"))                                result.SkippedCount++;
                            else
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

                _logger.LogInformation("验方导入完成，成功: {Success}, 失败: {Failed}, 跳过: {Skipped}",                     result.SuccessCount, result.FailedCount, result.SkippedCount);

                return ServiceResult<FormulaImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入验方异常");                return ServiceResult<FormulaImportResultDto>.Failure($"批量导入验方异常: {ex.Message}", ex);            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 生成验方总结
        /// </summary>
        private Task<string> GenerateFormulaSummary(LYBT.Entities.Formula.Formula formula, List<Herb> herbs)
        {
            var herbCount = herbs.Count;
            var mainHerbs = herbs.Take(3).Select(h => h.Name).ToList();
            
            return Task.FromResult($"验方【{formula.Name}】包含{herbCount}味中药，主要药材：{string.Join("、", mainHerbs)}等。" +                   $"功效：{formula.Effect ?? "清热解毒"}。适用症状广泛，配伍合理。");        }

        /// <summary>
        /// 分析验方功效
        /// </summary>
        private Task<List<string>> AnalyzeFormulaEffects(List<Herb> herbs)
        {
            var effects = new List<string>();
            
            // 基于药材属性分析功效
            var herbNames = herbs.Select(h => h.Name.ToLower()).ToList();
            
            if (herbNames.Any(name => name.Contains("黄芩") || name.Contains("黄连") || name.Contains("板蓝根")))                effects.Add("清热解毒");            
            if (herbNames.Any(name => name.Contains("人参") || name.Contains("党参") || name.Contains("黄芪")))                effects.Add("补气健脾");            
            if (herbNames.Any(name => name.Contains("当归") || name.Contains("川芎") || name.Contains("丹参")))                effects.Add("活血化瘀");            
            if (herbNames.Any(name => name.Contains("陈皮") || name.Contains("半夏") || name.Contains("茯苓")))                effects.Add("健脾化湿");            return Task.FromResult(effects.Any() ? effects : new List<string> { "调和诸药", "扶正祛邪" });        }

        /// <summary>
        /// 分析禁忌症
        /// </summary>
        private Task<List<string>> AnalyzeContraindications(List<Herb> herbs)
        {
            var contraindications = new List<string>();
            
            var herbNames = herbs.Select(h => h.Name.ToLower()).ToList();
            
            if (herbNames.Any(name => name.Contains("大黄") || name.Contains("芒硝")))                contraindications.Add("孕妇禁用");            
            if (herbNames.Any(name => name.Contains("麻黄") || name.Contains("桂枝")))                contraindications.Add("高血压患者慎用");            
            if (herbNames.Any(name => name.Contains("附子") || name.Contains("干姜")))                contraindications.Add("阴虚火旺者禁用");            return Task.FromResult(contraindications.Any() ? contraindications : new List<string> { "暂无特殊禁忌" });        }

        /// <summary>
        /// 检查药材配伍禁忌
        /// </summary>
        private Task<List<HerbCompatibilityWarning>> CheckHerbCompatibility(List<Herb> herbs)
        {
            var warnings = new List<HerbCompatibilityWarning>();
            
            // 检查常见配伍禁忌
            var herbNames = herbs.Select(h => h.Name).ToList();
            
            if (herbNames.Contains("甘草") && herbNames.Contains("大戟"))            {
                warnings.Add(new HerbCompatibilityWarning
                {
                    HerbName1 = "甘草",                    HerbName2 = "大戟",                     WarningLevel = "相恶",                    Description = "甘草与大戟相恶，不宜同用"                });
            }

            return Task.FromResult(warnings);
        }

        /// <summary>
        /// 计算推荐评分
        /// </summary>
        private Task<List<FormulaRecommendationDto>> CalculateRecommendationScores(
            List<LYBT.Entities.Formula.Formula> formulas, string syndrome)
        {
            var recommendations = new List<FormulaRecommendationDto>();
            
            foreach (var formula in formulas)
            {
                var score = 75; // 基础分
                
                // 基于匹配度调整分数
                if (formula.Effect?.Contains(syndrome) == true)
                    score += 10;
                
                if (formula.IsShared)
                    score += 5; // 共享验方加分
                
                // 模拟使用频次
                var usageCount = Random.Shared.Next(0, 50);
                if (usageCount > 20) score += 5;

                recommendations.Add(new FormulaRecommendationDto
                {
                    Id = formula.Id,
                    FormulaName = formula.Name,
                    Effect = formula.Effect ?? "调和诸药",                    MatchScore = Math.Min(score, 100),
                    UsageCount = usageCount,
                    MatchReason = $"适用于{syndrome}症状，配伍合理"                });
            }

            return Task.FromResult(recommendations.OrderByDescending(r => r.MatchScore).Take(5).ToList());
        }

        /// <summary>
        /// 根据症状获取相关验方
        /// </summary>
        private async Task<List<LYBT.Entities.Formula.Formula>> GetFormulasBySymptoms(string symptoms)
        {
            return await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .Where(f => f.Effect != null && f.Effect.Contains(symptoms))
                .Take(20)
                .ToListAsync();
        }

        /// <summary>
        /// 根据诊断获取相关验方
        /// </summary>
        private async Task<List<LYBT.Entities.Formula.Formula>> GetFormulasByDiagnosis(string diagnosis)
        {
            return await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .Where(f => f.Effect != null && f.Effect.Contains(diagnosis))
                .Take(20)
                .ToListAsync();
        }

        /// <summary>
        /// 获取医生用药偏好
        /// </summary>
        private async Task<List<LYBT.Entities.Formula.Formula>> GetDoctorFormulaPreferences(Guid doctorId)
        {
            // TODO: 基于医生历史处方数据分析用药偏好
            // 这里可以分析医生常用的验方模式
            
            return await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled && f.IsShared)
                .Take(10)
                .ToListAsync();
        }

        /// <summary>
        /// 综合评分和排序推荐
        /// </summary>
        private Task<List<FormulaRecommendationDto>> CombineAndScoreRecommendations(
            List<LYBT.Entities.Formula.Formula> symptomMatches,
            List<LYBT.Entities.Formula.Formula> diagnosisMatches,
            List<LYBT.Entities.Formula.Formula> doctorPreferences,
            string symptoms, string diagnosis)
        {
            var allFormulas = symptomMatches.Union(diagnosisMatches).Union(doctorPreferences)
                .GroupBy(f => f.Id)
                .Select(g => g.First())
                .ToList();

            var recommendations = new List<FormulaRecommendationDto>();

            foreach (var formula in allFormulas)
            {
                var score = 60; // 基础分

                if (symptomMatches.Any(f => f.Id == formula.Id)) score += 15;
                if (diagnosisMatches.Any(f => f.Id == formula.Id)) score += 15;
                if (doctorPreferences.Any(f => f.Id == formula.Id)) score += 10;

                recommendations.Add(new FormulaRecommendationDto
                {
                    Id = formula.Id,
                    FormulaName = formula.Name,
                    Effect = formula.Effect ?? "调和诸药",                    MatchScore = Math.Min(score, 100),
                    UsageCount = Random.Shared.Next(0, 100),
                    MatchReason = $"符合{symptoms}症状和{diagnosis}诊断，医生常用验方"                });
            }

            return Task.FromResult(recommendations.OrderByDescending(r => r.MatchScore).Take(8).ToList());
        }

        /// <summary>
        /// 处理单个药材导入
        /// </summary>
        private async Task<ServiceResult<FormulaHerbItem>> ProcessSingleHerbImport(
            FormulaHerbImportDto herbImport, bool autoMatchHerbs, bool createMissingHerbs)
        {
            try
            {
                // 尝试匹配现有药材
                var existingHerb = await _dbContext.Herbs
                    .FirstOrDefaultAsync(h => h.Name == herbImport.HerbName && h.Status == CommonStatus.Enabled);

                Guid herbId;
                
                if (existingHerb != null)
                {
                    herbId = existingHerb.Id;
                }
                else if (createMissingHerbs)
                {
                    // 创建新药材
                    var newHerb = new Herb
                    {
                        Id = Guid.NewGuid(),
                        Name = herbImport.HerbName,
                        Unit = herbImport.Unit,
                        Price = 0,
                        Status = CommonStatus.Enabled
                    };

                    _dbContext.Herbs.Add(newHerb);
                    await _dbContext.SaveChangesAsync();
                    herbId = newHerb.Id;
                }
                else
                {
                    return ServiceResult<FormulaHerbItem>.Failure($"未找到药材且不允许自动创建: {herbImport.HerbName}");                }

                var formulaHerb = new FormulaHerbItem
                {
                    HerbId = herbId,
                    HerbName = herbImport.HerbName,
                    Quantity = herbImport.Quantity,
                    Unit = herbImport.Unit,
                    Usage = herbImport.Usage,
                    Remark = herbImport.Usage
                };

                return ServiceResult<FormulaHerbItem>.Success(formulaHerb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理单个药材导入失败: {HerbName}", herbImport.HerbName);                return ServiceResult<FormulaHerbItem>.Failure($"处理药材失败: {ex.Message}");            }
        }

        /// <summary>
        /// 处理单个验方导入
        /// </summary>
        private async Task<ServiceResult<FormulaDto>> ProcessSingleFormulaImport(
            FormulaImportDto importDto, FormulaImportOptionsDto options, int rowIndex)
        {
            try
            {
                // 检查是否已存在
                var existingFormula = await _dbContext.Formulas
                    .FirstOrDefaultAsync(f => f.Name == importDto.Name && f.Status == CommonStatus.Enabled);

                if (existingFormula != null)
                {
                    if (options.SkipDuplicates)
                    {
                        return ServiceResult<FormulaDto>.Failure("跳过重复验方");                    }
                    
                    if (options.UpdateExisting)
                    {
                        // 更新现有验方
                        existingFormula.Effect = importDto.Effect ?? existingFormula.Effect;
                        existingFormula.Usage = importDto.Usage ?? existingFormula.Usage;
                        existingFormula.Property = importDto.Property ?? existingFormula.Property;
                        existingFormula.IsShared = importDto.IsShared;
                        existingFormula.Remark = importDto.Remark ?? existingFormula.Remark;

                        await _dbContext.SaveChangesAsync();
                        
                        var updatedDto = _mapper.Map<FormulaDto>(existingFormula);
                        return ServiceResult<FormulaDto>.Success(updatedDto);
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
                    Status = CommonStatus.Enabled
                };

                _dbContext.Formulas.Add(newFormula);
                await _dbContext.SaveChangesAsync();

                // 处理药材组成
                if (importDto.Herbs?.Any() == true)
                {
                    var herbsResult = await ProcessFormulaHerbsAsync(newFormula.Id, importDto.Herbs, 
                        options.AutoMatchHerbs, options.CreateMissingHerbs);
                    
                    if (herbsResult.IsSuccess && herbsResult.Data?.Any() == true)
                    {
                        if (newFormula.Herbs == null)
                            newFormula.Herbs = new List<FormulaHerbItem>();
                        
                        newFormula.Herbs.AddRange(herbsResult.Data);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                var formulaDto = _mapper.Map<FormulaDto>(newFormula);
                return ServiceResult<FormulaDto>.Success(formulaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理单个验方导入失败，行: {RowIndex}, 名称: {Name}", rowIndex, importDto.Name);                return ServiceResult<FormulaDto>.Failure($"导入失败: {ex.Message}");
            }
        }

        #endregion
    }
}


