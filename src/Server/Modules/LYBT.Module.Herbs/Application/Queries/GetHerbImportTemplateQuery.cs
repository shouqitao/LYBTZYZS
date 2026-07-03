using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Herbs.Application.Queries;

public record GetHerbImportTemplateQuery() : IRequest<Result<byte[]>>;
