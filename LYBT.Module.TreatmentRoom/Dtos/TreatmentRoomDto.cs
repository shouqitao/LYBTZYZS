using System;

namespace LYBT.Module.TreatmentRoom.Dtos {
    /// <summary>
    /// 治疗室单列表 DTO
    /// </summary>
    public class TreatmentRoomDto {
        /// <summary>治疗室单ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>诊疗项目</summary>
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗状态</summary>
        public int Status { get; set; }

        /// <summary>治疗开始时间</summary>
        public DateTime StartTime { get; set; }
    }
}
