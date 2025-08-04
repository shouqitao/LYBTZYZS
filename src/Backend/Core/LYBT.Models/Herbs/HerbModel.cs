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
        /// 药材基础规格数值（如：1，用于计算实际用量）
        /// </summary>
        [DisplayName("规格")]
        public decimal Specification { get; set; } = 1;

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