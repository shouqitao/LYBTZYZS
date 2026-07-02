using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.Module.Herbs.Domain;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Application.Mappers;

namespace LYBT.Module.Herbs.Application.Queries;

/// <summary>
/// 根据ID获取药材详情查询处理器。
/// </summary>
public class GetHerbQueryHandler : IRequestHandler<GetHerbQuery, Result<HerbDetailDto>>
{
    private readonly IHerbRepository _herbRepository;

    public GetHerbQueryHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<HerbDetailDto>> Handle(
        GetHerbQuery request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdAsync(request.Id, cancellationToken);

        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材不存在");

        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }
}


