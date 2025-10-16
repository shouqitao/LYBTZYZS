using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 验方验证状态枚举 - 标识验方是否已验证
    /// 支持延迟绑定工作流：从老系统导入的验方初始为Draft状态，待验证后标记为Validated
    /// </summary>
    [Description("验方验证状态")]
    public enum FormulaValidationStatus
    {
        /// <summary>
        /// 草稿/未验证 - 默认状态
        /// 验方刚创建或从老系统导入时的初始状态，尚未经过审核验证
        /// </summary>
        [Display(Name = "草稿")]
        [Description("草稿/未验证")]
        Draft = 0,

        /// <summary>
        /// 已验证 - 经过审核确认的验方
        /// 验方的药材组成、剂量、功效等已经过医生审核，可以安全使用
        /// </summary>
        [Display(Name = "已验证")]
        [Description("已验证")]
        Validated = 1
    }
}
