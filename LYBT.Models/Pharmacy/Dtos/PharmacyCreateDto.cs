using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Pharmacy.Dtos {

    /// <summary>
    /// 新增药房单 DTO
    /// </summary>
    public class PharmacyCreateDto {

        /// <summary>处方ID</summary>
        [Required(ErrorMessage = "处方ID不能为空")]
        [DisplayName("处方ID")]
/// <summary>
/// PrescriptionId 属性。
/// </summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>药房操作员ID</summary>
        [Required(ErrorMessage = "药房操作员ID不能为空")]
        [DisplayName("药房操作员ID")]
/// <summary>
/// OperatorId 属性。
/// </summary>
        public Guid OperatorId { get; set; }

        /// <summary>抓药时间</summary>
        [DisplayName("抓药时间")]
/// <summary>
/// DispenseTime 属性。
/// </summary>
        public DateTime DispenseTime { get; set; } = DateTime.Now;

        /// <summary>药房状态（如已完成/待取药）</summary>
        [DisplayName("药房状态（如已完成/待取药）")]
/// <summary>
/// Status 属性。
/// </summary>
        public int Status { get; set; } = 0;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
