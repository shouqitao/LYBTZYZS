using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Queries;

/// <summary>
/// 根据ID获取药材详情查询。
/// </summary>
public record GetHerbQuery(Guid Id) : IRequest<Result<HerbDetailDto>>;


