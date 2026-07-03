using FluentValidation;
using LYBT.Module.Formulas.Application.Commands;

namespace LYBT.Module.Formula.Application.Validators;

public class FormulaBatchImportCommandValidator : AbstractValidator<BatchImportFormulasCommand>
{
    public FormulaBatchImportCommandValidator()
    {
        RuleFor(x => x.Formulas)
            .NotEmpty().WithMessage("验方列表不能为空");

        RuleForEach(x => x.Formulas).ChildRules(formula =>
        {
            formula.RuleFor(f => f.Name)
                .NotEmpty().WithMessage("验方名称不能为空");

            formula.RuleFor(f => f.Effect)
                .NotEmpty().WithMessage("功效不能为空");

            formula.RuleFor(f => f.Usage)
                .NotEmpty().WithMessage("用法不能为空");
        });
    }
}
