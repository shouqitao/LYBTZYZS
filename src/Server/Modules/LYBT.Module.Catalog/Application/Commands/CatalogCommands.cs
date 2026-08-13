using LYBT.Entities.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 创建目录实体命令（A-31-C3b 合并 CreateHerb/CreateFormula 同构命令）。
/// </summary>
public record CreateEntityCommand<TInput, TDetail>(TInput Input, Guid CurrentUserId) : IRequest<Result<TDetail>>;

/// <summary>
/// 更新目录实体命令（合并 UpdateHerb/UpdateFormula）。
/// P1-7/8（2026-08-14）: 加 OperatorRole——所有权检查移入 Handler（原 Controller 内 Get+ValidateOwnership）
/// </summary>
public record UpdateEntityCommand<TInput, TDetail>(Guid Id, TInput Input, Guid CurrentUserId, UserRole OperatorRole) : IRequest<Result<TDetail>>;

/// <summary>
/// 软删除目录实体命令（合并 DeleteHerb/DeleteFormula）。
/// P1-7/8（2026-08-14）: 加 OperatorRole——所有权检查移入 Handler
/// </summary>
public record DeleteEntityCommand<TEntity>(Guid Id, Guid CurrentUserId, UserRole OperatorRole) : IRequest<Result>
    where TEntity : BaseEntity;

/// <summary>
/// 恢复已删除目录实体命令（合并 RestoreHerb/RestoreFormula）。
/// </summary>
public record RestoreEntityCommand<TEntity, TDetail>(Guid Id, Guid CurrentUserId) : IRequest<Result<TDetail>>
    where TEntity : BaseEntity;

/// <summary>
/// 切换目录实体状态命令（合并 ToggleHerbStatus/ToggleFormulaStatus）。
/// P1-7/8（2026-08-14）: 加 OperatorRole——所有权检查移入 Handler
/// </summary>
public record ToggleEntityStatusCommand<TEntity, TDetail>(Guid Id, Guid CurrentUserId, UserRole OperatorRole) : IRequest<Result<TDetail>>
    where TEntity : BaseEntity;
