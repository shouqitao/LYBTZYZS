using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Queueing;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Queueing.Interfaces {

    /// <summary>
    /// 排队业务服务接口
    /// </summary>
    public interface IQueueingService {

        /// <summary>
        /// 获取排队详情
        /// </summary>
        Task<QueueingDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取排队列表
        /// </summary>
        Task<List<QueueingDto>> GetListAsync();

        /// <summary>
        /// 分页获取排队列表
        /// </summary>
        Task<PaginatedResult<QueueingDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增排队
        /// </summary>
        Task<QueueingDto?> AddAsync(QueueingCreateDto dto);

        /// <summary>
        /// 编辑排队信息
        /// </summary>
        Task<bool> UpdateAsync(QueueingEditDto dto);

        /// <summary>
        /// 删除排队信息
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 取消排队
        /// </summary>
        Task<bool> CancelAsync(Guid id);

        /// <summary>
        /// 完成排队
        /// </summary>
        Task<bool> CompleteAsync(Guid id);

        /// <summary>
        /// 挂起排队
        /// </summary>
        Task<bool> HoldAsync(Guid id);

        // ==================== 现场叫号特有功能 ====================

        /// <summary>
        /// 获取今日排队列表
        /// </summary>
        Task<List<QueueingDto>> GetTodayQueuesAsync(Guid? doctorId = null);

        /// <summary>
        /// 获取当前正在就诊的排队
        /// </summary>
        Task<QueueingDto?> GetCurrentQueueAsync(Guid doctorId);

        /// <summary>
        /// 获取下一个等待的排队
        /// </summary>
        Task<QueueingDto?> GetNextWaitingQueueAsync(Guid doctorId);

        /// <summary>
        /// 叫号（开始就诊）
        /// </summary>
        Task<bool> CallNextAsync(Guid doctorId, Guid operatorId, string operatorName);

        /// <summary>
        /// 重新排队（过号重排）
        /// </summary>
        Task<bool> RequeueAsync(Guid queueId, Guid operatorId, string operatorName);

        /// <summary>
        /// 过号处理
        /// </summary>
        Task<bool> MarkAsMissedAsync(Guid queueId, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取排队统计
        /// </summary>
        Task<QueueStatisticsDto> GetStatisticsAsync(Guid? doctorId = null);

        /// <summary>
        /// 插队（VIP或加急）
        /// </summary>
        Task<bool> InsertQueueAsync(Guid queueId, int position, Guid operatorId, string operatorName);
    }
}