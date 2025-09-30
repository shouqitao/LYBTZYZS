namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方验证器 - UltraThink架构实现
    /// 负责处方的各种验证逻辑
    /// </summary>
    public class PrescriptionValidator
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
            var result = new ValidationResult();

            if (items == null || !items.Any())
            {
                result.AddError("处方至少需要包含一味药材");
                return result;
            }

            var itemList = items.ToList();

            // 检查重复药材
            var duplicateHerbs = itemList
                .GroupBy(i => i.HerbId)
                .Where(g => g.Count() > 1)
                .Select(g => g.First().HerbName)
                .ToList();

            if (duplicateHerbs.Any())
            {
                result.AddError($"处方中存在重复药材：{string.Join("、", duplicateHerbs)}");
            }

            // 验证每个项目
            foreach (var item in itemList)
            {
                var itemValidation = ValidatePrescriptionItem(item);
                result.Merge(itemValidation);
            }

            return result;
        }

        /// <summary>
        /// 验证单个处方项目
        /// </summary>
        public ValidationResult ValidatePrescriptionItem(PrescriptionItemViewModel item)
        {
            var result = new ValidationResult();

            if (item == null)
            {
                result.AddError("处方项目不能为空");
                return result;
            }

            if (item.HerbId == Guid.Empty)
            {
                result.AddError("药材不能为空");
            }

            if (string.IsNullOrWhiteSpace(item.HerbName))
            {
                result.AddError("药材名称不能为空");
            }

            if (item.Dosage <= 0)
            {
                result.AddError($"{item.HerbName} 用量必须大于0");
            }

            if (item.Dosage > 500)
            {
                result.AddWarning($"{item.HerbName} 用量较大（{item.Dosage}{item.Unit}），请确认是否正确");
            }

            if (string.IsNullOrWhiteSpace(item.Unit))
            {
                result.AddError($"{item.HerbName} 单位不能为空");
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

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public bool IsValid => !Errors.Any();
        public bool HasWarnings => Warnings.Any();

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
            }
        }

        public void Merge(ValidationResult other)
        {
            if (other != null)
            {
                Errors.AddRange(other.Errors);
                Warnings.AddRange(other.Warnings);
            }
        }

        public string GetErrorSummary()
        {
            return string.Join("; ", Errors);
        }

        public string GetWarningSummary()
        {
            return string.Join("; ", Warnings);
        }
    }

    /// <summary>
    /// 药材配伍禁忌
    /// </summary>
    public record HerbContraindication(string Herb1, string Herb2);
}
