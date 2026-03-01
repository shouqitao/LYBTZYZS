using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Formula.Models.Items;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels.Handlers;

/// <summary>
/// 验方状态处理实现
/// </summary>
public class FormulaStatusHandler : IFormulaStatusHandler
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IMasterDetailServices<FormulaListDto, FormulaItem> _masterDetailServices;
    private readonly ILogger<FormulaStatusHandler> _logger;

    public FormulaStatusHandler(
        IFormulaRepository formulaRepository,
        IMasterDetailServices<FormulaListDto, FormulaItem> masterDetailServices,
        ILogger<FormulaStatusHandler> logger)
    {
        _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> ToggleStatusAsync(FormulaListDto formula)
    {
        try
        {
            var newStatus = formula.Status == CommonStatus.Enabled ? "禁用" : "启用";
            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                $"确认{newStatus}验方 [{formula.Name}] 吗？", "状态切换确认");
            if (!confirmed) return false;

            var result = await _formulaRepository.ToggleStatusAsync(formula.Id);
            if (result != null)
            {
                _logger.LogInformation("验方状态已切换: {FormulaName} -> {NewStatus}", formula.Name, result.Status);
                await _masterDetailServices.Dialog.ShowSuccessAsync(
                    $"验方 '{formula.Name}' 已{(result.Status == CommonStatus.Enabled ? "启用" : "禁用")}", "操作成功");
                return true;
            }

            await _masterDetailServices.Dialog.ShowErrorAsync("切换验方状态失败", "操作失败");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换验方状态失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("切换验方状态失败", "操作失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RestoreAsync(FormulaListDto formula)
    {
        try
        {
            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                $"确认恢复验方 [{formula.Name}] 吗？", "恢复确认");
            if (!confirmed) return false;

            var result = await _formulaRepository.RestoreAsync(formula.Id);
            if (result != null)
            {
                _logger.LogInformation("验方已恢复: {FormulaName}", formula.Name);
                await _masterDetailServices.Dialog.ShowSuccessAsync($"验方 '{formula.Name}' 已恢复", "操作成功");
                return true;
            }

            await _masterDetailServices.Dialog.ShowErrorAsync("恢复验方失败", "操作失败");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复验方失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("恢复验方失败", "操作失败");
            return false;
        }
    }
}
