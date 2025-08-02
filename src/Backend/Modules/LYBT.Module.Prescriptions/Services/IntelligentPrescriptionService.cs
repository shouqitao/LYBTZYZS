using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Models.Prescriptions;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Prescriptions.Interfaces;

namespace LYBT.Module.Prescriptions.Services {

    /// <summary>
    /// 智能处方服务 - 处理验方组合、重复药材检测、缺药提醒等功能
    /// </summary>
    public class IntelligentPrescriptionService : IIntelligentPrescriptionService {
        private readonly IFormulaTemplateService _formulaTemplateService;
        private readonly IHerbService _herbService;

        public IntelligentPrescriptionService(
            IFormulaTemplateService formulaTemplateService,
            IHerbService herbService) {
            _formulaTemplateService = formulaTemplateService;
            _herbService = herbService;
        }

        /// <summary>
        /// 智能组合多个验方模板生成处方
        /// </summary>
        public async Task<PrescriptionCompositionResult> ComposeFromFormulaTemplatesAsync(
            List<Guid> formulaTemplateIds, int dosageCount = 7) {
            var result = new PrescriptionCompositionResult();
            var allHerbs = new Dictionary<string, PrescriptionItemModel>();
            var formulaNames = new List<string>();
            var duplicateWarnings = new List<string>();

            // 1. 获取所有验方模板
            var formulaTemplates = new List<FormulaTemplateDetailDto>();
            foreach (var templateId in formulaTemplateIds) {
                var template = await _formulaTemplateService.GetByIdAsync(templateId);
                if (template != null) {
                    formulaTemplates.Add(template);
                    formulaNames.Add(template.Name);
                }
            }

            // 2. 处理每个验方模板的药材
            foreach (var template in formulaTemplates) {
                foreach (var herb in template.Herbs) {
                    ProcessFormulaItem(herb, allHerbs, duplicateWarnings, template.Name);
                }
            }

            // 3. 检查药材库存状态
            var availabilityCheck = await CheckHerbAvailabilityAsync(allHerbs.Values.ToList());

            // 4. 计算价格和重量
            var priceCalculation = CalculatePrescriptionPrice(allHerbs.Values.ToList(), dosageCount);

            // 5. 组装结果
            result.Items = allHerbs.Values.Cast<object>().ToList();
            result.FormulaTemplateNames = formulaNames;
            result.DuplicateHerbWarning = string.Join("；", duplicateWarnings);
            result.DrugAvailability = availabilityCheck.Status;
            result.MissingHerbs = availabilityCheck.MissingHerbs;
            result.SingleDosePrice = priceCalculation.SingleDosePrice;
            result.TotalPrice = priceCalculation.TotalPrice;
            result.TotalWeight = priceCalculation.TotalWeight;
            result.DosageCount = dosageCount;

            return result;
        }

        /// <summary>
        /// 处理验方模板中的单个药材项
        /// </summary>
        private void ProcessFormulaItem(FormulaIngredientDto herb, Dictionary<string, PrescriptionItemModel> allHerbs,
            List<string> duplicateWarnings, string templateName) {
            var herbName = herb.Name?.Trim().ToUpper();
            if (string.IsNullOrEmpty(herbName))
                return;

            if (allHerbs.ContainsKey(herbName)) {
                // 处理重复药材，采用第一个遇到的剂量（逍遥散优先逻辑）
                var existingItem = allHerbs[herbName];
                duplicateWarnings.Add($"{herb.Name}在验方{templateName}中重复，已采用标准剂量：{existingItem.Quantity}{existingItem.Unit}");
            } else {
                // 创建新的处方项目
                var prescriptionItem = new PrescriptionItemModel {
                    Id = Guid.NewGuid(),
                    HerbId = herb.HerbId,
                    HerbName = herb.Name!,
                    Quantity = 10, // 默认剂量，实际应从验方模板中获取
                    Unit = herb.Unit ?? "g",
                    Usage = "水煎服" // 默认用法
                };

                allHerbs[herbName] = prescriptionItem;
            }
        }

        /// <summary>
        /// 智能重复药材检测和处理
        /// </summary>
        public PrescriptionDuplicateCheckResult DetectDuplicateHerbs(List<PrescriptionItemModel> items) {
            var result = new PrescriptionDuplicateCheckResult();
            var herbGroups = items.GroupBy(item => item.HerbName?.Trim().ToUpper()).ToList();

            foreach (var group in herbGroups.Where(g => g.Count() > 1)) {
                var duplicateItems = group.ToList();
                var herbName = group.Key;

                // 取第一个药材的剂量作为标准（按逍遥散优先的逻辑）
                var standardItem = duplicateItems.OrderBy(item => item.Id).First(); // 使用Id排序而不是Sequence
                var standardQuantity = standardItem.Quantity;

                // 记录重复警告信息
                var conflictingQuantities = duplicateItems
                    .Where(item => item.Quantity != standardQuantity)
                    .Select(item => $"{item.Quantity}{item.Unit}")
                    .ToList();

                if (conflictingQuantities.Any()) {
                    result.Warnings.Add($"{herbName}在多个验方中重复，剂量冲突：{string.Join(", ", conflictingQuantities)}，已采用标准剂量：{standardQuantity}{standardItem.Unit}");
                } else {
                    result.Warnings.Add($"{herbName}在多个验方中重复，剂量相同：{standardQuantity}{standardItem.Unit}");
                }

                result.DuplicateHerbs.Add(herbName ?? "");

                // 移除重复项，保留标准剂量的项
                items.RemoveAll(item => item.HerbName?.Trim().ToUpper() == herbName && item.Id != standardItem.Id);
            }

            result.HasDuplicates = result.DuplicateHerbs.Any();
            result.WarningMessage = string.Join("；", result.Warnings);

            return result;
        }

