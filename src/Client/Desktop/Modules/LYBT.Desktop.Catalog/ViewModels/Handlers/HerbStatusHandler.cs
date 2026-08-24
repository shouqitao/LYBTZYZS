using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Catalog.ViewModels.Handlers;

/// <summary>
/// 药材状态处理实现
/// </summary>
public class HerbStatusHandler : BaseStatusHandler<HerbListDto>, IHerbStatusHandler
{
    private readonly IHerbService _herbService;
    private readonly IHerbRepository _herbRepository;

    public HerbStatusHandler(
        IHerbService herbService,
        IHerbRepository herbRepository,
        IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices,
        ILogger<HerbStatusHandler> logger)
        : base(masterDetailServices.Dialog, logger)
    {
        _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
    }

    protected override string EntityTypeName => "药材";
    protected override Guid GetEntityId(HerbListDto e) => e.Id;
    protected override string GetEntityDisplayName(HerbListDto e) => e.Name;
    protected override CommonStatus GetEntityStatus(HerbListDto e) => e.Status;

    protected override async Task<object?> ExecuteRestoreAsync(Guid id)
        => await _herbRepository.RestoreAsync(id);

    protected override async Task<CommonStatus?> ExecuteSetStatusAsync(Guid id, CommonStatus targetStatus)
    {
        var result = await _herbService.SetStatusAsync(id, targetStatus);
        return result.Success ? result.Data?.Status : null;
    }
}
