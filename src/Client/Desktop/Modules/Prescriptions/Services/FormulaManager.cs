using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// UltraThink v2.0: 移除已删除的Info模型和接口引用，直接使用DTO
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using AutoMapper;
using LYBT.Desktop.Core.Enums;

using IFormulaService = LYBT.Shared.Interfaces.Services.IFormulaService;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 验方管理器 - 负责验方模板的应用、合并和管理
    /// UltraThink v2.0: 移除已删除的接口，直接实现验方管理逻辑
    /// </summary>
    public class FormulaManager
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
        private List<FormulaDto>? _cachedFormulas;

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
        public List<PrescriptionItemDto> ApplyFormulaTemplate(FormulaDto formula)
        {
            try
            {
                if (formula?.Items == null || !formula.Items.Any())
                {
                    _logger.LogWarning("验方模板为空或没有药材");
                    return new List<PrescriptionItemDto>();
                }

                var prescriptionItems = new List<PrescriptionItemDto>();

                foreach (var item in formula.Items)
                {
                    var prescriptionItem = new PrescriptionItemDto
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
                return new List<PrescriptionItemDto>();
            }
        }

        /// <summary>
        /// 合并验方到现有处方
        /// </summary>
        public List<PrescriptionItemDto> MergeFormulaToPrescription(
            FormulaDto formula,
            IEnumerable<PrescriptionItemDto> existingItems,
            FormulaMergeMode mergeMode = FormulaMergeMode.Merge)
        {
            try
            {
                var validation = ValidateFormula(formula);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"验方验证失败: {validation.ErrorMessage}");
                    return existingItems?.ToList() ?? new List<PrescriptionItemDto>();
                }

                var formulaItems = ApplyFormulaTemplate(formula);
                
                switch (mergeMode)
                {
                    case FormulaMergeMode.Replace:
                        return formulaItems;

                    case FormulaMergeMode.Append:
                        var appendedList = existingItems?.ToList() ?? new List<PrescriptionItemDto>();
                        appendedList.AddRange(formulaItems);
                        return appendedList;

                    case FormulaMergeMode.Merge:
                        return MergeItems(existingItems, formulaItems);

                    default:
                        return existingItems?.ToList() ?? new List<PrescriptionItemDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "合并验方到处方时发生异常");
                return existingItems?.ToList() ?? new List<PrescriptionItemDto>();
            }
        }

        #endregion

        #region 验方创建

        /// <summary>
        /// 创建自定义验方
        /// </summary>
        public async Task<FormulaDto?> CreateCustomFormulaAsync(
            string name,
            IEnumerable<PrescriptionItemDto> items,
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
                    var formula = result.Data; // 直接使用DTO，不需要映射
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
        public (bool IsValid, string? ErrorMessage) ValidateFormula(FormulaDto formula)
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
        public decimal CalculateFormulaPrice(FormulaDto formula)
        {
            if (formula?.Items == null || !formula.Items.Any())
            {
                return 0;
            }

            return formula.Items.Sum(x => x.Quantity * x.UnitPrice);
        }

        #endregion

        #region 验方推荐 - UltraThink v2.0: 删除智能推荐功能，保留基础验方列表

        /*
        // UltraThink v2.0: 删除过度设计的智能推荐功能 - 20人以下小诊所不需要复杂的推荐算法
        // 医生有专业经验，可以直接选择需要的验方，不需要系统智能推荐
        // 保留基础的验方列表加载功能即可
        
        /// <summary>
        /// 获取常用验方列表 - UltraThink v2.0: 已简化，删除复杂的使用统计
        /// </summary>
        public async Task<List<FormulaDto>> GetFrequentlyUsedFormulasAsync(int count = DEFAULT_FREQUENTLY_USED_COUNT)
        {
            // 删除复杂的使用次数统计和推荐算法
            // 直接返回基础的验方列表
        }

        /// <summary>
        /// 按症状推荐验方 - UltraThink v2.0: 已删除，过度设计
        /// </summary>
        public async Task<List<FormulaDto>> RecommendFormulasBySymptoms(IEnumerable<string> symptoms)
        {
            // 删除复杂的症状匹配和推荐算法
            // 医生可以根据经验直接选择合适的验方
        }
        */

        #endregion

        #region 辅助方法

        /// <summary>
        /// 合并处方项目
        /// </summary>
        private List<PrescriptionItemDto> MergeItems(
            IEnumerable<PrescriptionItemDto>? existingItems,
            List<PrescriptionItemDto> newItems)
        {
            var mergedItems = existingItems?.ToList() ?? new List<PrescriptionItemDto>();

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
        private bool MatchesSymptoms(FormulaDto formula, List<string> symptomKeywords)
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
        private int CalculateMatchScore(FormulaDto formula, List<string> symptomKeywords)
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