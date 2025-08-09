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
    /// 特殊人群用药安全验证器 - UltraThink重构专门组件
    /// 专门负责孕妇、哺乳期、儿童、老年人等特殊人群的用药安全检查
    /// </summary>
    public class SpecialPopulationValidator
    {
        private readonly ILogger<SpecialPopulationValidator> _logger;

        #region 特殊人群禁用药材数据

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

        // 肝毒性药材
        private readonly List<string> _hepatotoxicHerbs = new() 
        { 
            "何首乌", "大黄", "番泻叶", "土三七", "艾叶", "苍耳子" 
        };

        // 肾毒性药材
        private readonly List<string> _nephrotoxicHerbs = new() 
        { 
            "马兜铃", "木通", "细辛", "厚朴", "朱砂", "雄黄" 
        };

        #endregion

        #region 构造函数

        public SpecialPopulationValidator(ILogger<SpecialPopulationValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查特殊人群用药安全
        /// </summary>
        public async Task<List<SpecialPopulationWarning>> ValidateSpecialPopulationSafetyAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            PatientValidationInfo patientInfo)
        {
            try
            {
                await Task.CompletedTask;
                var items = prescriptionItems.ToList();
                var warnings = new List<SpecialPopulationWarning>();

                _logger.LogInformation("开始特殊人群用药安全检查，患者年龄: {Age}岁", patientInfo.Age);

                // 孕妇用药检查
                if (patientInfo.IsPregnant)
                {
                    CheckPregnantPatientSafety(items, warnings);
                }

                // 哺乳期用药检查
                if (patientInfo.IsLactating)
                {
                    CheckLactatingPatientSafety(items, warnings);
                }

                // 儿童用药检查
                if (patientInfo.Age < 18)
                {
                    CheckPediatricPatientSafety(items, warnings);
                }

                // 老年人用药检查
                if (patientInfo.Age > 65)
                {
                    CheckGeriatricPatientSafety(items, warnings);
                }

                // 肝功能不全检查
                if (patientInfo.LiverFunction.HasValue && 
                    patientInfo.LiverFunction.Value != OrganFunctionStatus.Normal)
                {
                    CheckHepaticImpairmentSafety(items, patientInfo.LiverFunction.Value, warnings);
                }

                // 肾功能不全检查
                if (patientInfo.KidneyFunction.HasValue && 
                    patientInfo.KidneyFunction.Value != OrganFunctionStatus.Normal)
                {
                    CheckRenalImpairmentSafety(items, patientInfo.KidneyFunction.Value, warnings);
                }

                _logger.LogInformation("特殊人群用药安全检查完成，发现{Count}个安全问题", warnings.Count);
                return warnings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "特殊人群用药安全检查时发生异常");
                return new List<SpecialPopulationWarning>();
            }
        }

        /// <summary>
        /// 检查特定人群对特定药材的禁忌
        /// </summary>
        public bool IsContraindicated(string herbName, SpecialPopulationType populationType)
        {
            if (!_contraindications.TryGetValue(populationType, out var contraindicatedHerbs))
                return false;

            return contraindicatedHerbs.Any(contraindicated => herbName.Contains(contraindicated));
        }

        /// <summary>
        /// 获取特定人群的所有禁用药材列表
        /// </summary>
        public List<string> GetContraindicatedHerbs(SpecialPopulationType populationType)
        {
            return _contraindications.TryGetValue(populationType, out var herbs) ? 
                   herbs.ToList() : new List<string>();
        }

        /// <summary>
        /// 是否为肝毒性药材
        /// </summary>
        public bool IsHepatotoxic(string herbName)
        {
            return _hepatotoxicHerbs.Any(hepatotoxic => herbName.Contains(hepatotoxic));
        }

        /// <summary>
        /// 是否为肾毒性药材
        /// </summary>
        public bool IsNephrotoxic(string herbName)
        {
            return _nephrotoxicHerbs.Any(nephrotoxic => herbName.Contains(nephrotoxic));
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查孕妇用药安全
        /// </summary>
        private void CheckPregnantPatientSafety(List<PrescriptionItemInfo> items, List<SpecialPopulationWarning> warnings)
        {
            CheckSpecialPopulationHerbs(items, SpecialPopulationType.Pregnant, warnings);
            _logger.LogInformation("孕妇用药安全检查完成");
        }

        /// <summary>
        /// 检查哺乳期用药安全
        /// </summary>
        private void CheckLactatingPatientSafety(List<PrescriptionItemInfo> items, List<SpecialPopulationWarning> warnings)
        {
            CheckSpecialPopulationHerbs(items, SpecialPopulationType.Lactating, warnings);
            _logger.LogInformation("哺乳期用药安全检查完成");
        }

        /// <summary>
        /// 检查儿童用药安全
        /// </summary>
        private void CheckPediatricPatientSafety(List<PrescriptionItemInfo> items, List<SpecialPopulationWarning> warnings)
        {
            CheckSpecialPopulationHerbs(items, SpecialPopulationType.Pediatric, warnings);
            _logger.LogInformation("儿童用药安全检查完成");
        }

        /// <summary>
        /// 检查老年人用药安全
        /// </summary>
        private void CheckGeriatricPatientSafety(List<PrescriptionItemInfo> items, List<SpecialPopulationWarning> warnings)
        {
            CheckSpecialPopulationHerbs(items, SpecialPopulationType.Geriatric, warnings);
            _logger.LogInformation("老年人用药安全检查完成");
        }

        /// <summary>
        /// 检查肝功能不全患者用药安全
        /// </summary>
        private void CheckHepaticImpairmentSafety(List<PrescriptionItemInfo> items, 
            OrganFunctionStatus liverFunction, List<SpecialPopulationWarning> warnings)
        {
            var affectedHerbs = items.Where(item => IsHepatotoxic(item.HerbName)).ToList();

            if (!affectedHerbs.Any()) return;

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

            _logger.LogWarning("发现肝功能不全患者肝毒性药材使用: {Count}个", affectedHerbs.Count);
        }

        /// <summary>
        /// 检查肾功能不全患者用药安全
        /// </summary>
        private void CheckRenalImpairmentSafety(List<PrescriptionItemInfo> items, 
            OrganFunctionStatus kidneyFunction, List<SpecialPopulationWarning> warnings)
        {
            var affectedHerbs = items.Where(item => IsNephrotoxic(item.HerbName)).ToList();

            if (!affectedHerbs.Any()) return;

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

            _logger.LogWarning("发现肾功能不全患者肾毒性药材使用: {Count}个", affectedHerbs.Count);
        }

        /// <summary>
        /// 通用特殊人群药材检查
        /// </summary>
        private void CheckSpecialPopulationHerbs(List<PrescriptionItemInfo> items, 
            SpecialPopulationType populationType, List<SpecialPopulationWarning> warnings)
        {
            if (!_contraindications.TryGetValue(populationType, out var contraindicatedHerbs))
                return;

            var affectedHerbs = items.Where(item => 
                contraindicatedHerbs.Any(contraindicated => item.HerbName.Contains(contraindicated)))
                .ToList();

            if (!affectedHerbs.Any()) return;

            warnings.Add(new SpecialPopulationWarning
            {
                PopulationType = populationType,
                AffectedHerbs = affectedHerbs.Select(h => h.HerbName).ToList(),
                RiskLevel = RiskLevel.High,
                RiskDescription = $"{GetPopulationTypeText(populationType)}禁用或慎用药材：{string.Join("、", affectedHerbs.Select(h => h.HerbName))}",
                Recommendation = "建议删除这些药材或选择安全的替代药物"
            });

            _logger.LogWarning("发现{PopulationType}禁用药材: {Count}个", 
                GetPopulationTypeText(populationType), affectedHerbs.Count);
        }

        /// <summary>
        /// 获取人群类型文本描述
        /// </summary>
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

        #endregion
    }
}