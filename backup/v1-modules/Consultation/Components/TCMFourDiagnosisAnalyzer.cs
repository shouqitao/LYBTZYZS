using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Consultation.ViewModels;

namespace LYBT.Desktop.Consultation.Components
{
    /// <summary>
    /// 中医四诊症状分析器 - UltraThink重构专门组件
    /// 专门负责症状分析和智能诊断建议
    /// </summary>
    public class TCMFourDiagnosisAnalyzer
    {
        private readonly ILogger<TCMFourDiagnosisAnalyzer>? _logger;
        private readonly TCMFourDiagnosisDescriptionGenerator _descriptionGenerator;

        #region 诊断规则数据

        // 证型诊断规则
        private readonly Dictionary<string, SyndromeRule> _syndromeRules = new();

        #endregion

        #region 构造函数

        public TCMFourDiagnosisAnalyzer(
            ILogger<TCMFourDiagnosisAnalyzer>? logger = null,
            TCMFourDiagnosisDescriptionGenerator? descriptionGenerator = null)
        {
            _logger = logger;
            _descriptionGenerator = descriptionGenerator ?? new TCMFourDiagnosisDescriptionGenerator();
            InitializeSyndromeRules();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 分析症状并推荐证型
        /// </summary>
        public async Task<List<string>> AnalyzeSyndromeAsync(TCMFourDiagnosisDataManager dataManager)
        {
            try
            {
                await Task.Delay(100); // 模拟分析过程
                
                var fourDiagnosisData = _descriptionGenerator.GenerateCompleteDescription(dataManager);
                var recommendations = new List<string>();

                _logger?.LogInformation("开始智能证型分析");

                // 分析每个证型的匹配度
                foreach (var rule in _syndromeRules)
                {
                    var matchScore = CalculateSyndromeMatchScore(fourDiagnosisData, dataManager, rule.Value);
                    if (matchScore >= 0.6) // 60%以上匹配度
                    {
                        recommendations.Add(rule.Key);
                    }
                }

                // 按匹配度排序
                var sortedRecommendations = recommendations
                    .Select(syndrome => new { 
                        Syndrome = syndrome, 
                        Score = CalculateSyndromeMatchScore(fourDiagnosisData, dataManager, _syndromeRules[syndrome]) 
                    })
                    .OrderByDescending(r => r.Score)
                    .Select(r => r.Syndrome)
                    .ToList();

                _logger?.LogInformation("证型分析完成，推荐{Count}个证型", sortedRecommendations.Count);
                return sortedRecommendations;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "症状分析时发生异常");
                return new List<string>();
            }
        }

        /// <summary>
        /// 推荐治疗原则
        /// </summary>
        public async Task<List<string>> RecommendTreatmentAsync(string syndrome)
        {
            await Task.Delay(50); // 模拟分析过程

            return syndrome switch
            {
                "风寒感冒" or "风寒束表证" => new List<string> { "辛温解表", "宣肺散寒" },
                "风热感冒" or "风热犯表证" => new List<string> { "辛凉解表", "清热宣肺" },
                "脾胃虚弱" or "脾胃虚弱证" => new List<string> { "健脾益胃", "补中益气" },
                "肾虚证" or "肾阳虚" or "肾阴虚" => new List<string> { "补肾固本", "滋阴壮阳" },
                "肝郁气滞" or "肝郁气滞证" => new List<string> { "疏肝理气", "调畅气机" },
                "血瘀证" => new List<string> { "活血化瘀", "理气止痛" },
                "痰湿蕴肺" or "痰湿蕴肺证" => new List<string> { "燥湿化痰", "宣肺止咳" },
                "心血虚" => new List<string> { "补血养心", "安神定志" },
                "肝火上炎" => new List<string> { "清肝泻火", "平肝潜阳" },
                "肺热咳嗽" => new List<string> { "清热宣肺", "止咳化痰" },
                _ => new List<string> { "辨证论治" }
            };
        }

        /// <summary>
        /// 分析诊断一致性
        /// </summary>
        public DiagnosisConsistencyResult AnalyzeDiagnosisConsistency(TCMFourDiagnosisDataManager dataManager)
        {
            var result = new DiagnosisConsistencyResult();
            var fourDiagnosisData = _descriptionGenerator.GenerateCompleteDescription(dataManager);

            // 检查四诊信息的完整性
            result.InspectionCompleteness = CalculateInspectionCompleteness(dataManager);
            result.AuscultationCompleteness = CalculateAuscultationCompleteness(dataManager);
            result.InquiryCompleteness = CalculateInquiryCompleteness(dataManager);
            result.PalpationCompleteness = CalculatePalpationCompleteness(dataManager);

            // 检查诊断与症状的一致性
            if (!string.IsNullOrWhiteSpace(dataManager.TCMSyndrome))
            {
                result.DiagnosisConsistency = CalculateDiagnosisConsistency(fourDiagnosisData, dataManager.TCMSyndrome);
            }

            // 计算总体完整性得分
            result.OverallCompleteness = (result.InspectionCompleteness + result.AuscultationCompleteness + 
                                        result.InquiryCompleteness + result.PalpationCompleteness) / 4.0;

            return result;
        }

