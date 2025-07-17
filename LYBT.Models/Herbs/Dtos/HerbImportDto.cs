using System;
using System.ComponentModel;

namespace LYBT.Module.Herbs.Dtos {
    /// <summary>
    /// 药材导入 DTO
    /// </summary>
    public class HerbImportDto {
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;
        [DisplayName("Origin")]
        public string? Origin { get; set; }
        [DisplayName("Spec")]
        public string? Spec { get; set; }
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
