using FluentValidation;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 批量导入验方项校验（P1-5 同 Herb，补 <> 校验）
/// </summary>
public class FormulaImportItemDtoValidator : AbstractValidator<FormulaImportItemDto>
{
    public FormulaImportItemDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("验方名称不能为空")
            .MaximumLength(100).WithMessage("验方名称长度不能超过100个字符")
            .Matches(@"^[^<>]*$").WithMessage("验方名称不能包含非法字符 < >");

        RuleFor(x => x.Category)
            .Matches(@"^[^<>]*$").WithMessage("分类不能包含非法字符 < >")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleForEach(x => x.Herbs).ChildRules(herb =>
        {
            herb.RuleFor(h => h.HerbName)
                .NotEmpty().WithMessage("药材名称不能为空")
                .Matches(@"^[^<>]*$").WithMessage("药材名称不能包含非法字符 < >");
        });
    }
}
