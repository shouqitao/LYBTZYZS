using LYBT.Module.Herbs.Application.Mappers;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Herbs.Services;

/// <summary>
/// 药材服务实现 — 读操作直查（写操作已收敛至 MediatR Handler，见蓝图 §2.2）。
/// </summary>
internal class HerbService : IHerbService
{
    private readonly IHerbRepository _herbRepository;

    public HerbService(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)
    {
        var result = await _herbRepository.GetPagedAsync(page, pageSize, keyword, null, ct);
        var dtos = result.Items.Select(HerbDtoMapper.ToListDto).ToList();
        var pagedResult = new PagedResult<HerbListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };
        return Result<PagedResult<HerbListDto>>.Success(pagedResult);
    }

    public async Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, ErrorMessages.Get(ErrorCode.HerbNotFound));
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }
}
