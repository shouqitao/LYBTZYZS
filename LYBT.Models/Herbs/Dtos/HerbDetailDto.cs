using System.ComponentModel;
namespace LYBT.Module.Herbs.Dtos {

    /// <summary>
    /// 药材详情 DTO
    /// </summary>
    public class HerbDetailDto {

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

        [DisplayName("Stock")]
        public int Stock { get; set; }
        [DisplayName("BatchNo")]
        public string? BatchNo { get; set; }
        [DisplayName("ExpireDate")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}