using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 恢复验方命令验证器。
/// </summary>
public class RestoreFormulaValidator : AbstractValidator<RestoreEntityCommand<Formula, FormulaDetailDto>>
{
    public RestoreFormulaValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("验方ID不能为空");
    }
}
