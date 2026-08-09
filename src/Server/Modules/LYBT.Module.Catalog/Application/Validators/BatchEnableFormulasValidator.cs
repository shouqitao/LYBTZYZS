using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 批量启用药方命令验证器。
/// </summary>
public class BatchEnableFormulasValidator : AbstractValidator<BatchEnableFormulasCommand>
{
    public BatchEnableFormulasValidator()
    {
        RuleFor(x => x.Ids)
            .NotNull().WithMessage("验方ID列表不能为空")
            .NotEmpty().WithMessage("验方ID列表不能为空");
    }
}
