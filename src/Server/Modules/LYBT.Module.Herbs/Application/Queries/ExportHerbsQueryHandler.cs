using MediatR;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Application.Queries;

public class ExportHerbsQueryHandler : IRequestHandler<ExportHerbsQuery, Result<byte[]>>
{
    private readonly IHerbImportExportService _importExportService;
    private readonly ILogger<ExportHerbsQueryHandler> _logger;

    public ExportHerbsQueryHandler(
        IHerbImportExportService importExportService,
        ILogger<ExportHerbsQueryHandler> logger)
    {
        _importExportService = importExportService;
        _logger = logger;
    }

    public async Task<Result<byte[]>> Handle(ExportHerbsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[Handler] Export herbs - Category={Category}", request.Category);

        var stream = await _importExportService.ExportAsync(request.Category);
        var bytes = stream.ToArray();
        await stream.DisposeAsync();

        return Result<byte[]>.Success(bytes);
    }
}
