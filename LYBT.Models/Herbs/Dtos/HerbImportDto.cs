using System;

namespace LYBT.Module.Herbs.Dtos {
    /// <summary>
    /// 药材导入 DTO
    /// </summary>
    public class HerbImportDto {
        public string Name { get; set; } = string.Empty;
        public string? Pinyin { get; set; }
        public string? Origin { get; set; }
        public string? Spec { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? BatchNo { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? Effect { get; set; }
        public string? Remark { get; set; }
    }
}
