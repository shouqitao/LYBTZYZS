using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Formulas.Application.Queries;

public record ExportFormulasQuery(string? Category = null) : IRequest<Result<byte[]>>;
