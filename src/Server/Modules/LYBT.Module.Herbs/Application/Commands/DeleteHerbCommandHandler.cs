using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Interfaces;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 删除药材命令处理器（软删除）。
/// </summary>
public class DeleteHerbCommandHandler : IRequestHandler<DeleteHerbCommand, Result>
{
    private readonly IHerbRepository _herbRepository;

    public DeleteHerbCommandHandler(
        IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result> Handle(
        DeleteHerbCommand request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdAsync(request.Id, cancellationToken);
        if (herb == null)
            return Result.Failure(ErrorCode.HerbNotFound, "药材不存在");

        herb.SoftDelete(request.CurrentUserId);

        await _herbRepository.UpdateAsync(herb, cancellationToken);

        return Result.Success();
    }
}


