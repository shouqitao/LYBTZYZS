using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Catalog.Application.Validators;

public class HerbBatchImportCommandValidator : AbstractValidator<BatchImportHerbsCommand>
{
    public HerbBatchImportCommandValidator()
    {
        RuleFor(x => x.Herbs)
            .NotEmpty().WithMessage("药材列表不能为空");

        RuleForEach(x => x.Herbs).ChildRules(herb =>
        {
            herb.RuleFor(h => h.Name)
                .NotEmpty().WithMessage("药材名称不能为空");

            herb.RuleFor(h => h.Price)
                .GreaterThan(0).WithMessage("单价必须大于0");
        });
    }
}
