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
    /// 处方配伍禁忌验证器 - UltraThink重构专门组件
    /// 专门负责中医药配伍禁忌检查，包括十八反、十九畏等
    /// </summary>
    public class PrescriptionInteractionValidator
    {
        private readonly ILogger<PrescriptionInteractionValidator> _logger;

        #region 配伍禁忌数据

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

        #endregion

        #region 构造函数

        public PrescriptionInteractionValidator(ILogger<PrescriptionInteractionValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查药物配伍禁忌
        /// </summary>
        public async Task<List<DrugInteractionWarning>> CheckInteractionsAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems)
        {
            try
            {
                await Task.CompletedTask;
                var items = prescriptionItems.ToList();
                var warnings = new List<DrugInteractionWarning>();

                _logger.LogInformation("开始检查配伍禁忌，药材数量: {Count}", items.Count);

                // 检查十八反配伍禁忌
                CheckEighteenAntagonisms(items, warnings);

                // 检查十九畏配伍关系
                CheckNineteenFears(items, warnings);

                _logger.LogInformation("配伍禁忌检查完成，发现{Count}个问题", warnings.Count);
                return warnings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查药物配伍禁忌时发生异常");
                return new List<DrugInteractionWarning>();
            }
        }

        /// <summary>
        /// 获取特定药材的配伍禁忌信息
        /// </summary>
        public List<string> GetContraindicatedHerbs(string herbName)
        {
            var contraindicated = new List<string>();

            // 检查十八反
            if (_eighteenAntagonisms.TryGetValue(herbName, out var antagonisms))
            {
                contraindicated.AddRange(antagonisms);
            }

            // 反向检查十八反
            foreach (var antagonism in _eighteenAntagonisms)
            {
                if (antagonism.Value.Any(h => herbName.Contains(h)))
                {
                    contraindicated.Add(antagonism.Key);
                }
            }

            // 检查十九畏
            if (_nineteenFears.TryGetValue(herbName, out var fears))
            {
                contraindicated.AddRange(fears);
            }

            // 反向检查十九畏
            foreach (var fear in _nineteenFears)
            {
                if (fear.Value.Any(h => herbName.Contains(h)))
                {
                    contraindicated.Add(fear.Key);
                }
            }

            return contraindicated.Distinct().ToList();
        }

        /// <summary>
        /// 检查两味药材是否存在配伍禁忌
        /// </summary>
        public bool HasInteraction(string herb1, string herb2)
        {
            return CheckSingleInteraction(herb1, herb2, _eighteenAntagonisms) ||
                   CheckSingleInteraction(herb1, herb2, _nineteenFears) ||
                   CheckSingleInteraction(herb2, herb1, _eighteenAntagonisms) ||
                   CheckSingleInteraction(herb2, herb1, _nineteenFears);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查十八反配伍禁忌
        /// </summary>
        private void CheckEighteenAntagonisms(List<PrescriptionItemInfo> items, List<DrugInteractionWarning> warnings)
        {
            foreach (var antagonism in _eighteenAntagonisms)
            {
                var mainHerb = items.FirstOrDefault(i => i.HerbName.Contains(antagonism.Key));
                if (mainHerb == null) continue;

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

                    _logger.LogWarning("发现十八反配伍禁忌: {Herb1} - {Herb2}", 
                        mainHerb.HerbName, conflictHerb.HerbName);
                }
            }
        }

        /// <summary>
        /// 检查十九畏配伍关系
        /// </summary>
        private void CheckNineteenFears(List<PrescriptionItemInfo> items, List<DrugInteractionWarning> warnings)
        {
            foreach (var fear in _nineteenFears)
            {
                var mainHerb = items.FirstOrDefault(i => i.HerbName.Contains(fear.Key));
                if (mainHerb == null) continue;

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

                    _logger.LogWarning("发现十九畏配伍关系: {Herb1} - {Herb2}", 
                        mainHerb.HerbName, fearHerb.HerbName);
                }
            }
        }

        /// <summary>
        /// 检查单个配伍禁忌
        /// </summary>
        private bool CheckSingleInteraction(string herb1, string herb2, 
            Dictionary<string, List<string>> interactionDict)
        {
            return interactionDict.Any(kvp => 
                herb1.Contains(kvp.Key) && 
                kvp.Value.Any(forbidden => herb2.Contains(forbidden)));
        }

        #endregion
    }
}