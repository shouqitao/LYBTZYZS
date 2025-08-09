using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Services.Interfaces;

namespace LYBT.WPF.Client.Services.Validation
{
    /// <summary>
    /// 处方改进建议生成器 - UltraThink重构专门组件
    /// 专门负责分析处方并生成改进建议，包括方剂配伍、剂量优化、功效协同等建议
    /// </summary>
    public class PrescriptionSuggestionGenerator
    {
        private readonly ILogger<PrescriptionSuggestionGenerator> _logger;
        private readonly PrescriptionDosageValidator _dosageValidator;

        #region 中医配伍数据

        // 经典药对配伍
        private readonly Dictionary<string, string> _classicPairs = new()
        {
            ["当归"] = "川芎",
            ["白芍"] = "甘草",
            ["黄芪"] = "白术",
            ["人参"] = "白术",
            ["附子"] = "干姜",
            ["桂枝"] = "白芍",
            ["麻黄"] = "桂枝",
            ["柴胡"] = "黄芩"
        };

        // 常用方剂核心药物组合
        private readonly Dictionary<string, List<string>> _classicFormulas = new()
        {
            ["四君子汤"] = new() { "人参", "白术", "茯苓", "甘草" },
            ["四物汤"] = new() { "当归", "川芎", "白芍", "熟地" },
            ["逍遥散"] = new() { "柴胡", "当归", "白芍", "白术", "茯苓", "薄荷", "生姜", "甘草" },
            ["补中益气汤"] = new() { "黄芪", "人参", "白术", "甘草", "当归", "陈皮", "升麻", "柴胡" }
        };

        // 脏器保护药物
        private readonly Dictionary<string, List<string>> _organProtectiveHerbs = new()
        {
            ["肝脏"] = new() { "柴胡", "郁金", "茵陈", "丹参", "赤芍" },
            ["脾胃"] = new() { "白术", "茯苓", "陈皮", "山药", "党参" },
            ["肾脏"] = new() { "山茱萸", "熟地", "枸杞", "菟丝子", "杜仲" },
            ["心脏"] = new() { "丹参", "远志", "茯神", "酸枣仁", "龙骨" }
        };

        #endregion

        #region 构造函数

        public PrescriptionSuggestionGenerator(
            ILogger<PrescriptionSuggestionGenerator> logger,
            PrescriptionDosageValidator dosageValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dosageValidator = dosageValidator ?? throw new ArgumentNullException(nameof(dosageValidator));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 生成处方改进建议
        /// </summary>
        public async Task<List<PrescriptionSuggestion>> GenerateImprovementSuggestionsAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            string diagnosis = "")
        {
            try
            {
                await Task.CompletedTask;
                var items = prescriptionItems.ToList();
                var suggestions = new List<PrescriptionSuggestion>();

                _logger.LogInformation("开始生成处方改进建议，药材数量: {Count}，诊断: {Diagnosis}", 
                    items.Count, diagnosis);

                // 1. 方剂配伍建议
                GenerateFormulaCompositionSuggestions(items, diagnosis, suggestions);

                // 2. 剂量优化建议
                GenerateDosageOptimizationSuggestions(items, suggestions);

                // 3. 功效协同建议
                GenerateSynergisticEffectSuggestions(items, diagnosis, suggestions);

                // 4. 安全性改进建议
                GenerateSafetyImprovementSuggestions(items, suggestions);

                // 5. 经典方剂匹配建议
                GenerateClassicFormulaSuggestions(items, diagnosis, suggestions);

                var sortedSuggestions = suggestions.OrderByDescending(s => s.Priority).ToList();
                _logger.LogInformation("处方改进建议生成完成，共{Count}条建议", sortedSuggestions.Count);

                return sortedSuggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成处方改进建议时发生异常");
                return new List<PrescriptionSuggestion>();
            }
        }

        /// <summary>
        /// 分析处方结构并给出结构化建议
        /// </summary>
        public PrescriptionStructureAnalysis AnalyzePrescriptionStructure(
            IEnumerable<PrescriptionItemInfo> prescriptionItems)
        {
            var items = prescriptionItems.ToList();
            var analysis = new PrescriptionStructureAnalysis();

            // 分析药味数量
            analysis.HerbCount = items.Count;
            analysis.IsOptimalCount = items.Count >= 4 && items.Count <= 15;

            // 分析是否有君臣佐使结构
            analysis.HasMonarchHerb = HasMonarchHerb(items);
            analysis.HasMinisterHerbs = HasMinisterHerbs(items);
            analysis.HasAssistantHerbs = HasAssistantHerbs(items);
            analysis.HasEnvoyHerb = HasEnvoyHerb(items);

            // 分析经典配伍
            analysis.ClassicPairings = IdentifyClassicPairings(items);
            analysis.PotentialFormulas = IdentifyPotentialFormulas(items);

            return analysis;
        }

