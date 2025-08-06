using LYBT.Models.Queueing;

namespace LYBT.Module.Queueing.Interfaces
{
    /// <summary>
    /// 队列管理服务接口 - 统一排队协调器
    /// </summary>
    public interface IQueueManagementService
    {
        /// <summary>
        /// 添加到排队队列
        /// </summary>
        Task<QueueItemModel> AddToQueueAsync(Guid medicalCaseId, QueueType queueType, Guid? servicePointId = null);

        /// <summary>
        /// 获取指定类型的排队列表
        /// </summary>
        Task<List<QueueItemModel>> GetQueueByTypeAsync(QueueType queueType, Guid? servicePointId = null);

        /// <summary>
        /// 获取医生的看诊队列
        /// </summary>
        Task<List<QueueItemModel>> GetDoctorConsultationQueueAsync(Guid doctorId);

        /// <summary>
        /// 获取收费台队列
        /// </summary>
        Task<List<QueueItemModel>> GetPaymentQueueAsync();

        /// <summary>
        /// 获取药房队列
        /// </summary>
        Task<List<QueueItemModel>> GetPharmacyQueueAsync(Guid? pharmacyId = null);

        /// <summary>
        /// 获取理疗室队列
        /// </summary>
        Task<List<QueueItemModel>> GetTreatmentRoomQueueAsync(Guid? roomId = null);

        /// <summary>
        /// 叫号
        /// </summary>
        Task<bool> CallNextAsync(QueueType queueType, Guid? servicePointId = null);

        /// <summary>
        /// 叫指定号码
        /// </summary>
        Task<bool> CallSpecificAsync(Guid queueItemId);

        /// <summary>
        /// 开始服务
        /// </summary>
        Task<bool> StartServiceAsync(Guid queueItemId);

        /// <summary>
        /// 完成服务
        /// </summary>
        Task<bool> CompleteServiceAsync(Guid queueItemId);

        /// <summary>
        /// 跳过当前号码
        /// </summary>
        Task<bool> SkipAsync(Guid queueItemId);

        /// <summary>
        /// 取消排队
        /// </summary>
        Task<bool> CancelQueueAsync(Guid queueItemId);

        /// <summary>
        /// 获取患者当前排队状态
        /// </summary>
        Task<List<QueueItemModel>> GetPatientCurrentQueuesAsync(Guid patientId);

        /// <summary>
        /// 获取队列统计信息
        /// </summary>
        Task<QueueStatistics> GetQueueStatisticsAsync(QueueType queueType, Guid? servicePointId = null);

        /// <summary>
        /// 自动分配队列号
        /// </summary>
        Task<int> GetNextQueueNumberAsync(QueueType queueType, Guid? servicePointId = null);

        /// <summary>
        /// 估算等待时间
        /// </summary>
        Task<int> EstimateWaitTimeAsync(QueueType queueType, Guid? servicePointId = null);

        /// <summary>
        /// 清理过期队列项
        /// </summary>
        Task<int> CleanExpiredQueueItemsAsync();
    }

    /// <summary>
    /// 队列统计信息
    /// </summary>
    public class QueueStatistics
    {
        /// <summary>等待中数量</summary>
        public int WaitingCount { get; set; }

        /// <summary>服务中数量</summary>
        public int InServiceCount { get; set; }

        /// <summary>今日完成数量</summary>
        public int CompletedTodayCount { get; set; }

        /// <summary>平均等待时间（分钟）</summary>
        public double AverageWaitTime { get; set; }

        /// <summary>平均服务时间（分钟）</summary>
        public double AverageServiceTime { get; set; }

        /// <summary>最长等待时间（分钟）</summary>
        public int MaxWaitTime { get; set; }
    }
}