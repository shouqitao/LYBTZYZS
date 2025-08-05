using System.ComponentModel;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 治疗室任务基础模型 - 前后端共享核心字段
    /// 包含所有通用的治疗室任务信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseTreatmentRoomModel {

        /// <summary>任务唯一标识</summary>
        [DisplayName("任务ID")]
        public Guid Id { get; set; }

        /// <summary>执行ID</summary>
        [DisplayName("执行ID")]
        public Guid ExecutionId { get; set; }

        /// <summary>计划ID</summary>
        [DisplayName("计划ID")]
        public Guid PlanId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>治疗类型</summary>
        [DisplayName("治疗类型")]
        public string TreatmentType { get; set; } = string.Empty;

        /// <summary>已执行次数</summary>
        [DisplayName("已执行次数")]
        public int ExecutedCount { get; set; }

        /// <summary>总次数</summary>
        [DisplayName("总次数")]
        public int TotalCount { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public string Status { get; set; } = string.Empty;

        /// <summary>执行人</summary>
        [DisplayName("执行人")]
        public string Executor { get; set; } = string.Empty;

        /// <summary>最后执行时间</summary>
        [DisplayName("最后执行时间")]
        public DateTime LastExecuteTime { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>治疗项目</summary>
        [DisplayName("治疗项目")]
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime EndTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>次数</summary>
        [DisplayName("次数")]
        public int Count { get; set; }
    }
}