        /// <summary>
        /// 根据诊断生成针对性建议
        /// </summary>
        public List<PrescriptionSuggestion> GenerateDiagnosisSpecificSuggestions(
            IEnumerable<PrescriptionItemInfo> prescriptionItems, 
            string diagnosis)
        {
            var items = prescriptionItems.ToList();
            var suggestions = new List<PrescriptionSuggestion>();
            var herbNames = items.Select(i => i.HerbName).ToList();

            // 根据不同病症给出建议
            if (diagnosis.Contains("脾虚") || diagnosis.Contains("胃虚"))
            {
                GenerateSplenStomachSuggestions(herbNames, suggestions);
            }

            if (diagnosis.Contains("肾虚"))
            {
                GenerateKidneySuggestions(herbNames, suggestions);
            }

            if (diagnosis.Contains("肝郁"))
            {
                GenerateLiverQiSuggestions(herbNames, suggestions);
            }

            if (diagnosis.Contains("血瘀"))
            {
                GenerateBloodStasisSuggestions(herbNames, suggestions);
            }

            return suggestions;
        }

        #endregion

        #region 私有方法 - 建议生成

        /// <summary>
        /// 生成方剂配伍建议
        /// </summary>
        private void GenerateFormulaCompositionSuggestions(List<PrescriptionItemInfo> items, 
            string diagnosis, List<PrescriptionSuggestion> suggestions)
        {
            var herbNames = items.Select(i => i.HerbName).ToList();

            // 检查是否缺少调和药
            if (!herbNames.Any(h => h.Contains("甘草")) && items.Count > 5)
            {
                suggestions.Add(new PrescriptionSuggestion
                {
                    Type = SuggestionType.AddHerb,
                    Priority = SuggestionPriority.Medium,
                    Content = "考虑添加甘草",
                    Rationale = "方中药味较多，缺少调和诸药的甘草",
                    ExpectedOutcome = "调和诸药，减少药物间的冲突"
                });
            }

            // 检查是否有健脾护胃药
            if (diagnosis.Contains("脾虚") || diagnosis.Contains("胃虚"))
            {
                var hasStomachProtection = herbNames.Any(h => 
                    h.Contains("白术") || h.Contains("茯苓") || h.Contains("陈皮") || h.Contains("山药"));
                
                if (!hasStomachProtection)
                {
                    suggestions.Add(new PrescriptionSuggestion
                    {
                        Type = SuggestionType.AddHerb,
                        Priority = SuggestionPriority.High,
                        Content = "建议添加健脾和胃药物如白术、茯苓或陈皮",
                        Rationale = "诊断提示脾胃虚弱，需要健脾和胃药物",
                        ExpectedOutcome = "改善脾胃功能，增强消化吸收"
                    });
                }
            }
        }

        /// <summary>
        /// 生成剂量优化建议
        /// </summary>
        private void GenerateDosageOptimizationSuggestions(List<PrescriptionItemInfo> items, 
            List<PrescriptionSuggestion> suggestions)
        {
            foreach (var item in items)
            {
                var recommendedRange = _dosageValidator.GetRecommendedDosage(item.HerbName);
                if (recommendedRange != null && item.Quantity != recommendedRange.TypicalDose)
                {
                    suggestions.Add(new PrescriptionSuggestion
                    {
                        Type = SuggestionType.AdjustDosage,
                        Priority = SuggestionPriority.Low,
                        Content = $"建议{item.HerbName}调整为{recommendedRange.TypicalDose}g",
                        Rationale = $"当前剂量{item.Quantity}g，常用剂量为{recommendedRange.TypicalDose}g",
                        ExpectedOutcome = "优化药效，减少不良反应"
                    });
                }
            }
        }

