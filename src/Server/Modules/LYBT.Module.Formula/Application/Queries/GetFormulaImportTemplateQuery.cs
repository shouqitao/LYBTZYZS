using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Queries;

public record GetFormulaImportTemplateQuery() : IRequest<Result<byte[]>>;
