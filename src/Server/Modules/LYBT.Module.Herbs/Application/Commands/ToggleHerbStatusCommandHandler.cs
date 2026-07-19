using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Domain;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Application.Mappers;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 切换药材状态命令处理器（启用/禁用）。
/// </summary>
public class ToggleHerbStatusCommandHandler : IRequestHandler<ToggleHerbStatusCommand, Result<HerbDetailDto>>
{
    private readonly IHerbRepository _herbRepository;

    public ToggleHerbStatusCommandHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<HerbDetailDto>> Handle(
        ToggleHerbStatusCommand request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdAsync(request.Id, cancellationToken);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材不存在");

        herb.ChangeStatus(
            herb.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled,
            request.CurrentUserId);

        await _herbRepository.UpdateAsync(herb, cancellationToken);

        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }
}


