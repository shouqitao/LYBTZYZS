using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.Module.Herbs.Domain;
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
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材不存在");

        var dto = request.Input;

        if (herb.Name != dto.Name)
        {
            if (await _herbRepository.ExistsByNameAsync(dto.Name, request.Id, ct: cancellationToken))
                return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, $"药材名称 '{dto.Name}' 已存在");
        }

        herb.UpdateProfile(
            dto.Name,
            dto.Unit,
            dto.Price,
            dto.PinYinCode,
            dto.Category,
            dto.Properties,
            dto.Origin,
            dto.Spec,
            dto.CostPrice,
            dto.Effect,
            dto.Usage,
            dto.Remark,
            request.CurrentUserId);

        await _herbRepository.UpdateAsync(herb, cancellationToken);

        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }
}


