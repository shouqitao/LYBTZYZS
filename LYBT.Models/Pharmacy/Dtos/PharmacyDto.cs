using System.ComponentModel;
namespace LYBT.Module.Pharmacy.Dtos {

    /// <summary>
    /// 药房单列表 DTO
    /// </summary>
    public class PharmacyDto {

        /// <summary>药房单ID</summary>
        [DisplayName("药房单ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
/// <summary>
/// PatientName 属性。
/// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>药房状态</summary>
        [DisplayName("药房状态")]
/// <summary>
/// Status 属性。
/// </summary>
        public int Status { get; set; }

        /// <summary>抓药时间</summary>
        [DisplayName("抓药时间")]
/// <summary>
/// DispenseTime 属性。
/// </summary>
        public DateTime DispenseTime { get; set; }
    }
}
