namespace LYBT.Desktop.Controls.Models;

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