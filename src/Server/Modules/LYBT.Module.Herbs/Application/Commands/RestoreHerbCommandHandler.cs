using MediatR;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.SharedKernel.Common;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Application.Mappers;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 恢复已删除药材命令处理器。
/// </summary>
public class RestoreHerbCommandHandler : IRequestHandler<RestoreHerbCommand, Result<HerbDetailDto>>
{
    private readonly IHerbRepository _herbRepository;

    public RestoreHerbCommandHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<HerbDetailDto>> Handle(
        RestoreHerbCommand request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdIncludingDeletedAsync(request.HerbId, cancellationToken);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材不存在");

        if (!herb.IsDeleted)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材未被删除，无需恢复");

        var nameExists = await _herbRepository.ExistsByNameAsync(herb.Name, herb.Id, cancellationToken);
        if (nameExists)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, $"药材名称「{herb.Name}」已存在，无法恢复");

        herb.Restore(request.OperatorId);

        await _herbRepository.UpdateAsync(herb, cancellationToken);

        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }
}
