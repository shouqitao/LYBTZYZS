using LYBT.Models.Queueing;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Queueing.Interfaces {

    /// <summary>
    /// 排队仓储接口，定义排队数据操作
    /// </summary>
    public interface IQueueingRepository {

        /// <summary>
        /// 根据ID获取排队详情
        /// </summary>
        Task<QueueingModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有排队信息
        /// </summary>
        Task<List<QueueingModel>> GetListAsync();

        /// <summary>
        /// 新增排队信息
        /// </summary>
        Task<bool> AddAsync(QueueingModel model);

        /// <summary>
        /// 更新排队信息
        /// </summary>
        Task<bool> UpdateAsync(QueueingModel model);

        /// <summary>
        /// 删除排队信息
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 将排队信息标记为已取消
        /// </summary>
        Task<bool> CancelAsync(Guid id);

        /// <summary>
        /// 将排队信息标记为已完成
        /// </summary>
        Task<bool> CompleteAsync(Guid id);

        /// <summary>
        /// 将排队信息标记为挂起
        /// </summary>
        Task<bool> HoldAsync(Guid id);

        /// <summary>
        /// 更新排队状态
        /// </summary>
        Task<bool> UpdateStatusAsync(Guid registrationId, QueueStatus status);
    }
}