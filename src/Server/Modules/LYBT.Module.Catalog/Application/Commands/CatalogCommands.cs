using LYBT.Entities.Common;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 创建目录实体命令（A-31-C3b 合并 CreateHerb/CreateFormula 同构命令）。
/// </summary>
public record CreateEntityCommand<TInput, TDetail>(TInput Input, Guid CurrentUserId) : IRequest<Result<TDetail>>;

/// <summary>
/// 更新目录实体命令（合并 UpdateHerb/UpdateFormula）。
/// </summary>
public record UpdateEntityCommand<TInput, TDetail>(Guid Id, TInput Input, Guid CurrentUserId) : IRequest<Result<TDetail>>;

/// <summary>
/// 软删除目录实体命令（合并 DeleteHerb/DeleteFormula）。
/// </summary>
public record DeleteEntityCommand<TEntity>(Guid Id, Guid CurrentUserId) : IRequest<Result>
    where TEntity : BaseEntity;

/// <summary>
/// 恢复已删除目录实体命令（合并 RestoreHerb/RestoreFormula）。
/// </summary>
public record RestoreEntityCommand<TEntity, TDetail>(Guid Id, Guid CurrentUserId) : IRequest<Result<TDetail>>
    where TEntity : BaseEntity;

/// <summary>
/// 切换目录实体状态命令（合并 ToggleHerbStatus/ToggleFormulaStatus）。
/// </summary>
public record ToggleEntityStatusCommand<TEntity, TDetail>(Guid Id, Guid CurrentUserId) : IRequest<Result<TDetail>>
    where TEntity : BaseEntity;
