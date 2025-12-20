using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Validators.BusinessRules
{
    /// <summary>
    /// 处方业务规则验证器
    /// Phase 3 Task 3.4: 统一业务规则验证框架
    /// 整合PrescriptionService中的业务规则验证逻辑
    /// 注意：Shared层不能直接引用Entity，只验证输入DTO
    /// </summary>
    public class PrescriptionBusinessRuleValidator : BaseBusinessOperationValidator<PrescriptionInputDto>
    {
        public override string ValidatorName => "PrescriptionBusinessRuleValidator";

        public override string Description => "处方业务规则验证器，处理处方状态、关联验证、药材验证等业务规则";

        public PrescriptionBusinessRuleValidator(ILogger<PrescriptionBusinessRuleValidator> logger) : base(logger) { }

        #region 操作验证

        /// <summary>
        /// 验证处方输入DTO的业务规则
        /// </summary>
        public override async Task<ValidationResult> ValidateAsync(PrescriptionInputDto input, ValidationContext? context = null)
        {
            if (input == null)
            {
                return Failure("处方输入数据不能为空");
            }

            var results = new List<ValidationResult>
            {
                await ValidateBasicInputAsync(input),
                await ValidateMedicalCaseAsync(input, context),
                await ValidatePrescriptionItemsAsync(input)
            };

            foreach (var result in results)
            {
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return Success();
        }

        /// <summary>
        /// 验证基本输入信息
        /// OpenSpec: simplify-medicalcase-dataflow - Indication/FormulaSource已删除
        /// </summary>
        private Task<ValidationResult> ValidateBasicInputAsync(PrescriptionInputDto input)
        {
            // Indication已删除 - 打印时从Consultation.TCMDiagnosis获取
            // FormulaSource已删除 - 与ReferencedFormulas功能重复

            // 引用验方验证
            if (!string.IsNullOrEmpty(input.ReferencedFormulas) && input.ReferencedFormulas.Length > 500)
            {
                return Task.FromResult(Failure("引用验方长度不能超过500个字符"));
            }

            // 医嘱验证
            if (!string.IsNullOrEmpty(input.Advice) && input.Advice.Length > 500)
            {
                return Task.FromResult(Failure("医嘱长度不能超过500个字符"));
            }

            // 备注验证
            if (!string.IsNullOrEmpty(input.Remark) && input.Remark.Length > 500)
            {
                return Task.FromResult(Failure("备注长度不能超过500个字符"));
            }

            // 剂数验证
            if (input.DosageCount <= 0)
            {
                return Task.FromResult(Failure("处方剂数必须大于0"));
            }

            // 剂数上限（防止不合理的大量处方）
            if (input.DosageCount > 365)
            {
                return Task.FromResult(Failure("处方剂数不能超过365天（一年）"));
            }

            return Task.FromResult(Success());
        }

        /// <summary>
        /// 验证医疗案例关联
        /// </summary>
        private Task<ValidationResult> ValidateMedicalCaseAsync(PrescriptionInputDto input, ValidationContext? context)
        {
            if (input.MedicalCaseId == Guid.Empty)
            {
                return Task.FromResult(Failure("处方必须关联医疗案例"));
            }

            // TODO: 验证医疗案例是否存在且状态有效
            // 需要在Service层实现，因为Shared层无法访问Repository

            return Task.FromResult(Success());
        }

        /// <summary>
        /// 验证处方药材项目
        /// </summary>
        private Task<ValidationResult> ValidatePrescriptionItemsAsync(PrescriptionInputDto input)
        {
            if (input.Items == null || !input.Items.Any())
            {
                return Task.FromResult(Failure("处方必须包含至少一味药材"));
            }

            // 验证项目数量上限
            if (input.Items.Count > 50)
            {
                return Task.FromResult(Failure("处方药材数量不能超过50味"));
            }

            // 验证每个药材项目
            for (int i = 0; i < input.Items.Count; i++)
            {
                var item = input.Items[i];
                var prefix = $"第{i + 1}味药材";

                if (item.HerbId == Guid.Empty)
                {
                    return Task.FromResult(Failure($"{prefix}必须关联有效药材"));
                }

                if (item.Dosage <= 0)
                {
                    return Task.FromResult(Failure($"{prefix}数量必须大于0"));
                }

                if (item.Dosage > 1000) // 假设合理上限
                {
                    return Task.FromResult(Failure($"{prefix}数量不能超过1000"));
                }

                if (string.IsNullOrWhiteSpace(item.Unit))
                {
                    return Task.FromResult(Failure($"{prefix}单位不能为空"));
                }

                if (item.Unit.Length > 20)
                {
                    return Task.FromResult(Failure($"{prefix}单位长度不能超过20个字符"));
                }

                if (!string.IsNullOrEmpty(item.Remark) && item.Remark.Length > 200)
                {
                    return Task.FromResult(Failure($"{prefix}备注长度不能超过200个字符"));
                }
            }

            return Task.FromResult(Success());
        }

        /// <summary>
        /// 验证处方访问权限（Read操作）
        /// Phase 3 Task 3.5: 验证处方资源存在性和访问权限
        /// </summary>
        public Task<ValidationResult> ValidatePrescriptionAccessAsync(Guid prescriptionId, ValidationContext? context = null)
        {
            if (prescriptionId == Guid.Empty)
            {
                return Task.FromResult(Failure("处方ID不能为空"));
            }

            // 基本的业务规则检查
            // 更复杂的权限检查需要在Service层进行，因为需要访问数据库
            return Task.FromResult(Success());
        }

        /// <summary>
        /// 验证MedicalCase关联性
        /// Phase 3 Task 3.5: 验证MedicalCase存在性和关联关系
        /// </summary>
        public Task<ValidationResult> ValidateMedicalCaseAssociationAsync(Guid medicalCaseId, ValidationContext? context = null)
        {
            if (medicalCaseId == Guid.Empty)
            {
                return Task.FromResult(Failure("MedicalCase ID不能为空"));
            }

            // 基本的业务规则检查
            // 实际的MedicalCase存在性检查需要在Service层进行
            return Task.FromResult(Success());
        }

        /// <summary>
        /// 验证患者相关操作的访问权限
        /// Phase 3 Task 3.5: 验证患者相关操作的业务规则
        /// </summary>
        public Task<ValidationResult> ValidatePatientAccessAsync(Guid patientId, ValidationContext? context = null)
        {
            if (patientId == Guid.Empty)
            {
                return Task.FromResult(Failure("患者ID不能为空"));
            }

            // 基本的业务规则检查
            // 实际的患者存在性检查需要在Service层进行
            return Task.FromResult(Success());
        }

        /// <summary>
        /// 验证搜索参数的业务规则
        /// Phase 3 Task 3.5: 验证处方搜索参数的合理性
        /// </summary>
        public Task<ValidationResult> ValidateSearchParametersAsync(string? patientName, string? symptomKeyword, ValidationContext? context = null)
        {
            // 验证患者姓名参数长度
            if (!string.IsNullOrEmpty(patientName) && patientName.Length > 50)
            {
                return Task.FromResult(Failure("患者姓名搜索关键字长度不能超过50个字符"));
            }

            // 验证症状关键字参数长度
            if (!string.IsNullOrEmpty(symptomKeyword) && symptomKeyword.Length > 100)
            {
                return Task.FromResult(Failure("症状/诊断关键字长度不能超过100个字符"));
            }

            // 验证特殊字符（防止SQL注入等）
            if (!string.IsNullOrEmpty(patientName) && ContainsInvalidCharacters(patientName))
            {
                return Task.FromResult(Failure("患者姓名搜索关键字包含非法字符"));
            }

            if (!string.IsNullOrEmpty(symptomKeyword) && ContainsInvalidCharacters(symptomKeyword))
            {
                return Task.FromResult(Failure("症状/诊断关键字包含非法字符"));
            }

            return Task.FromResult(Success());
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证处方编号格式（如果不为空）
        /// </summary>
        private static bool IsValidPrescriptionNumber(string? prescriptionNumber)
        {
            if (string.IsNullOrEmpty(prescriptionNumber))
                return true; // 空值是有效的（自动生成）

            // 处方编号格式：RX-YYYYMMDD-NNNN
            return System.Text.RegularExpressions.Regex.IsMatch(
                prescriptionNumber,
                @"^RX-\d{8}-\d{4}$"
            );
        }

        /// <summary>
        /// 检查输入是否包含非法字符
        /// Phase 3 Task 3.5: 防止SQL注入、XSS等安全漏洞
        /// </summary>
        private static bool ContainsInvalidCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // 检查SQL注入相关字符
            var sqlInjectionChars = new[] { "'", "\"", ";", "--", "/*", "*/", "xp_", "sp_" };
            foreach (var chars in sqlInjectionChars)
            {
                if (input.Contains(chars, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // 检查XSS相关字符
            var xssChars = new[] { "<", ">", "&", "javascript:", "vbscript:", "onload=", "onerror=" };
            foreach (var chars in xssChars)
            {
                if (input.Contains(chars, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // 检查控制字符（除了常见的空白字符）
            foreach (char c in input)
            {
                if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t')
                    return true;
            }

            return false;
        }

        #endregion
    }
}
