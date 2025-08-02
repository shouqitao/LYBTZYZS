using LYBT.Shared.Models.Core;

namespace LYBT.Shared.Models.Contracts.Herbs {

    /// <summary>
    /// 药材列表DTO（简化版，用于列表显示）
    /// </summary>
    public class HerbDto : BaseHerbModel {

        /// <summary>
        /// 药材ID
        /// </summary>
        public new Guid Id { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public new DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public new DateTime? UpdateTime { get; set; }
    }
}