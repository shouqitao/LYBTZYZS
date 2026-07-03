using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Herbs.Application.Queries;

public record ExportHerbsQuery(string? Category = null) : IRequest<Result<byte[]>>;
