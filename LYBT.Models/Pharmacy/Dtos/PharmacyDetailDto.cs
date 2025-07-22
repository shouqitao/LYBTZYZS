using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Pharmacy.Dtos {

    /// <summary>
    /// 药房单详情 DTO
    /// </summary>
    public class PharmacyDetailDto {

        /// <summary>药房单ID</summary>
        [DisplayName("药房单ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
/// <summary>
/// PrescriptionId 属性。
/// </summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
/// <summary>
/// PatientName 属性。
/// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>药房操作员姓名</summary>
        [DisplayName("药房操作员姓名")]
/// <summary>
/// OperatorName 属性。
/// </summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>抓药时间</summary>
        [DisplayName("抓药时间")]
/// <summary>
/// DispenseTime 属性。
/// </summary>
        public DateTime DispenseTime { get; set; }

        /// <summary>药房状态</summary>
        [DisplayName("药房状态")]
/// <summary>
/// Status 属性。
/// </summary>
        public PharmacyStatus Status { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
