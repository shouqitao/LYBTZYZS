using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Domain.Aggregates.HerbAggregate;
using LYBT.Domain.Aggregates.FormulaAggregate;
using LYBT.Domain.Common;
using LYBT.Domain.Exceptions;
using LYBT.Domain.SeedWork;

namespace LYBT.Domain.Services
{
    /// <summary>
    /// 处方领域服务 - 处理跨聚合的处方业务逻辑
    /// 
    /// 职责：
    /// 1. 配伍禁忌检查（十八反、十九畏）
    /// 2. 剂量合理性验证
    /// 3. 处方组成优化
    /// 4. 君臣佐使配伍分析
    /// </summary>
    public class PrescriptionDomainService : IDomainService
    {
        private readonly IRepository<Herb> _herbRepository;
        private readonly IRepository<Formula> _formulaRepository;

        public PrescriptionDomainService(
            IRepository<Herb> herbRepository,
            IRepository<Formula> formulaRepository)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
        }

        #region 配伍禁忌检查

        /// <summary>
        /// 检查处方中的配伍禁忌
        /// </summary>
        public async Task<IncompatibilityCheckResult> CheckIncompatibilities(
            List<PrescriptionHerbItem> prescriptionItems)
        {
            if (prescriptionItems == null || !prescriptionItems.Any())
                throw new PrescriptionDomainException("处方项目不能为空");

            var result = new IncompatibilityCheckResult();
            var herbIds = prescriptionItems.Select(p => p.HerbId).Distinct().ToList();
            var herbs = await _herbRepository.GetByIdsAsync(herbIds);

            // 检查每对药材的配伍关系
            for (int i = 0; i < herbs.Count - 1; i++)
            {
                for (int j = i + 1; j < herbs.Count; j++)
                {
                    var herb1 = herbs[i];
                    var herb2 = herbs[j];

                    // 检查十八反（绝对禁忌）
                    if (!herb1.IsCompatibleWith(herb2.Id))
                    {
                        result.AddIncompatibility(new IncompatibilityInfo
                        {
                            Herb1Id = herb1.Id,
                            Herb1Name = herb1.Name,
                            Herb2Id = herb2.Id,
                            Herb2Name = herb2.Name,
                            Type = IncompatibilityType.Eighteen,
                            Severity = IncompatibilitySeverity.Absolute,
                            Description = $"{herb1.Name}与{herb2.Name}存在十八反配伍禁忌"
                        });
                    }

                    // 检查十九畏（相对禁忌）
                    if (herb1.RequiresCautionWith(herb2.Id))
                    {
                        result.AddCaution(new IncompatibilityInfo
                        {
                            Herb1Id = herb1.Id,
                            Herb1Name = herb1.Name,
                            Herb2Id = herb2.Id,
                            Herb2Name = herb2.Name,
                            Type = IncompatibilityType.Nineteen,
                            Severity = IncompatibilitySeverity.Caution,
                            Description = $"{herb1.Name}与{herb2.Name}存在十九畏配伍慎用"
                        });
                    }
                }
            }

            // 检查特殊配伍规则
            CheckSpecialCombinations(herbs, result);

            return result;
        }

        /// <summary>
        /// 检查特殊配伍规则
        /// </summary>
        private void CheckSpecialCombinations(List<Herb> herbs, IncompatibilityCheckResult result)
        {
            // 实现特殊配伍规则检查
            // 例如：某些药材的特定组合需要特别注意

            // 检查是否有孕妇禁用药
            var pregnancyContraindicated = herbs.Where(h => 
                h.Contraindications.Any(c => c.Contains("孕妇")))
                .ToList();

            if (pregnancyContraindicated.Any())
            {
                result.AddWarning(new CompatibilityWarning
                {
                    Type = WarningType.PregnancyContraindication,
                    Message = $"处方中包含孕妇禁用药材：{string.Join("、", pregnancyContraindicated.Select(h => h.Name))}",
                    HerbIds = pregnancyContraindicated.Select(h => h.Id).ToList()
                });
            }

            // 检查是否有儿童慎用药
            var childrenCaution = herbs.Where(h => 
                h.Contraindications.Any(c => c.Contains("儿童")))
                .ToList();

            if (childrenCaution.Any())
            {
                result.AddWarning(new CompatibilityWarning
                {
                    Type = WarningType.ChildrenCaution,
                    Message = $"处方中包含儿童慎用药材：{string.Join("、", childrenCaution.Select(h => h.Name))}",
                    HerbIds = childrenCaution.Select(h => h.Id).ToList()
                });
            }
        }

