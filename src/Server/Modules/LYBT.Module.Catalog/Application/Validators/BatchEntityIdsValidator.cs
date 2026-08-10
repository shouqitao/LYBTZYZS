using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 目录批量状态命令验证器（合并 BatchEnable/DisableHerbsValidator 与 BatchEnable/DisableFormulasValidator 四个孪生）。
/// 仅校验 Ids 非空；实体显示名由构造参数注入（消息文案与原实现等价）。
/// </summary>
/// <typeparam name="TCommand">批量状态命令（实现 <see cref="IBatchIdsCommand"/>）</typeparam>
public class BatchEntityIdsValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : IBatchIdsCommand
{
    public BatchEntityIdsValidator(string entityDisplayName)
    {
        RuleFor(x => x.Ids)
            .NotNull().WithMessage($"{entityDisplayName}ID列表不能为空")
            .NotEmpty().WithMessage($"{entityDisplayName}ID列表不能为空");
    }
}
