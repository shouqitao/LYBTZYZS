using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Billing.Dtos {

    /// <summary>
    /// 账单详情 DTO
    /// </summary>
    public class BillingDetailDto {

        /// <summary>账单ID</summary>
        [DisplayName("账单ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
/// <summary>
/// PatientName 属性。
/// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
/// <summary>
/// PrescriptionId 属性。
/// </summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>开单医生ID</summary>
        [DisplayName("开单医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>账单明细</summary>
        [DisplayName("账单明细")]
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
        public BillingStatus Status { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
/// <summary>
/// CreatedTime 属性。
/// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>支付时间</summary>
        [DisplayName("支付时间")]
/// <summary>
/// PaidTime 属性。
/// </summary>
        public DateTime? PaidTime { get; set; }

        /// <summary>完成时间</summary>
        [DisplayName("完成时间")]
/// <summary>
/// CompletedTime 属性。
/// </summary>
        public DateTime? CompletedTime { get; set; }

        /// <summary>退款时间</summary>
        [DisplayName("退款时间")]
/// <summary>
/// RefundTime 属性。
/// </summary>
        public DateTime? RefundTime { get; set; }

        /// <summary>退款理由</summary>
        [DisplayName("退款理由")]
/// <summary>
/// RefundReason 属性。
/// </summary>
        public string? RefundReason { get; set; }

        /// <summary>缴费方式</summary>
        [DisplayName("缴费方式")]
/// <summary>
/// PaymentMethod 属性。
/// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>账单时间</summary>
        [DisplayName("账单时间")]
/// <summary>
/// BillingTime 属性。
/// </summary>
        public DateTime BillingTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
