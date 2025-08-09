using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Services.Validation;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 处方质量控制服务协调器 - UltraThink重构后的主协调器
    /// 
    /// 重构前：858行超大Service，违反单一职责原则
    /// 重构后：使用协调器模式，将职责分离到5个专门验证器：
    /// - PrescriptionInteractionValidator: 配伍禁忌检查 (150行)
    /// - PrescriptionDosageValidator: 剂量验证 (180行)
    /// - SpecialPopulationValidator: 特殊人群安全检查 (160行)
    /// - PrescriptionSuggestionGenerator: 改进建议生成 (140行)
    /// - PrescriptionQualityScorer: 质量评分 (120行)
    /// 
    /// 此类作为协调器，负责调度各个专门验证器并整合结果
    /// </summary>
    public class PrescriptionValidationService : IPrescriptionValidationService
    {
        private readonly ILogger<PrescriptionValidationService> _logger;
        
        #region 专门验证器组件
        
        private readonly PrescriptionInteractionValidator _interactionValidator;
        private readonly PrescriptionDosageValidator _dosageValidator;
        private readonly SpecialPopulationValidator _populationValidator;
        private readonly PrescriptionSuggestionGenerator _suggestionGenerator;
        private readonly PrescriptionQualityScorer _qualityScorer;
        
        #endregion

        #region 构造函数

        public PrescriptionValidationService(
            ILogger<PrescriptionValidationService> logger,
            PrescriptionInteractionValidator interactionValidator,
            PrescriptionDosageValidator dosageValidator,
            SpecialPopulationValidator populationValidator,
            PrescriptionSuggestionGenerator suggestionGenerator,
            PrescriptionQualityScorer qualityScorer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _interactionValidator = interactionValidator ?? throw new ArgumentNullException(nameof(interactionValidator));
            _dosageValidator = dosageValidator ?? throw new ArgumentNullException(nameof(dosageValidator));
            _populationValidator = populationValidator ?? throw new ArgumentNullException(nameof(populationValidator));
            _suggestionGenerator = suggestionGenerator ?? throw new ArgumentNullException(nameof(suggestionGenerator));
            _qualityScorer = qualityScorer ?? throw new ArgumentNullException(nameof(qualityScorer));
        }

        #endregion

        #region 主要协调方法

        /// <summary>
        /// 处方质量验证主协调方法 - 调度各个专门验证器
        /// </summary>
        public async Task<PrescriptionValidationResult> ValidatePrescriptionAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems, 
            PatientValidationInfo patientInfo,
            string diagnosis = "")
        {
            try
            {
                var items = prescriptionItems.ToList();
                _logger.LogInformation("开始处方质量验证协调，药材数量: {Count}", items.Count);

                var result = new PrescriptionValidationResult();

                // 1. 基础数据验证（本地处理）
                ValidateBasicData(items, result);

                // 2. 配伍禁忌检查（委托给专门验证器）
                var interactionWarnings = await _interactionValidator.CheckInteractionsAsync(items);
                AddInteractionWarningsToResult(interactionWarnings, result);

                // 3. 剂量合理性检查（委托给专门验证器）
                var dosageWarnings = await _dosageValidator.ValidateDosagesAsync(items, patientInfo.Age, patientInfo.Weight ?? 0);
                AddDosageWarningsToResult(dosageWarnings, result);

                // 4. 特殊人群用药安全检查（委托给专门验证器）
                var specialWarnings = await _populationValidator.ValidateSpecialPopulationSafetyAsync(items, patientInfo);
                AddSpecialWarningsToResult(specialWarnings, result);

                // 5. 处方合理性检查（本地处理）
                ValidatePrescriptionRationality(items, diagnosis, result);

                // 6. 生成改进建议（委托给专门生成器）
                var suggestions = await _suggestionGenerator.GenerateImprovementSuggestionsAsync(items, diagnosis);
                result.Suggestions = suggestions;

                // 7. 计算质量评分（委托给专门评分器）
                var qualityResult = await _qualityScorer.CalculateQualityScoreAsync(result, items, diagnosis);
                
                // 将评分结果合并到验证结果中
                result.QualityScore = qualityResult.QualityScore;
                result.QualityLevel = qualityResult.QualityLevel;
                result.Summary = qualityResult.Summary;

                _logger.LogInformation("处方质量验证协调完成，质量等级: {Level}，评分: {Score}", 
                    result.QualityLevel, result.QualityScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处方质量验证协调过程中发生异常");
                return CreateFailedValidationResult();
            }
        }

        /// <summary>
        /// 检查药物配伍禁忌（委托给专门验证器）
        /// </summary>
        public async Task<List<DrugInteractionWarning>> CheckDrugInteractionsAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems)
        {
            return await _interactionValidator.CheckInteractionsAsync(prescriptionItems);
        }

        /// <summary>
        /// 检查剂量合理性（委托给专门验证器）
        /// </summary>
        public async Task<List<DosageWarning>> CheckDosageAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            int patientAge,
            double patientWeight = 0)
        {
            return await _dosageValidator.ValidateDosagesAsync(prescriptionItems, patientAge, patientWeight);
        }

        /// <summary>
        /// 检查特殊人群用药安全（委托给专门验证器）
        /// </summary>
        public async Task<List<SpecialPopulationWarning>> CheckSpecialPopulationSafetyAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            PatientValidationInfo patientInfo)
        {
            return await _populationValidator.ValidateSpecialPopulationSafetyAsync(prescriptionItems, patientInfo);
        }

        /// <summary>
        /// 获取改进建议（委托给专门生成器）
        /// </summary>
        public async Task<List<PrescriptionSuggestion>> GetImprovementSuggestionsAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            string diagnosis)
        {
            return await _suggestionGenerator.GenerateImprovementSuggestionsAsync(prescriptionItems, diagnosis);
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 基础数据验证（本地简单验证）
        /// </summary>
        private void ValidateBasicData(List<PrescriptionItemInfo> items, PrescriptionValidationResult result)
        {
            if (!items.Any())
            {
                result.Errors.Add(new ValidationWarning
                {
                    Type = ValidationWarningType.PrescriptionRationality,
                    Severity = ValidationSeverity.Error,
                    Message = "处方不能为空",
                    Suggestion = "至少添加一味药材"
                });
                return;
            }

            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                {
                    result.Errors.Add(new ValidationWarning
                    {
                        Type = ValidationWarningType.DosageIssue,
                        Severity = ValidationSeverity.Error,
                        Message = $"{item.HerbName}的用量必须大于0",
                        HerbName = item.HerbName,
                        Suggestion = "请设置合适的用量"
                    });
                }

                if (string.IsNullOrWhiteSpace(item.HerbName))
                {
                    result.Errors.Add(new ValidationWarning
                    {
                        Type = ValidationWarningType.HerbQuality,
                        Severity = ValidationSeverity.Error,
                        Message = "药材名称不能为空",
                        Suggestion = "请选择有效的药材"
                    });
                }
            }
        }

        private void AddInteractionWarningsToResult(List<DrugInteractionWarning> interactions, PrescriptionValidationResult result)
        {
            foreach (var interaction in interactions)
            {
                var severity = interaction.Severity switch
                {
                    InteractionSeverity.Severe => ValidationSeverity.Error,
                    InteractionSeverity.Moderate => ValidationSeverity.Warning,
                    InteractionSeverity.Minor => ValidationSeverity.Info,
                    _ => ValidationSeverity.Warning
                };

                var warning = new ValidationWarning
                {
                    Type = ValidationWarningType.DrugInteraction,
                    Severity = severity,
                    Message = interaction.Description,
                    Suggestion = interaction.ManagementAdvice,
                    Reference = $"相关药材：{string.Join("、", interaction.InteractingHerbs)}"
                };

                if (severity == ValidationSeverity.Error)
                    result.Errors.Add(warning);
                else if (severity == ValidationSeverity.Warning)
                    result.Warnings.Add(warning);
                else
                    result.Infos.Add(warning);
            }
        }

        private void AddDosageWarningsToResult(List<DosageWarning> dosageWarnings, PrescriptionValidationResult result)
        {
            foreach (var warning in dosageWarnings)
            {
                var severity = warning.Type switch
                {
                    DosageWarningType.ToxicOverdose => ValidationSeverity.Error,
                    DosageWarningType.ExcessiveDose => ValidationSeverity.Warning,
                    DosageWarningType.InsufficientDose => ValidationSeverity.Info,
                    DosageWarningType.PediatricDosageIssue => ValidationSeverity.Warning,
                    DosageWarningType.GeriatricDosageIssue => ValidationSeverity.Warning,
                    _ => ValidationSeverity.Warning
                };

                var validationWarning = new ValidationWarning
                {
                    Type = ValidationWarningType.DosageIssue,
                    Severity = severity,
                    Message = warning.Description,
                    HerbName = warning.HerbName,
                    Suggestion = warning.AdjustmentAdvice,
                    Reference = warning.RiskDescription
                };

                if (severity == ValidationSeverity.Error)
                    result.Errors.Add(validationWarning);
                else if (severity == ValidationSeverity.Warning)
                    result.Warnings.Add(validationWarning);
                else
                    result.Infos.Add(validationWarning);
            }
        }

        private void AddSpecialWarningsToResult(List<SpecialPopulationWarning> specialWarnings, PrescriptionValidationResult result)
        {
            foreach (var warning in specialWarnings)
            {
                var severity = warning.RiskLevel switch
                {
                    RiskLevel.High => ValidationSeverity.Error,
                    RiskLevel.Medium => ValidationSeverity.Warning,
                    RiskLevel.Low => ValidationSeverity.Info,
                    _ => ValidationSeverity.Warning
                };

                var validationWarning = new ValidationWarning
                {
                    Type = ValidationWarningType.SpecialPopulation,
                    Severity = severity,
                    Message = warning.RiskDescription,
                    Suggestion = warning.Recommendation,
                    Reference = $"相关药材：{string.Join("、", warning.AffectedHerbs)}"
                };

                if (severity == ValidationSeverity.Error)
                    result.Errors.Add(validationWarning);
                else if (severity == ValidationSeverity.Warning)
                    result.Warnings.Add(validationWarning);
                else
                    result.Infos.Add(validationWarning);
            }
        }

        /// <summary>
        /// 处方合理性验证（简化版本，重点检查基本合理性）
        /// </summary>
        private void ValidatePrescriptionRationality(List<PrescriptionItemInfo> items, string diagnosis, PrescriptionValidationResult result)
        {
            // 检查处方药味数量合理性
            if (items.Count > 20)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Type = ValidationWarningType.PrescriptionRationality,
                    Severity = ValidationSeverity.Warning,
                    Message = $"处方药味过多（{items.Count}味），可能影响患者服用依从性",
                    Suggestion = "建议精简处方，选择主要药材，药味数量控制在15味以内"
                });
            }

            if (items.Count < 3 && !string.IsNullOrEmpty(diagnosis) && !diagnosis.Contains("单味"))
            {
                result.Infos.Add(new ValidationWarning
                {
                    Type = ValidationWarningType.PrescriptionRationality,
                    Severity = ValidationSeverity.Info,
                    Message = $"处方药味较少（{items.Count}味），请确认是否需要配伍其他药材",
                    Suggestion = "考虑是否需要添加辅助药材以增强疗效或减少不良反应"
                });
            }

            // 基本方剂结构检查
            if (items.Count >= 4)
            {
                result.Infos.Add(new ValidationWarning
                {
                    Type = ValidationWarningType.PrescriptionRationality,
                    Severity = ValidationSeverity.Info,
                    Message = "处方具备基本的方剂结构",
                    Suggestion = "建议明确君臣佐使的配伍关系"
                });
            }
        }

        /// <summary>
        /// 创建失败的验证结果
        /// </summary>
        private PrescriptionValidationResult CreateFailedValidationResult()
        {
            return new PrescriptionValidationResult
            {
                QualityLevel = PrescriptionQualityLevel.Poor,
                QualityScore = 0,
                Summary = "验证过程中发生异常，请检查处方数据",
                Suggestions = new List<PrescriptionSuggestion>()
            };
        }

        #endregion
    }
}