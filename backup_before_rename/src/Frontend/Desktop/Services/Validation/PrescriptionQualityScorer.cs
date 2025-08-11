using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Services.Interfaces;

namespace LYBT.Desktop.Services.Validation
{
    /// <summary>
    /// 处方质量评分器 - UltraThink重构专门组件
    /// 专门负责处方质量评分计算和等级判定
    /// </summary>
    public class PrescriptionQualityScorer
    {
        private readonly ILogger<PrescriptionQualityScorer> _logger;

        #region 评分权重配置

        private const int ERROR_PENALTY = 20;      // 错误扣分
        private const int WARNING_PENALTY = 10;    // 警告扣分
        private const int INFO_PENALTY = 2;        // 信息扣分
        private const int BASE_SCORE = 100;        // 基础分数

        // 质量维度权重
        private readonly Dictionary<QualityDimension, decimal> _dimensionWeights = new()
        {
            [QualityDimension.Safety] = 0.4m,           // 安全性40%
            [QualityDimension.Effectiveness] = 0.3m,    // 有效性30%
            [QualityDimension.Rationality] = 0.2m,      // 合理性20%
            [QualityDimension.Innovation] = 0.1m        // 创新性10%
        };

        #endregion

        #region 构造函数

        public PrescriptionQualityScorer(ILogger<PrescriptionQualityScorer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 计算处方质量评分
        /// </summary>
        public async Task<PrescriptionQualityResult> CalculateQualityScoreAsync(
            PrescriptionValidationResult validationResult,
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            string diagnosis = "")
        {
            try
            {
                await Task.CompletedTask;
                var items = prescriptionItems.ToList();
                
                _logger.LogInformation("开始计算处方质量评分，药材数量: {Count}", items.Count);

                var qualityResult = new PrescriptionQualityResult();

                // 1. 基础评分计算
                var basicScore = CalculateBasicScore(validationResult);
                
                // 2. 维度评分计算
                var dimensionScores = CalculateDimensionScores(validationResult, items, diagnosis);
                
                // 3. 加权总分计算
                var weightedScore = CalculateWeightedScore(dimensionScores);

                // 4. 最终分数确定（取基础分数和加权分数的较低值）
                qualityResult.QualityScore = Math.Min(basicScore, (int)weightedScore);
                qualityResult.QualityScore = Math.Max(0, qualityResult.QualityScore); // 保证不小于0

                // 5. 质量等级判定
                qualityResult.QualityLevel = DetermineQualityLevel(qualityResult.QualityScore);

                // 6. 可开具性判断
                qualityResult.CanPrescribe = !validationResult.Errors.Any();

                // 7. 生成质量摘要
                qualityResult.Summary = GenerateQualitySummary(qualityResult, validationResult);

                // 8. 设置维度分数
                qualityResult.DimensionScores = dimensionScores;

                _logger.LogInformation("处方质量评分完成: {Score}分，等级: {Level}", 
                    qualityResult.QualityScore, qualityResult.QualityLevel);

                return qualityResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算处方质量评分时发生异常");
                return CreateFailedQualityResult();
            }
        }

        /// <summary>
        /// 获取质量等级描述
        /// </summary>
        public string GetQualityLevelDescription(PrescriptionQualityLevel level)
        {
            return level switch
            {
                PrescriptionQualityLevel.Excellent => "优秀 - 处方质量极佳，配伍合理，安全有效",
                PrescriptionQualityLevel.Good => "良好 - 处方质量较好，基本符合要求",
                PrescriptionQualityLevel.Fair => "一般 - 处方质量一般，需要适当改进",
                PrescriptionQualityLevel.NeedsImprovement => "需改进 - 处方存在较多问题，需要改进",
                PrescriptionQualityLevel.Poor => "不合格 - 处方质量差，不建议使用",
                _ => "未知质量等级"
            };
        }

        /// <summary>
        /// 生成质量改进建议
        /// </summary>
        public List<string> GenerateQualityImprovementAdvice(PrescriptionQualityResult qualityResult)
        {
            var advice = new List<string>();

            if (qualityResult.QualityScore < 60)
            {
                advice.Add("处方质量偏低，建议全面审查和重新设计");
            }
            else if (qualityResult.QualityScore < 80)
            {
                advice.Add("处方质量有待提升，建议优化配伍和剂量");
            }

            // 基于维度分数给出具体建议
            foreach (var dimension in qualityResult.DimensionScores)
            {
                if (dimension.Value < 70)
                {
                    advice.Add(GetDimensionImprovementAdvice(dimension.Key));
                }
            }

            return advice;
        }

        #endregion

        #region 私有方法 - 评分计算

        /// <summary>
        /// 计算基础评分（基于验证结果）
        /// </summary>
        private int CalculateBasicScore(PrescriptionValidationResult validationResult)
        {
            int score = BASE_SCORE;
            
            // 错误扣分
            score -= validationResult.Errors.Count * ERROR_PENALTY;
            
            // 警告扣分
            score -= validationResult.Warnings.Count * WARNING_PENALTY;
            
            // 信息提示扣分
            score -= validationResult.Infos.Count * INFO_PENALTY;

            return Math.Max(0, score);
        }

        /// <summary>
        /// 计算各维度评分
        /// </summary>
        private Dictionary<QualityDimension, int> CalculateDimensionScores(
            PrescriptionValidationResult validationResult, 
            List<PrescriptionItemInfo> items, 
            string diagnosis)
        {
            return new Dictionary<QualityDimension, int>
            {
                [QualityDimension.Safety] = CalculateSafetyScore(validationResult),
                [QualityDimension.Effectiveness] = CalculateEffectivenessScore(validationResult, items, diagnosis),
                [QualityDimension.Rationality] = CalculateRationalityScore(validationResult, items),
                [QualityDimension.Innovation] = CalculateInnovationScore(items, diagnosis)
            };
        }

        /// <summary>
        /// 计算安全性评分
        /// </summary>
        private int CalculateSafetyScore(PrescriptionValidationResult validationResult)
        {
            int score = BASE_SCORE;

            // 安全性错误严重扣分
            var safetyErrors = validationResult.Errors.Where(e => 
                e.Type == ValidationWarningType.DrugInteraction ||
                e.Type == ValidationWarningType.SpecialPopulation ||
                e.Type == ValidationWarningType.DosageIssue).ToList();

            score -= safetyErrors.Count * 25; // 安全性错误扣分更严重

            // 安全性警告扣分
            var safetyWarnings = validationResult.Warnings.Where(w =>
                w.Type == ValidationWarningType.DrugInteraction ||
                w.Type == ValidationWarningType.SpecialPopulation ||
                w.Type == ValidationWarningType.DosageIssue).ToList();

            score -= safetyWarnings.Count * 15;

            return Math.Max(0, score);
        }

        /// <summary>
        /// 计算有效性评分
        /// </summary>
        private int CalculateEffectivenessScore(PrescriptionValidationResult validationResult, 
            List<PrescriptionItemInfo> items, string diagnosis)
        {
            int score = BASE_SCORE;

            // 基于药材数量的合理性
            if (items.Count < 3)
            {
                score -= 20; // 药味过少可能影响疗效
            }
            else if (items.Count > 20)
            {
                score -= 15; // 药味过多可能影响依从性
            }

            // 基于诊断匹配度（简化评估）
            if (!string.IsNullOrEmpty(diagnosis))
            {
                if (diagnosis.Contains("虚") && !HasTonicHerbs(items))
                {
                    score -= 20; // 虚证缺少补益药
                }
                
                if (diagnosis.Contains("实") && !HasPurgativeHerbs(items))
                {
                    score -= 15; // 实证缺少泻下药
                }
            }

            return Math.Max(0, score);
        }

        /// <summary>
        /// 计算合理性评分
        /// </summary>
        private int CalculateRationalityScore(PrescriptionValidationResult validationResult, 
            List<PrescriptionItemInfo> items)
        {
            int score = BASE_SCORE;

            // 配伍合理性
            var rationalityIssues = validationResult.Warnings.Where(w =>
                w.Type == ValidationWarningType.PrescriptionRationality).ToList();

            score -= rationalityIssues.Count * 10;

            // 剂量合理性
            var dosageIssues = validationResult.Infos.Where(i =>
                i.Type == ValidationWarningType.DosageIssue).ToList();

            score -= dosageIssues.Count * 5;

            // 是否有调和药（甘草）
            if (items.Count > 5 && !items.Any(i => i.HerbName.Contains("甘草")))
            {
                score -= 10;
            }

            return Math.Max(0, score);
        }

        /// <summary>
        /// 计算创新性评分
        /// </summary>
        private int CalculateInnovationScore(List<PrescriptionItemInfo> items, string diagnosis)
        {
            int score = BASE_SCORE;

            // 这里可以根据是否使用了创新的配伍、现代药理研究支持的组合等来评分
            // 当前简化为基础分数
            
            // 如果处方完全是经典配伍，可以给适当加分
            if (HasClassicFormulaStructure(items))
            {
                score += 10;
            }

            return Math.Min(100, score);
        }

        /// <summary>
        /// 计算加权总分
        /// </summary>
        private decimal CalculateWeightedScore(Dictionary<QualityDimension, int> dimensionScores)
        {
            decimal weightedScore = 0;

            foreach (var dimension in dimensionScores)
            {
                if (_dimensionWeights.TryGetValue(dimension.Key, out var weight))
                {
                    weightedScore += dimension.Value * weight;
                }
            }

            return weightedScore;
        }

        /// <summary>
        /// 判定质量等级
        /// </summary>
        private PrescriptionQualityLevel DetermineQualityLevel(int score)
        {
            return score switch
            {
                >= 90 => PrescriptionQualityLevel.Excellent,
                >= 80 => PrescriptionQualityLevel.Good,
                >= 70 => PrescriptionQualityLevel.Fair,
                >= 60 => PrescriptionQualityLevel.NeedsImprovement,
                _ => PrescriptionQualityLevel.Poor
            };
        }

        #endregion

        #region 私有方法 - 辅助判断

        /// <summary>
        /// 是否有补益药材
        /// </summary>
        private bool HasTonicHerbs(List<PrescriptionItemInfo> items)
        {
            var tonicHerbs = new[] { "人参", "黄芪", "党参", "白术", "熟地", "当归", "枸杞" };
            return items.Any(i => tonicHerbs.Any(tonic => i.HerbName.Contains(tonic)));
        }

        /// <summary>
        /// 是否有泻下药材
        /// </summary>
        private bool HasPurgativeHerbs(List<PrescriptionItemInfo> items)
        {
            var purgativeHerbs = new[] { "大黄", "芒硝", "番泻叶", "枳实", "厚朴" };
            return items.Any(i => purgativeHerbs.Any(purgative => i.HerbName.Contains(purgative)));
        }

        /// <summary>
        /// 是否具有经典方剂结构
        /// </summary>
        private bool HasClassicFormulaStructure(List<PrescriptionItemInfo> items)
        {
            // 简化判断：是否有君臣佐使的基本结构
            var hasMonarch = items.Any(i => i.Quantity >= 15); // 君药剂量大
            var hasMinister = items.Count(i => i.Quantity >= 9 && i.Quantity < 15) >= 1; // 臣药
            var hasEnvoy = items.Any(i => i.HerbName.Contains("甘草")); // 使药

            return hasMonarch && hasMinister && hasEnvoy;
        }

        /// <summary>
        /// 获取维度改进建议
        /// </summary>
        private string GetDimensionImprovementAdvice(QualityDimension dimension)
        {
            return dimension switch
            {
                QualityDimension.Safety => "加强用药安全性检查，注意配伍禁忌和特殊人群用药",
                QualityDimension.Effectiveness => "优化药材选择和配伍，提高治疗针对性",
                QualityDimension.Rationality => "改进处方结构，注意君臣佐使配伍原则",
                QualityDimension.Innovation => "考虑现代药理研究成果，适当创新配伍",
                _ => "继续改进处方质量"
            };
        }

        /// <summary>
        /// 生成质量摘要
        /// </summary>
        private string GenerateQualitySummary(PrescriptionQualityResult qualityResult, 
            PrescriptionValidationResult validationResult)
        {
            var summary = $"处方质量等级：{GetQualityLevelText(qualityResult.QualityLevel)}（{qualityResult.QualityScore}分）";
            
            if (validationResult.Errors.Any())
            {
                summary += $"，发现{validationResult.Errors.Count}个严重问题需要立即处理";
            }
            
            if (validationResult.Warnings.Any())
            {
                summary += $"，有{validationResult.Warnings.Count}个警告需要注意";
            }

            if (qualityResult.CanPrescribe)
            {
                summary += "，可以开具处方";
            }
            else
            {
                summary += "，请修正错误后再开具处方";
            }

            return summary;
        }

        /// <summary>
        /// 获取质量等级文本
        /// </summary>
        private string GetQualityLevelText(PrescriptionQualityLevel level)
        {
            return level switch
            {
                PrescriptionQualityLevel.Excellent => "优秀",
                PrescriptionQualityLevel.Good => "良好",
                PrescriptionQualityLevel.Fair => "一般",
                PrescriptionQualityLevel.NeedsImprovement => "需改进",
                PrescriptionQualityLevel.Poor => "不合格",
                _ => "未知"
            };
        }

        /// <summary>
        /// 创建失败的质量结果
        /// </summary>
        private PrescriptionQualityResult CreateFailedQualityResult()
        {
            return new PrescriptionQualityResult
            {
                QualityLevel = PrescriptionQualityLevel.Poor,
                QualityScore = 0,
                CanPrescribe = false,
                Summary = "质量评估过程中发生异常，请检查处方数据",
                DimensionScores = new Dictionary<QualityDimension, int>
                {
                    [QualityDimension.Safety] = 0,
                    [QualityDimension.Effectiveness] = 0,
                    [QualityDimension.Rationality] = 0,
                    [QualityDimension.Innovation] = 0
                }
            };
        }

        #endregion
    }

    /// <summary>
    /// 质量维度枚举
    /// </summary>
    public enum QualityDimension
    {
        Safety,         // 安全性
        Effectiveness,  // 有效性
        Rationality,    // 合理性
        Innovation      // 创新性
    }

    /// <summary>
    /// 处方质量结果
    /// </summary>
    public class PrescriptionQualityResult
    {
        public PrescriptionQualityLevel QualityLevel { get; set; }
        public int QualityScore { get; set; }
        public bool CanPrescribe { get; set; }
        public string Summary { get; set; } = "";
        public Dictionary<QualityDimension, int> DimensionScores { get; set; } = new();
    }
}