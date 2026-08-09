using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using MediatR;

namespace LYBT.Module.Catalog.Application.Queries;

public record CheckHerbReferenceQuery(Guid HerbId) : IRequest<Result<HerbReferenceCheckDto>>;

public record BatchCheckHerbReferenceQuery(List<Guid> HerbIds) : IRequest<Result<List<HerbReferenceCheckDto>>>;
