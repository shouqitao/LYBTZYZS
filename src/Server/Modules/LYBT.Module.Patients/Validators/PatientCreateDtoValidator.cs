using FluentValidation;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Validators
{
    /// <summary>
    /// 患者创建DTO验证器
    /// </summary>
    public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
    {
        public PatientCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("姓名不能为空")
                .MaximumLength(50).WithMessage("姓名长度不能超过50个字符");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.IdNumber)
                .Matches(@"^\d{15}|\d{18}|\d{17}[xX]$").WithMessage("身份证号格式不正确")
                .When(x => !string.IsNullOrEmpty(x.IdNumber));
        }
    }
}
