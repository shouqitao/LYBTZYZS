namespace LYBT.Core.EventBus.Module;

/// <summary>
/// 模块状态枚举
/// </summary>
public enum ModuleState
{
    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 未初始化
    /// 模块刚被发现但尚未初始化
    /// </summary>
    Uninitialized = 1,

    /// <summary>
    /// 初始化中
    /// 模块正在执行初始化过程
    /// </summary>
    Initializing = 2,

    /// <summary>
    /// 已初始化
    /// 模块已完成初始化但尚未启动
    /// </summary>
    Initialized = 3,

    /// <summary>
    /// 启动中
    /// 模块正在启动过程中
    /// </summary>
    Starting = 4,

    /// <summary>
    /// 已启动/运行中
    /// 模块正常运行状态
    /// </summary>
    Running = 5,

    /// <summary>
    /// 停止中
    /// 模块正在停止过程中
    /// </summary>
    Stopping = 6,

    /// <summary>
    /// 已停止
    /// 模块已停止但仍可重新启动
    /// </summary>
    Stopped = 7,

    /// <summary>
    /// 错误状态
    /// 模块遇到错误无法正常运行
    /// </summary>
    Error = 8,

    /// <summary>
    /// 禁用状态
    /// 模块被管理员禁用
    /// </summary>
    Disabled = 9,

    /// <summary>
    /// 正在清理
    /// 模块正在清理资源准备卸载
    /// </summary>
    Disposing = 10,

    /// <summary>
    /// 已卸载
    /// 模块已完全卸载
    /// </summary>
    Disposed = 11
}

/// <summary>
/// 模块状态扩展方法
/// </summary>
public static class ModuleStateExtensions
{
    /// <summary>
    /// 检查模块是否处于活跃状态
    /// </summary>
    /// <param name="state">模块状态</param>
    /// <returns>是否活跃</returns>
    public static bool IsActive(this ModuleState state)
    {
        return state == ModuleState.Running;
    }

    /// <summary>
    /// 检查模块是否处于过渡状态
    /// </summary>
    /// <param name="state">模块状态</param>
    /// <returns>是否处于过渡状态</returns>
    public static bool IsTransitioning(this ModuleState state)
    {
        return state is ModuleState.Initializing or 
                       ModuleState.Starting or 
                       ModuleState.Stopping or 
                       ModuleState.Disposing;
    }

    /// <summary>
    /// 检查模块是否可以启动
    /// </summary>
    /// <param name="state">模块状态</param>
    /// <returns>是否可以启动</returns>
    public static bool CanStart(this ModuleState state)
    {
        return state is ModuleState.Initialized or 
                       ModuleState.Stopped;
    }

    /// <summary>
    /// 检查模块是否可以停止
    /// </summary>
    /// <param name="state">模块状态</param>
    /// <returns>是否可以停止</returns>
    public static bool CanStop(this ModuleState state)
    {
        return state == ModuleState.Running;
    }

    /// <summary>
    /// 检查模块是否处于错误状态
    /// </summary>
    /// <param name="state">模块状态</param>
    /// <returns>是否处于错误状态</returns>
    public static bool IsError(this ModuleState state)
    {
        return state == ModuleState.Error;
    }

    /// <summary>
    /// 检查模块是否已被释放
    /// </summary>
    /// <param name="state">模块状态</param>
    /// <returns>是否已被释放</returns>
    public static bool IsDisposed(this ModuleState state)
    {
        return state == ModuleState.Disposed;
    }

    /// <summary>
    /// 获取状态的友好显示名称
    /// </summary>
    /// <param name="state">模块状态</param>
    /// <returns>显示名称</returns>
    public static string GetDisplayName(this ModuleState state)
    {
        return state switch
        {
            ModuleState.Unknown => "未知",
            ModuleState.Uninitialized => "未初始化",
            ModuleState.Initializing => "初始化中",
            ModuleState.Initialized => "已初始化",
            ModuleState.Starting => "启动中",
            ModuleState.Running => "运行中",
            ModuleState.Stopping => "停止中",
            ModuleState.Stopped => "已停止",
            ModuleState.Error => "错误",
            ModuleState.Disabled => "已禁用",
            ModuleState.Disposing => "清理中",
            ModuleState.Disposed => "已卸载",
            _ => state.ToString()
        };
    }
}