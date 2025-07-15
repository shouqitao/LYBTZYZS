using System;
using System.ComponentModel;

namespace LYBT.Module.Herbs.Dtos {
    /// <summary>
    /// 药材导入 DTO
    /// </summary>
    public class HerbImportDto {
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;
        [DisplayName("Pinyin")]
        public string? Pinyin { get; set; }
        [DisplayName("Origin")]
        public string? Origin { get; set; }
        [DisplayName("Spec")]
        public string? Spec { get; set; }
        [DisplayName("Unit")]
        public string? Unit { get; set; }
        [DisplayName("Price")]
        public decimal Price { get; set; }
        [DisplayName("Stock")]
        public int Stock { get; set; }
        [DisplayName("BatchNo")]
        public string? BatchNo { get; set; }
        [DisplayName("ExpireDate")]
        public DateTime? ExpireDate { get; set; }
        [DisplayName("Effect")]
        public string? Effect { get; set; }
        [DisplayName("Remark")]
        public string? Remark { get; set; }
    }
}
