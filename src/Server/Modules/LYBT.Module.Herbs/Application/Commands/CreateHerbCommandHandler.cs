using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Application.Mappers;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 创建药材命令处理器。
/// </summary>
public class CreateHerbCommandHandler : IRequestHandler<CreateHerbCommand, Result<HerbDetailDto>>
{
    private readonly IHerbRepository _herbRepository;

    public CreateHerbCommandHandler(
        IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<HerbDetailDto>> Handle(
        CreateHerbCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _herbRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken))
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, ErrorMessages.Get(ErrorCode.HerbNameExists));

        var herb = HerbDtoMapper.ToEntity(dto, request.CurrentUserId);

        await _herbRepository.AddAsync(herb, cancellationToken);

        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }
}


