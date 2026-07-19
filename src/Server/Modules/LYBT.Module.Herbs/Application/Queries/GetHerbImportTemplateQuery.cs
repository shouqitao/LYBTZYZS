using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Queries;

public record GetHerbImportTemplateQuery() : IRequest<Result<byte[]>>;
