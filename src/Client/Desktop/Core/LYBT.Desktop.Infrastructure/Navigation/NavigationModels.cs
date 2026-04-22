using System.Windows.Input;

using Prism.Regions;
namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// 导航记录 - Phase 2.1: Navigation Improvements
    /// 记录单次导航的完整信息，用于历史记录和状态恢复
    /// </summary>
    public record NavigationEntry(
        string Uri,
        string Title,
        NavigationParameters Parameters,
        DateTime Timestamp,
        object? State = null,
        string? RegionName = null
    )
    {
        /// <summary>
        /// 导航 URI（例如：/MedicalCase/Details/123）
        /// </summary>
        public string Uri { get; init; } = Uri;

        /// <summary>
        /// 导航标题（显示用）
        /// </summary>
        public string Title { get; init; } = Title;

        /// <summary>
        /// 导航参数
        /// </summary>
        public NavigationParameters Parameters { get; init; } = Parameters;

        /// <summary>
        /// 导航时间戳
        /// </summary>
        public DateTime Timestamp { get; init; } = Timestamp;

        /// <summary>
        /// 视图状态（用于恢复滚动位置、表单数据等）
        /// </summary>
        public object? State { get; init; } = State;

        /// <summary>
        /// 区域名称（如果导航到特定区域）
        /// </summary>
        public string? RegionName { get; init; } = RegionName;

        /// <summary>
        /// 判断是否为同一导航（忽略时间戳和状态）
        /// </summary>
        public bool IsSameNavigationAs(NavigationEntry other)
        {
            if (other is null) return false;
            return Uri == other.Uri &&
                   Title == other.Title &&
                   RegionName == other.RegionName;
        }
    }

    /// <summary>
    /// 面包屑导航项 - Phase 2.1: Navigation Improvements
    /// 表示导航层级中的一层
    /// </summary>
    public record BreadcrumbItem(
        string Title,
        string Uri,
        bool IsActive,
        int Level,
        ICommand? NavigateCommand = null
    )
    {
        /// <summary>
        /// 显示标题
        /// </summary>
        public string Title { get; init; } = Title;

        /// <summary>
        /// 目标 URI
        /// </summary>
        public string Uri { get; init; } = Uri;

        /// <summary>
        /// 是否为当前激活位置
        /// </summary>
        public bool IsActive { get; init; } = IsActive;

        /// <summary>
        /// 层级深度（0 = 根目录）
        /// </summary>
        public int Level { get; init; } = Level;

        /// <summary>
        /// 导航命令（点击面包屑时执行）
        /// </summary>
        public ICommand? NavigateCommand { get; init; } = NavigateCommand;
    }

    /// <summary>
    /// 导航建议 - Phase 2.1: Navigation Improvements
    /// 基于上下文和频率的智能导航建议
    /// </summary>
    public record NavigationSuggestion(
        string Title,
        string Uri,
        double Confidence,
        string Reason,
        SuggestionType Type,
        int? Frequency = null
    )
    {
        /// <summary>
        /// 建议标题
        /// </summary>
        public string Title { get; init; } = Title;

        /// <summary>
        /// 目标 URI
        /// </summary>
        public string Uri { get; init; } = Uri;

        /// <summary>
        /// 置信度（0.0 - 1.0）
        /// </summary>
        public double Confidence { get; init; } = Confidence;

        /// <summary>
        /// 建议原因（显示给用户）
        /// </summary>
        public string Reason { get; init; } = Reason;

        /// <summary>
        /// 建议类型
        /// </summary>
        public SuggestionType Type { get; init; } = Type;

        /// <summary>
        /// 访问频率（如果是基于频率的建议）
        /// </summary>
        public int? Frequency { get; init; } = Frequency;
    }

    /// <summary>
    /// 导航建议类型
    /// </summary>
    public enum SuggestionType
    {
        /// <summary>
        /// 基于上下文的建议
        /// </summary>
        Contextual,

        /// <summary>
        /// 基于频率的建议
        /// </summary>
        Frequent,

        /// <summary>
        /// 基于时间的建议（例如：早晨显示门诊列表）
        /// </summary>
        TimeBased,

        /// <summary>
        /// 最近访问
        /// </summary>
        Recent,

        /// <summary>
        /// 固定/收藏
        /// </summary>
        Pinned
    }

    /// <summary>
    /// 导航参数扩展方法
    /// </summary>
    public static class NavigationParametersExtensions
    {
        /// <summary>
        /// 从导航参数中获取值
        /// </summary>
        public static T? GetValue<T>(this NavigationParameters parameters, string key)
        {
            if (parameters == null) return default;
            if (parameters.ContainsKey(key))
            {
                var value = parameters[key];
                if (value is T typedValue)
                    return typedValue;
            }
            return default;
        }

        /// <summary>
        /// 从导航参数中获取值（带默认值）
        /// </summary>
        public static T GetValueOrDefault<T>(this NavigationParameters parameters, string key, T defaultValue)
        {
            var value = GetValue<T>(parameters, key);
            return value ?? defaultValue;
        }
    }
}
