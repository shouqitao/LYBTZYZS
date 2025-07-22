using System;
using System.ComponentModel;

namespace LYBT.Module.Herbs.Dtos {
    /// <summary>
    /// 药材导入 DTO
    /// </summary>
    public class HerbImportDto {
        [DisplayName("Name")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;
        [DisplayName("Origin")]
/// <summary>
/// Origin 属性。
/// </summary>
        public string? Origin { get; set; }
        [DisplayName("Spec")]
/// <summary>
/// Spec 属性。
/// </summary>
        public string? Spec { get; set; }
        [DisplayName("Unit")]
/// <summary>
/// Unit 属性。
/// </summary>
        public string? Unit { get; set; }
        [DisplayName("Price")]
/// <summary>
/// Price 属性。
/// </summary>
        public decimal Price { get; set; }
        [DisplayName("库存数量")]
/// <summary>
/// Stock 属性。
/// </summary>
        public int Stock { get; set; }
        [DisplayName("批号")]
/// <summary>
/// BatchNo 属性。
/// </summary>
        public string? BatchNo { get; set; }
        [DisplayName("有效期")]
/// <summary>
/// ExpireDate 属性。
/// </summary>
        public DateTime? ExpireDate { get; set; }
        [DisplayName("Effect")]
/// <summary>
/// Effect 属性。
/// </summary>
        public string? Effect { get; set; }
        [DisplayName("Remark")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
