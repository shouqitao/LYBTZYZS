using System.ComponentModel;
namespace LYBT.Module.Records.Dtos {

    /// <summary>
    /// 病历列表 DTO（简要信息）
    /// </summary>
    public class RecordDto {

        /// <summary>病历ID</summary>
        [DisplayName("病历ID")]
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>病历时间</summary>
        [DisplayName("病历时间")]
        public DateTime RecordTime { get; set; }

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; }
    }
}