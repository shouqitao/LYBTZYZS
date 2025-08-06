using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Frontend.TreatmentRoom
{
    /// <summary>
    /// 治疗室前端模型（理疗室）
    /// </summary>
    public class TreatmentRoomInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 理疗项目列表
        /// </summary>
        public List<string> TreatmentItems { get; set; } = new List<string>();

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 状态显示名称
        /// </summary>
        public string StatusName { get; set; } = string.Empty;

        /// <summary>
        /// 治疗师姓名
        /// </summary>
        public string TherapistName { get; set; } = string.Empty;

        /// <summary>
        /// 治疗室号
        /// </summary>
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 总时长（分钟）
        /// </summary>
        public int TotalDuration { get; set; }

        /// <summary>
        /// 已完成次数
        /// </summary>
        public int CompletedSessions { get; set; }

        /// <summary>
        /// 总次数
        /// </summary>
        public int TotalSessions { get; set; }

        /// <summary>
        /// 治疗记录
        /// </summary>
        public string TreatmentNotes { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}