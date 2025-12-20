using FluentValidation;
using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Shared.Validators.Patients
{
    /// <summary>
    /// 患者输入DTO验证器
    /// BR-001: 实现8个验证点，支持批量导入（Epic #1934）
    /// </summary>
    public class PatientInputDtoValidator : AbstractValidator<PatientInputDto>
    {
        public PatientInputDtoValidator()
        {
            // 1. 必填字段：Name（姓名）
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("姓名不能为空")
                .MaximumLength(ValidationConstants.NameMaxLength)
                .WithMessage($"姓名长度不能超过{ValidationConstants.NameMaxLength}个字符");

            // 2. Gender：枚举范围验证
            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("性别值无效");

            // 3. BirthDate：如果提供，必须≤当前日期
            RuleFor(x => x.BirthDate)
                .LessThanOrEqualTo(DateTime.Today).WithMessage("出生日期不能晚于当前日期")
                .When(x => x.BirthDate.HasValue);

            // Issue #2240: 移除Age验证，因为Age不再是输入属性（改为从BirthDate计算）

            // 4. IdNumber：如果提供，必须符合18位身份证格式
            RuleFor(x => x.IdNumber)
                .Matches(ValidationConstants.IdCardRegex).WithMessage("身份证号格式不正确（应为18位）")
                .When(x => !string.IsNullOrEmpty(x.IdNumber));

            // 6. PhoneNumber：如果提供，必须符合手机号格式
            RuleFor(x => x.PhoneNumber)
                .Matches(ValidationConstants.PhoneRegex).WithMessage("手机号格式不正确")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            // 7. Address：长度限制
            RuleFor(x => x.Address)
                .MaximumLength(ValidationConstants.AddressMaxLength)
                .WithMessage($"地址长度不能超过{ValidationConstants.AddressMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Address));

            // 8. AllergyHistory：长度限制
            RuleFor(x => x.AllergyHistory)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"过敏史长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.AllergyHistory));

            // Epic #1934新增：MedicalHistory（既往病史）验证
            RuleFor(x => x.MedicalHistory)
                .MaximumLength(ValidationConstants.LongRemarkMaxLength)
                .WithMessage($"既往病史长度不能超过{ValidationConstants.LongRemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.MedicalHistory));

            // 紧急联系人信息验证
            RuleFor(x => x.EmergencyContactName)
                .MaximumLength(ValidationConstants.NameMaxLength)
                .WithMessage($"紧急联系人姓名长度不能超过{ValidationConstants.NameMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.EmergencyContactName));

            RuleFor(x => x.EmergencyContactPhone)
                .MaximumLength(ValidationConstants.PhoneMaxLength)
                .WithMessage($"紧急联系人电话长度不能超过{ValidationConstants.PhoneMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.EmergencyContactPhone));

            RuleFor(x => x.EmergencyContactRelation)
                .MaximumLength(ValidationConstants.NameMaxLength)
                .WithMessage($"紧急联系人关系长度不能超过{ValidationConstants.NameMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.EmergencyContactRelation));
        }
    }
}
