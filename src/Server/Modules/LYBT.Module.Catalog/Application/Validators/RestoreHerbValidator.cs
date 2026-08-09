using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 恢复药材命令验证器。
/// </summary>
public class RestoreHerbValidator : AbstractValidator<RestoreEntityCommand<Herb, HerbDetailDto>>
{
    public RestoreHerbValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("药材ID不能为空");
    }
}
