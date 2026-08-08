using FluentValidation;
using LYBT.Module.Herbs.Application.Commands;

namespace LYBT.Module.Herbs.Application.Validators;

/// <summary>
/// 恢复药材命令验证器。
/// </summary>
public class RestoreHerbValidator : AbstractValidator<RestoreHerbCommand>
{
    public RestoreHerbValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("药材ID不能为空");
    }
}
