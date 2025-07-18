using System.ComponentModel;

namespace LYBT.Module.Herbs.Dtos {

    /// <summary>
    /// 药材列表 DTO
    /// </summary>
    public class HerbDto {

        /// <summary>药材ID</summary>
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? Pinyin { get; set; }

        /// <summary>产地</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        [DisplayName("总价")]
        public int TotalPrice { get; set; }

        [DisplayName("库存数量")]
        public int Stock { get; set; }
        [DisplayName("批号")]
        public string? BatchNo { get; set; }
        [DisplayName("有效期")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        [DisplayName("功效说明")]
        public string? Effect { get; set; }
    }
}