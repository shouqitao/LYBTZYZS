using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Queries;

public record ExportFormulasQuery(string? Category = null) : IRequest<Result<byte[]>>;