        #endregion

        #region 剂量验证

        /// <summary>
        /// 验证处方剂量合理性
        /// </summary>
        public async Task<DosageValidationResult> ValidateDosages(
            List<PrescriptionHerbItem> prescriptionItems,
            PatientInfo patientInfo = null)
        {
            var result = new DosageValidationResult();
            var herbIds = prescriptionItems.Select(p => p.HerbId).ToList();
            var herbs = await _herbRepository.GetByIdsAsync(herbIds);

            foreach (var item in prescriptionItems)
            {
                var herb = herbs.FirstOrDefault(h => h.Id == item.HerbId);
                if (herb == null)
                {
                    result.AddError($"药材{item.HerbName}不存在");
                    continue;
                }

                // 检查剂量范围
                if (!herb.IsValidDosage(item.Dosage))
                {
                    var dosageRange = herb.DosageRange;
                    result.AddWarning(new DosageWarning
                    {
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        CurrentDosage = item.Dosage,
                        MinDosage = dosageRange.MinDosage,
                        MaxDosage = dosageRange.MaxDosage,
                        RecommendedDosage = dosageRange.CommonDosage,
                        Message = $"{herb.Name}剂量{item.Dosage}g超出常规范围{dosageRange}"
                    });
                }

                // 根据患者信息调整剂量建议
                if (patientInfo != null)
                {
                    AdjustDosageForPatient(herb, item, patientInfo, result);
                }
            }

            // 检查总剂量
            var totalDosage = prescriptionItems.Sum(p => p.Dosage);
            if (totalDosage > 500) // 单剂总量超过500g需要警告
            {
                result.AddWarning(new DosageWarning
                {
                    Message = $"处方总剂量{totalDosage}g过大，建议复核"
                });
            }

            return result;
        }

        /// <summary>
        /// 根据患者信息调整剂量建议
        /// </summary>
        private void AdjustDosageForPatient(
            Herb herb,
            PrescriptionHerbItem item,
            PatientInfo patientInfo,
            DosageValidationResult result)
        {
            // 儿童剂量调整
            if (patientInfo.Age < 14)
            {
                var recommendedDosage = CalculateChildDosage(item.Dosage, patientInfo.Age);
                if (Math.Abs(item.Dosage - recommendedDosage) > 1)
                {
                    result.AddSuggestion(new DosageSuggestion
                    {
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        CurrentDosage = item.Dosage,
                        SuggestedDosage = recommendedDosage,
                        Reason = $"儿童用药建议剂量（{patientInfo.Age}岁）"
                    });
                }
            }

            // 老年人剂量调整
            if (patientInfo.Age > 65)
            {
                var recommendedDosage = item.Dosage * 0.8m; // 老年人通常减量20%
                if (item.Dosage > recommendedDosage)
                {
                    result.AddSuggestion(new DosageSuggestion
                    {
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        CurrentDosage = item.Dosage,
                        SuggestedDosage = recommendedDosage,
                        Reason = "老年人用药建议适当减量"
                    });
                }
            }
        }

        /// <summary>
        /// 计算儿童剂量
        /// </summary>
        private decimal CalculateChildDosage(decimal adultDosage, int age)
        {
            // Young公式：儿童剂量 = 成人剂量 × 年龄 / (年龄 + 12)
            return Math.Round(adultDosage * age / (age + 12), 1);
        }

        #endregion

        #region 君臣佐使分析

