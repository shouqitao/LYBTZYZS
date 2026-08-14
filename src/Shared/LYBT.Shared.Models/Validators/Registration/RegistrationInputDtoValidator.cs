using FluentValidation;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Shared.Models.Validators.Registration;

/// <summary>
/// 挂号输入DTO验证器（DTO 级——审计补全：原缺失，仅命令级 CreateRegistrationValidator 存在）。
/// 规则与 RegistrationInputDto DataAnnotations（[Required]/[StringLength]/[Range]）及
/// 命令级 CreateRegistrationValidator 对齐，另补 Source 枚举验证（任务书点名）。
/// </summary>
public class RegistrationInputDtoValidator : AbstractValidator<RegistrationInputDto>
{
    public RegistrationInputDtoValidator()
    {
        // 患者：必填（Guid.Empty 视为未提供）
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("患者不能为空");

        // 患者姓名：必填 + 长度
        RuleFor(x => x.PatientName)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(100).WithMessage("患者姓名长度不能超过100个字符");

        // 医生：必填（Guid.Empty 视为未提供）
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("医生不能为空");

        // 医生姓名：必填 + 长度
        RuleFor(x => x.DoctorName)
            .NotEmpty().WithMessage("医生姓名不能为空")
            .MaximumLength(100).WithMessage("医生姓名长度不能超过100个字符");

        // 挂号来源：枚举范围验证
        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("挂号来源值无效");

        // 挂号费：非负
        RuleFor(x => x.RegistrationFee)
            .GreaterThanOrEqualTo(0).WithMessage("挂号费不能为负数");

        // 备注：有值时长度校验
        RuleFor(x => x.Remark)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Remark));
    }
}
