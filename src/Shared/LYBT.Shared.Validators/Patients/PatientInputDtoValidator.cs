using FluentValidation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Primitives.Validation;

namespace LYBT.Shared.Validators.Patients
{
    /// <summary>
    /// 患者输入DTO验证器
    /// BR-001: 实现验证点，支持批量导入（Epic #1934）
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

            // 4. IdNumber：必填，必须符合18位身份证格式 (T5-P2-24: 移除条件，成为真正的必填字段)
            RuleFor(x => x.IdNumber)
                .NotEmpty().WithMessage("身份证号不能为空")
                .Matches(ValidationConstants.IdCardRegex).WithMessage("身份证号格式不正确（应为18位）");

            // 6. PhoneNumber：必填，必须符合手机号格式
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("手机号不能为空")
                .Matches(ValidationConstants.PhoneRegex).WithMessage("手机号格式不正确")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }
    }
}
