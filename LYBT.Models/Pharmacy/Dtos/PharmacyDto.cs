using System.ComponentModel;
namespace LYBT.Module.Pharmacy.Dtos {

    /// <summary>
    /// 药房单列表 DTO
    /// </summary>
    public class PharmacyDto {

        /// <summary>药房单ID</summary>
        [DisplayName("药房单ID")]
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>药房状态</summary>
        [DisplayName("药房状态")]
        public int Status { get; set; }

        /// <summary>抓药时间</summary>
        [DisplayName("抓药时间")]
        public DateTime DispenseTime { get; set; }
    }
}