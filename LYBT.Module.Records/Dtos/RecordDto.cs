using System;

namespace LYBT.Module.Records.Dtos {
    /// <summary>
    /// 病历列表 DTO（简要信息）
    /// </summary>
    public class RecordDto {
        /// <summary>病历ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>诊断内容</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>病历时间</summary>
        public DateTime RecordTime { get; set; }

        /// <summary>是否共享</summary>
        public bool IsShared { get; set; }
    }
}
