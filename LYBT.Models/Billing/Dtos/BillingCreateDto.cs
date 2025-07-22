using LYBT.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Billing.Dtos {

    /// <summary>
    /// 新增账单 DTO
    /// </summary>
    public class BillingCreateDto {

        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        [DisplayName("病人ID")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }

        /// <summary>处方ID（可选）</summary>
        [DisplayName("处方ID（可选）")]
/// <summary>
/// PrescriptionId 属性。
/// </summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>开单医生ID</summary>
        [Required(ErrorMessage = "开单医生ID不能为空")]
        [DisplayName("开单医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public Guid DoctorId { get; set; }

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

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
/// <summary>
/// CreatedTime 属性。
/// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

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
        public DateTime BillingTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 账单明细 DTO
    /// </summary>
    public class BillingItemDto {

        /// <summary>项目名称</summary>
        [DisplayName("项目名称")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>单价</summary>
        [DisplayName("单价")]
/// <summary>
/// UnitPrice 属性。
/// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>数量</summary>
        [DisplayName("数量")]
/// <summary>
/// Quantity 属性。
/// </summary>
        public decimal Quantity { get; set; }

        /// <summary>小计</summary>
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
