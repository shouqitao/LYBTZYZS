using System.Net.Http;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.ViewModels.Handlers;

/// <summary>
/// 状态处理泛型基类 - 统一 Restore/Toggle 操作的确认、执行、通知、异常处理流程
/// </summary>
/// <typeparam name="TListDto">列表DTO类型</typeparam>
public abstract class BaseStatusHandler<TListDto> where TListDto : class
{
    protected IDialogManager Dialog { get; }
    protected ILogger Logger { get; }

    protected BaseStatusHandler(IDialogManager dialog, ILogger logger)
    {
        Dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>实体类型显示名称 (如 "药材"、"验方"、"患者"、"用户")</summary>
    protected abstract string EntityTypeName { get; }

    /// <summary>获取实体ID</summary>
    protected abstract Guid GetEntityId(TListDto entity);

    /// <summary>获取实体显示名称</summary>
    protected abstract string GetEntityDisplayName(TListDto entity);

    /// <summary>执行恢复操作 (调用 repository)</summary>
    protected abstract Task<object?> ExecuteRestoreAsync(Guid id);

    /// <summary>获取实体当前状态 (Toggle 操作需要)</summary>
    protected virtual CommonStatus GetEntityStatus(TListDto entity)
        => throw new NotSupportedException($"{GetType().Name} 不支持状态切换");

    /// <summary>执行状态切换操作 (调用 repository)</summary>
    protected virtual Task<CommonStatus?> ExecuteToggleStatusAsync(Guid id)
        => throw new NotSupportedException($"{GetType().Name} 不支持状态切换");

    /// <summary>
    /// 恢复已删除的实体 (统一实现)
    /// 流程: 确认 -> 执行 -> 结果通知 -> 异常处理
    /// </summary>
    public async Task<bool> RestoreAsync(TListDto entity)
    {
        var displayName = GetEntityDisplayName(entity);
        try
        {
            var confirmed = await Dialog.ShowConfirmAsync(
                $"确认恢复{EntityTypeName} [{displayName}] 吗？", "恢复确认");
            if (!confirmed) return false;

            var result = await ExecuteRestoreAsync(GetEntityId(entity));
            if (result != null)
            {
                Logger.LogInformation("{EntityType}已恢复: {DisplayName}", EntityTypeName, displayName);
                await Dialog.ShowSuccessAsync($"{EntityTypeName} '{displayName}' 已恢复", "操作成功");
                return true;
            }

            await Dialog.ShowErrorAsync($"恢复{EntityTypeName}失败", "操作失败");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "恢复{EntityType}失败", EntityTypeName);
            await Dialog.ShowErrorAsync($"恢复{EntityTypeName}失败", "操作失败");
            return false;
        }
    }

    /// <summary>
    /// 切换实体启用/禁用状态 (默认实现，含确认对话框)
    /// UserStatusHandler 独立实现，PatientStatusHandler 不使用
    /// </summary>
    public virtual async Task<bool> ToggleStatusAsync(TListDto entity)
    {
        var displayName = GetEntityDisplayName(entity);
        var currentStatus = GetEntityStatus(entity);
        var newStatusText = currentStatus == CommonStatus.Enabled ? "禁用" : "启用";

        try
        {
            var confirmed = await Dialog.ShowConfirmAsync(
                $"确认{newStatusText}{EntityTypeName} [{displayName}] 吗？", "状态切换确认");
            if (!confirmed) return false;

            var newStatus = await ExecuteToggleStatusAsync(GetEntityId(entity));
            if (newStatus != null)
            {
                Logger.LogInformation("{EntityType}状态已切换: {DisplayName} -> {NewStatus}",
                    EntityTypeName, displayName, newStatus);
                await Dialog.ShowSuccessAsync(
                    $"{EntityTypeName} '{displayName}' 已{(newStatus == CommonStatus.Enabled ? "启用" : "禁用")}",
                    "操作成功");
                return true;
            }

            await Dialog.ShowErrorAsync($"切换{EntityTypeName}状态失败", "操作失败");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "切换{EntityType}状态失败", EntityTypeName);
            await Dialog.ShowErrorAsync($"切换{EntityTypeName}状态失败", "操作失败");
            return false;
        }
    }
}
