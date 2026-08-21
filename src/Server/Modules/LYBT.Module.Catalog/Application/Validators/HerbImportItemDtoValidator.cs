using FluentValidation;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 批量导入药材项校验（P1-5：单体/批量校验不一致修复，批量路径补 <> 非法字符校验）
/// </summary>
public class HerbImportItemDtoValidator : AbstractValidator<HerbInputDto>
{
    public HerbImportItemDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("药材名称不能为空")
            .MaximumLength(100).WithMessage("药材名称长度不能超过100个字符")
            .Matches(@"^[^<>]*$").WithMessage("药材名称不能包含非法字符 < >");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("单位不能为空")
            .Matches(@"^[^<>]*$").WithMessage("单位不能包含非法字符 < >")
            .When(x => !string.IsNullOrEmpty(x.Unit));

        RuleFor(x => x.Category)
            .Matches(@"^[^<>]*$").WithMessage("分类不能包含非法字符 < >")
            .When(x => !string.IsNullOrEmpty(x.Category));
    }
}
