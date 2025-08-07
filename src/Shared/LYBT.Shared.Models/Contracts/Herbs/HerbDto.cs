using LYBT.Shared.Models.Core;

namespace LYBT.Shared.Models.Contracts.Herbs {

    /// <summary>
    /// 药材列表DTO（简化版，用于列表显示）
    /// </summary>
    public class HerbDto : BaseHerbModel {
        // 继承BaseHerbModel的所有字段，无需额外字段
        // CreateTime 和 UpdateTime 字段已移除（按照字段标准化要求）
    }
}