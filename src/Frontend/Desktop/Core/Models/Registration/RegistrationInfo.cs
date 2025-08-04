using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Models.Registration {
    /// <summary>
    /// 挂号信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class RegistrationInfo : BaseRegistrationModel {
        /// <summary>
        /// 挂号编号
        /// </summary>
        public string RegistrationNumber { get; set; } = string.Empty;

        /// <summary>
        /// 挂号编号（别名）
        /// </summary>
        public string RegistrationNo => RegistrationNumber;

        /// <summary>
        /// 患者电话
        /// </summary>
        public string PatientPhone { get; set; } = string.Empty;

        /// <summary>
        /// 科室
        /// </summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// 挂号类型名称
        /// </summary>
        public string RegistrationTypeName => GetRegistrationTypeName();

        /// <summary>
        /// 挂号费用
        /// </summary>
        public decimal RegistrationFee { get; set; }

        /// <summary>
        /// 预约日期
        /// </summary>
        public DateTime? AppointmentDate { get; set; }

        /// <summary>
        /// 预约时间段
        /// </summary>
        public string? AppointmentTimeSlot { get; set; }

        /// <summary>
        /// 状态名称
        /// </summary>
        public string StatusName => GetStatusName();

        /// <summary>
        /// 队列号
        /// </summary>
        public int? QueueNumber { get; set; }

        /// <summary>
        /// 是否已支付
        /// </summary>
        public bool IsPaid { get; set; }

        /// <summary>
        /// 支付状态名称
        /// </summary>
        public string PaymentStatusName => IsPaid ? "已支付" : "未支付";

        /// <summary>
        /// 获取挂号类型名称
        /// </summary>
        private string GetRegistrationTypeName() {
            return RegistrationType switch {
                RegistrationType.Regular => "普通号",
                RegistrationType.Expert => "专家号",
                RegistrationType.Emergency => "急诊号",
                RegistrationType.Appointment => "预约号",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取状态名称
        /// </summary>
        private string GetStatusName() {
            return Status switch {
                RegistrationStatus.Scheduled => "已预约",
                RegistrationStatus.Arrived => "已到达",
                RegistrationStatus.InConsultation => "就诊中",
                RegistrationStatus.Completed => "已完成",
                RegistrationStatus.Cancelled => "已取消",
                RegistrationStatus.NoShow => "爽约",
                RegistrationStatus.Expired => "已过期",
                _ => "未知"
            };
        }

        /// <summary>
        /// 是否可以取消
        /// </summary>
        public bool CanCancel => Status == RegistrationStatus.Scheduled;

        /// <summary>
        /// 是否可以签到
        /// </summary>
        public bool CanCheckIn => Status == RegistrationStatus.Scheduled && IsPaid;

        /// <summary>
        /// 是否可以编辑
        /// </summary>
        public bool CanEdit => Status == RegistrationStatus.Scheduled;

        /// <summary>
        /// 是否选中（用于批量操作）
        /// </summary>
        public bool IsSelected { get; set; }
    }
}