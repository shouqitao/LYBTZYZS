using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 病历状态
    /// </summary>
    [Description("病历状态")]
/// <summary>
/// 表示RecordStatus。
/// </summary>
    public enum RecordStatus {
        Draft = 0,      // 草稿
        Completed = 1,  // 已完成
        Archived = 2    // 已归档
    }
}