        /// <summary>
        /// 生成功效协同建议
        /// </summary>
        private void GenerateSynergisticEffectSuggestions(List<PrescriptionItemInfo> items, 
            string diagnosis, List<PrescriptionSuggestion> suggestions)
        {
            var herbNames = items.Select(i => i.HerbName).ToList();

            foreach (var pair in _classicPairs)
            {
                if (herbNames.Contains(pair.Key) && !herbNames.Contains(pair.Value))
                {
                    suggestions.Add(new PrescriptionSuggestion
                    {
                        Type = SuggestionType.AddHerb,
                        Priority = SuggestionPriority.Medium,
                        Content = $"考虑添加{pair.Value}与{pair.Key}配伍",
                        Rationale = $"{pair.Key}与{pair.Value}为经典药对，配伍使用效果更佳",
                        ExpectedOutcome = "增强药效的协同作用"
                    });
                }
            }
        }

        /// <summary>
        /// 生成安全性改进建议
        /// </summary>
        private void GenerateSafetyImprovementSuggestions(List<PrescriptionItemInfo> items, 
            List<PrescriptionSuggestion> suggestions)
        {
            var herbNames = items.Select(i => i.HerbName).ToList();

            // 检查是否有保护脏器的药物
            var hasLiverProtection = herbNames.Any(h => 
                _organProtectiveHerbs["肝脏"].Any(protectiveHerb => h.Contains(protectiveHerb)));

            if (items.Any(i => _dosageValidator.IsToxicHerb(i.HerbName)) && !hasLiverProtection)
            {
                suggestions.Add(new PrescriptionSuggestion
                {
                    Type = SuggestionType.AddHerb,
                    Priority = SuggestionPriority.High,
                    Content = "建议添加护肝药物如柴胡或郁金",
                    Rationale = "方中含有可能影响肝功能的药物",
                    ExpectedOutcome = "减少对肝脏的不良影响"
                });
            }
        }

        /// <summary>
        /// 生成经典方剂匹配建议
        /// </summary>
        private void GenerateClassicFormulaSuggestions(List<PrescriptionItemInfo> items, 
            string diagnosis, List<PrescriptionSuggestion> suggestions)
        {
            var herbNames = items.Select(i => i.HerbName).ToList();

            foreach (var formula in _classicFormulas)
            {
                var matchingHerbs = formula.Value.Where(herb => 
                    herbNames.Any(h => h.Contains(herb))).ToList();

                if (matchingHerbs.Count >= formula.Value.Count / 2)
                {
                    var missingHerbs = formula.Value.Where(herb => 
                        !herbNames.Any(h => h.Contains(herb))).ToList();

                    if (missingHerbs.Any())
                    {
                        suggestions.Add(new PrescriptionSuggestion
                        {
                            Type = SuggestionType.OptimizeCombination,
                            Priority = SuggestionPriority.Medium,
                            Content = $"考虑参考{formula.Key}，可添加{string.Join("、", missingHerbs)}",
                            Rationale = $"当前处方与{formula.Key}有{matchingHerbs.Count}味药相同",
                            ExpectedOutcome = "完善方剂结构，增强整体疗效"
                        });
                    }
                }
            }
        }

        #endregion

        #region 私有方法 - 诊断特异性建议

        /// <summary>
        /// 生成脾胃虚弱建议
        /// </summary>
        private void GenerateSplenStomachSuggestions(List<string> herbNames, List<PrescriptionSuggestion> suggestions)
        {
            var splenStomachHerbs = _organProtectiveHerbs["脾胃"];
            var hasSplenSupport = herbNames.Any(h => splenStomachHerbs.Any(supportHerb => h.Contains(supportHerb)));

            if (!hasSplenSupport)
            {
                suggestions.Add(new PrescriptionSuggestion
                {
                    Type = SuggestionType.AddHerb,
                    Priority = SuggestionPriority.High,
                    Content = "建议添加健脾药如白术、党参或山药",
                    Rationale = "脾虚证型需要健脾益气药物",
                    ExpectedOutcome = "改善脾胃功能，增强运化能力"
                });
            }
        }

        /// <summary>
        /// 生成肾虚建议
        /// </summary>
        private void GenerateKidneySuggestions(List<string> herbNames, List<PrescriptionSuggestion> suggestions)
        {
            var kidneyHerbs = _organProtectiveHerbs["肾脏"];
            var hasKidneySupport = herbNames.Any(h => kidneyHerbs.Any(supportHerb => h.Contains(supportHerb)));

            if (!hasKidneySupport)
            {
                suggestions.Add(new PrescriptionSuggestion
                {
                    Type = SuggestionType.AddHerb,
                    Priority = SuggestionPriority.High,
                    Content = "建议添加补肾药如熟地、山茱萸或枸杞",
                    Rationale = "肾虚证型需要补肾药物",
                    ExpectedOutcome = "补肾填精，改善肾功能"
                });
            }
        }

