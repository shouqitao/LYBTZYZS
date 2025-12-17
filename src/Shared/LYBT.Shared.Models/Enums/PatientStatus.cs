using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 患者状态枚举 - Record-Only模式简化版本（仅Active/Inactive）
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
    [Description("患者状态")]
    public enum PatientStatus
    {

        /// <summary>停用</summary>
        [Description("停用")]
        Inactive = 0,

        /// <summary>活跃</summary>
        [Description("活跃")]
        Active = 1
    }
}
