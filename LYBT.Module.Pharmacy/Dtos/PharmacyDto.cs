using System;

namespace LYBT.Module.Pharmacy.Dtos {
    /// <summary>
    /// 药房单列表 DTO
    /// </summary>
    public class PharmacyDto {
        /// <summary>药房单ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>药房状态</summary>
        public int Status { get; set; }

        /// <summary>抓药时间</summary>
        public DateTime DispenseTime { get; set; }
    }
}