        /// <summary>
        /// 检查药材库存状态
        /// </summary>
        public async Task<HerbAvailabilityCheckResult> CheckHerbAvailabilityAsync(List<PrescriptionItemModel> items) {
            var result = new HerbAvailabilityCheckResult();
            var allHerbs = await _herbService.GetAllActiveHerbsAsync();
            var availableHerbNames = allHerbs.Select(h => h.Name?.Trim().ToUpper()).ToHashSet();

            foreach (var item in items) {
                var herbName = item.HerbName?.Trim().ToUpper();
                if (!string.IsNullOrEmpty(herbName) && !availableHerbNames.Contains(herbName)) {
                    result.MissingHerbs.Add(item.HerbName!);
                }
            }

            // 确定整体供应状态
            if (!result.MissingHerbs.Any()) {
                result.Status = DrugAvailabilityStatus.FullyAvailable;
            } else if (result.MissingHerbs.Count == items.Count) {
                result.Status = DrugAvailabilityStatus.FullyMissing;
            } else {
                result.Status = DrugAvailabilityStatus.PartiallyMissing;
            }

            return result;
        }

        /// <summary>
        /// 计算处方价格和重量
        /// </summary>
        public PrescriptionPriceCalculationResult CalculatePrescriptionPrice(List<PrescriptionItemModel> items, int dosageCount) {
            var result = new PrescriptionPriceCalculationResult();

            decimal singleDosePrice = 0;
            decimal totalWeight = 0;

            foreach (var item in items) {
                // 单帖价格 = 药材单价 × 用量 (注意：这里需要获取实际单价，暂时使用0)
                var itemPrice = 0 * item.Quantity; // 需要从药材信息中获取单价
                singleDosePrice += itemPrice;

                // 单帖重量
                totalWeight += item.Quantity;
            }

            result.SingleDosePrice = singleDosePrice;
            result.TotalPrice = singleDosePrice * dosageCount;
            result.TotalWeight = totalWeight * dosageCount;
            result.DosageCount = dosageCount;

            return result;
        }

        /// <summary>
        /// 生成处方智能建议
        /// </summary>
        public async Task<PrescriptionSuggestionResult> GeneratePrescriptionSuggestionsAsync(
            string diagnosis, List<string> symptoms, Guid? doctorId = null) {
            var result = new PrescriptionSuggestionResult();

            // 根据医生权限获取可见的验方模板
            List<FormulaTemplateDetailDto> allFormulas;
            if (doctorId.HasValue) {
                // 获取该医生可见的验方（包括共享验方和自己创建的验方）
                allFormulas = await _formulaTemplateService.GetVisibleFormulasForDoctorAsync(doctorId.Value);
            } else {
                // 获取所有活动状态的验方模板（管理员权限）
                allFormulas = await _formulaTemplateService.GetAllActiveFormulasAsync();
            }

            foreach (var formula in allFormulas) {
                // 基于验方名称和备注进行关键词匹配
                if (ContainsRelevantKeywords(formula, diagnosis, symptoms)) {
                    result.SuggestedFormulas.Add(formula.Name);
                }
            }

            // 生成用药建议
            if (symptoms.Contains("失眠") || diagnosis.Contains("不寐")) {
                result.SuggestedAdvice.Add("建议睡前30分钟温服");
                result.Precautions.Add("服药期间避免浓茶咖啡");
            }

            if (symptoms.Contains("腹泻") || diagnosis.Contains("泄泻")) {
                result.SuggestedAdvice.Add("温服，忌食生冷");
                result.Precautions.Add("腹泻严重时及时就医");
            }

            if (symptoms.Contains("感冒") || diagnosis.Contains("外感")) {
                result.SuggestedAdvice.Add("热服取汗");
                result.Precautions.Add("服药后避风寒，适当休息");
            }

            return result;
        }

        /// <summary>
        /// 检查验方是否包含相关关键词
        /// </summary>
        private bool ContainsRelevantKeywords(FormulaTemplateDetailDto formula, string diagnosis, List<string> symptoms) {
            var searchText = $"{formula.Name} {formula.Remark}".ToLower();

            // 诊断关键词匹配
            if (!string.IsNullOrEmpty(diagnosis) && searchText.Contains(diagnosis.ToLower())) {
                return true;
            }

            // 症状关键词匹配
            foreach (var symptom in symptoms) {
                if (!string.IsNullOrEmpty(symptom) && searchText.Contains(symptom.ToLower())) {
                    return true;
                }
            }

            return false;
        }
    }
}