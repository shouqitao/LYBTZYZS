using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Herbs.Dtos {

    /// <summary>
    /// 编辑药材 DTO
    /// </summary>
    public class HerbEditDto {

        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        [DisplayName("药材ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>药材名称</summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [DisplayName("药材名称")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
/// <summary>
/// Pinyin 属性。
/// </summary>
        public string? Pinyin { get; set; }

        /// <summary>产地</summary>
        [DisplayName("产地")]
/// <summary>
/// Origin 属性。
/// </summary>
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [DisplayName("规格")]
/// <summary>
/// Spec 属性。
/// </summary>
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        [DisplayName("单位")]
/// <summary>
/// Unit 属性。
/// </summary>
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [Range(0, double.MaxValue, ErrorMessage = "单价不能为负数")]
        [DisplayName("单价")]
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

        /// <summary>功效说明</summary>
        [DisplayName("功效说明")]
/// <summary>
/// Effect 属性。
/// </summary>
        public string? Effect { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
