using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量导入验方命令。
/// </summary>
public record BatchImportFormulasCommand(
    List<FormulaImportItemDto> Formulas,
    string? FileName
) : IRequest<Result<FormulaBatchImportResultDto>>;
