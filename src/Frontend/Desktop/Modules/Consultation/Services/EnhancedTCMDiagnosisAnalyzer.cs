using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Modules.Consultation.ViewModels;

namespace LYBT.WPF.Client.Modules.Consultation.Services
{
    /// <summary>
    /// 增强版中医诊断分析器 - 基于传统中医理论的智能分析系统
    /// </summary>
    public class EnhancedTCMDiagnosisAnalyzer : ITCMDiagnosisAnalyzer
    {
        private readonly Dictionary<string, TCMSyndromePattern> _syndromePatterns;
        private readonly Dictionary<string, List<string>> _symptomKeywords;

        public EnhancedTCMDiagnosisAnalyzer()
        {
            _syndromePatterns = InitializeSyndromePatterns();
            _symptomKeywords = InitializeSymptomKeywords();
        }

        public async Task<List<string>> AnalyzeSyndromeAsync(TCMFourDiagnosisData data)
        {
            await Task.Delay(200); // 模拟分析过程

            var syndromeScores = new Dictionary<string, double>();

            // 分析每个证型的匹配度
            foreach (var pattern in _syndromePatterns)
            {
                var score = CalculateSyndromeScore(pattern.Value, data);
                if (score > 0.3) // 阈值筛选
                {
                    syndromeScores[pattern.Key] = score;
                }
            }

            // 按匹配度排序返回前5个
            return syndromeScores
                .OrderByDescending(x => x.Value)
                .Take(5)
                .Select(x => $"{x.Key} ({x.Value:P0})")
                .ToList();
        }

        public async Task<List<string>> RecommendTreatmentAsync(string syndrome)
        {
            await Task.Delay(100);

            // 移除匹配度百分比，提取纯证型名
            var pureSyndrome = syndrome.Split('(')[0].Trim();

            if (_syndromePatterns.TryGetValue(pureSyndrome, out var pattern))
            {
                return pattern.TreatmentPrinciples.ToList();
            }

            return new List<string> { "辨证论治" };
        }

        /// <summary>
        /// 计算证型匹配分数
        /// </summary>
        private double CalculateSyndromeScore(TCMSyndromePattern pattern, TCMFourDiagnosisData data)
        {
            double totalScore = 0;
            int totalCriteria = 0;

            // 望诊评分
            totalScore += ScoreSymptoms(pattern.InspectionSymptoms, data.Inspection);
            totalCriteria += pattern.InspectionSymptoms.Count;

            // 闻诊评分
            totalScore += ScoreSymptoms(pattern.AuscultationSymptoms, data.Auscultation);
            totalCriteria += pattern.AuscultationSymptoms.Count;

            // 问诊评分
            totalScore += ScoreSymptoms(pattern.InquirySymptoms, data.Inquiry);
            totalCriteria += pattern.InquirySymptoms.Count;

            // 切诊评分
            totalScore += ScoreSymptoms(pattern.PalpationSymptoms, data.Palpation);
            totalCriteria += pattern.PalpationSymptoms.Count;

            // 舌诊评分
            totalScore += ScoreSymptoms(pattern.TongueSymptoms, data.TongueInspection);
            totalCriteria += pattern.TongueSymptoms.Count;

            // 脉诊评分
            totalScore += ScoreSymptoms(pattern.PulseSymptoms, data.PulseCondition);
            totalCriteria += pattern.PulseSymptoms.Count;

            return totalCriteria > 0 ? totalScore / totalCriteria : 0;
        }

        /// <summary>
        /// 症状匹配评分
        /// </summary>
        private double ScoreSymptoms(List<string> patternSymptoms, string actualData)
        {
            if (string.IsNullOrWhiteSpace(actualData) || !patternSymptoms.Any())
                return 0;

            int matches = 0;
            foreach (var symptom in patternSymptoms)
            {
                if (actualData.Contains(symptom))
                {
                    matches++;
                }
            }

            return (double)matches / patternSymptoms.Count;
        }

