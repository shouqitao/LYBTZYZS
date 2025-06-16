using System;

namespace LYBT.Models.TreatmentRoom {
    /// <summary>
    /// 治疗执行记录模型
    /// </summary>
    public class TreatmentRoomModel {
        public Guid ExecutionId { get; set; }       // 执行记录ID
        public Guid PlanId { get; set; }            // 治疗计划ID
        public Guid PatientId { get; set; }         // 病人ID
        public string TreatmentType { get; set; }     // 治疗类型（针灸/推拿）
        public int ExecutedCount { get; set; }        // 已执行次数
        public int TotalCount { get; set; }           // 总疗程次数
        public string Status { get; set; }            // 当前状态
        public string Executor { get; set; }          // 治疗人
        public DateTime LastExecuteTime { get; set; } // 最后执行时间
        public Guid DoctorId { get; set; }
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public string TreatmentItem { get; set; }
        public DateTime EndTime { get; set; }
        public string? Remark { get; set; }
        public int Count { get; set; }
    }
}
