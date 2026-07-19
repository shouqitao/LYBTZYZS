using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Events;
using LYBT.Module.Herbs.Domain;
using LYBT.Module.Herbs.Domain.Events;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Application.Mappers;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 创建药材命令处理器。
/// </summary>
public class CreateHerbCommandHandler : IRequestHandler<CreateHerbCommand, Result<HerbDetailDto>>
{
    private readonly IHerbRepository _herbRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateHerbCommandHandler(
        IHerbRepository herbRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _herbRepository = herbRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result<HerbDetailDto>> Handle(
        CreateHerbCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _herbRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken))
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, "药材名称已存在");

        var herb = HerbDtoMapper.ToEntity(dto, request.CurrentUserId);

        await _herbRepository.AddAsync(herb, cancellationToken);

        await _eventDispatcher.DispatchAsync(new[]
        {
            new HerbCreatedEvent(herb.Id, herb.Name, herb.Price, request.CurrentUserId)
        }, cancellationToken);

        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }
}


