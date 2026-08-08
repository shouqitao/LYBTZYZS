using FluentValidation;
using LYBT.Module.Herbs.Application.Commands;

namespace LYBT.Module.Herbs.Application.Validators;

/// <summary>
/// 批量启用药材命令验证器。
/// </summary>
public class BatchEnableHerbsValidator : AbstractValidator<BatchEnableHerbsCommand>
{
    public BatchEnableHerbsValidator()
    {
        RuleFor(x => x.Ids)
            .NotNull().WithMessage("药材ID列表不能为空")
            .NotEmpty().WithMessage("药材ID列表不能为空");
    }
}
