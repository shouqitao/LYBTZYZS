using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Queueing.Dtos {

    /// <summary>
    /// 编辑排队信息 DTO
    /// </summary>
    public class QueueingEditDto {

        /// <summary>排队ID</summary>
        [Required(ErrorMessage = "排队ID不能为空")]
        [DisplayName("排队ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>排队类型</summary>
        [Required(ErrorMessage = "排队类型不能为空")]
        [DisplayName("排队类型")]
/// <summary>
/// QueueType 属性。
/// </summary>
        public string QueueType { get; set; } = "普通";

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
