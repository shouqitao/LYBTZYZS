using LYBT.Shared.Components;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方验证器 - UltraThink架构实现
    /// Issue #1153: 继承HerbValidatorBase共享基类
    /// </summary>
    public class PrescriptionValidator : HerbValidatorBase<PrescriptionItemViewModel>
    {
        #region 基础验证

        /// <summary>
        /// 验证处方基本信息
        /// </summary>
        public ValidationResult ValidateBasicInfo(string prescriptionNumber, Guid patientId, string doctorName)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(prescriptionNumber))
            {
                result.AddError("处方编号不能为空");
            }

            if (patientId == Guid.Empty)
            {
                result.AddError("患者信息不能为空");
            }

            if (string.IsNullOrWhiteSpace(doctorName))
            {
                result.AddError("医生信息不能为空");
            }

            return result;
        }

        /// <summary>
        /// 验证处方项目列表
        /// </summary>
        public ValidationResult ValidatePrescriptionItems(IEnumerable<PrescriptionItemViewModel> items)
        {
            // 使用基类的ValidateHerbList方法（包含重复检测和必填项验证）
            return ValidateHerbList(items, "处方");
        }

        /// <summary>
        /// 验证单个处方项目
        /// </summary>
        public ValidationResult ValidatePrescriptionItem(PrescriptionItemViewModel item)
        {
            // 使用基类的ValidateRequiredFields方法
            var result = ValidateRequiredFields(item);

            // 添加剂量警告（使用基类方法）
            var warning = GetDosageWarning(item, 0.1m, 500m);
            if (!string.IsNullOrWhiteSpace(warning))
            {
                result.AddWarning(warning);
            }

            return result;
        }

        #endregion

        #region 药材相互作用验证

        /// <summary>
        /// 验证药材相互作用
        /// </summary>
        public ValidationResult ValidateHerbInteractions(IEnumerable<PrescriptionItemViewModel> items)
        {
            var result = new ValidationResult();

            if (items == null || !items.Any())
            {
                return result;
            }

            var herbNames = items.Select(i => i.HerbName).ToList();

            // 简化的配伍禁忌检查（实际应该基于药材数据库）
            var knownContraindications = GetKnownContraindications();

            foreach (var contraindication in knownContraindications)
            {
                if (herbNames.Contains(contraindication.Herb1) && herbNames.Contains(contraindication.Herb2))
                {
                    result.AddWarning($"注意：{contraindication.Herb1} 与 {contraindication.Herb2} 可能存在配伍禁忌");
                }
            }

            return result;
        }

        #endregion

        #region 用量安全验证

        /// <summary>
        /// 验证用量安全性
        /// </summary>
        public ValidationResult ValidateDosageSafety(IEnumerable<PrescriptionItemViewModel> items)
        {
            var result = new ValidationResult();

            if (items == null || !items.Any())
            {
                return result;
            }

            var calculator = new PrescriptionCalculator();
            var analysis = calculator.AnalyzeDosageDistribution(items);
            var warnings = calculator.ValidateDosageReasonableness(items);

            foreach (var warning in warnings)
            {
                result.AddWarning(warning);
            }

            // 检查处方总剂数
            if (analysis.TotalItems > 20)
            {
                result.AddWarning($"处方药味较多（{analysis.TotalItems}味），请确认是否合理");
            }

            // 检查用量分布
            if (analysis.StandardDeviation > 50)
            {
                result.AddWarning("处方各味药用量差异较大，请确认配比是否合理");
            }

            return result;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取已知的配伍禁忌
        /// </summary>
        private List<HerbContraindication> GetKnownContraindications()
        {
            // 简化实现，实际应该从数据库或配置文件读取
            return new List<HerbContraindication>
            {
                new("甘草", "甘遂"),
                new("甘草", "大戟"),
                new("甘草", "芫花"),
                new("乌头", "半夏"),
                new("乌头", "瓜蒌"),
                new("藜芦", "人参"),
                new("藜芦", "沙参")
            };
        }

        #endregion
    }

    // ValidationResult类已经在HerbValidatorBase基类中定义，无需重复定义

    /// <summary>
    /// 药材配伍禁忌
    /// </summary>
    public record HerbContraindication(string Herb1, string Herb2);
}
