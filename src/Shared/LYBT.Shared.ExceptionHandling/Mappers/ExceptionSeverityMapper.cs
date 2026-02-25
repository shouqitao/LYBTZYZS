// -----------------------------------------------------------------------
// <copyright file="ExceptionSeverityMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Shared.ExceptionHandling.Handlers;

namespace LYBT.Shared.ExceptionHandling.Mappers;

/// <summary>
/// 异常严重度映射结果 - 包含通知类型和是否需要弹窗
/// </summary>
public record ExceptionNotificationMapping(
    string NotificationType,
    bool RequiresDialog,
    bool RequiresDetailedLog);

/// <summary>
/// 异常严重度到通知类型的映射器
/// </summary>
/// <remarks>
/// 映射规则:
/// - Information -> Info (Toast)
/// - Warning -> Warning (Toast)
/// - Error -> Error (Dialog)
/// - Critical -> Error (Dialog + 详细日志)
///
/// NotificationType 使用字符串而非枚举引用，避免 Shared 层依赖 Desktop.Infrastructure。
/// Desktop 调用方负责将字符串转换为 NotificationType 枚举。
/// </remarks>
public static class ExceptionSeverityMapper
{
    /// <summary>
    /// 将异常严重度映射为通知配置
    /// </summary>
    /// <param name="severity">异常严重度</param>
    /// <returns>通知映射结果</returns>
    public static ExceptionNotificationMapping ToNotificationMapping(ExceptionSeverity severity)
    {
        return severity switch
        {
            ExceptionSeverity.Information => new ExceptionNotificationMapping(
                NotificationType: "Info",
                RequiresDialog: false,
                RequiresDetailedLog: false),

            ExceptionSeverity.Warning => new ExceptionNotificationMapping(
                NotificationType: "Warning",
                RequiresDialog: false,
                RequiresDetailedLog: false),

            ExceptionSeverity.Error => new ExceptionNotificationMapping(
                NotificationType: "Error",
                RequiresDialog: true,
                RequiresDetailedLog: false),

            ExceptionSeverity.Critical => new ExceptionNotificationMapping(
                NotificationType: "Error",
                RequiresDialog: true,
                RequiresDetailedLog: true),

            _ => new ExceptionNotificationMapping(
                NotificationType: "Error",
                RequiresDialog: true,
                RequiresDetailedLog: false)
        };
    }

    /// <summary>
    /// 获取通知类型字符串
    /// </summary>
    /// <param name="severity">异常严重度</param>
    /// <returns>通知类型 (Info/Warning/Error)</returns>
    public static string ToNotificationType(ExceptionSeverity severity)
    {
        return ToNotificationMapping(severity).NotificationType;
    }

    /// <summary>
    /// 判断是否需要弹窗显示
    /// </summary>
    /// <param name="severity">异常严重度</param>
    /// <returns>true 表示需要 Dialog，false 表示 Toast 即可</returns>
    public static bool RequiresDialog(ExceptionSeverity severity)
    {
        return ToNotificationMapping(severity).RequiresDialog;
    }
}
