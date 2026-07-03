using FluentValidation;
using LYBT.Module.Registration.Application.Commands;

namespace LYBT.Module.Registration.Application.Validators;

public class QuickVisitCommandValidator : AbstractValidator<QuickVisitCommand>
{
    public QuickVisitCommandValidator()
    {
        RuleFor(x => x.Input.PatientId)
            .NotEmpty().WithMessage("患者ID不能为空");

        RuleFor(x => x.Input.PatientName)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(100).WithMessage("患者姓名长度不能超过100个字符");
    }
}