        /// <summary>
        /// 生成肝郁建议
        /// </summary>
        private void GenerateLiverQiSuggestions(List<string> herbNames, List<PrescriptionSuggestion> suggestions)
        {
            var hasLiverQiRegulation = herbNames.Any(h => h.Contains("柴胡") || h.Contains("香附") || h.Contains("郁金"));

            if (!hasLiverQiRegulation)
            {
                suggestions.Add(new PrescriptionSuggestion
                {
                    Type = SuggestionType.AddHerb,
                    Priority = SuggestionPriority.High,
                    Content = "建议添加疏肝理气药如柴胡、香附或郁金",
                    Rationale = "肝郁证型需要疏肝解郁药物",
                    ExpectedOutcome = "疏肝理气，改善情志症状"
                });
            }
        }

        /// <summary>
        /// 生成血瘀建议
        /// </summary>
        private void GenerateBloodStasisSuggestions(List<string> herbNames, List<PrescriptionSuggestion> suggestions)
        {
            var hasBloodActivation = herbNames.Any(h => 
                h.Contains("丹参") || h.Contains("红花") || h.Contains("桃仁") || h.Contains("川芎"));

            if (!hasBloodActivation)
            {
                suggestions.Add(new PrescriptionSuggestion
                {
                    Type = SuggestionType.AddHerb,
                    Priority = SuggestionPriority.High,
                    Content = "建议添加活血化瘀药如丹参、红花或川芎",
                    Rationale = "血瘀证型需要活血化瘀药物",
                    ExpectedOutcome = "活血化瘀，改善血液循环"
                });
            }
        }

        #endregion

        #region 私有方法 - 结构分析

        private bool HasMonarchHerb(List<PrescriptionItemInfo> items)
        {
            return items.Any(i => i.Quantity >= 15); // 君药通常剂量较大
        }

        private bool HasMinisterHerbs(List<PrescriptionItemInfo> items)
        {
            return items.Count(i => i.Quantity >= 9 && i.Quantity < 15) >= 1; // 臣药
        }

        private bool HasAssistantHerbs(List<PrescriptionItemInfo> items)
        {
            return items.Count(i => i.Quantity >= 6 && i.Quantity < 9) >= 1; // 佐药
        }

        private bool HasEnvoyHerb(List<PrescriptionItemInfo> items)
        {
            return items.Any(i => i.HerbName.Contains("甘草")); // 使药通常是甘草
        }

        private List<string> IdentifyClassicPairings(List<PrescriptionItemInfo> items)
        {
            var herbNames = items.Select(i => i.HerbName).ToList();
            var pairings = new List<string>();

            foreach (var pair in _classicPairs)
            {
                if (herbNames.Contains(pair.Key) && herbNames.Contains(pair.Value))
                {
                    pairings.Add($"{pair.Key}-{pair.Value}");
                }
            }

            return pairings;
        }

        private List<string> IdentifyPotentialFormulas(List<PrescriptionItemInfo> items)
        {
            var herbNames = items.Select(i => i.HerbName).ToList();
            var potentialFormulas = new List<string>();

            foreach (var formula in _classicFormulas)
            {
                var matchCount = formula.Value.Count(herb => 
                    herbNames.Any(h => h.Contains(herb)));

                if (matchCount >= formula.Value.Count * 0.6) // 60%以上匹配
                {
                    potentialFormulas.Add(formula.Key);
                }
            }

            return potentialFormulas;
        }

        #endregion
    }

    /// <summary>
    /// 处方结构分析结果
    /// </summary>
    public class PrescriptionStructureAnalysis
    {
        public int HerbCount { get; set; }
        public bool IsOptimalCount { get; set; }
        public bool HasMonarchHerb { get; set; }
        public bool HasMinisterHerbs { get; set; }
        public bool HasAssistantHerbs { get; set; }
        public bool HasEnvoyHerb { get; set; }
        public List<string> ClassicPairings { get; set; } = new();
        public List<string> PotentialFormulas { get; set; } = new();
    }
}