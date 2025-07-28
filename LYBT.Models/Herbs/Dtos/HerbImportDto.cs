using System.ComponentModel;

namespace LYBT.Models.Herbs {

    /// <summary>
    /// 药材导入 DTO
    /// </summary>
    public class HerbImportDto {

        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Origin")]
        public string? Origin { get; set; }

        /// <summary>基础规格数值（如：1，用于计算实际用量）</summary>
        [DisplayName("Spec")]
        public decimal Spec { get; set; } = 1;

        [DisplayName("Unit")]
        public string? Unit { get; set; }

        [DisplayName("Price")]
        public decimal Price { get; set; }

        [DisplayName("库存数量")]
        public int Stock { get; set; }

        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        [DisplayName("有效期")]
        public DateTime? ExpireDate { get; set; }

        [DisplayName("Effect")]
        public string? Effect { get; set; }

        [DisplayName("Remark")]
        public string? Remark { get; set; }
    }
}