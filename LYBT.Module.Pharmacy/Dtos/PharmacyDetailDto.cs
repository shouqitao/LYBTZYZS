using LYBT.Common.Enums;
using System;

namespace LYBT.Module.Pharmacy.Dtos {
    /// <summary>
    /// 药房单详情 DTO
    /// </summary>
    public class PharmacyDetailDto {
        /// <summary>药房单ID</summary>
        public Guid Id { get; set; }

        /// <summary>处方ID</summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>药房操作员姓名</summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>抓药时间</summary>
        public DateTime DispenseTime { get; set; }

        /// <summary>药房状态</summary>
        public PharmacyStatus Status { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
