using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.ViewModels.Handlers;

/// <summary>
/// 药材状态处理实现
/// </summary>
public class HerbStatusHandler : BaseStatusHandler<HerbListDto>, IHerbStatusHandler
{
    private readonly IHerbRepository _herbRepository;

    public HerbStatusHandler(
        IHerbRepository herbRepository,
        IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices,
        ILogger<HerbStatusHandler> logger)
        : base(masterDetailServices.Dialog, logger)
    {
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
    }

    protected override string EntityTypeName => "药材";
    protected override Guid GetEntityId(HerbListDto e) => e.Id;
    protected override string GetEntityDisplayName(HerbListDto e) => e.Name;
    protected override CommonStatus GetEntityStatus(HerbListDto e) => e.Status;

    protected override async Task<object?> ExecuteRestoreAsync(Guid id)
        => await _herbRepository.RestoreAsync(id);

    protected override async Task<CommonStatus?> ExecuteToggleStatusAsync(Guid id)
    {
        var result = await _herbRepository.ToggleStatusAsync(id);
        return result?.Status;
    }
}
