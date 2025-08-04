using LYBT.Shared.Models.Core;
using System;

namespace LYBT.WPF.Client.Core.Models.TreatmentRoom {
    /// <summary>
    /// 治疗室任务信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class TreatmentRoomInfo : BaseTreatmentRoomModel {
        /// <summary>患者姓名（前端显示字段）</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名（前端显示字段）</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>治疗类型名称（前端显示字段）</summary>
        public string TreatmentTypeName { get; set; } = string.Empty;

        /// <summary>状态名称（前端显示字段）</summary>
        public string StatusName { get; set; } = string.Empty;

        /// <summary>执行进度百分比</summary>
        public double ProgressPercentage => TotalCount > 0 ? (double)ExecutedCount / TotalCount * 100 : 0;

        /// <summary>剩余次数</summary>
        public int RemainingCount => TotalCount - ExecutedCount;

        /// <summary>是否已完成</summary>
        public bool IsCompleted => ExecutedCount >= TotalCount;

        /// <summary>是否可执行</summary>
        public bool CanExecute => !IsCompleted && Status != "已取消";

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>治疗时长（分钟）</summary>
        public int DurationMinutes => EndTime > StartTime ? (int)(EndTime - StartTime).TotalMinutes : 0;

        /// <summary>预计完成日期</summary>
        public DateTime? EstimatedCompletionDate { get; set; }

        /// <summary>实际花费</summary>
        public decimal ActualCost { get; set; }

        /// <summary>执行记录（前端展示字段）</summary>
        public string ExecutionHistory { get; set; } = string.Empty;
    }
}