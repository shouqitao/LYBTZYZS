using MediatR;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.SharedKernel.Events;
using LYBT.Module.Herbs.Domain;
using LYBT.Module.Herbs.Domain.Events;
using LYBT.Module.Herbs.Interfaces;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 删除药材命令处理器（软删除）。
/// </summary>
public class DeleteHerbCommandHandler : IRequestHandler<DeleteHerbCommand, Result>
{
    private readonly IHerbRepository _herbRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DeleteHerbCommandHandler(
        IHerbRepository herbRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _herbRepository = herbRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result> Handle(
        DeleteHerbCommand request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdAsync(request.Id, cancellationToken);
        if (herb == null)
            return Result.Failure(ErrorCode.HerbNotFound, "药材不存在");

        herb.SoftDelete(request.CurrentUserId);

        await _herbRepository.UpdateAsync(herb, cancellationToken);

        await _eventDispatcher.DispatchAsync(new[]
        {
            new HerbDeletedEvent(herb.Id, herb.Name, request.CurrentUserId)
        }, cancellationToken);

        return Result.Success();
    }
}


