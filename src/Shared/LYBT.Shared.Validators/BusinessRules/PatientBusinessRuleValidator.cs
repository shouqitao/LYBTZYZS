using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Validators.BusinessRules
{
    /// <summary>
    /// 患者业务规则验证器
    /// Phase 3 Task 3.4: 统一业务规则验证框架
    /// 整合PatientService中的业务规则验证逻辑
    /// 注意：Shared层不能直接引用Entity，只验证输入DTO
    /// </summary>
    public class PatientBusinessRuleValidator : BaseBusinessOperationValidator<PatientInputDto>
    {
        public override string ValidatorName => "PatientBusinessRuleValidator";

        public override string Description => "患者业务规则验证器，处理患者数据唯一性、年龄验证等业务规则";

        public PatientBusinessRuleValidator(ILogger<PatientBusinessRuleValidator> logger) : base(logger) { }

        #region 操作验证

        /// <summary>
        /// 验证患者输入DTO的业务规则
        /// </summary>
        public override Task<ValidationResult> ValidateAsync(PatientInputDto input, ValidationContext? context = null)
        {
            if (input == null)
            {
                return Task.FromResult(Failure("患者输入数据不能为空"));
            }

            var results = new List<ValidationResult>
            {
                ValidateBasicInfoAsync(input),
                ValidateContactInfoAsync(input),
                ValidateAgeAsync(input)
            };

            foreach (var result in results)
            {
                if (!result.IsValid)
                {
                    return Task.FromResult(result);
                }
            }

            return Task.FromResult(Success());
        }

        /// <summary>
        /// 验证基本信息
        /// </summary>
        private ValidationResult ValidateBasicInfoAsync(PatientInputDto input)
        {
            // 姓名验证
            if (string.IsNullOrWhiteSpace(input.Name))
            {
                return Failure("患者姓名不能为空");
            }

            if (input.Name.Length > 50)
            {
                return Failure("患者姓名长度不能超过50个字符");
            }

            // 性别验证
            if (input.Gender == Gender.Unknown)
            {
                return Failure("患者性别不能为空");
            }

            return Success();
        }

        /// <summary>
        /// 验证联系信息
        /// </summary>
        private ValidationResult ValidateContactInfoAsync(PatientInputDto input)
        {
            // 手机号验证（如果不为空）
            if (!string.IsNullOrEmpty(input.PhoneNumber))
            {
                if (!IsValidPhoneNumber(input.PhoneNumber))
                {
                    return Failure("手机号格式不正确");
                }

                if (input.PhoneNumber.Length > 20)
                {
                    return Failure("手机号长度不能超过20个字符");
                }
            }

            // 地址验证
            if (input.Address != null && input.Address.Length > 200)
            {
                return Failure("地址长度不能超过200个字符");
            }

            // 患者没有Remark字段，只有AllergyHistory和MedicalHistory
            if (input.AllergyHistory != null && input.AllergyHistory.Length > 500)
            {
                return Failure("过敏史长度不能超过500个字符");
            }

            return Success();
        }

        /// <summary>
        /// 验证年龄信息
        /// </summary>
        private ValidationResult ValidateAgeAsync(PatientInputDto input)
        {
            // 出生日期验证（如果不为空）
            if (input.BirthDate.HasValue)
            {
                var birthDate = input.BirthDate.Value;
                var today = DateTime.Today;
                var age = today.Year - birthDate.Year;

                // 检查日期合理性
                if (birthDate > today)
                {
                    return Failure("出生日期不能晚于当前日期");
                }

                // 检查年龄范围（假设0-150岁）
                if (age < 0 || age > 150)
                {
                    return Failure("年龄范围不合理，请检查出生日期");
                }

                // Issue #2240: 移除Age一致性验证，因为Age不再是输入属性
            }

            return Success();
        }

        #endregion

        #region 辅助方法

  
        /// <summary>
        /// 验证手机号格式
        /// </summary>
        private static bool IsValidPhoneNumber(string phone)
        {
            // 简单的手机号验证：1开头，11位数字
            return phone.Length == 11 && phone.StartsWith("1") && phone.All(char.IsDigit);
        }

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}