        /// <summary>
        /// 初始化证型模式库
        /// </summary>
        private Dictionary<string, TCMSyndromePattern> InitializeSyndromePatterns()
        {
            return new Dictionary<string, TCMSyndromePattern>
            {
                ["风寒感冒"] = new TCMSyndromePattern
                {
                    InspectionSymptoms = new List<string> { "面色苍白", "精神疲倦", "鼻塞" },
                    AuscultationSymptoms = new List<string> { "声音低微", "咳嗽无痰" },
                    InquirySymptoms = new List<string> { "恶寒重", "发热轻", "头痛", "无汗", "鼻流清涕" },
                    PalpationSymptoms = new List<string> { "脉浮", "脉紧", "脉缓" },
                    TongueSymptoms = new List<string> { "舌淡红", "苔薄白" },
                    PulseSymptoms = new List<string> { "浮", "紧", "缓" },
                    TreatmentPrinciples = new List<string> { "辛温解表", "宣肺散寒" }
                },

                ["风热感冒"] = new TCMSyndromePattern
                {
                    InspectionSymptoms = new List<string> { "面色潮红", "目赤" },
                    AuscultationSymptoms = new List<string> { "声音嘶哑", "咳嗽痰黄" },
                    InquirySymptoms = new List<string> { "发热重", "恶寒轻", "头痛", "咽痛", "口渴", "有汗" },
                    PalpationSymptoms = new List<string> { "脉浮", "脉数" },
                    TongueSymptoms = new List<string> { "舌红", "苔薄黄" },
                    PulseSymptoms = new List<string> { "浮", "数" },
                    TreatmentPrinciples = new List<string> { "辛凉解表", "清热宣肺" }
                },

                ["脾胃虚寒"] = new TCMSyndromePattern
                {
                    InspectionSymptoms = new List<string> { "面色萎黄", "精神疲倦", "形体消瘦" },
                    AuscultationSymptoms = new List<string> { "声音低微", "少言" },
                    InquirySymptoms = new List<string> { "食欲不振", "腹胀", "大便溏薄", "四肢不温", "喜温喜按" },
                    PalpationSymptoms = new List<string> { "脉虚", "脉弱", "脉迟" },
                    TongueSymptoms = new List<string> { "舌淡", "苔白" },
                    PulseSymptoms = new List<string> { "虚", "弱", "迟" },
                    TreatmentPrinciples = new List<string> { "健脾益胃", "温中散寒" }
                },

                ["肝郁气滞"] = new TCMSyndromePattern
                {
                    InspectionSymptoms = new List<string> { "面色青暗", "情志抑郁" },
                    AuscultationSymptoms = new List<string> { "叹息", "烦躁" },
                    InquirySymptoms = new List<string> { "胸胁胀痛", "善太息", "情志不舒", "月经不调", "乳房胀痛" },
                    PalpationSymptoms = new List<string> { "脉弦", "脉涩" },
                    TongueSymptoms = new List<string> { "舌正常", "苔薄白" },
                    PulseSymptoms = new List<string> { "弦", "涩" },
                    TreatmentPrinciples = new List<string> { "疏肝解郁", "理气和胃" }
                },

                ["血瘀证"] = new TCMSyndromePattern
                {
                    InspectionSymptoms = new List<string> { "面色晦暗", "口唇紫暗", "肌肤甲错" },
                    AuscultationSymptoms = new List<string> { },
                    InquirySymptoms = new List<string> { "疼痛固定", "痛如针刺", "夜间痛甚", "月经有血块" },
                    PalpationSymptoms = new List<string> { "脉涩", "脉细" },
                    TongueSymptoms = new List<string> { "舌紫暗", "有瘀斑", "舌下脉络曲张" },
                    PulseSymptoms = new List<string> { "涩", "细", "弱" },
                    TreatmentPrinciples = new List<string> { "活血化瘀", "理气止痛" }
                },

                ["肾阳虚"] = new TCMSyndromePattern
                {
                    InspectionSymptoms = new List<string> { "面色苍白", "精神萎靡", "形体畏寒" },
                    AuscultationSymptoms = new List<string> { "声音低微" },
                    InquirySymptoms = new List<string> { "腰膝酸软", "畏寒肢冷", "阳痿", "早泄", "小便清长", "夜尿频" },
                    PalpationSymptoms = new List<string> { "脉沉", "脉弱", "脉迟" },
                    TongueSymptoms = new List<string> { "舌淡", "苔白" },
                    PulseSymptoms = new List<string> { "沉", "弱", "迟" },
                    TreatmentPrinciples = new List<string> { "温补肾阳", "填精益髓" }
                },

                ["肾阴虚"] = new TCMSyndromePattern
                {
                    InspectionSymptoms = new List<string> { "面色潮红", "形体消瘦" },
                    AuscultationSymptoms = new List<string> { },
                    InquirySymptoms = new List<string> { "腰膝酸软", "五心烦热", "盗汗", "遗精", "月经量少", "头晕耳鸣" },
                    PalpationSymptoms = new List<string> { "脉细", "脉数" },
                    TongueSymptoms = new List<string> { "舌红", "少苔" },
                    PulseSymptoms = new List<string> { "细", "数" },
                    TreatmentPrinciples = new List<string> { "滋阴补肾", "清热降火" }
                }
            };
        }

        /// <summary>
        /// 初始化症状关键词库
        /// </summary>
        private Dictionary<string, List<string>> InitializeSymptomKeywords()
        {
            return new Dictionary<string, List<string>>
            {
                ["寒象"] = new List<string> { "恶寒", "畏寒", "四肢不温", "喜温", "得温则舒" },
                ["热象"] = new List<string> { "发热", "烦热", "五心烦热", "口渴", "喜冷饮" },
                ["虚象"] = new List<string> { "疲倦", "乏力", "气短", "声音低微", "精神萎靡" },
                ["实象"] = new List<string> { "烦躁", "声高", "脉洪", "便秘", "小便短黄" },
                ["痰象"] = new List<string> { "痰多", "胸闷", "恶心", "苔厚腻" },
                ["瘀象"] = new List<string> { "疼痛固定", "痛如针刺", "面色晦暗", "舌紫暗" }
            };
        }
    }

    /// <summary>
    /// 中医证型模式定义
    /// </summary>
    public class TCMSyndromePattern
    {
        public List<string> InspectionSymptoms { get; set; } = new();
        public List<string> AuscultationSymptoms { get; set; } = new();
        public List<string> InquirySymptoms { get; set; } = new();
        public List<string> PalpationSymptoms { get; set; } = new();
        public List<string> TongueSymptoms { get; set; } = new();
        public List<string> PulseSymptoms { get; set; } = new();
        public List<string> TreatmentPrinciples { get; set; } = new();
    }
}