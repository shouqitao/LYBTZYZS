using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Application.Mappers;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 更新药材命令处理器。
/// </summary>
public class UpdateHerbCommandHandler : IRequestHandler<UpdateHerbCommand, Result<HerbDetailDto>>
{
    private readonly IHerbRepository _herbRepository;

    public UpdateHerbCommandHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<HerbDetailDto>> Handle(
        UpdateHerbCommand request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdAsync(request.Id, cancellationToken);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, ErrorMessages.Get(ErrorCode.HerbNotFound));

        if (herb.Name != request.Input.Name)
        {
            if (await _herbRepository.ExistsByNameAsync(request.Input.Name, request.Id, cancellationToken))
                return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, $"药材名称 '{request.Input.Name}' 已存在");
        }

        herb.UpdateProfile(
            request.Input.Name,
            request.Input.Unit,
            request.Input.Price,
            request.Input.PinYinCode,
            request.Input.Category,
            request.Input.Properties,
            request.Input.Origin,
            request.Input.Spec,
            request.Input.CostPrice,
            request.Input.Effect,
            request.Input.Usage,
            request.Input.Remark,
            request.CurrentUserId);

        await _herbRepository.UpdateAsync(herb, cancellationToken);
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }
}
