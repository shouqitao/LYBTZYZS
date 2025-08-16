using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Enums;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Consultation.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using AutoMapper;
using LYBT.Shared.Interfaces.Services;

// UltraThink重构: 统一FormulaInfo和FormulaDto，使用FormulaDto作为统一模型
using FormulaInfo = LYBT.Shared.Models.Contracts.Formula.FormulaDto;
using IFormulaService = LYBT.Shared.Interfaces.Services.IFormulaService;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 验方管理器 - 负责验方模板的应用、合并和管理
    /// </summary>
    public class FormulaManager : IFormulaManager
    {
        #region 常量定义

        private const int DEFAULT_FREQUENTLY_USED_COUNT = 10;
        private const int MAX_FORMULA_NAME_LENGTH = 100;
        private const int MIN_FORMULA_ITEMS = 2;

        #endregion

        #region 依赖服务

        private readonly IFormulaService _formulaService;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaManager> _logger;

        #endregion

        #region 缓存字段

        private readonly Dictionary<Guid, int> _formulaUsageCount = new();
        private List<FormulaInfo>? _cachedFormulas;

        #endregion

        public FormulaManager(
            IFormulaService formulaService,
            IMapper mapper,
            ILogger<FormulaManager> logger)
        {
            _formulaService = formulaService;
            _mapper = mapper;
            _logger = logger;
        }

        #region 验方应用

        /// <summary>
        /// 应用验方模板到处方
        /// </summary>
        public List<PrescriptionItemInfo> ApplyFormulaTemplate(FormulaInfo formula)
        {
            try
            {
                if (formula?.Items == null || !formula.Items.Any())
                {
                    _logger.LogWarning("验方模板为空或没有药材");
                    return new List<PrescriptionItemInfo>();
                }

                var prescriptionItems = new List<PrescriptionItemInfo>();

                foreach (var item in formula.Items)
                {
                    var prescriptionItem = new PrescriptionItemInfo
                    {
                        Id = Guid.NewGuid(),
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Usage = item.ProcessingMethod ?? string.Empty,
                        Remark = item.SpecialInstructions ?? string.Empty
                    };

                    prescriptionItems.Add(prescriptionItem);
                }

                // 记录使用次数
                RecordFormulaUsage(formula.Id);

                _logger.LogInformation($"成功应用验方模板 {formula.Name}，包含 {prescriptionItems.Count} 味药材");
                return prescriptionItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"应用验方模板 {formula?.Name} 时发生异常");
                return new List<PrescriptionItemInfo>();
            }
        }

        /// <summary>
        /// 合并验方到现有处方
        /// </summary>
        public List<PrescriptionItemInfo> MergeFormulaToPrescription(
            FormulaInfo formula,
            IEnumerable<PrescriptionItemInfo> existingItems,
            FormulaMergeMode mergeMode = FormulaMergeMode.Merge)
        {
            try
            {
                var validation = ValidateFormula(formula);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"验方验证失败: {validation.ErrorMessage}");
                    return existingItems?.ToList() ?? new List<PrescriptionItemInfo>();
                }

                var formulaItems = ApplyFormulaTemplate(formula);
                
                switch (mergeMode)
                {
                    case FormulaMergeMode.Replace:
                        return formulaItems;

                    case FormulaMergeMode.Append:
                        var appendedList = existingItems?.ToList() ?? new List<PrescriptionItemInfo>();
                        appendedList.AddRange(formulaItems);
                        return appendedList;

                    case FormulaMergeMode.Merge:
                        return MergeItems(existingItems, formulaItems);

                    default:
                        return existingItems?.ToList() ?? new List<PrescriptionItemInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "合并验方到处方时发生异常");
                return existingItems?.ToList() ?? new List<PrescriptionItemInfo>();
            }
        }

        #endregion

        #region 验方创建

        /// <summary>
        /// 创建自定义验方
        /// </summary>
        public async Task<FormulaInfo?> CreateCustomFormulaAsync(
            string name,
            IEnumerable<PrescriptionItemInfo> items,
            string? description = null)
        {
            try
            {
                var itemsList = items?.ToList();
                if (string.IsNullOrWhiteSpace(name) || itemsList == null || itemsList.Count < MIN_FORMULA_ITEMS)
                {
                    _logger.LogWarning($"创建验方失败：名称为空或药材少于{MIN_FORMULA_ITEMS}味");
                    return null;
                }

                if (name.Length > MAX_FORMULA_NAME_LENGTH)
                {
                    name = name.Substring(0, MAX_FORMULA_NAME_LENGTH);
                }

                var createDto = new FormulaCreateDto
                {
                    Name = name,
                    Effect = description ?? string.Empty,
                    Herbs = itemsList.Select((item, index) => new FormulaHerbItemCreateDto
                    {
                        HerbId = item.HerbId,
                        Quantity = item.Quantity,
                        Usage = item.Usage,
                        SortOrder = index
                    }).ToList()
                };

                var result = await _formulaService.CreateAsync(createDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var formula = _mapper.Map<FormulaInfo>(result.Data);
                    _logger.LogInformation($"成功创建自定义验方: {name}");
                    
                    // 清除缓存以便重新加载
                    _cachedFormulas = null;
                    
                    return formula;
                }

                _logger.LogWarning($"创建验方失败: {result.ErrorMessage}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建自定义验方时发生异常");
                return null;
            }
        }

        #endregion

        #region 验方验证

        /// <summary>
        /// 验证验方是否可用
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidateFormula(FormulaInfo formula)
        {
            if (formula == null)
            {
                return (false, "验方为空");
            }

            if (string.IsNullOrWhiteSpace(formula.Name))
            {
                return (false, "验方名称不能为空");
            }

            if (formula.Items == null || !formula.Items.Any())
            {
                return (false, "验方不包含任何药材");
            }

            if (formula.Items.Count < MIN_FORMULA_ITEMS)
            {
                return (false, $"验方至少需要{MIN_FORMULA_ITEMS}味药材");
            }

            // 检查是否有重复药材
            var duplicateHerbs = formula.Items
                .GroupBy(x => x.HerbId)
                .Where(g => g.Count() > 1)
                .Select(g => g.First().HerbName);

            if (duplicateHerbs.Any())
            {
                return (false, $"验方中存在重复药材: {string.Join(", ", duplicateHerbs)}");
            }

            // 检查数量是否合理
            var invalidQuantities = formula.Items
                .Where(x => x.Quantity <= 0 || x.Quantity > 1000)
                .Select(x => x.HerbName);

            if (invalidQuantities.Any())
            {
                return (false, $"以下药材数量无效: {string.Join(", ", invalidQuantities)}");
            }

            return (true, null);
        }

        #endregion

        #region 价格计算

        /// <summary>
        /// 计算验方价格
        /// </summary>
        public decimal CalculateFormulaPrice(FormulaInfo formula)
        {
            if (formula?.Items == null || !formula.Items.Any())
            {
                return 0;
            }

            return formula.Items.Sum(x => x.Quantity * x.UnitPrice);
        }

        #endregion

        #region 验方推荐

        /// <summary>
        /// 获取常用验方列表
        /// </summary>
        public async Task<List<FormulaInfo>> GetFrequentlyUsedFormulasAsync(int count = DEFAULT_FREQUENTLY_USED_COUNT)
        {
            try
            {
                // 如果没有缓存的验方列表，先加载
                if (_cachedFormulas == null)
                {
                    var formulaResult = await _formulaService.GetFormulasAsync();
                    _cachedFormulas = formulaResult.IsSuccess ? formulaResult.Data ?? new List<FormulaInfo>() : new List<FormulaInfo>();
                }

                // 根据使用次数排序
                var frequentlyUsed = _cachedFormulas
                    .Where(f => _formulaUsageCount.ContainsKey(f.Id))
                    .OrderByDescending(f => _formulaUsageCount[f.Id])
                    .Take(count)
                    .ToList();

                // 如果常用验方不足，补充其他验方
                if (frequentlyUsed.Count < count)
                {
                    var remaining = _cachedFormulas
                        .Where(f => !frequentlyUsed.Contains(f))
                        .Take(count - frequentlyUsed.Count);
                    
                    frequentlyUsed.AddRange(remaining);
                }

                return frequentlyUsed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取常用验方列表时发生异常");
                return new List<FormulaInfo>();
            }
        }

        /// <summary>
        /// 按症状推荐验方
        /// </summary>
        public async Task<List<FormulaInfo>> RecommendFormulasBySymptoms(IEnumerable<string> symptoms)
        {
            try
            {
                if (symptoms == null || !symptoms.Any())
                {
                    return new List<FormulaInfo>();
                }

                // 如果没有缓存的验方列表，先加载
                if (_cachedFormulas == null)
                {
                    var formulaResult = await _formulaService.GetFormulasAsync();
                    _cachedFormulas = formulaResult.IsSuccess ? formulaResult.Data ?? new List<FormulaInfo>() : new List<FormulaInfo>();
                }

                var symptomKeywords = symptoms
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim().ToLower())
                    .ToList();

                // 基于症状关键词匹配验方
                var recommendations = _cachedFormulas
                    .Where(f => MatchesSymptoms(f, symptomKeywords))
                    .OrderByDescending(f => CalculateMatchScore(f, symptomKeywords))
                    .Take(10)
                    .ToList();

                _logger.LogInformation($"根据症状推荐了 {recommendations.Count} 个验方");
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按症状推荐验方时发生异常");
                return new List<FormulaInfo>();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 合并处方项目
        /// </summary>
        private List<PrescriptionItemInfo> MergeItems(
            IEnumerable<PrescriptionItemInfo>? existingItems,
            List<PrescriptionItemInfo> newItems)
        {
            var mergedItems = existingItems?.ToList() ?? new List<PrescriptionItemInfo>();

            foreach (var newItem in newItems)
            {
                var existingItem = mergedItems.FirstOrDefault(x => x.HerbId == newItem.HerbId);
                
                if (existingItem != null)
                {
                    // 相同药材，累加数量（Subtotal会自动重新计算）
                    existingItem.Quantity += newItem.Quantity;
                    
                    // 合并用法说明
                    if (!string.IsNullOrWhiteSpace(newItem.Usage) && 
                        existingItem.Usage != newItem.Usage)
                    {
                        existingItem.Usage = $"{existingItem.Usage}; {newItem.Usage}";
                    }
                }
                else
                {
                    // 新药材，直接添加
                    mergedItems.Add(newItem);
                }
            }

            return mergedItems;
        }

        /// <summary>
        /// 记录验方使用次数
        /// </summary>
        private void RecordFormulaUsage(Guid formulaId)
        {
            if (_formulaUsageCount.ContainsKey(formulaId))
            {
                _formulaUsageCount[formulaId]++;
            }
            else
            {
                _formulaUsageCount[formulaId] = 1;
            }
        }

        /// <summary>
        /// 检查验方是否匹配症状
        /// </summary>
        private bool MatchesSymptoms(FormulaInfo formula, List<string> symptomKeywords)
        {
            if (string.IsNullOrWhiteSpace(formula.Description))
            {
                return false;
            }

            var description = formula.Description.ToLower();
            var name = formula.Name.ToLower();

            return symptomKeywords.Any(keyword => 
                description.Contains(keyword) || name.Contains(keyword));
        }

        /// <summary>
        /// 计算症状匹配分数
        /// </summary>
        private int CalculateMatchScore(FormulaInfo formula, List<string> symptomKeywords)
        {
            int score = 0;
            var description = (formula.Description ?? string.Empty).ToLower();
            var name = formula.Name.ToLower();

            foreach (var keyword in symptomKeywords)
            {
                if (name.Contains(keyword))
                {
                    score += 3; // 名称匹配权重更高
                }
                if (description.Contains(keyword))
                {
                    score += 1;
                }
            }

            // 考虑使用频率
            if (_formulaUsageCount.ContainsKey(formula.Id))
            {
                score += Math.Min(_formulaUsageCount[formula.Id], 5); // 最多加5分
            }

            return score;
        }

        #endregion
    }
}