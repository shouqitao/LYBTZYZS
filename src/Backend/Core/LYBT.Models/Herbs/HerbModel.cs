using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Herbs {

    /// <summary>
    /// 药材信息实体 - 中药材基础信息管理，支持软删除策略和快速检索
    /// 继承BaseHerbModel，添加后端特有字段
    /// </summary>
    public class HerbModel : BaseHerbModel {

        /// <summary>
        /// 成本价（元/单位，用于进货成本计算）
        /// </summary>
        [DisplayName("成本价")]
        public decimal CostPrice { get; set; } = 0;

        /// <summary>
        /// 最后操作者ID
        /// </summary>
        [DisplayName("最后操作者ID")]
        public Guid? LastOperatorId { get; set; }

        /// <summary>
        /// 最后操作者姓名
        /// </summary>
        [StringLength(50)]
        [DisplayName("最后操作者姓名")]
        public string? LastOperatorName { get; set; }
    }
}