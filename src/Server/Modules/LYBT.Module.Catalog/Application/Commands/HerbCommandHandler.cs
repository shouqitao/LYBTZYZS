using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Application.Mappers;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 药材命令处理器（A-31-C3b 合并 Create/Update/Delete/Restore/Toggle 五个同构 Handler）。
/// </summary>
public class HerbCommandHandler :
    IRequestHandler<CreateEntityCommand<HerbInputDto, HerbDetailDto>, Result<HerbDetailDto>>,
    IRequestHandler<UpdateEntityCommand<HerbInputDto, HerbDetailDto>, Result<HerbDetailDto>>,
    IRequestHandler<DeleteEntityCommand<Herb>, Result>,
    IRequestHandler<RestoreEntityCommand<Herb, HerbDetailDto>, Result<HerbDetailDto>>,
    IRequestHandler<ToggleEntityStatusCommand<Herb, HerbDetailDto>, Result<HerbDetailDto>>
{
    private readonly IHerbRepository _herbRepository;

    public HerbCommandHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<HerbDetailDto>> Handle(
        CreateEntityCommand<HerbInputDto, HerbDetailDto> request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _herbRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken))
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, ErrorMessages.Get(ErrorCode.HerbNameExists));

        var herb = CatalogDtoMapper.ToEntity(dto, request.CurrentUserId);

        await _herbRepository.AddAsync(herb, cancellationToken);

        return Result<HerbDetailDto>.Success(CatalogDtoMapper.ToHerbDetailDto(herb));
    }

    public async Task<Result<HerbDetailDto>> Handle(
        UpdateEntityCommand<HerbInputDto, HerbDetailDto> request, CancellationToken cancellationToken)
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
        return Result<HerbDetailDto>.Success(CatalogDtoMapper.ToHerbDetailDto(herb));
    }

    public async Task<Result> Handle(
        DeleteEntityCommand<Herb> request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdAsync(request.Id, cancellationToken);
        if (herb == null)
            return Result.Failure(ErrorCode.HerbNotFound, ErrorMessages.Get(ErrorCode.HerbNotFound));

        herb.SoftDelete(request.CurrentUserId);

        await _herbRepository.UpdateAsync(herb, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<HerbDetailDto>> Handle(
        RestoreEntityCommand<Herb, HerbDetailDto> request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdIncludingDeletedAsync(request.Id, cancellationToken);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, ErrorMessages.Get(ErrorCode.HerbNotFound));

        if (!herb.IsDeleted)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, ErrorMessages.Get(ErrorCode.HerbNotDeleted));

        var nameExists = await _herbRepository.ExistsByNameAsync(herb.Name, herb.Id, cancellationToken);
        if (nameExists)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, $"药材名称「{herb.Name}」已存在，无法恢复");

        herb.Restore(request.CurrentUserId);
        await _herbRepository.UpdateAsync(herb, cancellationToken);
        return Result<HerbDetailDto>.Success(CatalogDtoMapper.ToHerbDetailDto(herb));
    }

    public async Task<Result<HerbDetailDto>> Handle(
        ToggleEntityStatusCommand<Herb, HerbDetailDto> request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdAsync(request.Id, cancellationToken);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, ErrorMessages.Get(ErrorCode.HerbNotFound));

        herb.ChangeStatus(
            herb.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled,
            request.CurrentUserId);

        await _herbRepository.UpdateAsync(herb, cancellationToken);
        return Result<HerbDetailDto>.Success(CatalogDtoMapper.ToHerbDetailDto(herb));
    }
}
