using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Formulas.Application.Queries;

public record GetFormulaImportTemplateQuery() : IRequest<Result<byte[]>>;
