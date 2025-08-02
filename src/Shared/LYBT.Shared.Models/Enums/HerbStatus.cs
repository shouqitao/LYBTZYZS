using System.ComponentModel;

namespace LYBT.Shared.Models.Enums {

    /// <summary>
    /// 药材状态枚举 - 前后端共享
    /// </summary>
    [Description("药材状态")]
    public enum HerbStatus {

        /// <summary>停用 - 不能开具处方，但保留历史记录</summary>
        [Description("停用")]
        Inactive = 0,

        /// <summary>正常使用 - 可以开具处方</summary>
        [Description("正常")]
        Active = 1,

        /// <summary>缺货 - 临时缺货，可以开具但需要提醒</summary>
        [Description("缺货")]
        OutOfStock = 2,

        /// <summary>停产 - 永久停产，建议替换</summary>
        [Description("停产")]
        Discontinued = 3,

        /// <summary>过期 - 需要更新或移除</summary>
        [Description("过期")]
        Expired = 4,

        /// <summary>审核中 - 新药材等待审核</summary>
        [Description("审核中")]
        UnderReview = 5
    }
}