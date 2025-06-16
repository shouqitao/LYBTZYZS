using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Queueing.Dtos;

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
        /// 新增排队
        /// </summary>
        Task<bool> AddAsync(QueueingCreateDto dto);

        /// <summary>
        /// 编辑排队信息
        /// </summary>
        Task<bool> UpdateAsync(QueueingEditDto dto);

        /// <summary>
        /// 删除排队信息
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
