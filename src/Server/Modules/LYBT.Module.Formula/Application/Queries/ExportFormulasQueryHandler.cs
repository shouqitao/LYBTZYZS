using MediatR;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formulas.Application.Queries;

public class ExportFormulasQueryHandler : IRequestHandler<ExportFormulasQuery, Result<byte[]>>
{
    private readonly IFormulaImportExportService _importExportService;
    private readonly ILogger<ExportFormulasQueryHandler> _logger;

    public ExportFormulasQueryHandler(
        IFormulaImportExportService importExportService,
        ILogger<ExportFormulasQueryHandler> logger)
    {
        _importExportService = importExportService;
        _logger = logger;
    }

    public async Task<Result<byte[]>> Handle(ExportFormulasQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[Handler] Export formulas - Category={Category}", request.Category);

        var stream = await _importExportService.ExportAsync(request.Category);
        var bytes = stream.ToArray();
        await stream.DisposeAsync();

        return Result<byte[]>.Success(bytes);
    }
}
