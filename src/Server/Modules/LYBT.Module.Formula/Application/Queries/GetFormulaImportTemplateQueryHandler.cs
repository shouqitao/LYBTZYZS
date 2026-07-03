using MediatR;
using LYBT.Module.Formulas.Interfaces;
using LYBT.SharedKernel.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formulas.Application.Queries;

public class GetFormulaImportTemplateQueryHandler : IRequestHandler<GetFormulaImportTemplateQuery, Result<byte[]>>
{
    private readonly IFormulaImportExportService _importExportService;
    private readonly ILogger<GetFormulaImportTemplateQueryHandler> _logger;

    public GetFormulaImportTemplateQueryHandler(
        IFormulaImportExportService importExportService,
        ILogger<GetFormulaImportTemplateQueryHandler> logger)
    {
        _importExportService = importExportService;
        _logger = logger;
    }

    public Task<Result<byte[]>> Handle(GetFormulaImportTemplateQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[Handler] Get formula import template");

        var stream = _importExportService.GenerateImportTemplate();
        var bytes = stream.ToArray();
        stream.Dispose();

        return Task.FromResult(Result<byte[]>.Success(bytes));
    }
}
