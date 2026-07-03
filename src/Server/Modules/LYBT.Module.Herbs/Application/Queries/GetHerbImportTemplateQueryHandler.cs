using MediatR;
using LYBT.Module.Herbs.Interfaces;
using LYBT.SharedKernel.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Application.Queries;

public class GetHerbImportTemplateQueryHandler : IRequestHandler<GetHerbImportTemplateQuery, Result<byte[]>>
{
    private readonly IHerbImportExportService _importExportService;
    private readonly ILogger<GetHerbImportTemplateQueryHandler> _logger;

    public GetHerbImportTemplateQueryHandler(
        IHerbImportExportService importExportService,
        ILogger<GetHerbImportTemplateQueryHandler> logger)
    {
        _importExportService = importExportService;
        _logger = logger;
    }

    public Task<Result<byte[]>> Handle(GetHerbImportTemplateQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[Handler] Get herb import template");

        var stream = _importExportService.GenerateImportTemplate();
        var bytes = stream.ToArray();
        stream.Dispose();

        return Task.FromResult(Result<byte[]>.Success(bytes));
    }
}
