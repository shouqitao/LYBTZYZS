using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Interfaces;

namespace LYBT.Desktop.Herbs.ViewModels.Handlers;

/// <summary>
/// 药材状态处理实现
/// </summary>
public class HerbStatusHandler : BaseStatusHandler<HerbListDto>, IHerbStatusHandler
{
    private readonly IHerbService _herbService;

    public HerbStatusHandler(
        IHerbService herbService,
        IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices,
        ILogger<HerbStatusHandler> logger)
        : base(masterDetailServices.Dialog, logger)
    {
        _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
    }

    protected override string EntityTypeName => "药材";
    protected override Guid GetEntityId(HerbListDto e) => e.Id;
    protected override string GetEntityDisplayName(HerbListDto e) => e.Name;
    protected override CommonStatus GetEntityStatus(HerbListDto e) => e.Status;

    protected override async Task<object?> ExecuteRestoreAsync(Guid id)
    {
        var result = await _herbService.RestoreAsync(id);
        return result.Success ? result.Data : null;
    }

    protected override async Task<CommonStatus?> ExecuteToggleStatusAsync(Guid id)
    {
        var result = await _herbService.ToggleStatusAsync(id);
        return result.Success ? result.Data?.Status : null;
    }
}
