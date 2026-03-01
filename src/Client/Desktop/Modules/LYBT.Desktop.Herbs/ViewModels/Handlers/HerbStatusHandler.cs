using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.ViewModels.Handlers;

/// <summary>
/// 药材状态处理实现
/// </summary>
public class HerbStatusHandler : IHerbStatusHandler
{
    private readonly IHerbRepository _herbRepository;
    private readonly IMasterDetailServices<HerbListDto, HerbDetailModel> _masterDetailServices;
    private readonly ILogger<HerbStatusHandler> _logger;

    public HerbStatusHandler(
        IHerbRepository herbRepository,
        IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices,
        ILogger<HerbStatusHandler> logger)
    {
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> ToggleStatusAsync(HerbListDto herb)
    {
        try
        {
            var newStatus = herb.Status == CommonStatus.Enabled ? "禁用" : "启用";
            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                $"确认{newStatus}药材 [{herb.Name}] 吗？", "状态切换确认");
            if (!confirmed) return false;

            var result = await _herbRepository.ToggleStatusAsync(herb.Id);
            if (result != null)
            {
                _logger.LogInformation("药材状态已切换: {HerbName} -> {NewStatus}", herb.Name, result.Status);
                await _masterDetailServices.Dialog.ShowSuccessAsync(
                    $"药材 '{herb.Name}' 已{(result.Status == CommonStatus.Enabled ? "启用" : "禁用")}", "操作成功");
                return true;
            }

            await _masterDetailServices.Dialog.ShowErrorAsync("切换药材状态失败", "操作失败");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换药材状态失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("切换药材状态失败", "操作失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RestoreAsync(HerbListDto herb)
    {
        try
        {
            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                $"确认恢复药材 [{herb.Name}] 吗？", "恢复确认");
            if (!confirmed) return false;

            var result = await _herbRepository.RestoreAsync(herb.Id);
            if (result != null)
            {
                _logger.LogInformation("药材已恢复: {HerbName}", herb.Name);
                await _masterDetailServices.Dialog.ShowSuccessAsync($"药材 '{herb.Name}' 已恢复", "操作成功");
                return true;
            }

            await _masterDetailServices.Dialog.ShowErrorAsync("恢复药材失败", "操作失败");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复药材失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("恢复药材失败", "操作失败");
            return false;
        }
    }
}
