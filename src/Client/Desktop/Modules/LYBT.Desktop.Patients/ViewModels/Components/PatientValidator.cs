using FluentValidation;
using FluentValidation.Results;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels.Components
{
    /// <summary>
    /// 患者验证器 - 组件化架构
    /// 集成FluentValidation Validators提供组件级验证接口
    /// Epic #1773 Task 4: Patients模块组件化改造
    /// </summary>
    public class PatientValidator : IPatientValidator
    {
        private readonly IValidator<PatientInputDto> _patientInputValidator;
        private readonly ILogger<PatientValidator> _logger;

        public PatientValidator(
            IValidator<PatientInputDto> patientInputValidator,
            ILogger<PatientValidator> logger)
        {
            _patientInputValidator = patientInputValidator ?? throw new ArgumentNullException(nameof(patientInputValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 验证患者输入DTO
        /// </summary>
        public async Task<ValidationResult> ValidatePatientInputAsync(PatientInputDto inputDto)
        {
            if (inputDto == null)
            {
                _logger.LogWarning("患者输入DTO为空");
                return new ValidationResult(new[] { new ValidationFailure("Patient", "患者数据为空") });
            }

            _logger.LogDebug("开始验证患者输入: {PatientName}", inputDto.Name);
            var result = await _patientInputValidator.ValidateAsync(inputDto);

            if (!result.IsValid)
            {
                _logger.LogWarning("患者输入验证失败，错误数量: {ErrorCount}", result.Errors.Count);
            }
            else
            {
                _logger.LogDebug("患者输入验证通过");
            }

            return result;
        }

        /// <summary>
        /// 验证患者基本信息
        /// </summary>
        public ValidationResult ValidateBasicInfo(string name, string? phoneNumber)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add(new ValidationFailure("Name", "患者姓名不能为空"));
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                // 简单的手机号验证
                if (phoneNumber.Length != 11 || !phoneNumber.All(char.IsDigit))
                {
                    result.Errors.Add(new ValidationFailure("PhoneNumber", "手机号格式不正确"));
                }
            }

            return result;
        }

        /// <summary>
        /// 验证身份证号
        /// </summary>
        public ValidationResult ValidateIdNumber(string? idNumber)
        {
            var result = new ValidationResult();

            if (!string.IsNullOrWhiteSpace(idNumber))
            {
                // 简单的身份证号验证（18位）
                if (idNumber.Length != 18)
                {
                    result.Errors.Add(new ValidationFailure("IdNumber", "身份证号长度必须为18位"));
                }
                else
                {
                    // 验证前17位是数字，最后一位是数字或X
                    bool isValid = idNumber.Take(17).All(char.IsDigit) &&
                                   (char.IsDigit(idNumber[17]) || idNumber[17] == 'X' || idNumber[17] == 'x');

                    if (!isValid)
                    {
                        result.Errors.Add(new ValidationFailure("IdNumber", "身份证号格式不正确"));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 验证年龄
        /// </summary>
        public ValidationResult ValidateAge(int? age)
        {
            var result = new ValidationResult();

            if (age.HasValue)
            {
                if (age.Value < 0 || age.Value > 150)
                {
                    result.Errors.Add(new ValidationFailure("Age", "年龄必须在0-150之间"));
                }
            }

            return result;
        }

        /// <summary>
        /// 验证紧急联系人信息
        /// </summary>
        public ValidationResult ValidateEmergencyContact(string? contactName, string? contactPhone)
        {
            var result = new ValidationResult();

            // 如果提供了联系人姓名，则联系电话也应该提供
            if (!string.IsNullOrWhiteSpace(contactName) && string.IsNullOrWhiteSpace(contactPhone))
            {
                result.Errors.Add(new ValidationFailure("EmergencyContactPhone", "请提供紧急联系人电话"));
            }

            // 如果提供了联系电话，验证格式
            if (!string.IsNullOrWhiteSpace(contactPhone))
            {
                if (contactPhone.Length != 11 || !contactPhone.All(char.IsDigit))
                {
                    result.Errors.Add(new ValidationFailure("EmergencyContactPhone", "紧急联系人电话格式不正确"));
                }
            }

            return result;
        }

        /// <summary>
        /// 检查验证结果是否有效
        /// </summary>
        public bool IsValid(ValidationResult result, out string errorMessage)
        {
            if (result.IsValid)
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            return false;
        }

        /// <summary>
        /// 从患者DTO转换为输入DTO（用于验证）
        /// </summary>
        // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由服务端管理
        public PatientInputDto ConvertToInputDto(PatientDetailDto patient)
        {
            return new PatientInputDto
            {
                Id = patient.Id,
                Name = patient.Name,
                Gender = patient.Gender,
                BirthDate = patient.BirthDate,
                // Issue #2240: Age不再是PatientInputDto的属性，仅BirthDate为输入
                IdNumber = patient.IdNumber,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                MaritalStatus = patient.MaritalStatus,
                IdType = patient.IdType,
                BloodType = patient.BloodType,
                AllergyHistory = patient.AllergyHistory,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                EmergencyContactRelation = patient.EmergencyContactRelation
            };
        }
    }
}
