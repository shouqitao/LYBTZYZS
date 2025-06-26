namespace LYBT.Models {

    /// <summary>
    /// 药材主表实体
    /// </summary>
    public class HerbModel {

        /// <summary>
        /// 药材ID（主键）
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 拼音码
        /// </summary>
        public string? Pinyin { get; set; }

        /// <summary>
        /// 产地
        /// </summary>
        public string? Origin { get; set; }

        /// <summary>
        /// 规格
        /// </summary>
        public string? Spec { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 功效说明
        /// </summary>
        public string? Effect { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}