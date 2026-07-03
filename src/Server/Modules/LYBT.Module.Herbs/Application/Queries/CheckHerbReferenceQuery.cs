using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Herbs.Application.Queries;

public record CheckHerbReferenceQuery(Guid HerbId) : IRequest<Result<HerbReferenceCheckDto>>;

public record BatchCheckHerbReferenceQuery(List<Guid> HerbIds) : IRequest<Result<List<HerbReferenceCheckDto>>>;
