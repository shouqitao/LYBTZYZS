using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Queries;

public record CheckHerbReferenceQuery(Guid HerbId) : IRequest<Result<HerbReferenceCheckDto>>;

public record BatchCheckHerbReferenceQuery(List<Guid> HerbIds) : IRequest<Result<List<HerbReferenceCheckDto>>>;
