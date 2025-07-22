using System.ComponentModel;
namespace LYBT.Module.Records.Dtos {

    /// <summary>
    /// 病历列表 DTO（简要信息）
    /// </summary>
    public class RecordDto {

        /// <summary>病历ID</summary>
        [DisplayName("病历ID")]
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

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
/// <summary>
/// Diagnosis 属性。
/// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>病历时间</summary>
        [DisplayName("病历时间")]
/// <summary>
/// RecordTime 属性。
/// </summary>
        public DateTime RecordTime { get; set; }

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
/// <summary>
/// IsShared 属性。
/// </summary>
        public bool IsShared { get; set; }
    }
}
