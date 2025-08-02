using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Pharmacy {

    /// <summary>
    /// 药房单详情 DTO
    /// </summary>
    public class PharmacyDetailDto {

        /// <summary>药房单ID</summary>
        [DisplayName("药房单ID")]
        public Guid Id { get; set; }

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>药房操作员姓名</summary>
        [DisplayName("药房操作员姓名")]
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>抓药时间</summary>
        [DisplayName("抓药时间")]
        public DateTime DispenseTime { get; set; }

        /// <summary>药房状态</summary>
        [DisplayName("药房状态")]
        public PharmacyStatus Status { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}