        /// <summary>
        /// 获取症状关键词分析
        /// </summary>
        public SymptomKeywordAnalysis AnalyzeSymptomKeywords(TCMFourDiagnosisDataManager dataManager)
        {
            var analysis = new SymptomKeywordAnalysis();
            var allSymptoms = GetAllSymptomsText(dataManager);

            // 分析虚实寒热
            analysis.DeficiencyPatterns = CountKeywordMatches(allSymptoms, new[] { "乏力", "疲倦", "气短", "自汗", "便溏" });
            analysis.ExcessPatterns = CountKeywordMatches(allSymptoms, new[] { "胀痛", "刺痛", "烦躁", "便秘", "口苦" });
            analysis.ColdPatterns = CountKeywordMatches(allSymptoms, new[] { "怕冷", "恶寒", "喜温", "肢冷", "清稀" });
            analysis.HeatPatterns = CountKeywordMatches(allSymptoms, new[] { "发热", "口渴", "烦热", "面红", "黄腻" });

            // 分析主要脏腑
            analysis.HeartPatterns = CountKeywordMatches(allSymptoms, new[] { "心悸", "胸闷", "失眠", "健忘" });
            analysis.LiverPatterns = CountKeywordMatches(allSymptoms, new[] { "胁痛", "易怒", "目赤", "头晕" });
            analysis.SpleenPatterns = CountKeywordMatches(allSymptoms, new[] { "腹胀", "纳差", "便溏", "肢倦" });
            analysis.LungPatterns = CountKeywordMatches(allSymptoms, new[] { "咳嗽", "气短", "胸闷", "咽干" });
            analysis.KidneyPatterns = CountKeywordMatches(allSymptoms, new[] { "腰酸", "耳鸣", "夜尿", "遗精" });

            return analysis;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化证型诊断规则
        /// </summary>
        private void InitializeSyndromeRules()
        {
            _syndromeRules["风寒感冒"] = new SyndromeRule
            {
                RequiredSymptoms = new[] { "恶寒", "发热轻", "鼻塞" },
                TongueFeatures = new[] { "淡红", "薄白" },
                PulseFeatures = new[] { "浮紧", "浮" },
                OptionalSymptoms = new[] { "头痛", "无汗", "流清涕" }
            };

            _syndromeRules["风热感冒"] = new SyndromeRule
            {
                RequiredSymptoms = new[] { "发热重", "恶寒轻", "咽痛" },
                TongueFeatures = new[] { "红", "薄黄" },
                PulseFeatures = new[] { "浮数", "数" },
                OptionalSymptoms = new[] { "口渴", "头痛", "咳嗽" }
            };

            _syndromeRules["脾胃虚弱"] = new SyndromeRule
            {
                RequiredSymptoms = new[] { "乏力", "食少", "腹胀" },
                TongueFeatures = new[] { "淡白", "白腻" },
                PulseFeatures = new[] { "沉弱", "细弱" },
                OptionalSymptoms = new[] { "便溏", "面黄", "肢冷" }
            };

            _syndromeRules["肾虚证"] = new SyndromeRule
            {
                RequiredSymptoms = new[] { "腰酸", "乏力" },
                TongueFeatures = new[] { "淡红", "少苔" },
                PulseFeatures = new[] { "沉细", "细弱" },
                OptionalSymptoms = new[] { "耳鸣", "夜尿", "健忘" }
            };

            _syndromeRules["肝郁气滞"] = new SyndromeRule
            {
                RequiredSymptoms = new[] { "胁痛", "情志不舒" },
                TongueFeatures = new[] { "红", "薄白" },
                PulseFeatures = new[] { "弦" },
                OptionalSymptoms = new[] { "易怒", "胸闷", "善太息" }
            };
        }

        /// <summary>
        /// 计算证型匹配得分
        /// </summary>
        private double CalculateSyndromeMatchScore(TCMFourDiagnosisData data, TCMFourDiagnosisDataManager dataManager, SyndromeRule rule)
        {
            var totalScore = 0.0;
            var maxScore = 0.0;

            var allSymptoms = $"{data.Inspection} {data.Auscultation} {data.Inquiry} {data.Palpation}";

            // 检查必要症状 (权重: 0.4)
            var requiredMatches = rule.RequiredSymptoms.Count(symptom => allSymptoms.Contains(symptom));
            totalScore += (requiredMatches / (double)rule.RequiredSymptoms.Length) * 0.4;
            maxScore += 0.4;

            // 检查舌象特征 (权重: 0.25)
            var tongueMatches = rule.TongueFeatures.Count(feature => 
                dataManager.TongueBody.Contains(feature) || dataManager.TongueCoating.Contains(feature));
            if (rule.TongueFeatures.Length > 0)
            {
                totalScore += (tongueMatches / (double)rule.TongueFeatures.Length) * 0.25;
                maxScore += 0.25;
            }

            // 检查脉象特征 (权重: 0.25)
            var pulseMatches = rule.PulseFeatures.Count(feature => 
                dataManager.LeftPulse.Contains(feature) || dataManager.RightPulse.Contains(feature));
            if (rule.PulseFeatures.Length > 0)
            {
                totalScore += (pulseMatches / (double)rule.PulseFeatures.Length) * 0.25;
                maxScore += 0.25;
            }

            // 检查可选症状 (权重: 0.1)
            var optionalMatches = rule.OptionalSymptoms.Count(symptom => allSymptoms.Contains(symptom));
            if (rule.OptionalSymptoms.Length > 0)
            {
                totalScore += (optionalMatches / (double)rule.OptionalSymptoms.Length) * 0.1;
                maxScore += 0.1;
            }

            return maxScore > 0 ? totalScore / maxScore : 0;
        }

        /// <summary>
        /// 计算各诊法完整性
        /// </summary>
        private double CalculateInspectionCompleteness(TCMFourDiagnosisDataManager dataManager)
        {
            var fields = new[] { dataManager.Complexion, dataManager.Spirit, dataManager.TongueBody, dataManager.TongueCoating };
            return fields.Count(f => !string.IsNullOrWhiteSpace(f)) / (double)fields.Length;
        }

        private double CalculateAuscultationCompleteness(TCMFourDiagnosisDataManager dataManager)
        {
            var fields = new[] { dataManager.Voice, dataManager.Breath, dataManager.Cough };
            return fields.Count(f => !string.IsNullOrWhiteSpace(f)) / (double)fields.Length;
        }

        private double CalculateInquiryCompleteness(TCMFourDiagnosisDataManager dataManager)
        {
            var fields = new[] { dataManager.ChiefComplaint, dataManager.ColdHeat, dataManager.Sweat, 
                               dataManager.Appetite, dataManager.Sleep, dataManager.StoolUrine };
            return fields.Count(f => !string.IsNullOrWhiteSpace(f)) / (double)fields.Length;
        }

        private double CalculatePalpationCompleteness(TCMFourDiagnosisDataManager dataManager)
        {
            var fields = new[] { dataManager.LeftPulse, dataManager.RightPulse };
            return fields.Count(f => !string.IsNullOrWhiteSpace(f)) / (double)fields.Length;
        }

        private double CalculateDiagnosisConsistency(TCMFourDiagnosisData data, string diagnosis)
        {
            if (_syndromeRules.TryGetValue(diagnosis, out var rule))
            {
                return CalculateSyndromeMatchScore(data, new TCMFourDiagnosisDataManager(), rule);
            }
            return 0.5; // 未知诊断给予中等一致性
        }

        private string GetAllSymptomsText(TCMFourDiagnosisDataManager dataManager)
        {
            return $"{dataManager.ChiefComplaint} {dataManager.ColdHeat} {dataManager.HeadBody} " +
                   $"{dataManager.ChestAbdomen} {dataManager.Appetite} {dataManager.Sleep} {dataManager.StoolUrine}";
        }

        private int CountKeywordMatches(string text, string[] keywords)
        {
            return keywords.Count(keyword => text.Contains(keyword));
        }

        #endregion
    }

