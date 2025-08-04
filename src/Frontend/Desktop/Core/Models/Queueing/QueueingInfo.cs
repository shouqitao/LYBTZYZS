using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System;

namespace LYBT.WPF.Client.Core.Models.Queueing {
    /// <summary>
    /// 排队信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class QueueingInfo : BaseQueueingModel {
        /// <summary>排队号码（前端显示字段）</summary>
        public string QueueNumber { get; set; } = string.Empty;

        /// <summary>预计等待时间（分钟）</summary>
        public int EstimatedWaitTime { get; set; }

        /// <summary>前面还有几人</summary>
        public int PeopleAhead { get; set; }

        /// <summary>患者电话（前端显示字段）</summary>
        public string PatientPhone { get; set; } = string.Empty;

        /// <summary>挂号类型（前端显示字段）</summary>
        public string RegistrationType { get; set; } = string.Empty;

        /// <summary>科室（前端显示字段）</summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>叫号时间</summary>
        public DateTime? CallTime { get; set; }

        /// <summary>就诊开始时间</summary>
        public DateTime? ConsultationStartTime { get; set; }

        /// <summary>就诊结束时间</summary>
        public DateTime? ConsultationEndTime { get; set; }

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>是否可叫号</summary>
        public bool CanCall => Status == QueueStatus.Waiting;

        /// <summary>是否可开始就诊</summary>
        public bool CanStartConsultation => Status == QueueStatus.Calling;

        /// <summary>状态名称（前端显示字段）</summary>
        public string StatusName => GetStatusName();

        private string GetStatusName() {
            return Status switch {
                QueueStatus.Waiting => "排队中",
                QueueStatus.Calling => "已叫号",
                QueueStatus.InService => "就诊中",
                QueueStatus.Completed => "已就诊",
                QueueStatus.Cancelled => "已取消",
                QueueStatus.Skipped => "未到",
                QueueStatus.Timeout => "超时",
                _ => "未知"
            };
        }

        /// <summary>等待时长描述</summary>
        public string WaitingTimeDescription {
            get {
                if (Status != QueueStatus.Waiting) return "-";
                var waitingTime = DateTime.Now - QueueTime;
                if (waitingTime.TotalHours >= 1)
                    return $"{(int)waitingTime.TotalHours}小时{waitingTime.Minutes}分钟";
                return $"{(int)waitingTime.TotalMinutes}分钟";
            }
        }
    }
}