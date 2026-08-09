using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 切换药材状态命令验证器。
/// </summary>
public class ToggleHerbStatusValidator : AbstractValidator<ToggleEntityStatusCommand<Herb, HerbDetailDto>>
{
    public ToggleHerbStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("药材ID不能为空");
    }
}
