using FluentValidation;
using LYBT.Module.Herbs.Application.Commands;

namespace LYBT.Module.Herbs.Application.Validators;

/// <summary>
/// 切换药材状态命令验证器。
/// </summary>
public class ToggleHerbStatusValidator : AbstractValidator<ToggleHerbStatusCommand>
{
    public ToggleHerbStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("药材ID不能为空");
    }
}
