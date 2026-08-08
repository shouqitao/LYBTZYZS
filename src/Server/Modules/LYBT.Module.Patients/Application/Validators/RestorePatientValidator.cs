using FluentValidation;
using LYBT.Module.Patients.Application.Commands;

namespace LYBT.Module.Patients.Application.Validators;

/// <summary>
/// 恢复患者命令验证器。
/// </summary>
public class RestorePatientValidator : AbstractValidator<RestorePatientCommand>
{
    public RestorePatientValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("患者ID不能为空");
    }
}
