using System;

namespace LYBT.WPF.Client.Core.Models.TreatmentRoom
{
    /// <summary>
    /// 理疗执行记录信息
    /// </summary>
    public class TreatmentExecutionInfo
    {
        /// <summary>执行记录ID</summary>
        public Guid Id { get; set; }

        /// <summary>执行编号</summary>
        public string ExecutionNumber { get; set; } = string.Empty;

        /// <summary>病历ID</summary>
        public Guid RecordId { get; set; }

        /// <summary>理疗项目ID</summary>
        public Guid TreatmentCatalogId { get; set; }

        /// <summary>理疗项目名称</summary>
        public string TreatmentCatalogName { get; set; } = string.Empty;

        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>患者性别</summary>
        public string PatientGender { get; set; } = string.Empty;

        /// <summary>患者年龄</summary>
        public int PatientAge { get; set; }

        /// <summary>患者电话</summary>
        public string PatientPhone { get; set; } = string.Empty;

        /// <summary>开单医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>开单医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>理疗师ID</summary>
        public Guid? TherapistId { get; set; }

        /// <summary>理疗师姓名</summary>
        public string TherapistName { get; set; } = string.Empty;

        /// <summary>执行状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>预约时间</summary>
        public DateTime? AppointmentTime { get; set; }

        /// <summary>时间段</summary>
        public string TimeSlot { get; set; } = string.Empty;

        /// <summary>开始时间</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>结束时间</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>费用</summary>
        public decimal Fee { get; set; }

        /// <summary>是否已收费</summary>
        public bool IsPaid { get; set; }

        /// <summary>备注</summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>取消原因</summary>
        public string CancelReason { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>状态显示</summary>
        public string StatusDisplay => Status switch
        {
            "Pending" => "待执行",
            "Appointed" => "已预约",
            "InProgress" => "执行中",
            "Completed" => "已完成",
            "Cancelled" => "已取消",
            "NoShow" => "未到",
            _ => Status
        };

        /// <summary>状态颜色</summary>
        public string StatusColor => Status switch
        {
            "Pending" => "#FFA500",     // 橙色
            "Appointed" => "#1E90FF",   // 蓝色
            "InProgress" => "#32CD32",  // 绿色
            "Completed" => "#808080",   // 灰色
            "Cancelled" => "#DC143C",   // 红色
            "NoShow" => "#FF6347",      // 番茄红
            _ => "#000000"
        };

        /// <summary>可以开始执行</summary>
        public bool CanStart => Status == "Appointed" && !StartTime.HasValue;

        /// <summary>可以完成</summary>
        public bool CanComplete => Status == "InProgress" && StartTime.HasValue;

        /// <summary>可以取消</summary>
        public bool CanCancel => Status == "Pending" || Status == "Appointed";
    }
}