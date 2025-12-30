namespace LYBT.Shared.Models.Enums;

/// <summary>
/// 待处理医案类型枚举
/// OpenSpec: optimize-medicalcase-navigation
/// </summary>
public enum PendingCaseType
{
    /// <summary>已挂号等候</summary>
    Registered,

    /// <summary>正在看诊</summary>
    InProgress,

    /// <summary>暂存草稿</summary>
    Suspended
}
