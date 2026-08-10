using FluentValidation;
using LYBT.Entities.Common;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 目录实体恢复命令验证器（合并 RestoreHerbValidator/RestoreFormulaValidator 孪生）。
/// 仅校验 Id 非空；实体显示名由构造参数注入（消息文案与原实现等价）。
/// </summary>
/// <typeparam name="TEntity">目录实体（Herb / Formula）</typeparam>
/// <typeparam name="TDetail">详情 DTO</typeparam>
public class RestoreEntityValidator<TEntity, TDetail> : AbstractValidator<RestoreEntityCommand<TEntity, TDetail>>
    where TEntity : BaseEntity
{
    public RestoreEntityValidator(string entityDisplayName)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage($"{entityDisplayName}ID不能为空");
    }
}
