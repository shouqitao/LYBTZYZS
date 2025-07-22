using LYBT.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Billing.Dtos {

    /// <summary>
    /// 编辑账单 DTO
    /// </summary>
    public class BillingEditDto {

        /// <summary>账单ID</summary>
        [Required(ErrorMessage = "账单ID不能为空")]
        [DisplayName("账单ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>账单明细列表</summary>
        [DisplayName("账单明细列表")]
/// <summary>
/// Items 属性。
/// </summary>
        public List<BillingItemDto> Items { get; set; } = new();

        /// <summary>账单总金额</summary>
        [DisplayName("账单总金额")]
/// <summary>
/// TotalAmount 属性。
/// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>已缴金额</summary>
        [DisplayName("已缴金额")]
/// <summary>
/// PaidAmount 属性。
/// </summary>
        public decimal PaidAmount { get; set; }

        /// <summary>账单状态</summary>
        [DisplayName("账单状态")]
/// <summary>
/// Status 属性。
/// </summary>
        public BillingStatus Status { get; set; } = BillingStatus.Pending;

        /// <summary>
        /// 账单时间（如有二次缴费等场景可与 CreateTime 区分）
        /// </summary>
        [DisplayName("账单时间（如有二次缴费等场景可与 CreateTime 区分）")]
/// <summary>
/// BillingTime 属性。
/// </summary>
        public DateTime BillingTime { get; set; } = DateTime.Now;

        /// <summary>缴费方式</summary>
        [DisplayName("缴费方式")]
/// <summary>
/// PaymentMethod 属性。
/// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
