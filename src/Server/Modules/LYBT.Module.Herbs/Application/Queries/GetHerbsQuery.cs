using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Queries;

/// <summary>
/// 获取药材分页列表查询。
/// </summary>
public record GetHerbsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    string? Category = null
) : IRequest<Result<PagedResult<HerbListDto>>>;