    #region 支持类

    /// <summary>
    /// 证型诊断规则
    /// </summary>
    public class SyndromeRule
    {
        public string[] RequiredSymptoms { get; set; } = Array.Empty<string>();
        public string[] TongueFeatures { get; set; } = Array.Empty<string>();
        public string[] PulseFeatures { get; set; } = Array.Empty<string>();
        public string[] OptionalSymptoms { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// 诊断一致性结果
    /// </summary>
    public class DiagnosisConsistencyResult
    {
        public double InspectionCompleteness { get; set; }
        public double AuscultationCompleteness { get; set; }
        public double InquiryCompleteness { get; set; }
        public double PalpationCompleteness { get; set; }
        public double OverallCompleteness { get; set; }
        public double DiagnosisConsistency { get; set; }
    }

    /// <summary>
    /// 症状关键词分析结果
    /// </summary>
    public class SymptomKeywordAnalysis
    {
        public int DeficiencyPatterns { get; set; }
        public int ExcessPatterns { get; set; }
        public int ColdPatterns { get; set; }
        public int HeatPatterns { get; set; }
        public int HeartPatterns { get; set; }
        public int LiverPatterns { get; set; }
        public int SpleenPatterns { get; set; }
        public int LungPatterns { get; set; }
        public int KidneyPatterns { get; set; }
    }

    #endregion
}