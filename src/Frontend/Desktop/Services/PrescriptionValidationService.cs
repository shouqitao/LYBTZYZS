using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Services.Interfaces;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 处方质量控制服务 - 基于中医理论的智能验证系统
    /// </summary>
    public class PrescriptionValidationService : IPrescriptionValidationService
    {
        private readonly ILogger<PrescriptionValidationService> _logger;

        // 中医十八反配伍禁忌
        private readonly Dictionary<string, List<string>> _eighteenAntagonisms = new()
        {
            ["乌头"] = new() { "贝母", "瓜蒌", "半夏", "白蔹", "白芨" },
            ["藜芦"] = new() { "人参", "沙参", "丹参", "玄参", "细辛", "芍药" },
            ["甘草"] = new() { "甘遂", "大戟", "海藻", "芫花" },
            ["附子"] = new() { "贝母", "瓜蒌", "半夏", "白蔹", "白芨" }
        };

        // 中医十九畏配伍禁忌
        private readonly Dictionary<string, List<string>> _nineteenFears = new()
        {
            ["硫磺"] = new() { "朴硝" },
            ["水银"] = new() { "砒霜" },
            ["狼毒"] = new() { "密陀僧" },
            ["巴豆"] = new() { "牵牛" },
            ["丁香"] = new() { "郁金" },
            ["川乌"] = new() { "犀角" },
            ["牙硝"] = new() { "三棱" },
            ["官桂"] = new() { "石脂" },
            ["人参"] = new() { "五灵脂" }
        };

        // 有毒药材及其安全剂量
        private readonly Dictionary<string, DosageRange> _toxicHerbDosages = new()
        {
            ["附子"] = new() { MinDose = 3, MaxDose = 15, TypicalDose = 6, Unit = "g" },
            ["乌头"] = new() { MinDose = 1.5m, MaxDose = 6, TypicalDose = 3, Unit = "g" },
            ["半夏"] = new() { MinDose = 3, MaxDose = 12, TypicalDose = 6, Unit = "g" },
            ["细辛"] = new() { MinDose = 1, MaxDose = 3, TypicalDose = 2, Unit = "g" },
            ["雄黄"] = new() { MinDose = 0.05m, MaxDose = 0.1m, TypicalDose = 0.05m, Unit = "g" },
            ["朱砂"] = new() { MinDose = 0.1m, MaxDose = 0.5m, TypicalDose = 0.3m, Unit = "g" },
            ["马钱子"] = new() { MinDose = 0.3m, MaxDose = 0.6m, TypicalDose = 0.3m, Unit = "g" }
        };

        // 常用药材标准剂量范围
        private readonly Dictionary<string, DosageRange> _standardDosages = new()
        {
            ["人参"] = new() { MinDose = 3, MaxDose = 15, TypicalDose = 9, Unit = "g" },
            ["黄芪"] = new() { MinDose = 9, MaxDose = 30, TypicalDose = 15, Unit = "g" },
            ["当归"] = new() { MinDose = 6, MaxDose = 15, TypicalDose = 10, Unit = "g" },
            ["白芍"] = new() { MinDose = 6, MaxDose = 15, TypicalDose = 10, Unit = "g" },
            ["川芎"] = new() { MinDose = 3, MaxDose = 10, TypicalDose = 6, Unit = "g" },
            ["熟地黄"] = new() { MinDose = 9, MaxDose = 24, TypicalDose = 15, Unit = "g" },
            ["甘草"] = new() { MinDose = 3, MaxDose = 12, TypicalDose = 6, Unit = "g" },
            ["桂枝"] = new() { MinDose = 3, MaxDose = 12, TypicalDose = 6, Unit = "g" },
            ["白术"] = new() { MinDose = 6, MaxDose = 15, TypicalDose = 10, Unit = "g" },
            ["茯苓"] = new() { MinDose = 9, MaxDose = 15, TypicalDose = 12, Unit = "g" }
        };

        // 特殊人群禁用药材
        private readonly Dictionary<SpecialPopulationType, List<string>> _contraindications = new()
        {
            [SpecialPopulationType.Pregnant] = new() 
            { 
                "巴豆", "牵牛子", "大戟", "芫花", "甘遂", "商陆", "斑蝥", "蜈蚣", 
                "水蛭", "虻虫", "莪术", "三棱", "红花", "桃仁", "牛膝", "薏苡仁",
                "附子", "肉桂", "干姜", "丁香", "小茴香"
            },
            [SpecialPopulationType.Lactating] = new() 
            { 
                "大黄", "芒硝", "番泻叶", "芦荟", "巴豆", "牵牛子",
                "麻黄", "薄荷", "人参", "西洋参"
            },
            [SpecialPopulationType.Pediatric] = new() 
            { 
                "附子", "乌头", "巴豆", "牵牛子", "大戟", "芫花", "甘遂",
                "雄黄", "朱砂", "轻粉", "红粉"
            },
            [SpecialPopulationType.Geriatric] = new() 
            { 
                "巴豆", "牵牛子", "大戟", "芫花", "甘遂", "商陆"
            }
        };

        public PrescriptionValidationService(ILogger<PrescriptionValidationService> logger)
        {
            _logger = logger;
        }

        public async Task<PrescriptionValidationResult> ValidatePrescriptionAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems, 
            PatientValidationInfo patientInfo,
            string diagnosis = "")
        {
            try
            {
                _logger.LogInformation("开始处方质量验证");

                var items = prescriptionItems.ToList();
                var result = new PrescriptionValidationResult();

                // 1. 基础数据验证
                ValidateBasicData(items, result);

                // 2. 配伍禁忌检查
                var interactionWarnings = await CheckDrugInteractionsAsync(items);
                AddInteractionWarningsToResult(interactionWarnings, result);

                // 3. 剂量合理性检查
                var dosageWarnings = await CheckDosageAsync(items, patientInfo.Age, patientInfo.Weight ?? 0);
                AddDosageWarningsToResult(dosageWarnings, result);

                // 4. 特殊人群用药检查
                var specialWarnings = await CheckSpecialPopulationSafetyAsync(items, patientInfo);
                AddSpecialWarningsToResult(specialWarnings, result);

                // 5. 处方合理性检查
                ValidatePrescriptionRationality(items, diagnosis, result);

                // 6. 生成改进建议
                var suggestions = await GetImprovementSuggestionsAsync(items, diagnosis);
                result.Suggestions = suggestions;

                // 7. 计算总体质量评分
                CalculateQualityScore(result);

                _logger.LogInformation($"处方质量验证完成，质量等级：{result.QualityLevel}，评分：{result.QualityScore}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处方质量验证过程中发生异常");
                return new PrescriptionValidationResult
                {
                    QualityLevel = PrescriptionQualityLevel.Poor,
                    QualityScore = 0,
                    Summary = "验证过程中发生异常，请检查处方数据"
                };
            }
        }

        public async Task<List<DrugInteractionWarning>> CheckDrugInteractionsAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems)
        {
            await Task.CompletedTask;

            var items = prescriptionItems.ToList();
            var warnings = new List<DrugInteractionWarning>();

            // 检查十八反
            foreach (var antagonism in _eighteenAntagonisms)
            {
                var mainHerb = items.FirstOrDefault(i => i.HerbName.Contains(antagonism.Key));
                if (mainHerb != null)
                {
                    var conflictingHerbs = items.Where(i => 
                        antagonism.Value.Any(forbidden => i.HerbName.Contains(forbidden))).ToList();

                    foreach (var conflictHerb in conflictingHerbs)
                    {
                        warnings.Add(new DrugInteractionWarning
                        {
                            InteractingHerbs = new List<string> { mainHerb.HerbName, conflictHerb.HerbName },
                            Type = InteractionType.EighteenAntagonisms,
                            Severity = InteractionSeverity.Severe,
                            Description = $"{mainHerb.HerbName}与{conflictHerb.HerbName}属十八反配伍禁忌",
                            ClinicalSignificance = "严重配伍禁忌，可能导致毒性反应或药效对抗",
                            ManagementAdvice = "立即删除其中一味药材，选择功效相似但无配伍禁忌的替代药物"
                        });
                    }
                }
            }

            // 检查十九畏
            foreach (var fear in _nineteenFears)
            {
                var mainHerb = items.FirstOrDefault(i => i.HerbName.Contains(fear.Key));
                if (mainHerb != null)
                {
                    var fearingHerbs = items.Where(i => 
                        fear.Value.Any(feared => i.HerbName.Contains(feared))).ToList();

                    foreach (var fearHerb in fearingHerbs)
                    {
                        warnings.Add(new DrugInteractionWarning
                        {
                            InteractingHerbs = new List<string> { mainHerb.HerbName, fearHerb.HerbName },
                            Type = InteractionType.NineteenFears,
                            Severity = InteractionSeverity.Moderate,
                            Description = $"{mainHerb.HerbName}与{fearHerb.HerbName}属十九畏配伍关系",
                            ClinicalSignificance = "可能影响药效或增加不良反应",
                            ManagementAdvice = "谨慎使用，如必须同用需调整剂量或加强监护"
                        });
                    }
                }
            }

            return warnings;
        }

        public async Task<List<DosageWarning>> CheckDosageAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            int patientAge,
            double patientWeight = 0)
        {
            await Task.CompletedTask;

            var items = prescriptionItems.ToList();
            var warnings = new List<DosageWarning>();

            foreach (var item in items)
            {
                // 检查有毒药材剂量
                if (_toxicHerbDosages.TryGetValue(item.HerbName, out var toxicRange))
                {
                    var currentDose = item.Quantity;
                    
                    if (currentDose > toxicRange.MaxDose)
                    {
                        warnings.Add(new DosageWarning
                        {
                            HerbName = item.HerbName,
                            CurrentDosage = currentDose,
                            RecommendedRange = toxicRange,
                            Type = DosageWarningType.ToxicOverdose,
                            Description = $"{item.HerbName}为有毒药材，当前剂量{currentDose}g超过安全上限{toxicRange.MaxDose}g",
                            RiskDescription = "超量使用有毒药材可能导致严重中毒反应",
                            AdjustmentAdvice = $"建议调整剂量至{toxicRange.TypicalDose}g，并加强用药监护"
                        });
                    }
                    else if (currentDose < toxicRange.MinDose)
                    {
                        warnings.Add(new DosageWarning
                        {
                            HerbName = item.HerbName,
                            CurrentDosage = currentDose,
                            RecommendedRange = toxicRange,
                            Type = DosageWarningType.InsufficientDose,
                            Description = $"{item.HerbName}当前剂量{currentDose}g低于有效剂量{toxicRange.MinDose}g",
                            RiskDescription = "剂量过低可能影响药效",
                            AdjustmentAdvice = $"建议调整剂量至{toxicRange.TypicalDose}g"
                        });
                    }
                }

                // 检查常用药材标准剂量
                if (_standardDosages.TryGetValue(item.HerbName, out var standardRange))
                {
                    var currentDose = item.Quantity;
                    var adjustedRange = AdjustDosageForAge(standardRange, patientAge);

                    if (currentDose > adjustedRange.MaxDose)
                    {
                        warnings.Add(new DosageWarning
                        {
                            HerbName = item.HerbName,
                            CurrentDosage = currentDose,
                            RecommendedRange = adjustedRange,
                            Type = patientAge < 18 ? DosageWarningType.PediatricDosageIssue : 
                                   patientAge > 65 ? DosageWarningType.GeriatricDosageIssue :
                                   DosageWarningType.ExcessiveDose,
                            Description = $"{item.HerbName}当前剂量{currentDose}g超过推荐上限{adjustedRange.MaxDose}g",
                            RiskDescription = "剂量过高可能增加不良反应风险",
                            AdjustmentAdvice = $"建议调整剂量至{adjustedRange.TypicalDose}g"
                        });
                    }
                    else if (currentDose < adjustedRange.MinDose)
                    {
                        warnings.Add(new DosageWarning
                        {
                            HerbName = item.HerbName,
                            CurrentDosage = currentDose,
                            RecommendedRange = adjustedRange,
                            Type = DosageWarningType.InsufficientDose,
                            Description = $"{item.HerbName}当前剂量{currentDose}g低于推荐下限{adjustedRange.MinDose}g",
                            RiskDescription = "剂量过低可能影响治疗效果",
                            AdjustmentAdvice = $"建议调整剂量至{adjustedRange.TypicalDose}g"
                        });
                    }
                }
            }

            return warnings;
        }

        public async Task<List<SpecialPopulationWarning>> CheckSpecialPopulationSafetyAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            PatientValidationInfo patientInfo)
        {
            await Task.CompletedTask;

            var items = prescriptionItems.ToList();
            var warnings = new List<SpecialPopulationWarning>();

            // 孕妇用药检查
            if (patientInfo.IsPregnant)
            {
                CheckSpecialPopulationHerbs(items, SpecialPopulationType.Pregnant, warnings);
            }

            // 哺乳期用药检查
            if (patientInfo.IsLactating)
            {
                CheckSpecialPopulationHerbs(items, SpecialPopulationType.Lactating, warnings);
            }

            // 儿童用药检查
            if (patientInfo.Age < 18)
            {
                CheckSpecialPopulationHerbs(items, SpecialPopulationType.Pediatric, warnings);
            }

            // 老年人用药检查
            if (patientInfo.Age > 65)
            {
                CheckSpecialPopulationHerbs(items, SpecialPopulationType.Geriatric, warnings);
            }

            // 肝功能不全检查
            if (patientInfo.LiverFunction.HasValue && 
                patientInfo.LiverFunction.Value != OrganFunctionStatus.Normal)
            {
                CheckHepaticImpairment(items, patientInfo.LiverFunction.Value, warnings);
            }

            // 肾功能不全检查
            if (patientInfo.KidneyFunction.HasValue && 
                patientInfo.KidneyFunction.Value != OrganFunctionStatus.Normal)
            {
                CheckRenalImpairment(items, patientInfo.KidneyFunction.Value, warnings);
            }

            return warnings;
        }

        public async Task<List<PrescriptionSuggestion>> GetImprovementSuggestionsAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            string diagnosis)
        {
            await Task.CompletedTask;

            var items = prescriptionItems.ToList();
            var suggestions = new List<PrescriptionSuggestion>();

            // 1. 方剂配伍建议
            AnalyzeFormulaComposition(items, diagnosis, suggestions);

            // 2. 剂量优化建议
            OptimizeDosages(items, suggestions);

            // 3. 功效协同建议
            AnalyzeSynergisticEffects(items, diagnosis, suggestions);

            // 4. 安全性改进建议
            ImproveSafety(items, suggestions);

            return suggestions.OrderByDescending(s => s.Priority).ToList();
        }

        #region 私有辅助方法

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

            // 检查是否有君臣佐使配伍
            AnalyzeFormulaStructure(items, diagnosis, result);
        }

        private void CalculateQualityScore(PrescriptionValidationResult result)
        {
            int baseScore = 100;
            
            // 错误每个扣20分
            baseScore -= result.Errors.Count * 20;
            
            // 警告每个扣10分
            baseScore -= result.Warnings.Count * 10;
            
            // 信息提示每个扣2分
            baseScore -= result.Infos.Count * 2;

            // 保证分数不低于0
            result.QualityScore = Math.Max(0, baseScore);

            // 根据分数确定质量等级
            result.QualityLevel = result.QualityScore switch
            {
                >= 90 => PrescriptionQualityLevel.Excellent,
                >= 80 => PrescriptionQualityLevel.Good,
                >= 70 => PrescriptionQualityLevel.Fair,
                >= 60 => PrescriptionQualityLevel.NeedsImprovement,
                _ => PrescriptionQualityLevel.Poor
            };

            // 生成总结
            result.Summary = GenerateQualitySummary(result);
        }

        private string GenerateQualitySummary(PrescriptionValidationResult result)
        {
            var summary = $"处方质量等级：{GetQualityLevelText(result.QualityLevel)}（{result.QualityScore}分）";
            
            if (result.Errors.Any())
            {
                summary += $"，发现{result.Errors.Count}个严重问题需要立即处理";
            }
            
            if (result.Warnings.Any())
            {
                summary += $"，有{result.Warnings.Count}个警告需要注意";
            }

            if (result.CanPrescribe)
            {
                summary += "，可以开具处方";
            }
            else
            {
                summary += "，请修正错误后再开具处方";
            }

            return summary;
        }

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

        private DosageRange AdjustDosageForAge(DosageRange standardRange, int age)
        {
            if (age < 18)
            {
                // 儿童剂量通常为成人剂量的0.5-0.8倍
                var factor = age < 6 ? 0.3m : age < 12 ? 0.5m : 0.7m;
                return new DosageRange
                {
                    MinDose = standardRange.MinDose * factor,
                    MaxDose = standardRange.MaxDose * factor,
                    TypicalDose = standardRange.TypicalDose * factor,
                    Unit = standardRange.Unit
                };
            }
            else if (age > 65)
            {
                // 老年人剂量通常为成人剂量的0.7-0.9倍
                var factor = 0.8m;
                return new DosageRange
                {
                    MinDose = standardRange.MinDose * factor,
                    MaxDose = standardRange.MaxDose * factor,
                    TypicalDose = standardRange.TypicalDose * factor,
                    Unit = standardRange.Unit
                };
            }

            return standardRange;
        }

        private void CheckSpecialPopulationHerbs(List<PrescriptionItemInfo> items, SpecialPopulationType populationType, List<SpecialPopulationWarning> warnings)
        {
            if (!_contraindications.TryGetValue(populationType, out var contraindicatedHerbs))
                return;

            var affectedHerbs = items.Where(item => 
                contraindicatedHerbs.Any(contraindicated => item.HerbName.Contains(contraindicated)))
                .ToList();

            if (affectedHerbs.Any())
            {
                warnings.Add(new SpecialPopulationWarning
                {
                    PopulationType = populationType,
                    AffectedHerbs = affectedHerbs.Select(h => h.HerbName).ToList(),
                    RiskLevel = RiskLevel.High,
                    RiskDescription = $"{GetPopulationTypeText(populationType)}禁用或慎用药材：{string.Join("、", affectedHerbs.Select(h => h.HerbName))}",
                    Recommendation = $"建议删除这些药材或选择安全的替代药物"
                });
            }
        }

        private void CheckHepaticImpairment(List<PrescriptionItemInfo> items, OrganFunctionStatus liverFunction, List<SpecialPopulationWarning> warnings)
        {
            var hepatotoxicHerbs = new List<string> { "何首乌", "大黄", "番泻叶", "土三七", "艾叶", "苍耳子" };
            
            var affectedHerbs = items.Where(item => 
                hepatotoxicHerbs.Any(hepatotoxic => item.HerbName.Contains(hepatotoxic)))
                .ToList();

            if (affectedHerbs.Any())
            {
                var riskLevel = liverFunction switch
                {
                    OrganFunctionStatus.SevereImpairment => RiskLevel.High,
                    OrganFunctionStatus.ModerateImpairment => RiskLevel.Medium,
                    _ => RiskLevel.Low
                };

                warnings.Add(new SpecialPopulationWarning
                {
                    PopulationType = SpecialPopulationType.HepaticImpairment,
                    AffectedHerbs = affectedHerbs.Select(h => h.HerbName).ToList(),
                    RiskLevel = riskLevel,
                    RiskDescription = $"肝功能不全患者使用{string.Join("、", affectedHerbs.Select(h => h.HerbName))}可能加重肝损害",
                    Recommendation = riskLevel == RiskLevel.High ? "建议避免使用" : "建议减量使用并加强肝功能监测"
                });
            }
        }

        private void CheckRenalImpairment(List<PrescriptionItemInfo> items, OrganFunctionStatus kidneyFunction, List<SpecialPopulationWarning> warnings)
        {
            var nephrotoxicHerbs = new List<string> { "马兜铃", "木通", "细辛", "厚朴", "朱砂", "雄黄" };
            
            var affectedHerbs = items.Where(item => 
                nephrotoxicHerbs.Any(nephrotoxic => item.HerbName.Contains(nephrotoxic)))
                .ToList();

            if (affectedHerbs.Any())
            {
                var riskLevel = kidneyFunction switch
                {
                    OrganFunctionStatus.SevereImpairment => RiskLevel.High,
                    OrganFunctionStatus.ModerateImpairment => RiskLevel.Medium,
                    _ => RiskLevel.Low
                };

                warnings.Add(new SpecialPopulationWarning
                {
                    PopulationType = SpecialPopulationType.RenalImpairment,
                    AffectedHerbs = affectedHerbs.Select(h => h.HerbName).ToList(),
                    RiskLevel = riskLevel,
                    RiskDescription = $"肾功能不全患者使用{string.Join("、", affectedHerbs.Select(h => h.HerbName))}可能加重肾损害",
                    Recommendation = riskLevel == RiskLevel.High ? "建议避免使用" : "建议减量使用并加强肾功能监测"
                });
            }
        }

        private string GetPopulationTypeText(SpecialPopulationType type)
        {
            return type switch
            {
                SpecialPopulationType.Pregnant => "孕妇",
                SpecialPopulationType.Lactating => "哺乳期妇女",
                SpecialPopulationType.Pediatric => "儿童",
                SpecialPopulationType.Geriatric => "老年人",
                SpecialPopulationType.HepaticImpairment => "肝功能不全患者",
                SpecialPopulationType.RenalImpairment => "肾功能不全患者",
                SpecialPopulationType.CardiacImpairment => "心功能不全患者",
                _ => "特殊人群"
            };
        }

        private void AnalyzeFormulaComposition(List<PrescriptionItemInfo> items, string diagnosis, List<PrescriptionSuggestion> suggestions)
        {
            // 分析处方是否有明显的方剂结构
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

        private void OptimizeDosages(List<PrescriptionItemInfo> items, List<PrescriptionSuggestion> suggestions)
        {
            foreach (var item in items)
            {
                if (_standardDosages.TryGetValue(item.HerbName, out var standardRange))
                {
                    if (item.Quantity != standardRange.TypicalDose)
                    {
                        suggestions.Add(new PrescriptionSuggestion
                        {
                            Type = SuggestionType.AdjustDosage,
                            Priority = SuggestionPriority.Low,
                            Content = $"建议{item.HerbName}调整为{standardRange.TypicalDose}g",
                            Rationale = $"当前剂量{item.Quantity}g，常用剂量为{standardRange.TypicalDose}g",
                            ExpectedOutcome = "优化药效，减少不良反应"
                        });
                    }
                }
            }
        }

        private void AnalyzeSynergisticEffects(List<PrescriptionItemInfo> items, string diagnosis, List<PrescriptionSuggestion> suggestions)
        {
            // 分析药对配伍
            var herbNames = items.Select(i => i.HerbName).ToList();

            // 经典药对建议
            var classicPairs = new Dictionary<string, string>
            {
                ["当归"] = "川芎",
                ["白芍"] = "甘草",
                ["黄芪"] = "白术",
                ["人参"] = "白术",
                ["附子"] = "干姜"
            };

            foreach (var pair in classicPairs)
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

        private void ImproveSafety(List<PrescriptionItemInfo> items, List<PrescriptionSuggestion> suggestions)
        {
            var herbNames = items.Select(i => i.HerbName).ToList();

            // 检查是否有保护脏器的药物
            var hasLiverProtection = herbNames.Any(h => 
                h.Contains("柴胡") || h.Contains("郁金") || h.Contains("茵陈"));
            
            var hasKidneyProtection = herbNames.Any(h => 
                h.Contains("山茱萸") || h.Contains("熟地") || h.Contains("枸杞"));

            if (items.Any(i => _toxicHerbDosages.ContainsKey(i.HerbName)))
            {
                if (!hasLiverProtection)
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
        }

        private void AnalyzeFormulaStructure(List<PrescriptionItemInfo> items, string diagnosis, PrescriptionValidationResult result)
        {
            // 简单的方剂结构分析
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

        #endregion
    }
}