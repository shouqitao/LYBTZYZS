using System;

namespace LYBT.Models.TreatmentRoom {
    /// <summary>
    /// 治疗执行记录模型
    /// </summary>
    public class TreatmentRoomModel {
        /// <summary>执行记录ID</summary>
        public Guid ExecutionId { get; set; }

        /// <summary>治疗计划ID</summary>
        public Guid PlanId { get; set; }

        /// <summary>病人ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>治疗类型（针灸/推拿）</summary>
        public string TreatmentType { get; set; } = string.Empty;

        /// <summary>已执行次数</summary>
        public int ExecutedCount { get; set; }

        /// <summary>总疗程次数</summary>
        public int TotalCount { get; set; }

        /// <summary>当前状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>治疗人</summary>
        public string Executor { get; set; } = string.Empty;

        /// <summary>最后执行时间</summary>
        public DateTime LastExecuteTime { get; set; }

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>主键ID</summary>
        public Guid Id { get; set; }

        /// <summary>开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>治疗项目</summary>
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>结束时间</summary>
        public DateTime EndTime { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>次数</summary>
        public int Count { get; set; }
    }
}