        /// <summary>
        /// 分析处方的君臣佐使配伍
        /// </summary>
        public async Task<HerbRoleAnalysis> AnalyzeHerbRoles(
            List<PrescriptionHerbItem> prescriptionItems,
            string targetSyndrome = null)
        {
            var analysis = new HerbRoleAnalysis();
            var herbIds = prescriptionItems.Select(p => p.HerbId).ToList();
            var herbs = await _herbRepository.GetByIdsAsync(herbIds);

            // 根据剂量和功效推断药材角色
            var sortedItems = prescriptionItems.OrderByDescending(p => p.Dosage).ToList();
            
            // 通常剂量最大的为君药
            if (sortedItems.Any())
            {
                var monarch = sortedItems.First();
                analysis.MonarchHerbs.Add(new HerbRoleItem
                {
                    HerbId = monarch.HerbId,
                    HerbName = monarch.HerbName,
                    Dosage = monarch.Dosage,
                    Role = "君药",
                    Function = "针对主病或主证起主要治疗作用"
                });
            }

            // 分析臣药、佐药、使药
            for (int i = 1; i < sortedItems.Count; i++)
            {
                var item = sortedItems[i];
                var herb = herbs.FirstOrDefault(h => h.Id == item.HerbId);
                
                if (herb == null) continue;

                // 根据药材特性和剂量判断角色
                if (i <= 2 && item.Dosage >= sortedItems[0].Dosage * 0.6m)
                {
                    // 臣药：剂量较大，辅助君药
                    analysis.MinisterHerbs.Add(new HerbRoleItem
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Dosage = item.Dosage,
                        Role = "臣药",
                        Function = "辅助君药加强治疗主病或主证"
                    });
                }
                else if (herb.Meridians.Any(m => m.Name.Contains("脾") || m.Name.Contains("胃")))
                {
                    // 使药：引经药或调和药
                    analysis.GuideHerbs.Add(new HerbRoleItem
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Dosage = item.Dosage,
                        Role = "使药",
                        Function = "引药归经或调和诸药"
                    });
                }
                else
                {
                    // 佐药：其他辅助药材
                    analysis.AssistantHerbs.Add(new HerbRoleItem
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Dosage = item.Dosage,
                        Role = "佐药",
                        Function = "配合君臣药治疗兼证或减缓君臣药毒性"
                    });
                }
            }

            analysis.IsBalanced = analysis.MonarchHerbs.Any() && 
                                 (analysis.MinisterHerbs.Any() || analysis.AssistantHerbs.Any());
            
            if (!analysis.IsBalanced)
            {
                analysis.Suggestions.Add("处方配伍不够均衡，建议增加臣药或佐药");
            }

            return analysis;
        }

        #endregion

        #region 验方应用

        /// <summary>
        /// 基于验方创建处方
        /// </summary>
        public async Task<List<PrescriptionHerbItem>> CreatePrescriptionFromFormula(
            Guid formulaId,
            decimal dosageMultiplier = 1.0m,
            Dictionary<Guid, decimal> adjustments = null)
        {
            var formula = await _formulaRepository.GetByIdAsync(formulaId);
            if (formula == null)
                throw new PrescriptionDomainException("验方不存在");

            if (!formula.CanBeUsed)
                throw new PrescriptionDomainException($"验方{formula.Name}当前状态不允许使用");

            var prescriptionItems = new List<PrescriptionHerbItem>();
            var baseItems = formula.CreatePrescriptionItems(dosageMultiplier);

            foreach (var item in baseItems)
            {
                var dosage = item.Value;
                
                // 应用个别调整
                if (adjustments != null && adjustments.ContainsKey(item.Key))
                {
                    dosage = adjustments[item.Key];
                }

                var herb = await _herbRepository.GetByIdAsync(item.Key);
                if (herb != null && herb.IsActive)
                {
                    prescriptionItems.Add(new PrescriptionHerbItem
                    {
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        Dosage = dosage,
                        Unit = herb.Unit,
                        ProcessingMethod = herb.DefaultProcessing.Name,
                        Price = herb.UnitPrice
                    });
                }
            }

            return prescriptionItems;
        }

        /// <summary>
        /// 推荐相似验方
        /// </summary>
        public async Task<List<FormulaRecommendation>> RecommendFormulas(
            string syndrome,
            List<string> symptoms,
            int maxResults = 5)
        {
            var allFormulas = await _formulaRepository.GetAllAsync();
            var recommendations = new List<FormulaRecommendation>();

            foreach (var formula in allFormulas.Where(f => f.CanBeUsed))
            {
                var score = CalculateFormulaMatchScore(formula, syndrome, symptoms);
                if (score > 0)
                {
                    recommendations.Add(new FormulaRecommendation
                    {
                        FormulaId = formula.Id,
                        FormulaName = formula.Name,
                        Source = formula.Source,
                        TargetSyndrome = formula.TargetSyndrome?.Name,
                        MatchScore = score,
                        SuccessRate = formula.SuccessRate,
                        UsageCount = formula.UsageCount,
                        Indication = formula.Indication
                    });
                }
            }

            return recommendations
                .OrderByDescending(r => r.MatchScore)
                .ThenByDescending(r => r.SuccessRate)
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// 计算验方匹配度评分
        /// </summary>
        private decimal CalculateFormulaMatchScore(
            Formula formula,
            string syndrome,
            List<string> symptoms)
        {
            decimal score = 0;

            // 证型匹配
            if (!string.IsNullOrWhiteSpace(syndrome) && 
                formula.TargetSyndrome != null &&
                formula.TargetSyndrome.Name.Contains(syndrome))
            {
                score += 50;
            }

            // 适应症匹配
            if (symptoms != null && symptoms.Any())
            {
                foreach (var symptom in symptoms)
                {
                    if (formula.Indication.Contains(symptom))
                    {
                        score += 10;
                    }
                }
            }

            // 成功率加成
            score += formula.SuccessRate * 0.2m;

            // 使用频次加成
            if (formula.UsageCount > 100)
            {
                score += 10;
            }
            else if (formula.UsageCount > 50)
            {
                score += 5;
            }

            return score;
        }

        #endregion
    }

    #region 领域服务返回类型

    /// <summary>
    /// 配伍禁忌检查结果
    /// </summary>
    public class IncompatibilityCheckResult
    {
        public List<IncompatibilityInfo> Incompatibilities { get; private set; }
        public List<IncompatibilityInfo> Cautions { get; private set; }
        public List<CompatibilityWarning> Warnings { get; private set; }
        public bool HasIncompatibilities => Incompatibilities.Any();
        public bool HasCautions => Cautions.Any();
        public bool IsValid => !HasIncompatibilities;

        public IncompatibilityCheckResult()
        {
            Incompatibilities = new List<IncompatibilityInfo>();
            Cautions = new List<IncompatibilityInfo>();
            Warnings = new List<CompatibilityWarning>();
        }

        public void AddIncompatibility(IncompatibilityInfo info)
        {
            Incompatibilities.Add(info);
        }

        public void AddCaution(IncompatibilityInfo info)
        {
            Cautions.Add(info);
        }

        public void AddWarning(CompatibilityWarning warning)
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// 配伍禁忌信息
    /// </summary>
    public class IncompatibilityInfo
    {
        public Guid Herb1Id { get; set; }
        public string Herb1Name { get; set; }
        public Guid Herb2Id { get; set; }
        public string Herb2Name { get; set; }
        public IncompatibilityType Type { get; set; }
        public IncompatibilitySeverity Severity { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 配伍警告
    /// </summary>
    public class CompatibilityWarning
    {
        public WarningType Type { get; set; }
        public string Message { get; set; }
        public List<Guid> HerbIds { get; set; }
    }

    /// <summary>
    /// 剂量验证结果
    /// </summary>
    public class DosageValidationResult
    {
        public List<DosageWarning> Warnings { get; private set; }
        public List<DosageSuggestion> Suggestions { get; private set; }
        public List<string> Errors { get; private set; }
        public bool IsValid => !Errors.Any();

        public DosageValidationResult()
        {
            Warnings = new List<DosageWarning>();
            Suggestions = new List<DosageSuggestion>();
            Errors = new List<string>();
        }

        public void AddWarning(DosageWarning warning)
        {
            Warnings.Add(warning);
        }

        public void AddSuggestion(DosageSuggestion suggestion)
        {
            Suggestions.Add(suggestion);
        }

        public void AddError(string error)
        {
            Errors.Add(error);
        }
    }

    /// <summary>
    /// 剂量警告
    /// </summary>
    public class DosageWarning
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; }
        public decimal CurrentDosage { get; set; }
        public decimal MinDosage { get; set; }
        public decimal MaxDosage { get; set; }
        public decimal RecommendedDosage { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 剂量建议
    /// </summary>
    public class DosageSuggestion
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; }
        public decimal CurrentDosage { get; set; }
        public decimal SuggestedDosage { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// 君臣佐使分析结果
    /// </summary>
    public class HerbRoleAnalysis
    {
        public List<HerbRoleItem> MonarchHerbs { get; set; }
        public List<HerbRoleItem> MinisterHerbs { get; set; }
        public List<HerbRoleItem> AssistantHerbs { get; set; }
        public List<HerbRoleItem> GuideHerbs { get; set; }
        public bool IsBalanced { get; set; }
        public List<string> Suggestions { get; set; }

        public HerbRoleAnalysis()
        {
            MonarchHerbs = new List<HerbRoleItem>();
            MinisterHerbs = new List<HerbRoleItem>();
            AssistantHerbs = new List<HerbRoleItem>();
            GuideHerbs = new List<HerbRoleItem>();
            Suggestions = new List<string>();
        }
    }

    /// <summary>
    /// 药材角色项
    /// </summary>
    public class HerbRoleItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; }
        public decimal Dosage { get; set; }
        public string Role { get; set; }
        public string Function { get; set; }
    }

    /// <summary>
    /// 验方推荐
    /// </summary>
    public class FormulaRecommendation
    {
        public Guid FormulaId { get; set; }
        public string FormulaName { get; set; }
        public string Source { get; set; }
        public string TargetSyndrome { get; set; }
        public decimal MatchScore { get; set; }
        public decimal SuccessRate { get; set; }
        public int UsageCount { get; set; }
        public string Indication { get; set; }
    }

    /// <summary>
    /// 处方药材项
    /// </summary>
    public class PrescriptionHerbItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; }
        public decimal Dosage { get; set; }
        public string Unit { get; set; }
        public string ProcessingMethod { get; set; }
        public LYBT.Domain.ValueObjects.Money Price { get; set; }
    }

    /// <summary>
    /// 患者信息（用于剂量调整）
    /// </summary>
    public class PatientInfo
    {
        public int Age { get; set; }
        public string Gender { get; set; }
        public decimal Weight { get; set; }
        public bool IsPregnant { get; set; }
        public bool IsBreastfeeding { get; set; }
        public List<string> Allergies { get; set; }
        public List<string> ChronicDiseases { get; set; }
    }

    /// <summary>
    /// 配伍禁忌严重程度
    /// </summary>
    public enum IncompatibilitySeverity
    {
        Absolute,  // 绝对禁忌
        Caution,   // 慎用
        Warning    // 警告
    }

    /// <summary>
    /// 配伍禁忌类型
    /// </summary>
    public enum IncompatibilityType
    {
        Eighteen,  // 十八反
        Nineteen,  // 十九畏
        Other      // 其他
    }

    /// <summary>
    /// 警告类型
    /// </summary>
    public enum WarningType
    {
        PregnancyContraindication,  // 孕妇禁用
        ChildrenCaution,           // 儿童慎用
        ElderlyAdjustment,         // 老年人剂量调整
        LiverDiseaseWarning,       // 肝病警告
        KidneyDiseaseWarning,      // 肾病警告
        Other                      // 其他
    }

    #endregion
}