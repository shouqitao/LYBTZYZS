using System.ComponentModel;
namespace LYBT.Module.Queueing.Dtos {

    /// <summary>
    /// 排队详情 DTO
    /// </summary>
    public class QueueingDetailDto {

        /// <summary>排队ID</summary>
        [DisplayName("排队ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
/// <summary>
/// PatientName 属性。
/// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
/// <summary>
/// DoctorName 属性。
/// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>排队类型</summary>
        [DisplayName("排队类型")]
/// <summary>
/// QueueType 属性。
/// </summary>
        public string QueueType { get; set; } = string.Empty;

        /// <summary>排队时间</summary>
        [DisplayName("排队时间")]
/// <summary>
/// QueueTime 属性。
/// </summary>
        public DateTime QueueTime { get; set; }

        /// <summary>当前状态（如“排队中”、“已叫号”、“已就诊”、“已取消”）</summary>
        [DisplayName("当前状态（如“排队中”、“已叫号”、“已就诊”、“已取消”）")]
/// <summary>
/// Status 属性。
/// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
