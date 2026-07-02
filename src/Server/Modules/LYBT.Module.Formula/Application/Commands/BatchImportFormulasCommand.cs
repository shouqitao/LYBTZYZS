using LYBT.Shared.Models.Contracts.Formula;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.Formulas.Application.Commands;

public record BatchImportFormulasCommand(
    List<FormulaImportItemDto> Formulas,
    string? FileName
) : IRequest<Result<FormulaBatchImportResultDto>>;


