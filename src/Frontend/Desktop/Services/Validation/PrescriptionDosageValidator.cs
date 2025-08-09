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
    /// 处方剂量验证器 - UltraThink重构专门组件
    /// 专门负责药材剂量合理性检查，包括有毒药材和常用药材的安全剂量范围
    /// </summary>
    public class PrescriptionDosageValidator
    {
        private readonly ILogger<PrescriptionDosageValidator> _logger;

        #region 剂量数据

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

        #endregion

        #region 构造函数

        public PrescriptionDosageValidator(ILogger<PrescriptionDosageValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查处方剂量合理性
        /// </summary>
        public async Task<List<DosageWarning>> ValidateDosagesAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            int patientAge,
            double patientWeight = 0)
        {
            try
            {
                await Task.CompletedTask;
                var items = prescriptionItems.ToList();
                var warnings = new List<DosageWarning>();

                _logger.LogInformation("开始检查剂量合理性，患者年龄: {Age}岁，体重: {Weight}kg", 
                    patientAge, patientWeight);

                foreach (var item in items)
                {
                    // 检查有毒药材剂量
                    CheckToxicHerbDosage(item, warnings);

                    // 检查常用药材标准剂量
                    CheckStandardHerbDosage(item, patientAge, warnings);
                }

                _logger.LogInformation("剂量检查完成，发现{Count}个剂量问题", warnings.Count);
                return warnings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查剂量合理性时发生异常");
                return new List<DosageWarning>();
            }
        }

        /// <summary>
        /// 获取药材的推荐剂量范围
        /// </summary>
        public DosageRange? GetRecommendedDosage(string herbName, int patientAge = 25)
        {
            // 优先检查有毒药材
            if (_toxicHerbDosages.TryGetValue(herbName, out var toxicRange))
            {
                return AdjustDosageForAge(toxicRange, patientAge);
            }

            // 检查常用药材
            if (_standardDosages.TryGetValue(herbName, out var standardRange))
            {
                return AdjustDosageForAge(standardRange, patientAge);
            }

            return null;
        }

        /// <summary>
        /// 检查单个药材剂量是否合理
        /// </summary>
        public DosageValidationResult ValidateSingleDosage(
            string herbName, 
            decimal currentDosage, 
            int patientAge)
        {
            var recommendedRange = GetRecommendedDosage(herbName, patientAge);
            if (recommendedRange == null)
            {
                return new DosageValidationResult
                {
                    IsValid = true,
                    Message = "未找到该药材的标准剂量参考"
                };
            }

            var isToxicHerb = _toxicHerbDosages.ContainsKey(herbName);

            if (currentDosage > recommendedRange.MaxDose)
            {
                return new DosageValidationResult
                {
                    IsValid = false,
                    RiskLevel = isToxicHerb ? RiskLevel.High : RiskLevel.Medium,
                    Message = $"剂量{currentDosage}g超过推荐上限{recommendedRange.MaxDose}g",
                    Recommendation = $"建议调整至{recommendedRange.TypicalDose}g"
                };
            }

            if (currentDosage < recommendedRange.MinDose)
            {
                return new DosageValidationResult
                {
                    IsValid = false,
                    RiskLevel = RiskLevel.Low,
                    Message = $"剂量{currentDosage}g低于推荐下限{recommendedRange.MinDose}g",
                    Recommendation = $"建议调整至{recommendedRange.TypicalDose}g"
                };
            }

            return new DosageValidationResult
            {
                IsValid = true,
                Message = "剂量在合理范围内"
            };
        }

        /// <summary>
        /// 是否为有毒药材
        /// </summary>
        public bool IsToxicHerb(string herbName)
        {
            return _toxicHerbDosages.ContainsKey(herbName);
        }

        /// <summary>
        /// 获取所有有毒药材列表
        /// </summary>
        public List<string> GetToxicHerbsList()
        {
            return _toxicHerbDosages.Keys.ToList();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查有毒药材剂量
        /// </summary>
        private void CheckToxicHerbDosage(PrescriptionItemInfo item, List<DosageWarning> warnings)
        {
            if (!_toxicHerbDosages.TryGetValue(item.HerbName, out var toxicRange))
                return;

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

                _logger.LogWarning("发现有毒药材超量: {HerbName} {CurrentDose}g > {MaxDose}g",
                    item.HerbName, currentDose, toxicRange.MaxDose);
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

                _logger.LogInformation("发现有毒药材剂量过低: {HerbName} {CurrentDose}g < {MinDose}g",
                    item.HerbName, currentDose, toxicRange.MinDose);
            }
        }

        /// <summary>
        /// 检查常用药材标准剂量
        /// </summary>
        private void CheckStandardHerbDosage(PrescriptionItemInfo item, int patientAge, List<DosageWarning> warnings)
        {
            if (!_standardDosages.TryGetValue(item.HerbName, out var standardRange))
                return;

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

        /// <summary>
        /// 根据年龄调整剂量
        /// </summary>
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

        #endregion
    }

    /// <summary>
    /// 剂量验证结果
    /// </summary>
    public class DosageValidationResult
    {
        public bool IsValid { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public string Message { get; set; } = "";
        public string Recommendation { get; set; } = "";
    }
}