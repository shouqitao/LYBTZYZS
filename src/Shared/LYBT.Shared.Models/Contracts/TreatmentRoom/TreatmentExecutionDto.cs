using System;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.TreatmentRoom
{
    /// <summary>
    /// 理疗执行记录DTO
    /// </summary>
    public class TreatmentExecutionDto
    {
        /// <summary>执行记录ID</summary>
        [DisplayName("执行记录ID")]
        public Guid Id { get; set; }

        /// <summary>执行编号</summary>
        [DisplayName("执行编号")]
        public string ExecutionNumber { get; set; } = string.Empty;

        /// <summary>病历ID</summary>
        [DisplayName("病历ID")]
        public Guid RecordId { get; set; }

        /// <summary>理疗项目ID</summary>
        [DisplayName("理疗项目ID")]
        public Guid TreatmentCatalogId { get; set; }

        /// <summary>理疗项目名称</summary>
        [DisplayName("理疗项目名称")]
        public string TreatmentCatalogName { get; set; } = string.Empty;

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>开单医生</summary>
        [DisplayName("开单医生")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>理疗师</summary>
        [DisplayName("理疗师")]
        public string? TherapistName { get; set; }

        /// <summary>执行状态</summary>
        [DisplayName("执行状态")]
        public string Status { get; set; } = string.Empty;

        /// <summary>预约时间</summary>
        [DisplayName("预约时间")]
        public DateTime? AppointmentTime { get; set; }

        /// <summary>开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime? StartTime { get; set; }

        /// <summary>结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>费用</summary>
        [DisplayName("费用")]
        public decimal Fee { get; set; }

        /// <summary>是否已收费</summary>
        [DisplayName("是否已收费")]
        public bool IsPaid { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
    }
}