namespace LYBT.Module.TreatmentRoom.Dtos {

    /// <summary>
    /// 治疗室单详情 DTO
    /// </summary>
    public class TreatmentRoomDetailDto {

        /// <summary>治疗室单ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>诊疗项目</summary>
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗次数</summary>
        public int Count { get; set; }

        /// <summary>治疗状态</summary>
        public int Status { get; set; }

        /// <summary>治疗开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>治疗结束时间</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}