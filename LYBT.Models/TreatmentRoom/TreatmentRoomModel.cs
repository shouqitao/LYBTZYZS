namespace LYBT.Models.TreatmentRoom {

    /// <summary>
    /// 治疗室任务记录
    /// </summary>
    public class TreatmentRoomModel {

        /// <summary>主键ID</summary>
        public Guid Id { get; set; }

        /// <summary>执行计划ID</summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>治疗方案ID</summary>
        public string PlanId { get; set; } = string.Empty;

        /// <summary>病人ID</summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>治疗类型</summary>
        public string TreatmentType { get; set; } = string.Empty;

        /// <summary>已执行次数</summary>
        public int ExecutedCount { get; set; }

        /// <summary>总执行次数</summary>
        public int TotalCount { get; set; }

        /// <summary>当前状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>执行人</summary>
        public string Executor { get; set; } = string.Empty;

        /// <summary>上次执行时间</summary>
        public DateTime LastExecuteTime { get; set; }

        /// <summary>负责医生ID</summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>治疗项目</summary>
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>结束时间</summary>
        public DateTime EndTime { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>执行次数</summary>
        public int Count { get; set; }
    }
}