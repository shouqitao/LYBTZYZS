using LYBT.WPF.Client.Registration.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.WPF.Client.Registration.Services.Interfaces
{
    /// <summary>
    /// 挂号服务接口
    /// </summary>
    public interface IRegistrationService
    {
        /// <summary>
        /// 获取今日挂号列表
        /// </summary>
        Task<List<RegistrationInfo>> GetTodayRegistrationsAsync();

        /// <summary>
        /// 获取指定日期的挂号列表
        /// </summary>
        Task<List<RegistrationInfo>> GetRegistrationsByDateAsync(DateTime date);

        /// <summary>
        /// 获取指定患者的挂号历史
        /// </summary>
        Task<List<RegistrationInfo>> GetPatientRegistrationsAsync(Guid patientId);

        /// <summary>
        /// 获取指定医生的挂号列表
        /// </summary>
        Task<List<RegistrationInfo>> GetDoctorRegistrationsAsync(Guid doctorId, DateTime? date = null);

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        Task<RegistrationInfo?> GetRegistrationDetailAsync(Guid registrationId);

        /// <summary>
        /// 创建新挂号
        /// </summary>
        Task<RegistrationInfo> CreateRegistrationAsync(CreateRegistrationRequest request);

        /// <summary>
        /// 取消挂号
        /// </summary>
        Task<bool> CancelRegistrationAsync(Guid registrationId, string reason);

        /// <summary>
        /// 开始看诊
        /// </summary>
        Task<bool> StartConsultationAsync(Guid registrationId);

        /// <summary>
        /// 搜索挂号记录
        /// </summary>
        Task<List<RegistrationInfo>> SearchRegistrationsAsync(RegistrationSearchCriteria criteria);

        /// <summary>
        /// 获取挂号统计信息
        /// </summary>
        Task<RegistrationStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取下一个排队号
        /// </summary>
        Task<int> GetNextQueueNumberAsync(DateTime date);

        /// <summary>
        /// 更新支付状态
        /// </summary>
        Task<bool> UpdatePaymentStatusAsync(Guid registrationId, bool isPaid, string? paymentMethod);
    }

    /// <summary>
    /// 创建挂号请求
    /// </summary>
    public class CreateRegistrationRequest
    {
        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 预约时间（可选）
        /// </summary>
        public DateTime? AppointmentTime { get; set; }

        /// <summary>
        /// 挂号费
        /// </summary>
        public decimal RegistrationFee { get; set; } = 20m;

        /// <summary>
        /// 是否已支付
        /// </summary>
        public bool IsPaid { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 挂号搜索条件
    /// </summary>
    public class RegistrationSearchCriteria
    {
        /// <summary>
        /// 关键字（患者姓名、电话、挂号单号）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid? DoctorId { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 支付状态
        /// </summary>
        public bool? IsPaid { get; set; }
    }

    /// <summary>
    /// 挂号统计信息
    /// </summary>
    public class RegistrationStatistics
    {
        /// <summary>
        /// 总挂号数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 已完成数
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// 已取消数
        /// </summary>
        public int CancelledCount { get; set; }

        /// <summary>
        /// 待看诊数
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// 总收入
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// 平均等待时间（分钟）
        /// </summary>
        public double AverageWaitingTime { get; set; }

        /// <summary>
        /// 医生挂号统计
        /// </summary>
        public List<DoctorRegistrationStats> DoctorStats { get; set; } = new();
    }

    /// <summary>
    /// 医生挂号统计
    /// </summary>
    public class DoctorRegistrationStats
    {
        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 挂号数
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 收入
        /// </summary>
        public decimal Revenue { get; set; }
    }
}