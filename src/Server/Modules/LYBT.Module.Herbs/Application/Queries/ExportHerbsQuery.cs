using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Queries;

public record ExportHerbsQuery(string? Category = null) : IRequest<Result<byte[]>>;
