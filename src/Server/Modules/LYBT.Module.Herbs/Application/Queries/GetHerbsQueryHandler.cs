using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Domain;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Application.Mappers;

namespace LYBT.Module.Herbs.Application.Queries;

/// <summary>
/// 获取药材分页列表查询处理器。
/// </summary>
public class GetHerbsQueryHandler : IRequestHandler<GetHerbsQuery, Result<PagedResult<HerbListDto>>>
{
    private readonly IHerbRepository _herbRepository;

    public GetHerbsQueryHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<PagedResult<HerbListDto>>> Handle(
        GetHerbsQuery request, CancellationToken cancellationToken)
    {
        var result = await _herbRepository.GetPagedAsync(
            request.Page, request.PageSize, request.Keyword, request.Category, cancellationToken);

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
}


