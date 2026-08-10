using LYBT.Entities.Common;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 目录实体命令处理器泛型基类（A-31-C3b 合并 HerbCommandHandler/FormulaCommandHandler 孪生）。
/// 收敛 Create/Update/Delete/Restore/Toggle 五个同构 Handler 的骨架，
/// 子类仅提供实体特有差异：仓储、DTO 映射、错误码、UpdateProfile 调用与消息文案。
/// </summary>
/// <typeparam name="TEntity">目录实体（Herb / Formula）</typeparam>
/// <typeparam name="TInput">输入 DTO</typeparam>
/// <typeparam name="TDetail">详情 DTO</typeparam>
public abstract class CatalogEntityCommandHandlerBase<TEntity, TInput, TDetail>
    : IRequestHandler<CreateEntityCommand<TInput, TDetail>, Result<TDetail>>,
      IRequestHandler<UpdateEntityCommand<TInput, TDetail>, Result<TDetail>>,
      IRequestHandler<DeleteEntityCommand<TEntity>, Result>,
      IRequestHandler<RestoreEntityCommand<TEntity, TDetail>, Result<TDetail>>,
      IRequestHandler<ToggleEntityStatusCommand<TEntity, TDetail>, Result<TDetail>>
    where TEntity : BaseEntity
{
    private readonly ICatalogRepository<TEntity> _repository;

    protected CatalogEntityCommandHandlerBase(ICatalogRepository<TEntity> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    #region 子类差异点

    /// <summary>实体显示名（用于 Update 重名消息，如「药材」/「方剂」）。</summary>
    protected abstract string EntityDisplayName { get; }

    /// <summary>从输入 DTO 创建实体（走领域工厂 / Mapper 手写方法）。</summary>
    protected abstract TEntity CreateEntity(TInput input, Guid currentUserId);

    /// <summary>将输入 DTO 应用到实体（UpdateProfile 调用，参数因实体而异）。</summary>
    protected abstract void ApplyUpdate(TEntity entity, TInput input, Guid currentUserId);

    /// <summary>实体 → 详情 DTO（Mapperly 生成方法）。</summary>
    protected abstract TDetail ToDetailDto(TEntity entity);

    /// <summary>软删除实体（委托实体 SoftDelete）。</summary>
    protected abstract void ApplySoftDelete(TEntity entity, Guid operatorId);

    /// <summary>恢复实体（委托实体 Restore）。</summary>
    protected abstract void ApplyRestore(TEntity entity, Guid operatorId);

    /// <summary>切换实体启用/禁用状态（委托实体 ChangeStatus）。</summary>
    protected abstract void ApplyToggleStatus(TEntity entity, Guid operatorId);

    /// <summary>名称重复错误码。</summary>
    protected abstract ErrorCode NameExistsErrorCode { get; }

    /// <summary>实体不存在错误码。</summary>
    protected abstract ErrorCode NotFoundErrorCode { get; }

    /// <summary>实体未删除错误码（Restore 时检查）。</summary>
    protected abstract ErrorCode NotDeletedErrorCode { get; }

    /// <summary>实体不存在时的失败消息（Delete/Toggle 使用）。</summary>
    protected virtual string NotFoundMessage => ErrorMessages.Get(NotFoundErrorCode);

    /// <summary>Restore 时实体不存在消息（Formula 使用字面量，与 ErrorMessages.Get 不同）。</summary>
    protected virtual string RestoreNotFoundMessage => NotFoundMessage;

    /// <summary>Update 时名称冲突消息（单引号风格）。</summary>
    protected virtual string UpdateNameConflictMessage(string name) => $"{EntityDisplayName}名称 '{name}' 已存在";

    /// <summary>Restore 时名称冲突消息（书名号风格，Herb/Formula 实体显示名不同）。</summary>
    protected virtual string RestoreNameConflictMessage(string name) => $"{EntityDisplayName}名称「{name}」已存在，无法恢复";

    #endregion

    #region IRequestHandler 实现

    public async Task<Result<TDetail>> Handle(
        CreateEntityCommand<TInput, TDetail> request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _repository.ExistsByNameAsync(GetName(dto), ct: cancellationToken))
            return Result<TDetail>.Failure(NameExistsErrorCode, ErrorMessages.Get(NameExistsErrorCode));

        var entity = CreateEntity(dto, request.CurrentUserId);
        await _repository.AddAsync(entity, cancellationToken);

        return Result<TDetail>.Success(ToDetailDto(entity));
    }

    public async Task<Result<TDetail>> Handle(
        UpdateEntityCommand<TInput, TDetail> request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            return Result<TDetail>.Failure(NotFoundErrorCode, NotFoundMessage);

        if (GetName(entity) != GetName(request.Input))
        {
            if (await _repository.ExistsByNameAsync(GetName(request.Input), request.Id, cancellationToken))
                return Result<TDetail>.Failure(NameExistsErrorCode, UpdateNameConflictMessage(GetName(request.Input)));
        }

        ApplyUpdate(entity, request.Input, request.CurrentUserId);
        await _repository.UpdateAsync(entity, cancellationToken);
        return Result<TDetail>.Success(ToDetailDto(entity));
    }

    public async Task<Result> Handle(
        DeleteEntityCommand<TEntity> request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            return Result.Failure(NotFoundErrorCode, NotFoundMessage);

        ApplySoftDelete(entity, request.CurrentUserId);
        await _repository.UpdateAsync(entity, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<TDetail>> Handle(
        RestoreEntityCommand<TEntity, TDetail> request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdIncludingDeletedAsync(request.Id, cancellationToken);
        if (entity == null)
            return Result<TDetail>.Failure(NotFoundErrorCode, RestoreNotFoundMessage);

        if (!entity.IsDeleted)
            return Result<TDetail>.Failure(NotFoundErrorCode, ErrorMessages.Get(NotDeletedErrorCode));

        var nameExists = await _repository.ExistsByNameAsync(GetName(entity), entity.Id, cancellationToken);
        if (nameExists)
            return Result<TDetail>.Failure(NameExistsErrorCode, RestoreNameConflictMessage(GetName(entity)));

        ApplyRestore(entity, request.CurrentUserId);
        await _repository.UpdateAsync(entity, cancellationToken);
        return Result<TDetail>.Success(ToDetailDto(entity));
    }

    public async Task<Result<TDetail>> Handle(
        ToggleEntityStatusCommand<TEntity, TDetail> request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            return Result<TDetail>.Failure(NotFoundErrorCode, NotFoundMessage);

        ApplyToggleStatus(entity, request.CurrentUserId);

        await _repository.UpdateAsync(entity, cancellationToken);
        return Result<TDetail>.Success(ToDetailDto(entity));
    }

    #endregion

    #region 实体名称访问（TEntity 无统一 Name 属性，经子类桥接）

    /// <summary>从实体取名称。</summary>
    protected abstract string GetName(TEntity entity);

    /// <summary>从输入 DTO 取名称。</summary>
    protected abstract string GetName(TInput input);

    #endregion
}
