using LYBT.Desktop.Infrastructure.Models;

namespace LYBT.Desktop.MedicalCase.ViewModels.Events;

/// <summary>
/// 药材列表操作请求类型
/// OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5)
/// </summary>
public enum HerbListRequestType
{
    /// <summary>
    /// 加载药材列表（替换）
    /// </summary>
    Load,

    /// <summary>
    /// 添加药材（合并）
    /// </summary>
    Add,

    /// <summary>
    /// 清空药材列表
    /// </summary>
    Clear
}

/// <summary>
/// 药材列表操作请求事件参数
/// OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5)
/// 用于ViewModel请求View执行控件操作
/// </summary>
public class HerbListRequestEventArgs : EventArgs
{
    /// <summary>
    /// 请求类型
    /// </summary>
    public HerbListRequestType RequestType { get; }

    /// <summary>
    /// 药材数据（用于Load和Add操作）
    /// </summary>
    public IEnumerable<HerbItemDto>? Items { get; }

    private HerbListRequestEventArgs(HerbListRequestType requestType, IEnumerable<HerbItemDto>? items = null)
    {
        RequestType = requestType;
        Items = items;
    }

    /// <summary>
    /// 创建加载请求
    /// </summary>
    public static HerbListRequestEventArgs CreateLoadRequest(IEnumerable<HerbItemDto> items)
        => new(HerbListRequestType.Load, items);

    /// <summary>
    /// 创建添加请求
    /// </summary>
    public static HerbListRequestEventArgs CreateAddRequest(IEnumerable<HerbItemDto> items)
        => new(HerbListRequestType.Add, items);

    /// <summary>
    /// 创建清空请求
    /// </summary>
    public static HerbListRequestEventArgs CreateClearRequest()
        => new(HerbListRequestType.Clear);
}
